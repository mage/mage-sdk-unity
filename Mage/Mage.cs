using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Net;

using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.MageClient.Message;
using Wizcorp.MageSDK.Network.Http;
using Wizcorp.MageSDK.Unity;
using Wizcorp.MageSDK.Utils;

namespace Wizcorp.MageSDK.MageClient
{
	public class Mage : Singleton<Mage>
	{
		//
		public EventManager EventManager;
		public Session Session;
		public Command.CommandCenter CommandCenter;
		public MessageStream MessageStream;
		public Archivist Archivist;

		private Logger logger;

		//
		private string baseUrl;
		private string appName;
		private Dictionary<string, string> headers;
		public CookieContainer Cookies;

		//
		private Dictionary<string, Logger> loggers = new Dictionary<string, Logger>();
		public Logger Logger(string context = null)
		{
			if (string.IsNullOrEmpty(context))
			{
				context = "Default";
			}

			if (loggers.ContainsKey(context))
			{
				return loggers[context];
			}

			var newLogger = new Logger(context);
			loggers.Add(context, newLogger);
			return newLogger;
		}


		// Avoid putting setup logic in the contstuctor. Only things that can be
		// carried between game sessions should go here. Otherwise we need to be
		// able to re-initialize them inside the setup function.
		public Mage()
		{
			// Setup log writters
			logger = Logger("mage");

			// TODO: properly check the damn certificate, for now ignore invalid ones (fix issue on Android/iOS)
			ServicePointManager.ServerCertificateValidationCallback += (o, cert, chain, errors) => true;
		}

		//
		public void SetEndpoints(string baseUrl, string appName, Dictionary<string, string> headers = null)
		{
			this.baseUrl = baseUrl;
			this.appName = appName;
			this.headers = new Dictionary<string, string>(headers);

			if (CommandCenter != null)
			{
				CommandCenter.SetEndpoint(baseUrl, appName, this.headers);
			}

			if (MessageStream != null)
			{
				MessageStream.SetEndpoint(baseUrl, this.headers);
			}
		}

		//
		public void Setup(Action<Exception> cb)
		{
			// Cleanup any existing internal modules
			if (MessageStream != null)
			{
				MessageStream.Dispose();
			}


			// Instantiate Singletons
			UnityApplicationState.Instantiate();
			HttpRequestManager.Instantiate();


			// Create a shared cookie container
			Cookies = new CookieContainer();


			// Initialize mage internal modules
			EventManager = new EventManager();
			CommandCenter = new Command.CommandCenter();
			MessageStream = new MessageStream();

			Session = new Session();
			Archivist = new Archivist();


			// Set endpoints
			CommandCenter.SetEndpoint(baseUrl, appName, headers);
			MessageStream.SetEndpoint(baseUrl, headers);

			cb(null);
		}

		//
		public void SetupModules(List<string> moduleNames, Action<Exception> cb)
		{
			// Setup application modules
			logger.Info("Setting up modules");
			Async.Each(
				moduleNames,
				(moduleName, callback) => {
					logger.Info("Setting up module: " + moduleName);

					// Use reflection to find module by name
					Assembly assembly = Assembly.GetExecutingAssembly();
					Type[] assemblyTypes = assembly.GetTypes();
					foreach (Type t in assemblyTypes)
					{
						if (moduleName == t.Name)
						{
							BindingFlags staticProperty = BindingFlags.Static | BindingFlags.GetProperty;
							BindingFlags publicMethod = BindingFlags.Public | BindingFlags.InvokeMethod;

							// Grab module instance from singleton base
							var singletonType = typeof(Singleton<>).MakeGenericType(t);
							Object instance = singletonType.InvokeMember("Instance", staticProperty, null, null, null);

							// Setup module
							Type moduleType = typeof(Module<>).MakeGenericType(t);
							Type t1 = t;
							Async.Series(
								new List<Action<Action<Exception>>>() {
									// Setup module user commands
									callbackInner => {
										moduleType.InvokeMember("SetupUsercommands", publicMethod, null, instance, new object[] { callbackInner });
									},
									// Setup module static data
									callbackInner => {
										moduleType.InvokeMember("SetupStaticData", publicMethod, null, instance, new object[] { callbackInner });
									}
								},
								error => {
									if (error != null)
									{
										callback(error);
										return;
									}

									// Check if the module has a setup method
									if (t1.GetMethod("Setup") == null)
									{
										Logger(moduleName).Info("No setup function");
										callback(null);
										return;
									}

									// Invoke the setup method on the module
									Logger(moduleName).Info("Executing setup function");
									t1.InvokeMember("Setup", publicMethod, null, instance, new object[] { callback });
								}
							);

							return;
						}
					}

					// If nothing found throw an error
					callback(new Exception("Can't find module " + moduleName));
				},
				error => {
					if (error != null)
					{
						logger.Data(error).Error("Setup failed!");
						cb(error);
						return;
					}

					logger.Info("Setup complete");
					cb(null);
				}
			);
		}


		//
		public IEnumerator SetupTask(Action<Exception> cb)
		{
			// Execute async setup function
			var setupStatus = new MageSetupStatus();
			Setup(error => {
				setupStatus.Error = error;
				setupStatus.Done = true;
			});

			// Wait for setup to return
			while (!setupStatus.Done)
			{
				yield return null;
			}

			// Call callback with error if there is one
			cb(setupStatus.Error);
		}

		//
		public IEnumerator SetupModulesTask(List<string> moduleNames, Action<Exception> cb)
		{
			// Execute async setup function
			var setupStatus = new MageSetupStatus();
			SetupModules(moduleNames, error => {
				setupStatus.Error = error;
				setupStatus.Done = true;
			});

			// Wait for setup to return
			while (!setupStatus.Done)
			{
				yield return null;
			}

			// Call callback with error if there is one
			cb(setupStatus.Error);
		}
	}
}
