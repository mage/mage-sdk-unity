using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;

using Newtonsoft.Json.Linq;

public class JSONRPC {
	private string _endpoint;

	// Basic authentication
	private string _username = null;
	private string _password = null;

	public void setEndpoint(string endpoint, string username = null, string password = null) {
		_endpoint = endpoint;
		_username = username;
		_password = password;
	}

	public void call(string methodName, JObject parameters, Dictionary<string, string> headers, Action<Exception, JObject> cb) {
		call(JValue.CreateNull(), methodName, parameters, headers, cb);
	}

	public void call(string id, string methodName, JObject parameters, Dictionary<string, string> headers, Action<Exception, JObject> cb) {
		call(new JValue(id), methodName, parameters, headers, cb);
	}

	public void call(int id, string methodName, JObject parameters, Dictionary<string, string> headers, Action<Exception, JObject> cb) {
		call(new JValue(id), methodName, parameters, headers, cb);
	}

	public void call(JValue id, string methodName, JObject parameters, Dictionary<string, string> headers, Action<Exception, JObject> cb) {
		// Make sure the endpoint is set
		if (string.IsNullOrEmpty(_endpoint)) {
			cb(new Exception("Endpoint has not been set"), null);
			return;
		}

		// Setup JSON request object
		JObject requestObject = new JObject ();
		requestObject.Add("jsonrpc", new JValue("2.0"));

		requestObject.Add("id", id);
		requestObject.Add("method", new JValue(methodName));
		requestObject.Add("params", parameters);

		// Serialize JSON request object into string
		string postData;
		try {
			postData = requestObject.ToString ();
		} catch (Exception serializeError) {
			cb(serializeError, null);
			return;
		}

		// Make a copy of the provided headers and add additional required headers
		Dictionary<string, string> _headers = new Dictionary<string, string>(headers);
		if (_username != null && _password != null) {
			string authInfo = _username + ":" + _password;
			string encodedAuth = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			_headers.Add("Authorization", "Basic " + encodedAuth);
		}

		// Send HTTP post to JSON rpc endpoint
		HTTPRequest.Post(_endpoint, "application/json", postData, _headers, (Exception requestError, string responseString) => {
			if (requestError != null) {
				cb(requestError, null);
				return;
			}

			// Deserialize the JSON response
			JObject responseObject;
			try {
				responseObject = JObject.Parse(responseString);
			} catch (Exception error) {
				cb(error, null);
				return;
			}

			cb(null, responseObject);
		});
	}
}
