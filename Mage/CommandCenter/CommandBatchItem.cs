using System;

using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.MageClient.Command
{
	public class CommandBatchItem
	{
		public string CommandName;
		public JObject Parameters;
		public Action<Exception, JToken> Cb;

		public CommandBatchItem(string commandName, JObject parameters, Action<Exception, JToken> cb)
		{
			CommandName = commandName;
			Parameters = parameters;
			Cb = cb;
		}
	}
}
