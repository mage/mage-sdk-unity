using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;


public class UsercommandStatus {
	public bool done = false;
	public Exception error = null;
	public JToken result = null;
}


public class Module<T> : Singleton<T> where T : class, new() {
	//
	protected Mage mage { get { return Mage.Instance; } }
	protected Logger logger { get { return mage.logger(this.GetType().Name); } }


	//
	protected virtual List<string> staticTopics { get { return null; } }
	public JToken staticData;
	public void setupStaticData (Action<Exception> cb) {
		logger.info ("Setting up static data");

		if (staticTopics == null) {
			cb(null);
			return;
		}

		JObject queries = new JObject();
		for (int i = 0; i < staticTopics.Count; i++) {
			string topic = staticTopics[i];

			JObject query = new JObject();
			query.Add("topic", new JValue(topic));
			query.Add("index", new JObject());
			queries.Add(topic, query);
		}

		mage.archivist.mget (queries, null, (Exception error, JToken data)=>{
			if (error != null) {
				cb(error);
				return;
			}

			staticData = data;
			cb(null);
		});
	}


	//
	protected virtual string commandPrefix { get { return null; } }
	protected virtual List<string> commands { get { return null; } }
	private Dictionary<string, Action<JObject, Action<Exception, JToken>>> commandHandlerActions;
	private Dictionary<string, Func<JObject, UsercommandStatus>> commandHandlerFuncs;
	
	public void command(string commandName, JObject arguments, Action<Exception, JToken> cb) {
		commandHandlerActions[commandName](arguments, cb);
	}
	
	public UsercommandStatus command(string commandName, JObject arguments) {
		return commandHandlerFuncs[commandName](arguments);
	}

	private void registerCommand(string command) {
		commandHandlerActions.Add(command, (JObject arguments, Action<Exception, JToken> commandCb) => {
			mage.rpcClient.call(commandPrefix + "." + command, arguments, (Exception error, JToken result) => {
				try {
					commandCb(error, result);
				} catch (Exception callbackError) {
					logger.data(callbackError).critical("Uncaught exception:");
				}
			});
		});
		
		commandHandlerFuncs.Add(command, (JObject arguments) => {
			UsercommandStatus commandStatus = new UsercommandStatus();

			mage.rpcClient.call(commandPrefix + "." + command, arguments, (Exception error, JToken result) => {
				commandStatus.error = error;
				commandStatus.result = result;
				commandStatus.done = true;
			});
			
			return commandStatus;
		});
	}
	
	public void setupUsercommands (Action<Exception> cb) {
		logger.info ("Setting up usercommands");

		if (commands == null) {
			cb(null);
			return;
		}
		
		commandHandlerActions = new Dictionary<string, Action<JObject, Action<Exception, JToken>>> ();
		commandHandlerFuncs = new Dictionary<string, Func<JObject, UsercommandStatus>> ();

		foreach (string command in commands) {
			registerCommand(command);
		}
		
		cb (null);
	}
}
