using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;

public class MessageStream {
	private Mage mage { get { return Mage.instance; } }
	private Logger logger { get { return mage.logger("messagestream"); } }

	// Endpoint and credentials
	private string _endpoint;
	private string _username;
	private string _password;
	private string _sessionKey;

	// Current transport client
	private TransportClient transportClient;

	// Current message stack
	private int currentMessageId;
	private int largestMessageId;
	private Dictionary<int, JToken> messageQueue;
	private List<string> confirmIds;


	//
	private void initializeMessageList() {
		currentMessageId = -1;
		largestMessageId = -1;
		messageQueue = new Dictionary<int, JToken>();
		confirmIds = new List<string>();

		logger.debug ("Initialized message queue");
	}


	// Constructor
	public MessageStream (TransportType transport = TransportType.LONGPOLLING) {
		//
		initializeMessageList ();

		// Start transport client when session is acquired
		mage.eventManager.on ("session.set", (object sender, JToken session) => {
			_sessionKey = UnityEngine.WWW.EscapeURL(session["key"].ToString());
			transportClient.start();
		});
		
		// Stop the message client when session is lost
		mage.eventManager.on ("session.unset", (object sender, JToken reason) => {
			transportClient.stop();
			initializeMessageList();
			_sessionKey = null;
		});

		// Also stop the message client when the editor is stopped
		UnityEditorPlayMode.onEditorModeChanged += (EditorPlayModeState newState) => {
			if (newState == EditorPlayModeState.Stopped) {
				transportClient.stop();
				initializeMessageList();
				_sessionKey = null;
			}
		};

		// Set the selected transport client (or the default)
		this.setTransport (transport);
	}


	// Updates URI and credentials 
	public void setEndpoint(string baseURL, string username = null, string password = null) {
		_endpoint = baseURL + "/msgstream";
		_username = username;
		_password = password;
	}


	// Sets up given transport client type
	public void setTransport(TransportType transport) {
		// Stop existing transport client if any, when nulled out it will be collected
		// by garbage collecter after existing connections have been terminated.
		if (transportClient != null) {
			transportClient.stop ();
			transportClient = null;
		}

		// Create new transport client instance
		if (transport == TransportType.SHORTPOLLING) {
			Func<string> getShortPollingEndpoint = () => {
				return getHttpPollingEndpoint ("shortpolling");
			};

			transportClient = new ShortPolling(getShortPollingEndpoint, getHttpHeaders, processMessagesString, 5000) as TransportClient;
		} else if (transport == TransportType.LONGPOLLING) {
			Func<string> getLongPollingEndpoint = () => {
				return getHttpPollingEndpoint("longpolling");
			};

			transportClient = new LongPolling(getLongPollingEndpoint, getHttpHeaders, processMessagesString) as TransportClient;
		} else {
			throw new Exception("Invalid transport type: " + transport);
		}
	}


	// Returns the endpoint URL for polling transport clients i.e. longpolling and shortpolling
	private string getHttpPollingEndpoint(string transport) {
		string endpoint = _endpoint + "?transport=" + transport + "&sessionKey=" + _sessionKey;
		if (confirmIds.Count > 0) {
			endpoint += "&confirmIds=" + string.Join(",", confirmIds.ToArray());
			confirmIds.Clear();
		}
		
		return endpoint;
	}
	
	
	// Returns the required HTTP headers
	private Dictionary<string, string> getHttpHeaders() {
		if (_username == null && _password == null) {
			return null;
		}
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		string authInfo = _username + ":" + _password;
		string encodedAuth = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
		headers.Add("Authorization", "Basic " + encodedAuth);
		return headers;
	}


	// Deserilizes and processes given messagesString
	private void processMessagesString(string messagesString) {
		if (messagesString == "" || messagesString == null) {
			return;
		}

		JObject messages = JObject.Parse(messagesString);
		addMessages(messages);
		processMessages();
	}


	// Add list of messages to message queue
	private void addMessages(JObject messages) {
		if (messages == null) {
			return;
		}

		int lowestMessageId = -1;

		foreach (var message in messages) {
			// Check if the messageId is lower than our current messageId
			int messageId = int.Parse(message.Key);
			if (messageId == 0) {
				messageQueue.Add(messageId, message.Value);
				continue;
			}
			if (messageId < currentMessageId) {
				continue;
			}
			
			// Keep track of the largest messageId in the list
			if (messageId > largestMessageId) {
				largestMessageId = messageId;
			}
			
			// Keep track of the lowest messageId in the list
			if (lowestMessageId == -1 || messageId < lowestMessageId) {
				lowestMessageId = messageId;
			}
			
			// Check if the message exists in the queue, if not add it
			if (!messageQueue.ContainsKey(messageId)) {
				messageQueue.Add(messageId, message.Value);
			}
		}
		
		// If the current messageId has never been set, set it to the current lowest
		if (currentMessageId == -1) {
			currentMessageId = lowestMessageId;
		}
	}


	// Process the message queue till we reach the end or a gap
	private void processMessages() {
		// Process all ordered messages in the order they appear
		while (currentMessageId <= largestMessageId) {
			// Check if the next messageId exists
			if (!messageQueue.ContainsKey(currentMessageId)) {
				break;
			}

			// Process the message
			mage.eventManager.emitEventList((JArray)messageQueue[currentMessageId]);
			confirmIds.Add(currentMessageId.ToString());
			messageQueue.Remove(currentMessageId);

			currentMessageId += 1;
		}

		// Finally emit any events that don't have an ID and thus don't need confirmation and lack order
		if (messageQueue.ContainsKey(0)) {
			mage.eventManager.emitEventList((JArray)messageQueue[0]);
			messageQueue.Remove(0);
		}
	}
}
