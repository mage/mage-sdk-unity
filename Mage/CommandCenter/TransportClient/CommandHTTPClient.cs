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

namespace Wizcorp.MageSDK.Command.Client {
	public class CommandHttpClientCache {
		public string batchUrl;
		public string postData;
		public Dictionary<string, string> headers;

		public CommandHttpClientCache(string batchUrl, string postData, Dictionary<string, string> headers) {
			this.batchUrl = batchUrl;
			this.postData = postData;
			this.headers = headers;
		}
	}

	public class CommandHTTPClient : CommandTransportClient {
		private Mage mage { get { return Mage.Instance; } }
		private Logger logger { get { return mage.logger("CommandHTTPClient"); } }

		//
		private string endpoint;
		private Dictionary<string, string> headers;

		//
		public override void SetEndpoint(string baseUrl, string appName, Dictionary<string, string> headers = null) {
			this.endpoint = baseUrl + "/" + appName;
			this.headers = new Dictionary<string, string>(headers);
		}

		//
		public override void SerialiseBatch(CommandBatch commandBatch) {
			List<string> commands = new List<string>();
			List<string> data = new List<string>();

			// Attach batch headers to post data
			JArray batchHeaders = new JArray();
			for (int batchHeaderI = 0; batchHeaderI < commandBatch.batchHeaders.Count; batchHeaderI += 1) {
				Dictionary<string, string> batchHeader = commandBatch.batchHeaders[batchHeaderI];
				batchHeaders.Add(JObject.FromObject(batchHeader));
			}
			data.Add(batchHeaders.ToString(Newtonsoft.Json.Formatting.None));

			// Attach command names to url and parameters to post data
			for (int batchItemI = 0; batchItemI < commandBatch.batchItems.Count; batchItemI += 1) {
				CommandBatchItem commandItem = commandBatch.batchItems[batchItemI];
				commands.Add(commandItem.commandName);
				data.Add(commandItem.parameters.ToString(Newtonsoft.Json.Formatting.None));
				logger.data(commandItem.parameters).verbose("sending command: " + commandItem.commandName);
			}

			string batchUrl = endpoint + "/" + String.Join(",", commands.ToArray()) + "?queryId=" + commandBatch.queryId.ToString();
			string postData = string.Join("\n", data.ToArray());

			// Cached the serialisation
			commandBatch.serialisedCache = (object)new CommandHttpClientCache(
				batchUrl,
				postData,
				new Dictionary<string, string>(this.headers)
			);
		}

		//
		public override void SendBatch(CommandBatch commandBatch) {
			// Extract serialisation from cache
			CommandHttpClientCache serialisedCache = (CommandHttpClientCache)commandBatch.serialisedCache;
			string batchUrl = serialisedCache.batchUrl;
			string postData = serialisedCache.postData;
			Dictionary<string, string> headers = serialisedCache.headers;

			// Send HTTP request
			SendRequest(batchUrl, postData, headers, (JArray responseArray) => {
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

		private void SendRequest(string batchUrl, string postData, Dictionary<string, string> headers, Action<JArray> cb) {
			HTTPRequest.Post(batchUrl, "", postData, headers, mage.cookies, (Exception requestError, string responseString) => {
				logger.verbose("Recieved response: " + responseString);

				// Check if there was a transport error
				if (requestError != null) {
					string error = "network";

					// On error
					var httpError = requestError as HTTPRequestException;
					if (httpError != null && httpError.Status == 503) {
						error = "maintenance";
					}

					OnTransportError.Invoke(error, requestError);
					return;
				}

				// Parse reponse array
				JArray responseArray;
				try {
					responseArray = JArray.Parse(responseString);
				} catch (Exception parseError) {
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
