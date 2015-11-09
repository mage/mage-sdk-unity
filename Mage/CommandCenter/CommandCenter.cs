using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;


public class CommandCenter {
	private Mage mage { get { return Mage.Instance; }}
	private Logger logger { get { return mage.logger("CommandCenter"); }}
	
	// Endpoint and credentials
	private string baseUrl;
	private string appName;
	private string username;
	private string password;

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
			transportClient.SetEndpoint(baseUrl, appName, username, password);
		} else if (transportType == CommandTransportType.JSONRPC) {
			transportClient = new CommandJSONRPCClient() as CommandTransportClient;
			transportClient.SetEndpoint(baseUrl, appName, username, password);
		} else {
			throw new Exception("Invalid transport type: " + transportType);
		}

		// Setup event handlers
		transportClient.OnSendComplete += BatchComplete;
	}

	//
	public void SetEndpoint(string baseUrl, string appName, string username = null, string password = null) {
		this.baseUrl = baseUrl;
		this.appName = appName;
		this.username = username;
		this.password = password;

		if (transportClient != null) {
			transportClient.SetEndpoint(baseUrl, appName, username, password);
		}
	}

	//
	private void SendBatch() {
		lock ((object)currentBatch) {
			// Swap batches around locking the queue
			sendingBatch = currentBatch;
			currentBatch = new CommandBatch(nextQueryId++);

			// Send the batch
			logger.debug("Sending batch: " + sendingBatch.queryId);
			transportClient.SendBatch(sendingBatch);
		}
	}

	//
	private void BatchComplete() {
		lock ((object)currentBatch) {
			sendingBatch = null;
			
			// Check if next queued batch should be sent as well
			if (currentBatch.batchItems.Count > 0) {
				SendBatch();
			}
		}
	}
	
	// Resend batch
	public void Resend() {
		logger.debug("Re-sending batch: " + sendingBatch.queryId);
		transportClient.SendBatch(sendingBatch);
	}

	// Try and send a command right away if there is nothing being sent.
	public void SendCommand(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		lock ((object)currentBatch) {
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

	// Either send this command immediately if nothing is being sent,
	// otherwise queue it and send it after the current send is complete.
	public void QueueCommand(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		lock ((object)currentBatch) {
			// If we are not sending anything, send immediately
			if (sendingBatch == null) {
				SendCommand(commandName, parameters, cb);
				return;
			}

			// Otherwise queue it to current
			currentBatch.Queue(commandName, parameters, cb);
		}
	}

	// Queue command to current batch and wait for it to get processed by another command
	public void PiggyBackCommand(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		lock ((object)currentBatch) {
			currentBatch.Queue(commandName, parameters, cb);
		}
	}
}