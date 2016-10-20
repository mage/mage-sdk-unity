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
		private void EmitLog()
		{
			Logger.LogEmitter.Emit("log", this);
			Logger.LogEmitter.Emit("log:" + Channel, this);
		}


		//
		public void Verbose(string message)
		{
			Channel = "verbose";
			Message = message;
			EmitLog();
		}

		public void Debug(string message)
		{
			Channel = "debug";
			Message = message;
			EmitLog();
		}

		public void Info(string message)
		{
			Channel = "info";
			Message = message;
			EmitLog();
		}

		public void Notice(string message)
		{
			Channel = "notice";
			Message = message;
			EmitLog();
		}

		public void Warning(string message)
		{
			Channel = "warning";
			Message = message;
			EmitLog();
		}

		public void Error(string message)
		{
			Channel = "error";
			Message = message;
			EmitLog();
		}

		public void Critical(string message)
		{
			Channel = "critical";
			Message = message;
			EmitLog();
		}

		public void Alert(string message)
		{
			Channel = "alert";
			Message = message;
			EmitLog();
		}

		public void Emergency(string message)
		{
			Channel = "emergency";
			Message = message;
			EmitLog();
		}
	}
}
