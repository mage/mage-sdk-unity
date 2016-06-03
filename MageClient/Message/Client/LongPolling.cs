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
			get { return Mage.Logger("longpolling"); }
		}

		// Whether or not the poller is working
		private bool running;

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
			if (running)
			{
				return;
			}

			Logger.Debug("Starting");
			running = true;
			RequestLoop();
		}


		// Stops the poller
		public override void Stop()
		{
			running = false;
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
				Timeout.Infinite);
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
			if (running == false)
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
				if (requestError != null && !running)
				{
					Logger.Debug("Stopped");
					return;
				}

				if (requestError != null)
				{
					var exception = requestError as HttpRequestException;
					if (exception != null)
					{
						// Only log web exceptions if they aren't an empty response or gateway timeout
						HttpRequestException requestException = exception;
						if (requestException.Status != 0 && requestException.Status != 504)
						{
							Logger.Error("(" + requestException.Status.ToString() + ") " + exception.Message);
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