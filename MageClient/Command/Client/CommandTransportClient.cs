using System;
using System.Collections.Generic;

using Wizcorp.MageSDK.MageClient.Command;

namespace Wizcorp.MageSDK.CommandCenter.Client
{
	public abstract class CommandTransportClient
	{
		public Action OnSendComplete;
		public Action<string, Exception> OnTransportError;

		public abstract void SetEndpoint(string baseUrl, string appName, Dictionary<string, string> headers = null);
		public abstract void SerialiseBatch(CommandBatch commandBatch);
		public abstract void SendBatch(CommandBatch batch);
	}
}
