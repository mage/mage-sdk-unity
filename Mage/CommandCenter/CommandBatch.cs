using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;


public class CommandBatch {
	public int queryId;
	public List<Dictionary<String, String>> batchHeaders = new List<Dictionary<String, String>>();
	public List<CommandBatchItem> batchItems = new List<CommandBatchItem>();

	public object serialisedCache;

	public CommandBatch(int queryId) {
		this.queryId = queryId;
	}

	public void Queue(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		batchItems.Add(new CommandBatchItem(commandName, parameters, cb));
	}
}
