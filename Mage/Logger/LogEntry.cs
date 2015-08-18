
public class LogEntry {
	//
	public string context;
	public object data;
	public string message;

	public LogEntry(string _context, object _data = null) {
		context = _context;
		data = _data;
	}
	
	
	//
	public void verbose (string _message) {
		message = _message;
		Logger.logEmitter.emit("verbose", this);
	}
	
	public void debug (string _message) {
		message = _message;
		Logger.logEmitter.emit("debug", this);
	}
	
	public void info (string _message) {
		message = _message;
		Logger.logEmitter.emit("info", this);
	}
	
	public void notice (string _message) {
		message = _message;
		Logger.logEmitter.emit("notice", this);
	}
	
	public void warning (string _message) {
		message = _message;
		Logger.logEmitter.emit("warning", this);
	}
	
	public void error (string _message) {
		message = _message;
		Logger.logEmitter.emit("error", this);
	}
	
	public void critical (string _message) {
		message = _message;
		Logger.logEmitter.emit("critical", this);
	}
	
	public void alert (string _message) {
		message = _message;
		Logger.logEmitter.emit("alert", this);
	}
	
	public void emergency (string _message) {
		message = _message;
		Logger.logEmitter.emit("emergency", this);
	}
}
