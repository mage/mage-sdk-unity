using System;
using System.Collections.Generic;
using System.Text;

namespace Wizcorp.MageSDK.MageClient.Message
{
	public class MessageStreamConfig
	{
		private string endpoint;
		private string username;
		private string password;
		public string Session { private get; set; }

		public MessageStreamConfig(string endpoint, string username, string password)
		{
			this.endpoint = endpoint;
			this.username = username;
			this.password = password;
		}

		public Dictionary<string, string> GetHeaders()
		{
			var headers = new Dictionary<string, string>();
			string authInfo = string.Format("{0}:{1}", username, password);
			string encodedAuth = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			headers.Add("Authorization", "Basic " + encodedAuth);
			return headers;
		}

		public string GetEndpoint(string transport, List<string> confirmIds = null)
		{
			string url = endpoint + "?transport=" + transport;
			if (!string.IsNullOrEmpty(Session))
			{
				url += "&sessionKey=" + Session;
			}

			if (confirmIds != null && confirmIds.Count > 0)
			{
				url += "&confirmIds=" + string.Join(",", confirmIds.ToArray());
			}

			return url;
		}
	}
}