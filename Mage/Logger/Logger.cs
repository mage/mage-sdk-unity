using System;
using System.Collections.Generic;

using Wizcorp.MageSDK.Event;
using Wizcorp.MageSDK.Log.Writers;

namespace Wizcorp.MageSDK.Log
{
	public class Logger
	{
		//
		public static EventEmitter<LogEntry> LogEmitter = new EventEmitter<LogEntry>();

		//
		public static Dictionary<string, LogWriter> LogWriters;
		public static void SetConfig(Dictionary<string, List<string>> config)
		{
			// Destroy existing log writers
			if (LogWriters != null)
			{
				foreach (LogWriter writer in LogWriters.Values)
				{
					writer.Dispose();
				}
				LogWriters = null;
			}

			// Make sure we have configured something
			if (config == null)
			{
				return;
			}

			// Create each writer with log levels
			LogWriters = new Dictionary<string, LogWriter>();
			foreach (KeyValuePair<string, List<string>> property in config)
			{
				string writer = property.Key;
				List<string> writerConfig = property.Value;

				switch (writer)
				{
					case "console":
						LogWriters.Add(writer, new ConsoleWriter(writerConfig));
						break;
					case "server":
						LogWriters.Add(writer, new ServerWriter(writerConfig));
						break;
					default:
						throw new Exception("Unknown Log Writer: " + writer);
				}
			}
		}


		//
		private string context;

		public Logger(string context)
		{
			this.context = context;
		}


		//
		public LogEntry Data(object data)
		{
			return new LogEntry(context, data);
		}


		//
		public void Verbose(string message)
		{
			(new LogEntry(context)).Verbose(message);
		}

		public void Debug(string message)
		{
			(new LogEntry(context)).Debug(message);
		}

		public void Info(string message)
		{
			(new LogEntry(context)).Info(message);
		}

		public void Notice(string message)
		{
			(new LogEntry(context)).Notice(message);
		}

		public void Warning(string message)
		{
			(new LogEntry(context)).Warning(message);
		}

		public void Error(string message)
		{
			(new LogEntry(context)).Error(message);
		}

		public void Critical(string message)
		{
			(new LogEntry(context)).Critical(message);
		}

		public void Alert(string message)
		{
			(new LogEntry(context)).Alert(message);
		}

		public void Emergency(string message)
		{
			(new LogEntry(context)).Emergency(message);
		}
	}
}
