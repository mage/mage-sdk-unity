namespace Wizcorp.MageSDK.Log
{
	public class LogEntry
	{
		//
		public string Channel;
		public string Context;
		public object Data;
		public string Message;

		public LogEntry(string context, object data = null)
		{
			Context = context;
			Data = data;
		}

		//
		private void emitLog() {
			Logger.LogEmitter.Emit("log", this);
			Logger.LogEmitter.Emit("log:" + Channel, this);
		}

		//
		public void Verbose(string message)
		{
			Channel = "verbose";
			Message = message;
			emitLog();
		}

		public void Debug(string message)
		{
			Channel = "debug";
			Message = message;
			emitLog();
		}

		public void Info(string message)
		{
			Channel = "info";
			Message = message;
			emitLog();
		}

		public void Notice(string message)
		{
			Channel = "notice";
			Message = message;
			emitLog();
		}

		public void Warning(string message)
		{
			Channel = "warning";
			Message = message;
			emitLog();
		}

		public void Error(string message)
		{
			Channel = "error";
			Message = message;
			emitLog();
		}

		public void Critical(string message)
		{
			Channel = "critical";
			Message = message;
			emitLog();
		}

		public void Alert(string message)
		{
			Channel = "alert";
			Message = message;
			emitLog();
		}

		public void Emergency(string message)
		{
			Channel = "emergency";
			Message = message;
			emitLog();
		}
	}
}