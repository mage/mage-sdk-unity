using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.MageClient;

namespace Wizcorp.MageSDK.Log.Writers
{
	public class ServerWriter : LogWriter
	{
		protected Mage Mage { get { return Mage.Instance; } }

		private List<string> config;


		public ServerWriter(List<string> logLevels)
		{
			config = logLevels;

			Logger.LogEmitter.On("log", HandleLog);
		}

		public override void Dispose()
		{
			Logger.LogEmitter.Off("log", HandleLog);
		}


		private void HandleLog(object sender, LogEntry logEntry)
		{
			if (config == null || !config.Contains(logEntry.Channel))
			{
				return;
			}

			string contextMessage = "[" + logEntry.Context + "] " + logEntry.Message;
			JObject dataObject = null;
			if (logEntry.Data != null)
			{
				dataObject = JObject.FromObject(logEntry.Data);
			}

			JObject arguments = new JObject();
			arguments.Add("channel", new JValue(logEntry.Channel));
			arguments.Add("message", new JValue(contextMessage));
			arguments.Add("data", dataObject);

			Mage.CommandCenter.SendCommand("logger.sendReport", arguments, (error, result) => {
				// if (error)
				// We honestly can't do anything about this....
			});
		}
	}
}
