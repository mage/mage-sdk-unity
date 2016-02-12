using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;


public class CommandBatch {
	public int queryId;
	public List<CommandBatchItem> batchItems = new List<CommandBatchItem>();

	public CommandBatch(int queryId) {
		this.queryId = queryId;
	}

	public void Queue(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		batchItems.Add(new CommandBatchItem(commandName, parameters, cb));
	}
}