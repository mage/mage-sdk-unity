using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.Utils;

namespace Wizcorp.MageSDK.MageClient
{

	public class Module<T> : Singleton<T> where T : class, new()
	{
		//
		protected Mage Mage
		{
			get { return Mage.Instance; }
		}

		protected Logger Logger
		{
			get { return Mage.Logger(GetType().Name); }
		}


		//
		protected virtual List<string> StaticTopics
		{
			get { return null; }
		}

		public JToken StaticData;

		public void SetupStaticData(Action<Exception> cb)
		{
			Logger.Info("Setting up static data");

			if (StaticTopics == null)
			{
				cb(null);
				return;
			}

			var queries = new JObject();
			for (var i = 0; i < StaticTopics.Count; i++)
			{
				string topic = StaticTopics[i];

				var query = new JObject {
					{"topic", new JValue(topic)},
					{"index", new JObject()}
				};
				queries.Add(topic, query);
			}

			StaticData = null;
			Mage.Archivist.MGet(queries, null, (error, data) => {
				if (error != null)
				{
					cb(error);
					return;
				}

				StaticData = data;
				cb(null);
			});
		}


		//
		protected virtual string CommandPrefix
		{
			get { return null; }
		}

		protected virtual List<string> Commands
		{
			get { return null; }
		}

		private Dictionary<string, Action<JObject, Action<Exception, JToken>>> commandHandlerActions;
		private Dictionary<string, Func<JObject, UserCommandStatus>> commandHandlerFuncs;

		public void Command(string commandName, JObject arguments, Action<Exception, JToken> cb)
		{
			commandHandlerActions[commandName](arguments, cb);
		}

		public UserCommandStatus Command(string commandName, JObject arguments)
		{
			return commandHandlerFuncs[commandName](arguments);
		}

		private void RegisterCommand(string command)
		{
			commandHandlerActions.Add(command, (arguments, commandCb) => {
				Mage.CommandCenter.SendCommand(CommandPrefix + "." + command, arguments, (error, result) => {
					try
					{
						commandCb(error, result);
					}
					catch (Exception callbackError)
					{
						Logger.Data(callbackError).Critical("Uncaught exception:");
					}
				});
			});

			commandHandlerFuncs.Add(command, arguments => {
				var commandStatus = new UserCommandStatus();

				Mage.CommandCenter.SendCommand(CommandPrefix + "." + command, arguments, (error, result) => {
					commandStatus.Error = error;
					commandStatus.Result = result;
					commandStatus.Done = true;
				});

				return commandStatus;
			});
		}

		public void SetupUsercommands(Action<Exception> cb)
		{
			Logger.Info("Setting up usercommands");

			commandHandlerActions = new Dictionary<string, Action<JObject, Action<Exception, JToken>>>();
			commandHandlerFuncs = new Dictionary<string, Func<JObject, UserCommandStatus>>();

			if (Commands == null)
			{
				cb(null);
				return;
			}

			foreach (string command in Commands)
			{
				RegisterCommand(command);
			}

			cb(null);
		}
	}
}
