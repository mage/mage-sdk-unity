using System;

using Newtonsoft.Json.Linq;


public class CommandBatchItem {
	public string commandName;
	public JObject parameters;
	public Action<Exception, JToken> cb;
	
	public CommandBatchItem(string commandName, JObject parameters, Action<Exception, JToken> cb) {
		this.commandName = commandName;
		this.parameters = parameters;
		this.cb = cb;
	}
}