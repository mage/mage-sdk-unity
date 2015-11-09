using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using Newtonsoft.Json.Linq;

public class CommandHTTPClient : CommandTransportClient {
	private Mage mage { get { return Mage.Instance; } }
	private Logger logger { get { return mage.logger("CommandHTTPClient"); } }
	
	//
	private string endpoint;
	private string username;
	private string password;
	
	//
	public override void SetEndpoint(string baseUrl, string appName, string username = null, string password = null) {
		this.endpoint = baseUrl + "/" + appName;
		this.username = username;
		this.password = password;
	}
	
	//
	public override void SendBatch(CommandBatch commandBatch) {
		List<string> commands = new List<string> ();
		List<string> data = new List<string>();

		// Attach batch headers to post data
		JArray batchHeader = new JArray();
		string sessionKey = mage.session.GetSessionKey();
		if (!string.IsNullOrEmpty(sessionKey)) {
			JObject sessionHeader = new JObject();
			sessionHeader.Add("name", new JValue("mage.session"));
			sessionHeader.Add("key", new JValue(sessionKey));
			batchHeader.Add(sessionHeader);
		}
		data.Add(batchHeader.ToString(Newtonsoft.Json.Formatting.None));

		// Attach command names to url and parameters to post data
		for (int batchId = 0; batchId < commandBatch.batchItems.Count; batchId += 1) {
			CommandBatchItem commandItem = commandBatch.batchItems[batchId];
			commands.Add(commandItem.commandName);
			data.Add(commandItem.parameters.ToString(Newtonsoft.Json.Formatting.None));
			logger.data(commandItem.parameters).verbose("sending command: " + commandItem.commandName);
		}

		// Serialise post data
		string batchUrl = endpoint + "/" + String.Join(",", commands.ToArray()) + "?queryId=" + commandBatch.queryId.ToString();
		string postData = string.Join("\n", data.ToArray());
		
		// Send HTTP request
		SendRequest(batchUrl, postData, (JArray responseArray) => {
			// Process each command response
			try {
				for (int batchId = 0; batchId < responseArray.Count; batchId += 1) {
					JArray commandResponse = responseArray[batchId] as JArray;
					CommandBatchItem commandItem = commandBatch.batchItems[batchId];
					string commandName = commandItem.commandName;
					Action<Exception, JToken> commandCb = commandItem.cb;
					
					// Check if there are any events attached to this request
					if (commandResponse.Count >= 3) {
						logger.verbose("[" + commandName + "] processing events");
						mage.eventManager.emitEventList((JArray)commandResponse[2]);
					}
					
					// Check if the response was an error
					if (commandResponse[0].Type != JTokenType.Null) {
						logger.verbose("[" + commandName + "] server error");
						commandCb(new Exception(commandResponse[0].ToString()), null);
						return;
					}
					
					// Pull off call result object, if it doesn't exist
					logger.verbose("[" + commandName + "] call response");
					commandCb(null, commandResponse[1]);
				}
			} catch (Exception error) {
				logger.data(error).error("Error when processing command batch responses");
			}
		});
	}

	private void SendRequest(string batchUrl, string postData, Action<JArray> cb) {
		Dictionary<string, string> headers = new Dictionary<string, string>();
		if (username != null && password != null) {
			string authInfo = username + ":" + password;
			string encodedAuth = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			headers.Add("Authorization", "Basic " + encodedAuth);
		}

		HTTPRequest.Post(batchUrl, "", postData, headers, mage.cookies, (Exception requestError, string responseString) => {
			// Check if there was a transport error
			if (requestError != null) {
				string error = "network";
				if (requestError is WebException) {
					WebException webException = requestError as WebException;
					HttpWebResponse webResponse = webException.Response as HttpWebResponse;
					if (
						webException.Status == WebExceptionStatus.ConnectFailure ||
						(webResponse != null && webResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
					) {
						error = "maintenance";
					}
				}

				JObject transportError = new JObject();
				transportError.Add("reason", new JValue(error));
				transportError.Add("info", new JValue(responseString));

				logger.data(requestError).error("Error when sending command batch request");
				mage.eventManager.emit("io.error." + error, transportError);
				return;
			}

			// Parse reponse array
			JArray responseArray;
			try {
				responseArray = JArray.Parse(responseString);
			} catch (Exception parseError) {
				logger.verbose("Error when parsing command batch response");
				mage.eventManager.emit("io.error.parse", null);
				return;
			}

			// Let CommandCenter know this batch was successful
			OnSendComplete.Invoke();

			// Return array for processing
			cb(responseArray);
		});
	}
}