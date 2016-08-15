using System;
using System.Collections.Generic;

public enum CommandTransportType {
	HTTP,
	JSONRPC
}

public abstract class CommandTransportClient {
	public Action OnSendComplete;
	public Action<string, Exception> OnTransportError;

	public abstract void SetEndpoint(string baseUrl, string appName, Dictionary<string, string> headers = null);
	public abstract void SendBatch(CommandBatch batch);
}