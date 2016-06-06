using System;
using System.Collections.Generic;

using Wizcorp.MageSDK.Network.Http;

namespace Wizcorp.MageSDK.MageClient.Message.Transports
{
	public class LongPolling : TransportClient
	{
		private List<string> requestedIds;
		public LongPolling(int errorInterval = 5000) : base(errorInterval) {}

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
			CurrentRequest = null;

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

			// Call the message processer hook and re-call request loop function
			try
			{
				UnityEngine.Debug.Log(this + "Received response: " + responseString);
				if (responseString != null)
				{
					ProcessMessagesString(responseString);
				}

				Request();
			}
			catch (Exception error)
			{
				UnityEngine.Debug.LogError(error.ToString());
				NextRequest(ErrorInterval);
			}
		}

		public override string ToString()
		{
			return "[MessageStream.LongPolling]";
		}
	}
}