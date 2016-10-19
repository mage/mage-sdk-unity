using System;
using System.Collections.Generic;

namespace Wizcorp.MageSDK.Log.Writers {
	public class ConsoleWriter : LogWriter {
		private List<string> config;

		public ConsoleWriter(List<string> logLevels) {
			config = logLevels;

			if (config.Contains("verbose")) {
				Logger.logEmitter.on("log:verbose", Verbose);
			}

			if (config.Contains("debug")) {
				Logger.logEmitter.on("log:debug", Debug);
			}

			if (config.Contains("info")) {
				Logger.logEmitter.on("log:info", Info);
			}

			if (config.Contains("notice")) {
				Logger.logEmitter.on("log:notice", Notice);
			}

			if (config.Contains("warning")) {
				Logger.logEmitter.on("log:warning", Warning);
			}

			if (config.Contains("error")) {
				Logger.logEmitter.on("log:error", Error);
			}

			if (config.Contains("critical")) {
				Logger.logEmitter.on("log:critical", Critical);
			}

			if (config.Contains("alert")) {
				Logger.logEmitter.on("log:alert", Alert);
			}

			if (config.Contains("emergency")) {
				Logger.logEmitter.on("log:emergency", Emergency);
			}
		}

		public override void Dispose() {
			if (config.Contains("verbose")) {
				Logger.logEmitter.off("log:verbose", Verbose);
			}

			if (config.Contains("debug")) {
				Logger.logEmitter.off("log:debug", Debug);
			}

			if (config.Contains("info")) {
				Logger.logEmitter.off("log:info", Info);
			}

			if (config.Contains("notice")) {
				Logger.logEmitter.off("log:notice", Notice);
			}

			if (config.Contains("warning")) {
				Logger.logEmitter.off("log:warning", Warning);
			}

			if (config.Contains("error")) {
				Logger.logEmitter.off("log:error", Error);
			}

			if (config.Contains("critical")) {
				Logger.logEmitter.off("log:critical", Critical);
			}

			if (config.Contains("alert")) {
				Logger.logEmitter.off("log:alert", Alert);
			}

			if (config.Contains("emergency")) {
				Logger.logEmitter.off("log:emergency", Emergency);
			}
		}

		private string makeLogString(string channel, string context, string message) {
			return String.Format("[{0}] [{1}] {2}", channel, context, message);
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
				UnityEngine.Debug.LogWarning(logEntry.data);
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
}
