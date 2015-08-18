using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;


public class Module<T> : Singleton<T> where T : class, new() {
	//
	protected Mage mage { get { return Mage.instance; } }
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
	private Dictionary<string, Action<JObject, Action<Exception, JToken>>> commandHandlers;
	public Action<JObject, Action<Exception, JToken>> this[string commandName] {
		get {
			return commandHandlers[commandName];
		}
	}
	public void setupUsercommands (Action<Exception> cb) {
		logger.info ("Setting up usercommands");

		commandHandlers = new Dictionary<string, Action<JObject, Action<Exception, JToken>>> ();

		Async.each<string> (commands, (string command, Action<Exception> callback) => {
			commandHandlers.Add(command, (JObject arguments, Action<Exception, JToken> commandCb) => {
				mage.rpcClient.call(commandPrefix + "." + command, arguments, (Exception error, JToken result) => {
					try {
						commandCb(error, result);
					} catch (Exception callbackError) {
						logger.data(callbackError).critical("Uncaught exception:");
					}
				});
			});

			callback(null);
		}, cb);
	}
}
