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

		//
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
			this.headers = (headers == null) ? new Dictionary<string, string>() : new Dictionary<string, string>(headers);

			if (CommandCenter != null)
			{
				CommandCenter.SetEndpoint(baseUrl, appName, this.headers);
			}

			if (MessageStream != null)
			{
				MessageStream.SetEndpoint(baseUrl, this.headers);
			}

			logger.Debug("Enpoints set!");
        }

		//
		public void Setup()
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

			logger.Debug("Setup Complete!");
        }
	}
}
