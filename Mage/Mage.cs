using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Net;

using Newtonsoft.Json.Linq;


public class MageSetupStatus {
	public bool done = false;
	public Exception error = null;
}


public class Mage : Singleton<Mage> {
	//
	public EventManager eventManager;
	public Session session;
	public RPCClient rpcClient;
	public MessageStream messageStream;
	public Archivist archivist;

	private ConsoleWriter _consoleWriter;
	private Logger _logger;

	//
	public CookieContainer cookies;

	//
	private Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
	public Logger logger (string context = null) {
		if (string.IsNullOrEmpty(context)) {
			context = "Default";
		}

		if (_loggers.ContainsKey(context)) {
			return _loggers[context];
		}

		Logger newLogger = new Logger (context);
		_loggers.Add (context, new Logger(context));
		return newLogger;
	}


	//
	public Mage() {
		eventManager = new EventManager();
		session = new Session();
		rpcClient = new RPCClient();
		messageStream = new MessageStream();
		archivist = new Archivist();

		_consoleWriter = new ConsoleWriter();
		_logger = logger("mage");

		// create a shared cookie container
		cookies = new CookieContainer();

		// TODO: properly check the damn certificate, for now ignore invalid ones (fix issue on Android/iOS)
		ServicePointManager.ServerCertificateValidationCallback += (o, cert, chain, errors) => true;
	}

	//
	public void setEndpoints (string baseUrl, string appName, string username = null, string password = null) {
		rpcClient.setEndpoint (baseUrl, appName, username, password);
		messageStream.setEndpoint (baseUrl, username, password);
	}

	//
	public void setup (List<string> moduleNames, Action<Exception> cb) {
		_logger.info ("Setting up modules");

		Async.each<string> (moduleNames, (string moduleName, Action<Exception> callback) => {
			_logger.info("Setting up module: " + moduleName);

			// Use reflection to find module by name
			Assembly assembly = Assembly.GetExecutingAssembly();
			Type[] assemblyTypes = assembly.GetTypes();
			foreach(Type t in assemblyTypes) {
				if (moduleName == t.Name) {
					// Invoke the setup method on the module
					BindingFlags memberType = BindingFlags.InvokeMethod;
					object[] arguments = new object[]{callback};
					t.InvokeMember("setupInstance", memberType, null, null, arguments);
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
	
	public IEnumerator setupTask (List<string> moduleNames, Action<Exception> cb) {
		// Execute async setup function
		MageSetupStatus setupStatus = new MageSetupStatus ();
		setup(moduleNames, (Exception error) => {
			setupStatus.error = error;
			setupStatus.done = true;
		});

		// Wait for setup to return
		while (!setupStatus.done) {
			yield return null;
		}

		// Call callback with error if there is one
		cb (setupStatus.error);
	}
}
