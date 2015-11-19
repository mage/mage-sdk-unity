using System;
using System.Collections.Generic;


public class ConsoleWriter : LogWriter {
	private List<string> config;

	public ConsoleWriter(List<string> logLevels) {
		config = logLevels;

		if (config.Contains("verbose")) {
			Logger.logEmitter.on("verbose", Verbose);
		}

		if (config.Contains("debug")) {
			Logger.logEmitter.on("debug", Debug);
		}

		if (config.Contains("info")) {
			Logger.logEmitter.on("info", Info);
		}

		if (config.Contains("notice")) {
			Logger.logEmitter.on("notice", Notice);
		}

		if (config.Contains("warning")) {
			Logger.logEmitter.on("warning", Warning);
		}

		if (config.Contains("error")) {
			Logger.logEmitter.on("error", Error);
		}

		if (config.Contains("critical")) {
			Logger.logEmitter.on("critical", Critical);
		}

		if (config.Contains("alert")) {
			Logger.logEmitter.on("alert", Alert);
		}
		
		if (config.Contains("emergency")) {
			Logger.logEmitter.on("emergency", Emergency);
		}
	}

	public override void Dispose() {
		if (config.Contains("verbose")) {
			Logger.logEmitter.off("verbose", Verbose);
		}
		
		if (config.Contains("debug")) {
			Logger.logEmitter.off("debug", Debug);
		}
		
		if (config.Contains("info")) {
			Logger.logEmitter.off("info", Info);
		}
		
		if (config.Contains("notice")) {
			Logger.logEmitter.off("notice", Notice);
		}
		
		if (config.Contains("warning")) {
			Logger.logEmitter.off("warning", Warning);
		}
		
		if (config.Contains("error")) {
			Logger.logEmitter.off("error", Error);
		}
		
		if (config.Contains("critical")) {
			Logger.logEmitter.off("critical", Critical);
		}
		
		if (config.Contains("alert")) {
			Logger.logEmitter.off("alert", Alert);
		}
		
		if (config.Contains("emergency")) {
			Logger.logEmitter.off("emergency", Emergency);
		}
	}

	private string makeLogString(string channel, string context, string message) {
		string messageString = "[" + channel + "] ";
		messageString += "[" + context + "] ";
		messageString += message;
		
		return messageString;
	}
	
	private void Verbose(object sender, LogEntry logEntry) {
		UnityEngine.Debug.Log(makeLogString("verbose", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.Log(logEntry.data);
		}
	}
	
	private void Debug(object sender, LogEntry logEntry) {
		UnityEngine.Debug.Log(makeLogString("debug", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.Log(logEntry.data);
		}
	}
	
	private void Info(object sender, LogEntry logEntry) {
		UnityEngine.Debug.Log(makeLogString("info", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.Log(logEntry.data);
		}
	}
	
	private void Notice(object sender, LogEntry logEntry) {
		UnityEngine.Debug.Log(makeLogString("notice", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.Log(logEntry.data);
		}
	}
	
	private void Warning(object sender, LogEntry logEntry) {
		UnityEngine.Debug.LogWarning(makeLogString("warning", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.LogWarning (logEntry.data);
		}
	}
	
	private void Error(object sender, LogEntry logEntry) {
		UnityEngine.Debug.LogError(makeLogString("error", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.LogError(logEntry.data);
		}
	}
	
	private void Critical(object sender, LogEntry logEntry) {
		UnityEngine.Debug.LogError(makeLogString("critical", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			if (logEntry.data is Exception && (logEntry.data as Exception).StackTrace != null) {
				Exception excpt = logEntry.data as Exception;
				UnityEngine.Debug.LogError(excpt.ToString() + ":\n" + excpt.StackTrace.ToString());
			} else {
				UnityEngine.Debug.LogError(logEntry.data);
			}
		}
	}
	
	private void Alert(object sender, LogEntry logEntry) {
		UnityEngine.Debug.LogError(makeLogString("alert", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.LogError(logEntry.data);
		}
	}
	
	private void Emergency(object sender, LogEntry logEntry) {
		UnityEngine.Debug.LogError(makeLogString("emergency", logEntry.context, logEntry.message));
		if (logEntry.data != null) {
			UnityEngine.Debug.LogError(logEntry.data);
		}
	}
}
