using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class LongPolling : TransportClient {
	private Mage mage { get { return Mage.Instance; } }
	private Logger logger { get { return mage.logger("longpolling"); } }

	// Whether or not the poller is working
	private bool _running = false;

	// Required functions for poll requests
	private Func<string> _getEndpoint;
	private Func<Dictionary<string, string>> _getHeaders;
	private Action<string> _processMessages;

	// Required interval timer for polling delay
	private int _errorInterval;
	private Timer _intervalTimer;


	// Constructor
	public LongPolling(Func<string> getEndpointFn, Func<Dictionary<string, string>> getHeadersFn, Action<string> processMessagesFn, int errorInterval = 5000) {
		_getEndpoint = getEndpointFn;
		_getHeaders = getHeadersFn;
		_processMessages = processMessagesFn;
		_errorInterval = errorInterval;
	}


	// Starts the poller
	public override void start() {
		if (_running == true) {
			return;
		}

		logger.debug ("Starting");
		_running = true;
		requestLoop ();
	}


	// Stops the poller
	public override void stop() {
		if (_intervalTimer != null) {
			_intervalTimer.Dispose();
			_intervalTimer = null;
			logger.debug ("Stopped");
		} else {
			logger.debug ("Stopping...");
		}
		_running = false;
	}


	// Queues the next poll request
	private void queueNextRequest(int waitFor) {
		// Wait _requestInterval milliseconds till next poll
		_intervalTimer = new Timer((object state) => {
			requestLoop();
		}, null, waitFor, Timeout.Infinite);
	}


	// Poller request function
	private void requestLoop() {
		// Clear the timer
		if (_intervalTimer != null) {
			_intervalTimer.Dispose();
			_intervalTimer = null;
		}

		// Check if the poller should be running
		if (_running == false) {
			logger.debug ("Stopped");
			return;
		}

		// Send poll request and wait for a response
		string endpoint = _getEndpoint();
		logger.debug ("Sending request: " + endpoint);
		HTTPRequest.Get(endpoint, _getHeaders(), (Exception requestError, string responseString) => {
			if (requestError != null) {
				logger.error (requestError.ToString());
				queueNextRequest(_errorInterval);
				return;
			}

			// Call the message processer hook and re-call request loop function
			try {
				logger.debug ("Recieved response: " + responseString);
				if (responseString != null) {
					_processMessages(responseString);
				}

				requestLoop();
			} catch (Exception error) {
				logger.error (error.ToString());
				queueNextRequest(_errorInterval);
			}
		});
	}
}
