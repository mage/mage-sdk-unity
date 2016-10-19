using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.MageClient.Command {
	public class CommandBatch {
		public int queryId;
		public List<Dictionary<string, string>> batchHeaders = new List<Dictionary<string, string>>();
		public List<CommandBatchItem> batchItems = new List<CommandBatchItem>();

		public object serialisedCache;

		public CommandBatch(int queryId) {
			this.queryId = queryId;
		}

		public void Queue(string commandName, JObject parameters, Action<Exception, JToken> cb) {
			batchItems.Add(new CommandBatchItem(commandName, parameters, cb));
		}
	}
}
