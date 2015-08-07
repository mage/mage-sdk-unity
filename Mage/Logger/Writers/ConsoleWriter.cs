using System;
using UnityEngine;

public class ConsoleWriter {
	private string makeLogString(string channel, string context, string message) {
		string messageString = "[" + channel + "] ";
		messageString += "[" + context + "] ";
		messageString += message;

		return messageString;
	}

	public ConsoleWriter() {
		ConsoleWriter self = this;

		Logger.logEmitter.on ("verbose", (object sender, LogEntry logEntry) => {
			Debug.Log (makeLogString("verbose", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.Log (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("debug", (object sender, LogEntry logEntry) => {
			Debug.Log (makeLogString("debug", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.Log (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("info", (object sender, LogEntry logEntry) => {
			Debug.Log (makeLogString("info", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.Log (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("notice", (object sender, LogEntry logEntry) => {
			Debug.Log (makeLogString("notice", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.Log (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("warning", (object sender, LogEntry logEntry) => {
			Debug.LogWarning (makeLogString("warning", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.LogWarning (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("error", (object sender, LogEntry logEntry) => {
			Debug.LogError (makeLogString("error", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.LogError (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("critical", (object sender, LogEntry logEntry) => {
			Debug.LogError (makeLogString("critical", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.LogError (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("alert", (object sender, LogEntry logEntry) => {
			Debug.LogError (makeLogString("alert", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.LogError (logEntry.data);
			}
		});
		
		Logger.logEmitter.on ("emergency", (object sender, LogEntry logEntry) => {
			Debug.LogError (makeLogString("emergency", logEntry.context, logEntry.message));
			if (logEntry.data != null) {
				Debug.LogError (logEntry.data);
			}
		});
	}
}
