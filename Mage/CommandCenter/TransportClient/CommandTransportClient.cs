using System;

public enum CommandTransportType {
	HTTP,
	JSONRPC
}

public abstract class CommandTransportClient {
	public Action OnSendComplete;
	public Action<string, Exception> OnTransportError;

	public abstract void SetEndpoint(string baseUrl, string appName, string username = null, string password = null);
	public abstract void SendBatch(CommandBatch batch);
}