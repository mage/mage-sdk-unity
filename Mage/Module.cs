using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.Utils;

namespace Wizcorp.MageSDK.MageClient
{
	using MageCommandAction = Action<JObject, Action<Exception, JToken>>;
	using MageCommandFunction = Func<JObject, UserCommandStatus>;
	
	public abstract class Module<T> : Singleton<T> where T : class, new()	
	{
		// Tells us if the module was set up
		private bool _SetupCompleted = false;
		public bool SetupCompleted { 
			get { return _SetupCompleted; }
			private set { _SetupCompleted = value; }
		}
		

		// Mage singleton accessor
		protected Mage Mage
		{
			get { return Mage.Instance; }
		}

		// Contextualized logger
		protected Logger Logger
		{
			get { return Mage.Logger(GetType().Name); }
		}

		// List of static topics to load during setup
		protected virtual List<string> StaticTopics
		{
			get { return null; }
		}

		// Static data container
		public JToken StaticData;

		// Static data setup
		//
		// Note that topics are not tied to MAGE modules; they are
		// essentially global to the MAGE server instance. This is 
		// simply a convenience function for loading data at setup time.
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


		// The module name as defined on the remote MAGE server
		protected abstract string CommandPrefix { get; }

		// The list of available user commands on the remote MAGE server
		protected abstract List<string> Commands { get; }

		private Dictionary<string, MageCommandAction> commandHandlerActions;
		private Dictionary<string, MageCommandFunction> commandHandlerFuncs;

		private void AssertSetupCompleted() 
		{
			if (SetupCompleted == false) 
			{
				throw new Exception("Module was not setup: " + CommandPrefix);
			}
		}

		private M GetCommand<M>(Dictionary<string, M> list, string commandName) {
			AssertSetupCompleted();

			if (list == null) {
				throw new Exception("Module does not define any user commands: " + CommandPrefix);
			}

			var command = list[commandName];

			if (command == null) 
			{
				throw new Exception("User command not found: " + CommandPrefix + "." + commandName);
			}

			return command;
		}

		public void Command(string commandName, JObject arguments, Action<Exception, JToken> cb)
		{
			var action = GetCommand(commandHandlerActions, commandName);
			action(arguments, cb);
		}

		public UserCommandStatus Command(string commandName, JObject arguments)
		{
			var action = GetCommand(commandHandlerFuncs, commandName);
			return action(arguments);
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

			if (Commands == null) {
				SetupCompleted = true;
				cb(null);
				return;
			}

			commandHandlerActions = new Dictionary<string, Action<JObject, Action<Exception, JToken>>>();
			commandHandlerFuncs = new Dictionary<string, Func<JObject, UserCommandStatus>>();

			foreach (string command in Commands)
			{
				RegisterCommand(command);
			}

			SetupCompleted = true;
			cb(null);
		}
	}
}
