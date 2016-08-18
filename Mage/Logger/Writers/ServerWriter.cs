using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;


public class ServerWriter : LogWriter {
	protected Mage mage { get { return Mage.Instance; } }

	private List<string> config;


	public ServerWriter(List<string> logLevels) {
		config = logLevels;

		Logger.logEmitter.on("log", HandleLog);
	}

	public override void Dispose() {
		Logger.logEmitter.off("log", HandleLog);
	}


	private void HandleLog(object sender, LogEntry logEntry) {
		if (config == null || !config.Contains(logEntry.channel)) {
			return;
		}

		string contextMessage = "[" + logEntry.context + "] " + logEntry.message;
		JObject dataObject = null;
		if (logEntry.data != null)
		{
			dataObject = JObject.FromObject(logEntry.data);
		}

		JObject arguments = new JObject();
		arguments.Add("channel", new JValue(logEntry.channel));
		arguments.Add("message", new JValue(contextMessage));
		arguments.Add("data", dataObject);

		mage.commandCenter.SendCommand("logger.sendReport", arguments, (Exception error, JToken result) => {
			// if (error)
			// We honestly can't do anything about this....
		});
	}
}
