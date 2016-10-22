using System;

using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.MageClient
{
	public class UserCommandStatus
	{
		public bool Done = false;
		public Exception Error = null;
		public JToken Result = null;
	}
}
