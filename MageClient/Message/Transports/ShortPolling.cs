using System;
using System.Collections.Generic;

using Wizcorp.MageSDK.Network.Http;

namespace Wizcorp.MageSDK.MageClient.Message.Transports
{
	public class ShortPolling : TransportClient
	{
		private List<string> requestedIds;
		public ShortPolling(int errorInterval = 5000, int requestInterval = 5000) : base(errorInterval, requestInterval) {}

		protected override void Request()
		{
			// Clear the timer
			if (IntervalTimer != null)
			{
				IntervalTimer.Dispose();
				IntervalTimer = null;
			}

			// Check if the poller should be running
			if (Running == false)
			{
				UnityEngine.Debug.Log(this + "Stopped");
				return;
			}

			// Get Request parameters
			requestedIds = new List<string>(ConfirmIds);
			string endpoint = Config.GetEndpoint("longpolling", requestedIds);
			Dictionary<string, string> headers = Config.GetHeaders();

			// Send poll request and wait for a response
			UnityEngine.Debug.Log(this + "Sending request: " + endpoint);
			CurrentRequest = HttpRequest.Get(endpoint, headers, Mage.Instance.Cookies, Received);
		}

		protected override void Received(Exception requestError, string responseString)
		{
			// Ignore errors if we have been stopped
			if (requestError != null && !Running)
			{
				UnityEngine.Debug.Log(this + "Stopped");
				return;
			}

			// Manage Network Exception
			if (requestError != null)
			{
				RequestException(requestError);
				return;
			}

			// No error, we can clean messages Ids
			CleanConfirmIds(requestedIds);

			// Call the message processer hook and queue the next request
			try
			{
				ProcessMessagesString(responseString);
				NextRequest(RequestInterval);
			}
			catch (Exception error)
			{
				UnityEngine.Debug.LogError(error.ToString());
				NextRequest(ErrorInterval);
			}
		}

		public override string ToString()
		{
			return "[MessageStream.ShortPolling]";
		}
	}
}