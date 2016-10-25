using System;
using System.Collections.Generic;

using Wizcorp.MageSDK.CommandCenter.Client;
using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.MageClient;
using Wizcorp.MageSDK.MageClient.Command;
using Wizcorp.MageSDK.Network.JsonRpc;

namespace Wizcorp.MageSDK.Command.Client
{
	public class CommandJsonRpcClient : CommandTransportClient
	{
		private Mage Mage
		{
			get { return Mage.Instance; }
		}

		private Logger Logger
		{
			get { return Mage.Logger("CommandJsonRpcClient"); }
		}

		private JsonRpc rpcClient = new JsonRpc();

		//
		public override void SetEndpoint(string baseUrl, string appName, Dictionary<string, string> headers = null)
		{
			rpcClient.SetEndpoint(baseUrl + "/" + appName + "/jsonrpc", headers);
		}

		//
		public override void SerialiseBatch(CommandBatch commandBatch)
		{
			Logger.Verbose("THIS TRANSPORT CLIENT IS NOT IMPLEMENTED");
			throw new Exception("THIS TRANSPORT CLIENT IS NOT IMPLEMENTED");
		}

		//
		public override void SendBatch(CommandBatch commandBatch)
		{
			// NOTE: This transport client cannot be implemented yet as JSON RPC support is
			// terminally broken in MAGE (does not support queryId and response caching).
			// Until this is fixed, this transport client cannot be used or completed.
			Logger.Verbose("THIS TRANSPORT CLIENT IS NOT IMPLEMENTED");
			throw new Exception("THIS TRANSPORT CLIENT IS NOT IMPLEMENTED");

			/*
			JSONRPCBatch rpcBatch = new JSONRPCBatch();
			for (int batchId = 0; batchId < commandBatch.batchItems.Count; batchId += 1) {
				CommandBatchItem commandItem = commandBatch.batchItems[batchId];
				rpcBatch.Add(batchId, commandItem.commandName, commandItem.parameters);
				logger.data(commandItem.parameters).verbose("[" + commandItem.commandName + "] executing command");
			}

			// Attach any required mage headers
			Dictionary<string, string> headers = new Dictionary<string, string>();

			string sessionKey = mage.session.GetSessionKey();
			if (!string.IsNullOrEmpty(sessionKey)) {
				headers.Add("X-MAGE-SESSION", sessionKey);
			}

			// Send rpc batch
			rpcClient.CallBatch(rpcBatch, headers, mage.cookies, (Exception error, JArray responseArray) => {
				logger.data(responseArray).verbose("Recieved response: ");

				if (error != null) {
					//TODO: OnTransportError.Invoke("", error);
					return;
				}

				// Process each command response
				foreach (JObject responseObject in responseArray) {
					int batchId = (int)responseObject["id"];
					CommandBatchItem commandItem = commandBatch.batchItems[batchId];
					string commandName = commandItem.commandName;
					Action<Exception, JToken> commandCb = commandItem.cb;

					// Check if there are any events attached to this request
					if (responseObject["result"]["myEvents"] != null) {
						logger.verbose("[" + commandName + "] processing events");
						mage.eventManager.emitEventList((JArray)responseObject["result"]["myEvents"]);
					}

					// Check if the response was an error
					if (responseObject["result"]["errorCode"] != null) {
						logger.verbose("[" + commandName + "] server error");
						commandCb(new Exception(responseObject["result"]["errorCode"].ToString()), null);
						return;
					}

					// Pull off call result object, if it doesn't exist
					logger.verbose("[" + commandName + "] call response");
					commandCb(null, responseObject["result"]["response"]);
				}
			});
			*/
		}
	}
}
