using Newtonsoft.Json.Linq;
using System;
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

		//
		protected Logger Logger
		{
			get { return Mage.Logger(GetType().Name); }
		}

		//
		protected virtual string CommandPrefix
		{
			get {
				var name = GetType().Name;
				return name.Substring(0, 1).ToLower() + name.Substring(1);
			}
		}

		//
		public void Command(string commandName, JObject arguments, Action<Exception, JToken> cb)
		{
			Mage.CommandCenter.SendCommand(CommandPrefix + "." + commandName, arguments, cb);
		}

		//
		public UserCommandStatus Command(string commandName, JObject arguments)
		{
			var commandStatus = new UserCommandStatus();

			Mage.CommandCenter.SendCommand(CommandPrefix + "." + commandName, arguments, (error, result) => {
				commandStatus.Error = error;
				commandStatus.Result = result;
				commandStatus.Done = true;
			});

			return commandStatus;
		}
	}
}
