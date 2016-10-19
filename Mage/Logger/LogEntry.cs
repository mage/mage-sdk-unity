namespace Wizcorp.MageSDK.Log {
	public class LogEntry {
		//
		public string channel;
		public string context;
		public object data;
		public string message;

		public LogEntry(string context, object data = null) {
			this.context = context;
			this.data = data;
		}


		//
		private void emitLog() {
			Logger.logEmitter.emit("log", this);
			Logger.logEmitter.emit("log:" + this.channel, this);
		}


		//
		public void verbose(string message) {
			this.channel = "verbose";
			this.message = message;
			emitLog();
		}

		public void debug(string message) {
			this.channel = "debug";
			this.message = message;
			emitLog();
		}

		public void info(string message) {
			this.channel = "info";
			this.message = message;
			emitLog();
		}

		public void notice(string message) {
			this.channel = "notice";
			this.message = message;
			emitLog();
		}

		public void warning(string message) {
			this.channel = "warning";
			this.message = message;
			emitLog();
		}

		public void error(string message) {
			this.channel = "error";
			this.message = message;
			emitLog();
		}

		public void critical(string message) {
			this.channel = "critical";
			this.message = message;
			emitLog();
		}

		public void alert(string message) {
			this.channel = "alert";
			this.message = message;
			emitLog();
		}

		public void emergency(string message) {
			this.channel = "emergency";
			this.message = message;
			emitLog();
		}
	}
}
