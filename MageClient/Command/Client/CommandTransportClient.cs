using System;

using Wizcorp.MageSDK.MageClient.Command;

namespace Wizcorp.MageSDK.CommandCenter.Client
{
	public abstract class CommandTransportClient
	{
		public Action OnSendComplete;
		public Action<string, Exception> OnTransportError;
		public abstract void SetEndpoint(string baseUrl, string appName, string username = null, string password = null);
		public abstract void SendBatch(CommandBatch batch);
	}
}