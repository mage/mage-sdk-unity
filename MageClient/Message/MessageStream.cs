using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.MageClient.Message.Client;

#if UNITY_EDITOR
using Wizcorp.MageSDK.Editor;
#endif

namespace Wizcorp.MageSDK.MageClient.Message
{
	public class MessageStream
	{
		private Mage Mage
		{
			get { return Mage.Instance; }
		}

		private Logger Logger
		{
			get { return Mage.Logger("messagestream"); }
		}

		private List<string> confirmIds;

		// Current message stack
		private int currentMessageId;

		// Endpoint and credentials
		private string endpoint;
		private int largestMessageId;
		private Dictionary<int, JToken> messageQueue;
		private string password;
		private string sessionKey;

		// Current transport client
		private TransportClient transportClient;
		private string username;


		// Constructor
		public MessageStream(TransportType transport = TransportType.LONGPOLLING)
		{
			//
			InitializeMessageList();

			// Start transport client when session is acquired
			Mage.EventManager.On(
				"session.set",
				(sender, session) => {
					sessionKey = UnityEngine.WWW.EscapeURL(session["key"].ToString());
					transportClient.Start();
				});

			// Stop the message client when session is lost
			Mage.EventManager.On(
				"session.unset",
				(sender, reason) => {
					transportClient.Stop();
					InitializeMessageList();
					sessionKey = null;
				});

			// Also stop the message client when the editor is stopped
			#if UNITY_EDITOR
			UnityEditorPlayMode.OnEditorModeChanged += newState => {
				if (newState == EditorPlayModeState.Stopped)
				{
					transportClient.Stop();
					InitializeMessageList();
					sessionKey = null;
				}
			};
			#endif

			// Set the selected transport client (or the default)
			SetTransport(transport);
		}


		//
		private void InitializeMessageList()
		{
			currentMessageId = -1;
			largestMessageId = -1;
			messageQueue = new Dictionary<int, JToken>();
			confirmIds = new List<string>();

			Logger.Debug("Initialized message queue");
		}


		//
		public void Dispose()
		{
			// Stop the transport client if it exists
			if (transportClient != null)
			{
				transportClient.Stop();
			}

			InitializeMessageList();
			sessionKey = null;
		}


		// Updates URI and credentials 
		public void SetEndpoint(string url, string login = null, string pass = null)
		{
			endpoint = url + "/msgstream";
			username = login;
			password = pass;
		}


		// Sets up given transport client type
		public void SetTransport(TransportType transport)
		{
			// Stop existing transport client if any, when nulled out it will be collected
			// by garbage collecter after existing connections have been terminated.
			if (transportClient != null)
			{
				transportClient.Stop();
				transportClient = null;
			}

			// Create new transport client instance
			if (transport == TransportType.SHORTPOLLING)
			{
				Func<string> getShortPollingEndpoint = () => {
					return GetHttpPollingEndpoint("shortpolling");
				};

				transportClient = new ShortPolling(getShortPollingEndpoint, GetHttpHeaders, ProcessMessagesString);
			}
			else if (transport == TransportType.LONGPOLLING)
			{
				Func<string> getLongPollingEndpoint = () => {
					return GetHttpPollingEndpoint("longpolling");
				};

				transportClient = new LongPolling(getLongPollingEndpoint, GetHttpHeaders, ProcessMessagesString);
			}
			else
			{
				throw new Exception("Invalid transport type: " + transport);
			}
		}


		// Returns the endpoint URL for polling transport clients i.e. longpolling and shortpolling
		private string GetHttpPollingEndpoint(string transport)
		{
			string url = endpoint + "?transport=" + transport + "&sessionKey=" + sessionKey;
			if (confirmIds.Count > 0)
			{
				url += "&confirmIds=" + string.Join(",", confirmIds.ToArray());
				confirmIds.Clear();
			}

			return url;
		}


		// Returns the required HTTP headers
		private Dictionary<string, string> GetHttpHeaders()
		{
			if (username == null && password == null)
			{
				return null;
			}

			var headers = new Dictionary<string, string>();
			string authInfo = username + ":" + password;
			string encodedAuth = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			headers.Add("Authorization", "Basic " + encodedAuth);
			return headers;
		}


		// Deserilizes and processes given messagesString
		private void ProcessMessagesString(string messagesString)
		{
			if (string.IsNullOrEmpty(messagesString))
			{
				return;
			}

			JObject messages = JObject.Parse(messagesString);
			AddMessages(messages);
			ProcessMessages();
		}


		// Add list of messages to message queue
		private void AddMessages(JObject messages)
		{
			if (messages == null)
			{
				return;
			}

			int lowestMessageId = -1;

			foreach (KeyValuePair<string, JToken> message in messages)
			{
				// Check if the messageId is lower than our current messageId
				int messageId = int.Parse(message.Key);
				if (messageId == 0)
				{
					messageQueue.Add(messageId, message.Value);
					continue;
				}
				if (messageId < currentMessageId)
				{
					continue;
				}

				// Keep track of the largest messageId in the list
				if (messageId > largestMessageId)
				{
					largestMessageId = messageId;
				}

				// Keep track of the lowest messageId in the list
				if (lowestMessageId == -1 || messageId < lowestMessageId)
				{
					lowestMessageId = messageId;
				}

				// Check if the message exists in the queue, if not add it
				if (!messageQueue.ContainsKey(messageId))
				{
					messageQueue.Add(messageId, message.Value);
				}
			}

			// If the current messageId has never been set, set it to the current lowest
			if (currentMessageId == -1)
			{
				currentMessageId = lowestMessageId;
			}
		}


		// Process the message queue till we reach the end or a gap
		private void ProcessMessages()
		{
			// Process all ordered messages in the order they appear
			while (currentMessageId <= largestMessageId)
			{
				// Check if the next messageId exists
				if (!messageQueue.ContainsKey(currentMessageId))
				{
					break;
				}

				// Process the message
				Mage.EventManager.EmitEventList((JArray)messageQueue[currentMessageId]);
				confirmIds.Add(currentMessageId.ToString());
				messageQueue.Remove(currentMessageId);

				currentMessageId += 1;
			}

			// Finally emit any events that don't have an ID and thus don't need confirmation and lack order
			if (messageQueue.ContainsKey(0))
			{
				Mage.EventManager.EmitEventList((JArray)messageQueue[0]);
				messageQueue.Remove(0);
			}
		}
	}
}