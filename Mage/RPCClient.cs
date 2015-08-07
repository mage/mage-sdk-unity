using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

public class RPCClient : JSONRPC {
	private Mage mage { get { return Mage.instance; } }
	private Logger logger { get { return mage.logger("rpcClient"); } }
	private string _sessionKey;

	public RPCClient () {
		mage.eventManager.on ("session.set", (object sender, JToken session) => {
			_sessionKey = session["key"].ToString();
		});

		mage.eventManager.on ("session.unset", (object sender, JToken reason) => {
			_sessionKey = null;
		});
	}

	public void setEndpoint(string baseUrl, string appName, string username = null, string password = null) {
		base.setEndpoint (baseUrl + "/" + appName + "/jsonrpc", username, password);
	}

	public void call(string methodName, JObject parameters, Action<Exception, JToken> cb) {
		// Attach anyre required mage headers
		Dictionary<string, string> headers = new Dictionary<string, string> ();
		if (_sessionKey != null) {
			headers.Add("X-MAGE-SESSION", _sessionKey);
		}

		//
		logger.verbose("[" + methodName + "] send to remote");

		// Make the RPC call
		this.call (methodName, parameters, headers, (Exception error, JObject responseObject) => {
			//
			if (error != null) {
				logger.verbose("[" + methodName + "] call error");
				cb(error, null);
				return;
			}

			// Check if there are any events attached to this request
			if (responseObject["result"]["myEvents"] != null) {
				logger.verbose("[" + methodName + "] processing events");
				mage.eventManager.emitEventList((JArray)responseObject["result"]["myEvents"]);
			}

			// Check if the response was an error
			if (responseObject["result"]["errorCode"] != null) {
				logger.verbose("[" + methodName + "] server error");
				cb(new Exception(responseObject["result"]["errorCode"].ToString()), null);
				return;
			}

			// Pull off call result object, if it doesn't exist
			logger.verbose("[" + methodName + "] call response");
			cb(null, responseObject["result"]["response"]);
		});
	}
}