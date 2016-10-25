using System;
using System.Collections.Generic;
using System.Threading;

using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.Network.Http;

namespace Wizcorp.MageSDK.MageClient.Message.Client
{
	public class LongPolling : TransportClient
	{
		private static Mage Mage
		{
			get { return Mage.Instance; }
		}

		private static Logger Logger
		{
			get { return Mage.Logger("LongPolling"); }
		}

		// Required functions for poll requests
		private Func<string> getEndpoint;
		private Func<Dictionary<string, string>> getHeaders;
		private Action<string> processMessages;

		//
		private HttpRequest currentRequest;

		// Required interval timer for polling delay
		private int errorInterval;
		private Timer intervalTimer;


		// Constructor
		public LongPolling(Func<string> getEndpointFn, Func<Dictionary<string, string>> getHeadersFn, Action<string> processMessagesFn, int errorInterval = 5000)
		{
			getEndpoint = getEndpointFn;
			getHeaders = getHeadersFn;
			processMessages = processMessagesFn;
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
			_running = false;
			Logger.Debug("Stopping...");

			if (intervalTimer != null)
			{
				intervalTimer.Dispose();
				intervalTimer = null;
			}
			else
			{
				Logger.Debug("Timer Stopped");
			}

			if (currentRequest != null)
			{
				currentRequest.Abort();
				currentRequest = null;
			}
			else
			{
				Logger.Debug("Connections Stopped");
			}
		}


		// Queues the next poll request
		private void QueueNextRequest(int waitFor)
		{
			// Wait _requestInterval milliseconds till next poll
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
			currentRequest = HttpRequest.Get(endpoint, getHeaders(), Mage.Cookies, (requestError, responseString) => {
				currentRequest = null;

				// Ignore errors if we have been stopped
				if (requestError != null && !_running)
				{
					Logger.Debug("Stopped");
					return;
				}

				if (requestError != null)
				{
					var requestHttpError = requestError as HttpRequestException;
					if (requestHttpError != null)
					{
						// Only log web exceptions if they aren't an empty response or gateway timeout
						if (requestHttpError.Status != 0 && requestHttpError.Status != 504)
						{
							Logger.Error("(" + requestHttpError.Status.ToString() + ") " + requestHttpError.Message);
						}
					}
					else
					{
						Logger.Error(requestError.ToString());
					}

					QueueNextRequest(errorInterval);
					return;
				}

				// Call the message processer hook and re-call request loop function
				try
				{
					Logger.Verbose("Recieved response: " + responseString);
					if (responseString != null)
					{
						processMessages(responseString);
					}

					RequestLoop();
				}
				catch (Exception error)
				{
					Logger.Error(error.ToString());
					QueueNextRequest(errorInterval);
				}
			});
		}
	}
}
