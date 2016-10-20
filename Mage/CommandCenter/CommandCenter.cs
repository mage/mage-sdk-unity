using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Command.Client;
using Wizcorp.MageSDK.CommandCenter.Client;
using Wizcorp.MageSDK.Log;

namespace Wizcorp.MageSDK.MageClient.Command
{
	public class CommandCenter
	{
		private Mage Mage
		{
			get { return Mage.Instance; }
		}

		private Logger Logger
		{
			get { return Mage.Logger("CommandCenter"); }
		}

		// Endpoint and credentials
		private string baseUrl;
		private string appName;
		private Dictionary<string, string> headers = new Dictionary<string, string>();

		// Message Hooks
		public delegate void MessageHook(CommandBatch commandBatch);
		public MessageHook preSerialiseHook;
		public MessageHook preNetworkHook;

		// Current transport client
		private CommandTransportClient transportClient;

		// Command Batches
		private int nextQueryId = 1;
		private CommandBatch currentBatch;
		private CommandBatch sendingBatch;

		//
		public CommandCenter(CommandTransportType transportType = CommandTransportType.HTTP)
		{
			currentBatch = new CommandBatch(nextQueryId++);
			SetTransport(transportType);
		}

		//
		public void SetTransport(CommandTransportType transportType)
		{
			// Cleanup existing transport client
			transportClient = null;

			// Create new transport client instance
			if (transportType == CommandTransportType.HTTP)
			{
				transportClient = new CommandHttpClient();
				transportClient.SetEndpoint(baseUrl, appName, headers);
			}
			else if (transportType == CommandTransportType.JSONRPC)
			{
				transportClient = new CommandJsonRpcClient();
				transportClient.SetEndpoint(baseUrl, appName, headers);
			}
			else
			{
				throw new Exception("Invalid transport type: " + transportType);
			}

			// Setup event handlers
			transportClient.OnSendComplete += BatchComplete;
			transportClient.OnTransportError += TransportError;
		}

		//
		public void SetEndpoint(string baseUrl, string appName, Dictionary<string, string> headers = null)
		{
			this.baseUrl = baseUrl;
			this.appName = appName;
			this.headers = new Dictionary<string, string>(headers);

			if (transportClient != null)
			{
				transportClient.SetEndpoint(this.baseUrl, this.appName, this.headers);
			}
		}

		//
		private void SendBatch()
		{
			Mage.EventManager.Emit("io.send", null);

			lock ((object)this)
			{
				// Swap batches around locking the queue
				sendingBatch = currentBatch;
				currentBatch = new CommandBatch(nextQueryId++);

				// Execute pre-serialisation message hooks
				if (preSerialiseHook != null)
				{
					preSerialiseHook.Invoke(sendingBatch);
				}

				// Serialise the batch
				Logger.Debug("Serialising batch: " + sendingBatch.QueryId);
				transportClient.SerialiseBatch(sendingBatch);

				// Execute pre-network message hooks
				if (preNetworkHook != null)
				{
					preNetworkHook.Invoke(sendingBatch);
				}

				// Send the batch
				Logger.Debug("Sending batch: " + sendingBatch.QueryId);
				transportClient.SendBatch(sendingBatch);
			}
		}

		// Resend batch
		public void Resend()
		{
			Mage.EventManager.Emit("io.resend", null);

			Logger.Debug("Re-sending batch: " + sendingBatch.QueryId);
			transportClient.SendBatch(sendingBatch);
		}

		//
		private void BatchComplete()
		{
			Mage.EventManager.Emit("io.response", null);

			lock ((object)this)
			{
				sendingBatch = null;

				// Check if next queued batch should be sent as well
				if (currentBatch.BatchItems.Count > 0)
				{
					SendBatch();
				}
			}
		}

		//
		private void TransportError(string errorType, Exception error)
		{
			Logger.Data(error).Error("Error when sending command batch request '" + errorType + "'");
			Mage.EventManager.Emit("io.error." + errorType, null);
		}

		// Try and send a command right away if there is nothing being sent.
		public void SendCommand(string commandName, JObject parameters, Action<Exception, JToken> cb)
		{
			lock ((object)this)
			{
				// Add command to queue
				currentBatch.Queue(commandName, parameters, cb);

				// Check if we are busy and should only queue
				if (sendingBatch != null)
				{
					return;
				}

				// Otherwise send the batch
				SendBatch();
			}
		}

		// Queue command to current batch and wait for it to get processed by another command
		public void PiggyBackCommand(string commandName, JObject parameters, Action<Exception, JToken> cb)
		{
			lock ((object)this)
			{
				currentBatch.Queue(commandName, parameters, cb);
			}
		}
	}
}
