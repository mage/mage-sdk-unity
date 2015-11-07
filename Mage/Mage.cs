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
	string baseUrl;
	string appName;
	string username;
	string password;
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


	// Avoid putting setup logic in the contstuctor. Only things that can be
	// carried between game sessions should go here. Otherwise we need to be
	// able to re-initialize them inside the setup function.
	public Mage() {
		// Setup log writters
		_consoleWriter = new ConsoleWriter();
		_logger = logger("mage");

		// TODO: properly check the damn certificate, for now ignore invalid ones (fix issue on Android/iOS)
		ServicePointManager.ServerCertificateValidationCallback += (o, cert, chain, errors) => true;
	}

	//
	public void setEndpoints (string baseUrl, string appName, string username = null, string password = null) {
		this.baseUrl = baseUrl;
		this.appName = appName;
		this.username = username;
		this.password = password;

		if (rpcClient != null) {
			rpcClient.setEndpoint(baseUrl, appName, username, password);
		}

		if (messageStream != null) {
			messageStream.setEndpoint(baseUrl, username, password);
		}
	}

	//
	public void setup (List<string> moduleNames, Action<Exception> cb) {
		// Cleanup any existing internal modules
		if (messageStream != null) {
			messageStream.Dispose();
		}


		// Create a shared cookie container
		cookies = new CookieContainer();


		// Initialize mage internal modules
		eventManager = new EventManager();
		session = new Session();
		rpcClient = new RPCClient();
		messageStream = new MessageStream();
		archivist = new Archivist();


		// Set endpoints
		rpcClient.setEndpoint(baseUrl, appName, username, password);
		messageStream.setEndpoint(baseUrl, username, password);


		// Setup application modules
		_logger.info ("Setting up modules");
		Async.each<string> (moduleNames, (string moduleName, Action<Exception> callback) => {
			_logger.info("Setting up module: " + moduleName);

			// Use reflection to find module by name
			Assembly assembly = Assembly.GetExecutingAssembly();
			Type[] assemblyTypes = assembly.GetTypes();
			foreach(Type moduleType in assemblyTypes) {
				if (moduleName == moduleType.Name) {
					// Grab module instance from singleton base
					BindingFlags staticInstanceGetter = BindingFlags.GetProperty;
					var singletonType = typeof(Singleton<>).MakeGenericType(moduleType);
					Object instance = singletonType.InvokeMember("Instance", staticInstanceGetter, null, null, null);

					// Invoke the setup method on the module
					BindingFlags publicSetupFunction = BindingFlags.InvokeMethod;
					object[] arguments = new object[]{callback};
					moduleType.InvokeMember("Setup", publicSetupFunction, null, instance, arguments);
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
