using System;
using System.Collections.Generic;

namespace Wizcorp.MageSDK.Log.Writers
{
	public class ConsoleWriter : LogWriter
	{
		private List<string> config;

		public ConsoleWriter(List<string> logLevels)
		{
			config = logLevels;

			if (config.Contains("verbose"))
			{
				Logger.LogEmitter.On("log:verbose", Verbose);
			}

			if (config.Contains("debug"))
			{
				Logger.LogEmitter.On("log:debug", Debug);
			}

			if (config.Contains("info"))
			{
				Logger.LogEmitter.On("log:info", Info);
			}

			if (config.Contains("notice"))
			{
				Logger.LogEmitter.On("log:notice", Notice);
			}

			if (config.Contains("warning"))
			{
				Logger.LogEmitter.On("log:warning", Warning);
			}

			if (config.Contains("error"))
			{
				Logger.LogEmitter.On("log:error", Error);
			}

			if (config.Contains("critical"))
			{
				Logger.LogEmitter.On("log:critical", Critical);
			}

			if (config.Contains("alert"))
			{
				Logger.LogEmitter.On("log:alert", Alert);
			}

			if (config.Contains("emergency"))
			{
				Logger.LogEmitter.On("log:emergency", Emergency);
			}
		}

		public override void Dispose()
		{
			if (config.Contains("verbose"))
			{
				Logger.LogEmitter.Off("log:verbose", Verbose);
			}

			if (config.Contains("debug"))
			{
				Logger.LogEmitter.Off("log:debug", Debug);
			}

			if (config.Contains("info"))
			{
				Logger.LogEmitter.Off("log:info", Info);
			}

			if (config.Contains("notice"))
			{
				Logger.LogEmitter.Off("log:notice", Notice);
			}

			if (config.Contains("warning"))
			{
				Logger.LogEmitter.Off("log:warning", Warning);
			}

			if (config.Contains("error"))
			{
				Logger.LogEmitter.Off("log:error", Error);
			}

			if (config.Contains("critical"))
			{
				Logger.LogEmitter.Off("log:critical", Critical);
			}

			if (config.Contains("alert"))
			{
				Logger.LogEmitter.Off("log:alert", Alert);
			}

			if (config.Contains("emergency"))
			{
				Logger.LogEmitter.Off("log:emergency", Emergency);
			}
		}

		private string makeLogString(string channel, string context, string message)
		{
			return String.Format("[{0}] [{1}] {2}", channel, context, message);
		}

		private object makeDataString(object logData)
		{
			// Check if data is an excetion with a stack trace
			var exception = logData as Exception;
			if (exception != null && exception.StackTrace != null)
			{
				return exception + ":\n" + exception.StackTrace;
			}

			// Otherwise log data as is using its toString function
			return logData;
		}

		private void Verbose(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.Log(makeLogString("verbose", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.Log(makeDataString(logEntry.Data));
			}
		}

		private void Debug(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.Log(makeLogString("debug", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.Log(makeDataString(logEntry.Data));
			}
		}

		private void Info(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.Log(makeLogString("info", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.Log(makeDataString(logEntry.Data));
			}
		}

		private void Notice(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.Log(makeLogString("notice", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.Log(makeDataString(logEntry.Data));
			}
		}

		private void Warning(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.LogWarning(makeLogString("warning", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.LogWarning(makeDataString(logEntry.Data));
			}
		}

		private void Error(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.LogError(makeLogString("error", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.LogError(makeDataString(logEntry.Data));
			}
		}

		private void Critical(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.LogError(makeLogString("critical", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.LogError(makeDataString(logEntry.Data));
			}
		}

		private void Alert(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.LogError(makeLogString("alert", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.LogError(makeDataString(logEntry.Data));
			}
		}

		private void Emergency(object sender, LogEntry logEntry)
		{
			UnityEngine.Debug.LogError(makeLogString("emergency", logEntry.Context, logEntry.Message));
			if (logEntry.Data != null)
			{
				UnityEngine.Debug.LogError(makeDataString(logEntry.Data));
			}
		}
	}
}
