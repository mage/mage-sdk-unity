namespace Wizcorp.MageSDK.Log
{
	public class LogEntry
	{
		//
		public string Context;
		public object Data;
		public string Message;

		public LogEntry(string context, object data = null)
		{
			Context = context;
			Data = data;
		}


		//
		public void Verbose(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("verbose", this);
		}

		public void Debug(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("debug", this);
		}

		public void Info(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("info", this);
		}

		public void Notice(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("notice", this);
		}

		public void Warning(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("warning", this);
		}

		public void Error(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("error", this);
		}

		public void Critical(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("critical", this);
		}

		public void Alert(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("alert", this);
		}

		public void Emergency(string message)
		{
			Message = message;
			Logger.LogEmitter.Emit("emergency", this);
		}
	}
}