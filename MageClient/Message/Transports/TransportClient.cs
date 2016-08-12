using System;
using System.Collections.Generic;
using System.Threading;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Network.Http;

namespace Wizcorp.MageSDK.MageClient.Message.Transports
{
	public abstract class TransportClient
	{
		public MessageStreamConfig Config { get; set; }

		//
		protected List<string> ConfirmIds;

		// Current message stack
		private int currentMessageId;
		protected HttpRequest CurrentRequest;
		protected int ErrorInterval;
		protected Timer IntervalTimer;

		// Endpoint and credentials
		private int largestMessageId;
		private Dictionary<int, JToken> messageQueue;

		// Required interval timer for polling delay
		protected int RequestInterval;

		protected bool Running;


		protected TransportClient(int errorInterval = 5000, int requestInterval = 5000)
		{
			currentMessageId = -1;
			largestMessageId = -1;
			RequestInterval = requestInterval;
			ErrorInterval = errorInterval;
			messageQueue = new Dictionary<int, JToken>();
			ConfirmIds = new List<string>();
		}

		public override string ToString()
		{
			return "[MessageStream.Transport]";
		}

		#region Public API

		public void Reset()
		{
			currentMessageId = -1;
			largestMessageId = -1;
			messageQueue = new Dictionary<int, JToken>();
			ConfirmIds = new List<string>();

			UnityEngine.Debug.Log(this + "Initialized message queue");
		}

		public void Start()
		{
			if (Running)
			{
				return;
			}

			UnityEngine.Debug.Log(this + "Starting");
			Running = true;
			Request();
		}

		public void Stop()
		{
			Running = false;
			UnityEngine.Debug.Log(this + "Stopping...");

			if (IntervalTimer != null)
			{
				IntervalTimer.Dispose();
				IntervalTimer = null;
			}
			else
			{
				UnityEngine.Debug.Log(this + "Timer Stopped");
			}

			if (CurrentRequest != null)
			{
				CurrentRequest.Abort();
				CurrentRequest = null;
			}
			else
			{
				UnityEngine.Debug.Log(this + "Connections Stopped");
			}
		}

		#endregion

		#region Network

		protected void CleanConfirmIds(List<string> messageIds)
		{
			//UnityEngine.Debug.Log(this + "Request Process, ConfirmIds to clean: " + string.Join(",", messageIds.ToArray()));
			ConfirmIds.RemoveAll(messageIds.Contains);
		}

		protected void RequestException(Exception requestError)
		{
			var exception = requestError as HttpRequestException;
			if (exception != null)
			{
				// Only log web exceptions if they aren't an empty response or gateway timeout
				HttpRequestException requestException = exception;
				if (requestException.Status != 0 && requestException.Status != 504)
				{
					UnityEngine.Debug.Log("(" + requestException.Status.ToString() + ") " + exception.Message);
				}
			}
			else
			{
				UnityEngine.Debug.Log(requestError.ToString());
			}

			NextRequest(ErrorInterval);
		}

		// Queues the next poll request
		protected void NextRequest(int waitFor)
		{
			IntervalTimer = new Timer(
				state => {
					Request();
				},
				null,
				waitFor,
				Timeout.Infinite);
		}

		// Method used to reveived the message from the server
		protected abstract void Request();

		protected abstract void Received(Exception requestError, string responseString);

		#endregion

		#region Process Messages Received

		// Deserializes and processes given messagesString
		protected void ProcessMessagesString(string messagesString)
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
					UnityEngine.Debug.LogWarning(this + "ProcessMessages " + currentMessageId + " already exist");
					break;
				}

				// Process the message
				Mage.Instance.EventManager.EmitEventList((JArray)messageQueue[currentMessageId]);
				ConfirmIds.Add(currentMessageId.ToString());
				messageQueue.Remove(currentMessageId);

				currentMessageId += 1;
			}

			// Finally emit any events that don't have an ID and thus don't need confirmation and lack order
			if (messageQueue.ContainsKey(0))
			{
				Mage.Instance.EventManager.EmitEventList((JArray)messageQueue[0]);
				messageQueue.Remove(0);
			}
		}

		#endregion
	}
}