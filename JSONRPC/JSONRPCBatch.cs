using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.Network.JsonRpc {
	public class JSONRPCBatch {
		public JArray batch = new JArray();

		public void Add(string methodName, JObject parameters) {
			Add(JValue.CreateNull(), methodName, parameters);
		}

		public void Add(string id, string methodName, JObject parameters) {
			Add(new JValue(id), methodName, parameters);
		}

		public void Add(int id, string methodName, JObject parameters) {
			Add(new JValue(id), methodName, parameters);
		}

		public void Add(JValue id, string methodName, JObject parameters) {
			JObject requestObject = new JObject();
			requestObject.Add("jsonrpc", new JValue("2.0"));

			requestObject.Add("id", id);
			requestObject.Add("method", new JValue(methodName));
			requestObject.Add("params", parameters);

			batch.Add(requestObject);
		}
	}
}
