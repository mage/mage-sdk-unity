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
			get { return Mage.Logger("CommandHttpClient"); }
		}

		//
		private string endpoint;
		private Dictionary<string, string> headers;

		//
		public override void SetEndpoint(string baseUrl, string appName, Dictionary<string, string> headers = null)
		{
			endpoint = baseUrl + "/" + appName;
			headers = new Dictionary<string, string>(headers);
		}

		//
		public override void SerialiseBatch(CommandBatch commandBatch)
		{
			List<string> commands = new List<string>();
			List<string> data = new List<string>();

			// Attach batch headers to post data
			JArray batchHeaders = new JArray();
			for (int batchHeaderI = 0; batchHeaderI < commandBatch.BatchHeaders.Count; batchHeaderI += 1)
			{
				Dictionary<string, string> batchHeader = commandBatch.BatchHeaders[batchHeaderI];
				batchHeaders.Add(JObject.FromObject(batchHeader));
			}
			data.Add(batchHeaders.ToString(Newtonsoft.Json.Formatting.None));

			// Attach command names to url and parameters to post data
			for (int batchItemI = 0; batchItemI < commandBatch.BatchItems.Count; batchItemI += 1)
			{
				CommandBatchItem commandItem = commandBatch.BatchItems[batchItemI];
				commands.Add(commandItem.CommandName);
				data.Add(commandItem.Parameters.ToString(Newtonsoft.Json.Formatting.None));
				Logger.Data(commandItem.Parameters).Verbose("sending command: " + commandItem.CommandName);
			}

			string batchUrl = endpoint + "/" + String.Join(",", commands.ToArray()) + "?queryId=" + commandBatch.QueryId.ToString();
			string postData = string.Join("\n", data.ToArray());

			// Cached the serialisation
			commandBatch.SerialisedCache = (object)new CommandHttpClientCache(
				batchUrl,
				postData,
				(headers != null) ? new Dictionary<string, string>(headers) : new Dictionary<string, string>()
			);
		}

		//
		public override void SendBatch(CommandBatch commandBatch)
		{
			// Extract serialisation from cache
			CommandHttpClientCache serialisedCache = (CommandHttpClientCache)commandBatch.SerialisedCache;
			string batchUrl = serialisedCache.BatchUrl;
			string postData = serialisedCache.PostData;
			Dictionary<string, string> headers = serialisedCache.Headers;

			// Send HTTP request
			SendRequest(batchUrl, postData, headers, responseArray => {
				// Process each command response
				try
				{
					for (var batchId = 0; batchId < responseArray.Count; batchId += 1)
					{
						var commandResponse = responseArray[batchId] as JArray;
						if (commandResponse == null)
						{
							throw new Exception("Response item " + batchId + " is not an Array:" + responseArray);
						}

						CommandBatchItem commandItem = commandBatch.BatchItems[batchId];
						string commandName = commandItem.CommandName;
						Action<Exception, JToken> commandCb = commandItem.Cb;

						// Check if there are any events attached to this request
						if (commandResponse.Count >= 3)
						{
							Logger.Verbose("[" + commandName + "] processing events");
							Mage.EventManager.EmitEventList((JArray)commandResponse[2]);
						}

						// Check if the response was an error
						if (commandResponse[0].Type != JTokenType.Null)
						{
							Logger.Verbose("[" + commandName + "] server error");
							commandCb(new Exception(commandResponse[0].ToString()), null);
							continue;
						}

						// Pull off call result object, if it doesn't exist
						Logger.Verbose("[" + commandName + "] call response");

						try
						{
							commandCb(null, commandResponse[1]);
						}
						catch (Exception error)
						{
							Logger.Data(error).Error("Error during command callback");
						}
					}
				}
				catch (Exception error)
				{
					Logger.Data(error).Error("Error when processing command batch responses");
				}
			});
		}

		private void SendRequest(string batchUrl, string postData, Dictionary<string, string> headers, Action<JArray> cb)
		{
			HttpRequest.Post(batchUrl, "", postData, headers, Mage.Cookies, (requestError, responseString) => {
				Logger.Verbose("Recieved response: " + responseString);

				// Check if there was a transport error
				if (requestError != null)
				{
					var error = "network";

					// On error
					var httpError = requestError as HttpRequestException;
					if (httpError != null && httpError.Status == 503)
					{
						error = "maintenance";
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
