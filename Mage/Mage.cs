using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Net;

using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.MageClient.Message;
using Wizcorp.MageSDK.Network.Http;
using Wizcorp.MageSDK.Utils;

namespace Wizcorp.MageSDK.MageClient {
	public class MageSetupStatus {
		public bool done = false;
		public Exception error = null;
	}


	public class Mage : Singleton<Mage> {
		//
		public EventManager eventManager;
		public Session session;
		public Command.CommandCenter commandCenter;
		public MessageStream messageStream;
		public Archivist archivist;

		private Logger _logger;

		//
		string baseUrl;
		string appName;
		Dictionary<string, string> headers;
		public CookieContainer cookies;

		//
		private Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
		public Logger logger(string context = null) {
			if (string.IsNullOrEmpty(context)) {
				context = "Default";
			}

			if (_loggers.ContainsKey(context)) {
				return _loggers[context];
			}

			Logger newLogger = new Logger(context);
			_loggers.Add(context, new Logger(context));
			return newLogger;
		}


		// Avoid putting setup logic in the contstuctor. Only things that can be
		// carried between game sessions should go here. Otherwise we need to be
		// able to re-initialize them inside the setup function.
		public Mage() {
			// Setup log writters
			_logger = logger("mage");

			// TODO: properly check the damn certificate, for now ignore invalid ones (fix issue on Android/iOS)
			ServicePointManager.ServerCertificateValidationCallback += (o, cert, chain, errors) => true;
		}

		//
		public void setEndpoints(string baseUrl, string appName, Dictionary<string, string> headers = null) {
			this.baseUrl = baseUrl;
			this.appName = appName;
			this.headers = new Dictionary<string, string>(headers);

			if (commandCenter != null) {
				commandCenter.SetEndpoint(baseUrl, appName, this.headers);
			}

			if (messageStream != null) {
				messageStream.SetEndpoint(baseUrl, this.headers);
			}
		}

		//
		public void Setup(Action<Exception> cb) {
			// Cleanup any existing internal modules
			if (messageStream != null) {
				messageStream.Dispose();
			}


			// Instantiate Singletons
			UnityApplicationState.Instantiate();
			HTTPRequestManager.Instantiate();


			// Create a shared cookie container
			cookies = new CookieContainer();


			// Initialize mage internal modules
			eventManager = new EventManager();
			commandCenter = new CommandCenter();
			messageStream = new MessageStream();

			session = new Session();
			archivist = new Archivist();


			// Set endpoints
			commandCenter.SetEndpoint(baseUrl, appName, headers);
			messageStream.SetEndpoint(baseUrl, headers);

			cb(null);
		}

		//
		public void SetupModules(List<string> moduleNames, Action<Exception> cb) {
			// Setup application modules
			_logger.info("Setting up modules");
			Async.each<string>(moduleNames, (string moduleName, Action<Exception> callback) => {
				_logger.info("Setting up module: " + moduleName);

				// Use reflection to find module by name
				Assembly assembly = Assembly.GetExecutingAssembly();
				Type[] assemblyTypes = assembly.GetTypes();
				foreach (Type t in assemblyTypes) {
					if (moduleName == t.Name) {
						BindingFlags staticProperty = BindingFlags.Static | BindingFlags.GetProperty;
						BindingFlags publicMethod = BindingFlags.Public | BindingFlags.InvokeMethod;

						// Grab module instance from singleton base
						var singletonType = typeof(Singleton<>).MakeGenericType(t);
						Object instance = singletonType.InvokeMember("Instance", staticProperty, null, null, null);

						// Setup module
						var moduleType = typeof(Module<>).MakeGenericType(t);
						Async.series(new List<Action<Action<Exception>>>() {
						(Action<Exception> callbackInner) => {
							// Setup module user commands
							object[] arguments = new object[]{callbackInner};
							moduleType.InvokeMember("setupUsercommands", publicMethod, null, instance, arguments);
						},
						(Action<Exception> callbackInner) => {
							// Setup module static data
							object[] arguments = new object[]{callbackInner};
							moduleType.InvokeMember("setupStaticData", publicMethod, null, instance, arguments);
						}
					}, (Exception error) => {
						if (error != null) {
							callback(error);
							return;
						}

						// Check if the module has a setup method
						if (t.GetMethod("Setup") == null) {
							logger(moduleName).info("No setup function");
							callback(null);
							return;
						}

						// Invoke the setup method on the module
						logger(moduleName).info("Executing setup function");
						object[] arguments = new object[] { callback };
						t.InvokeMember("Setup", publicMethod, null, instance, arguments);
					});

						return;
					}
				}

				// If nothing found throw an error
				callback(new Exception("Can't find module " + moduleName));
			}, (Exception error) => {
				if (error != null) {
					_logger.data(error).error("Setup failed!");
					cb(error);
					return;
				}

				_logger.info("Setup complete");
				cb(null);
			});
		}


		//
		public IEnumerator SetupTask(Action<Exception> cb) {
			// Execute async setup function
			MageSetupStatus setupStatus = new MageSetupStatus();
			Setup((Exception error) => {
				setupStatus.error = error;
				setupStatus.done = true;
			});

			// Wait for setup to return
			while (!setupStatus.done) {
				yield return null;
			}

			// Call callback with error if there is one
			cb(setupStatus.error);
		}

		//
		public IEnumerator SetupModulesTask(List<string> moduleNames, Action<Exception> cb) {
			// Execute async setup function
			MageSetupStatus setupStatus = new MageSetupStatus();
			SetupModules(moduleNames, (Exception error) => {
				setupStatus.error = error;
				setupStatus.done = true;
			});

			// Wait for setup to return
			while (!setupStatus.done) {
				yield return null;
			}

			// Call callback with error if there is one
			cb(setupStatus.error);
		}
	}
}
