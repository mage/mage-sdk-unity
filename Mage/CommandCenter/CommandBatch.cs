using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.MageClient.Command
{
	public class CommandBatch
	{
		public int QueryId;
		public List<Dictionary<string, string>> BatchHeaders = new List<Dictionary<string, string>>();
		public List<CommandBatchItem> BatchItems = new List<CommandBatchItem>();

		public Object SerialisedCache;

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
