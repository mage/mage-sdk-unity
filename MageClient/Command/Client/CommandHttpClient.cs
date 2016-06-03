using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.CommandCenter.Client;
using Wizcorp.MageSDK.Log;
using Wizcorp.MageSDK.MageClient;
using Wizcorp.MageSDK.MageClient.Command;
using Wizcorp.MageSDK.Network.Http;

namespace Wizcorp.MageSDK.Command.Client
{
	public class CommandHttpClient : CommandTransportClient
	{
		private Mage Mage
		{
			get { return Mage.Instance; }
		}

		private Logger Logger
		{
			get { return Mage.Logger("CommandHTTPClient"); }
		}

		//
		private string endpoint;
		private string password;
		private string username;

		//
		public override void SetEndpoint(string url, string app, string login = null, string pass = null)
		{
			endpoint = string.Format("{0}/{1}", url, app);
			username = login;
			password = pass;
		}

		//
		public override void SendBatch(CommandBatch commandBatch)
		{
			var commands = new List<string>();
			var data = new List<string>();

			// Attach batch headers to post data
			var batchHeader = new JArray();
			string sessionKey = Mage.Session.GetSessionKey();
			if (!string.IsNullOrEmpty(sessionKey))
			{
				var sessionHeader = new JObject();
				sessionHeader.Add("name", new JValue("mage.session"));
				sessionHeader.Add("key", new JValue(sessionKey));
				batchHeader.Add(sessionHeader);
			}
			data.Add(batchHeader.ToString(Newtonsoft.Json.Formatting.None));

			// Attach command names to url and parameters to post data
			for (var batchId = 0; batchId < commandBatch.BatchItems.Count; batchId += 1)
			{
				CommandBatchItem commandItem = commandBatch.BatchItems[batchId];
				commands.Add(commandItem.CommandName);
				data.Add(commandItem.Parameters.ToString(Newtonsoft.Json.Formatting.None));
				Logger.Data(commandItem.Parameters).Verbose("sending command: " + commandItem.CommandName);
			}

			// Serialise post data
			string batchUrl = endpoint + "/" + String.Join(",", commands.ToArray()) + "?queryId=" + commandBatch.QueryId.ToString();
			string postData = string.Join("\n", data.ToArray());

			// Send HTTP request
			SendRequest(
				batchUrl,
				postData,
				responseArray => {
					// Process each command response
					try
					{
						for (var batchId = 0; batchId < responseArray.Count; batchId += 1)
						{
							var commandResponse = responseArray[batchId] as JArray;
							CommandBatchItem commandItem = commandBatch.BatchItems[batchId];
							string commandName = commandItem.CommandName;
							Action<Exception, JToken> commandCb = commandItem.Cb;

							// Check if there are any events attached to this request
							if (commandResponse != null && commandResponse.Count >= 3)
							{
								Logger.Verbose("[" + commandName + "] processing events");
								Mage.EventManager.EmitEventList((JArray)commandResponse[2]);
							}

							// Check if the response was an error
							if (commandResponse != null && commandResponse[0].Type != JTokenType.Null)
							{
								Logger.Verbose("[" + commandName + "] server error");
								commandCb(new Exception(commandResponse[0].ToString()), null);
								return;
							}

							// Pull off call result object, if it doesn't exist
							Logger.Verbose("[" + commandName + "] call response");
							if (commandResponse != null)
							{
								commandCb(null, commandResponse[1]);
							}
						}
					}
					catch (Exception error)
					{
						Logger.Data(error).Error("Error when processing command batch responses");
					}
				});
		}

		private void SendRequest(string batchUrl, string postData, Action<JArray> cb)
		{
			var headers = new Dictionary<string, string>();
			if (username != null && password != null)
			{
				string authInfo = username + ":" + password;
				string encodedAuth = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
				headers.Add("Authorization", "Basic " + encodedAuth);
			}

			HttpRequest.Post(
				batchUrl,
				"",
				postData,
				headers,
				Mage.Cookies,
				(requestError, responseString) => {
					Logger.Verbose("Recieved response: " + responseString);

					// Check if there was a transport error
					if (requestError != null)
					{
						var error = "network";
						if (requestError is WebException)
						{
							var webException = requestError as WebException;
							var webResponse = webException.Response as HttpWebResponse;
							if (webResponse != null && webResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
							{
								error = "maintenance";
							}
						}

						OnTransportError.Invoke(error, requestError);
						return;
					}

					// Parse reponse array
					JArray responseArray;
					try
					{
						responseArray = JArray.Parse(responseString);
					}
					catch (Exception parseError)
					{
						OnTransportError.Invoke("parse", parseError);
						return;
					}

					// Let CommandCenter know this batch was successful
					OnSendComplete.Invoke();

					// Return array for processing
					cb(responseArray);
				});
		}
	}
}