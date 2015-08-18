
public class Logger {
	//
	public static EventEmitter<LogEntry> logEmitter = new EventEmitter<LogEntry>();


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
