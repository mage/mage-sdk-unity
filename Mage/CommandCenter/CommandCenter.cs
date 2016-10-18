using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;


public class CommandCenter {
	private Mage mage { get { return Mage.Instance; }}
	private Logger logger { get { return mage.logger("CommandCenter"); }}
	
	// Endpoint and credentials
	private string baseUrl;
	private string appName;
	Dictionary<string, string> headers = new Dictionary<string, string>();

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
	public CommandCenter(CommandTransportType transportType = CommandTransportType.HTTP) {
		currentBatch = new CommandBatch(nextQueryId++);
		SetTransport(transportType);
	}
	
	//
	public void SetTransport(CommandTransportType transportType) {
		// Cleanup existing transport client
		if (transportClient != null) {
			transportClient = null;
		}

		// Create new transport client instance
		if (transportType == CommandTransportType.HTTP) {
			transportClient = new CommandHTTPClient() as CommandTransportClient;
			transportClient.SetEndpoint(baseUrl, appName, headers);
		} else if (transportType == CommandTransportType.JSONRPC) {
			transportClient = new CommandJSONRPCClient() as CommandTransportClient;
			transportClient.SetEndpoint(baseUrl, appName, headers);
		} else {
			throw new Exception("Invalid transport type: " + transportType);
		}

		// Setup event handlers
		transportClient.OnSendComplete += BatchComplete;
		transportClient.OnTransportError += TransportError;
	}

	//
	public void SetEndpoint(string baseUrl, string appName, Dictionary<string, string> headers = null) {
		this.baseUrl = baseUrl;
		this.appName = appName;
		this.headers = new Dictionary<string, string>(headers);

		if (transportClient != null) {
			transportClient.SetEndpoint(this.baseUrl, this.appName, this.headers);
		}
	}

	//
	private void SendBatch() {
		mage.eventManager.emit("io.send", null);

		lock ((object)this) {
			// Swap batches around locking the queue
			sendingBatch = currentBatch;
			currentBatch = new CommandBatch(nextQueryId++);

			// Execute pre-serialisation message hooks
			if (preSerialiseHook != null) {
				preSerialiseHook.Invoke(sendingBatch);
			}

			// Serialise the batch
			logger.debug("Serialising batch: " + sendingBatch.queryId);
			transportClient.SerialiseBatch(sendingBatch);

			// Execute pre-network message hooks
			if (preNetworkHook != null) {
				preNetworkHook.Invoke(sendingBatch);
			}

			// Send the batch
			logger.debug("Sending batch: " + sendingBatch.queryId);
			transportClient.SendBatch(sendingBatch);
		}
	}
	
	// Resend batch
	public void Resend() {
		mage.eventManager.emit("io.resend", null);

		logger.debug("Re-sending batch: " + sendingBatch.queryId);
		transportClient.SendBatch(sendingBatch);
	}

	//
	private void BatchComplete() {
		mage.eventManager.emit("io.response", null);

		lock ((object)this) {
			sendingBatch = null;
			
			// Check if next queued batch should be sent as well
			if (currentBatch.batchItems.Count > 0) {
				SendBatch();
			}
		}
	}
	
	//
	private void TransportError(string errorType, Exception error) {
		logger.data(error).error("Error when sending command batch request '" + errorType + "'");
		mage.eventManager.emit("io.error." + errorType, null);
	}

	// Try and send a command right away if there is nothing being sent.
	public void SendCommand(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		lock ((object)this) {
			// Add command to queue
			currentBatch.Queue(commandName, parameters, cb);

			// Check if we are busy and should only queue
			if (sendingBatch != null) {
				return;
			}

			// Otherwise send the batch
			SendBatch();
		}
	}

	// Queue command to current batch and wait for it to get processed by another command
	public void PiggyBackCommand(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		lock ((object)this) {
			currentBatch.Queue(commandName, parameters, cb);
		}
	}
}
