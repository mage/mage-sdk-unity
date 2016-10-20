using System;
using System.Collections.Generic;
using System.Net;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Network.Http;

namespace Wizcorp.MageSDK.Network.JsonRpc
{
	public class JsonRpc
	{
		// Endpoint and Basic Authentication
		private string endpoint;
		private Dictionary<string, string> headers;

		public void SetEndpoint(string endpoint, Dictionary<string, string> headers = null)
		{
			this.endpoint = endpoint;
			this.headers = new Dictionary<string, string>(headers);
		}

		public void Call(string methodName, JObject parameters, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, JObject> cb)
		{
			Call(JValue.CreateNull(), methodName, parameters, headers, cookies, cb);
		}

		public void Call(string id, string methodName, JObject parameters, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, JObject> cb)
		{
			Call(new JValue(id), methodName, parameters, headers, cookies, cb);
		}

		public void Call(int id, string methodName, JObject parameters, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, JObject> cb)
		{
			Call(new JValue(id), methodName, parameters, headers, cookies, cb);
		}

		public void Call(JValue id, string methodName, JObject parameters, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, JObject> cb)
		{
			// Setup JSON request object
			var requestObject = new JObject();
			requestObject.Add("jsonrpc", new JValue("2.0"));

			requestObject.Add("id", id);
			requestObject.Add("method", new JValue(methodName));
			requestObject.Add("params", parameters);

			// Serialize JSON request object into string
			string postData;
			try
			{
				postData = requestObject.ToString();
			}
			catch (Exception serializeError)
			{
				cb(serializeError, null);
				return;
			}

			// Send request
			SendRequest(postData, headers, cookies, (requestError, responseString) => {
				if (requestError != null)
				{
					cb(requestError, null);
					return;
				}

				// Deserialize the JSON response
				JObject responseObject;
				try
				{
					responseObject = JObject.Parse(responseString);
				}
				catch (Exception parseError)
				{
					cb(parseError, null);
					return;
				}

				cb(null, responseObject);
			});
		}

		public void CallBatch(JsonRpcBatch rpcBatch, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, JArray> cb)
		{
			// Serialize JSON request object into string
			string postData;
			try
			{
				postData = rpcBatch.Batch.ToString();
			}
			catch (Exception serializeError)
			{
				cb(serializeError, null);
				return;
			}

			// Send request
			SendRequest(postData, headers, cookies, (requestError, responseString) => {
				if (requestError != null)
				{
					cb(requestError, null);
					return;
				}

				// Deserialize the JSON response
				JArray responseArray;
				try
				{
					responseArray = JArray.Parse(responseString);
				}
				catch (Exception parseError)
				{
					cb(parseError, null);
					return;
				}

				cb(null, responseArray);
			});
		}

		private void SendRequest(string postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			// Make sure the endpoint is set
			if (string.IsNullOrEmpty(endpoint))
			{
				cb(new Exception("Endpoint has not been set"), null);
				return;
			}

			// Make a copy of the provided headers and add additional required headers
			Dictionary<string, string> finalHeaders = new Dictionary<string, string>(this.headers);
			foreach (var header in headers)
			{
				if (finalHeaders.ContainsKey(header.Key))
				{
					continue;
				}

				finalHeaders.Add(header.Key, header.Value);
			}

			// Send HTTP post to JSON rpc endpoint
			HttpRequest.Post(endpoint, "application/json", postData, finalHeaders, cookies, cb);
		}
	}
}
