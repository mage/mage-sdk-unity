using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;

public class LongPolling : TransportClient {
	private Mage mage { get { return Mage.Instance; } }
	private Logger logger { get { return mage.logger("longpolling"); } }

	// Required functions for poll requests
	private Func<string> _getEndpoint;
	private Func<Dictionary<string, string>> _getHeaders;
	private Action<string> _processMessages;

	//
	HTTPRequest _currentRequest;

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

		logger.debug("Starting");
		_running = true;
		requestLoop ();
	}


	// Stops the poller
	public override void stop() {
		_running = false;
		logger.debug("Stopping...");

		if (_intervalTimer != null) {
			_intervalTimer.Dispose();
			_intervalTimer = null;
		} else {
			logger.debug("Timer Stopped");
		}

		if (_currentRequest != null) {
			_currentRequest.Abort();
			_currentRequest = null;
		} else {
			logger.debug("Connections Stopped");
		}
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
			logger.debug("Stopped");
			return;
		}

		// Send poll request and wait for a response
		string endpoint = _getEndpoint();
		logger.debug("Sending request: " + endpoint);
		_currentRequest = HTTPRequest.Get(endpoint, _getHeaders(), mage.cookies, (Exception requestError, string responseString) => {
			_currentRequest = null;

			// Ignore errors if we have been stopped
			if (requestError != null && !_running) {
				logger.debug("Stopped");
				return;
			}

			if (requestError != null) {
				if (requestError is HTTPRequestException) {
					// Only log web exceptions if they aren't an empty response or gateway timeout
					HTTPRequestException requestException = requestError as HTTPRequestException;
					if (requestException.Status != 0 && requestException.Status != 504) {
						logger.error("(" + requestException.Status.ToString() + ") " + requestError.Message);
					}
				} else {
					logger.error(requestError.ToString());
				}

				queueNextRequest(_errorInterval);
				return;
			}

			// Call the message processer hook and re-call request loop function
			try {
				logger.verbose("Recieved response: " + responseString);
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
