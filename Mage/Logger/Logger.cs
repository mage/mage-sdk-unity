using System;
using System.Collections.Generic;


public class Logger {
	//
	public static EventEmitter<LogEntry> logEmitter = new EventEmitter<LogEntry>();

	//
	public static Dictionary<string, LogWriter> logWriters;
	public static void SetConfig(Dictionary<string, List<string>> config) {
		// Destroy existing log writers
		if (logWriters != null) {
			foreach (var writer in logWriters.Values) {
				writer.Dispose();
			}
			logWriters = null;
		}

		// Make sure we have configured something
		if (config == null) {
			return;
		}

		// Create each writer with log levels
		logWriters = new Dictionary<string, LogWriter>();
		foreach (var property in config) {
			string writer = property.Key;
			List<string> writerConfig = property.Value;

			switch (writer) {
			case "console":
				logWriters.Add(writer, new ConsoleWriter(writerConfig) as LogWriter);
				break;
			case "server":
				logWriters.Add(writer, new ServerWriter(writerConfig) as LogWriter);
				break;
			default:
				throw new Exception("Unknown Log Writer: " + writer);
			}
		}
	}


	//
	private string _context;

	public Logger(string context) {
		_context = context;
	}
	
	
	//
	public LogEntry data (object data) {
		return new LogEntry (_context, data);
	}


	//
	public void verbose (string message) {
		(new LogEntry (_context)).verbose (message);
	}
	
	public void debug (string message) {
		(new LogEntry (_context)).debug (message);
	}

	public void info (string message) {
		(new LogEntry (_context)).info (message);
	}
	
	public void notice (string message) {
		(new LogEntry (_context)).notice (message);
	}
	
	public void warning (string message) {
		(new LogEntry (_context)).warning (message);
	}
	
	public void error (string message) {
		(new LogEntry (_context)).error (message);
	}
	
	public void critical (string message) {
		(new LogEntry (_context)).critical (message);
	}
	
	public void alert (string message) {
		(new LogEntry (_context)).alert (message);
	}
	
	public void emergency (string message) {
		(new LogEntry (_context)).emergency (message);
	}
}
