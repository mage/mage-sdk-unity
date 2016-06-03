using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.MageClient.Command
{
	public class CommandBatch
	{
		public List<CommandBatchItem> BatchItems = new List<CommandBatchItem>();
		public int QueryId;

		public CommandBatch(int queryId)
		{
			QueryId = queryId;
		}

		public void Queue(string commandName, JObject parameters, Action<Exception, JToken> cb)
		{
			BatchItems.Add(new CommandBatchItem(commandName, parameters, cb));
		}
	}
}