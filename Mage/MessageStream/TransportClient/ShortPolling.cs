using System;
using System.Collections.Generic;
using System.Threading;

using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.Network.Http;

namespace Wizcorp.MageSDK.MageClient.Message.Client
{
	public class ShortPolling : TransportClient
	{
		private static Mage Mage
		{
			get { return Mage.Instance; }
		}

		private static Logger Logger
		{
			get { return Mage.Logger("ShortPolling"); }
		}

		// Required functions for poll requests
		private Func<string> getEndpoint;
		private Func<Dictionary<string, string>> getHeaders;
		private Action<string> processMessages;

		// Required interval timer for polling delay
		private int requestInterval;
		private int errorInterval;
		private Timer intervalTimer;


		// Constructor
		public ShortPolling(Func<string> getEndpointFn, Func<Dictionary<string, string>> getHeadersFn, Action<string> processMessagesFn, int requestInterval = 5000, int errorInterval = 5000)
		{
			getEndpoint = getEndpointFn;
			getHeaders = getHeadersFn;
			processMessages = processMessagesFn;
			this.requestInterval = requestInterval;
			this.errorInterval = errorInterval;
		}


		// Starts the poller
		public override void Start()
		{
			if (_running)
			{
				return;
			}

			Logger.Debug("Starting");
			_running = true;
			RequestLoop();
		}


		// Stops the poller
		public override void Stop()
		{
			if (intervalTimer != null)
			{
				intervalTimer.Dispose();
				intervalTimer = null;
				Logger.Debug("Stopped");
			}
			else
			{
				Logger.Debug("Stopping...");
			}
			_running = false;
		}


		// Queues the next poll request
		private void QueueNextRequest(int waitFor)
		{
			// Wait n milliseconds till next poll
			intervalTimer = new Timer(
				state => {
					RequestLoop();
				},
				null,
				waitFor,
				Timeout.Infinite
			);
		}


		// Poller request function
		private void RequestLoop()
		{
			// Clear the timer
			if (intervalTimer != null)
			{
				intervalTimer.Dispose();
				intervalTimer = null;
			}

			// Check if the poller should be running
			if (_running == false)
			{
				Logger.Debug("Stopped");
				return;
			}

			// Send poll request and wait for a response
			string endpoint = getEndpoint();
			Logger.Debug("Sending request: " + endpoint);
			HttpRequest.Get(endpoint, getHeaders(), Mage.Cookies, (requestError, responseString) => {
				if (requestError != null)
				{
					Logger.Error(requestError.ToString());
					QueueNextRequest(errorInterval);
					return;
				}

				// Call the message processer hook and queue the next request
				try
				{
					processMessages(responseString);
					QueueNextRequest(requestInterval);
				}
				catch (Exception error)
				{
					Logger.Data(responseString).Error(error.ToString());
					QueueNextRequest(errorInterval);
				}
			});
		}
	}
}
