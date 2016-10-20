using System.Collections.Generic;

namespace Wizcorp.MageSDK.MageClient
{
	public class Session
	{
		private Mage Mage
		{
			get { return Mage.Instance; }
		}


		//
		private string sessionKey;
		private string actorId;


		//
		public Session()
		{
			Mage.EventManager.On("session.set", (sender, session) => {
				actorId = session["actorId"].ToString();
				sessionKey = session["key"].ToString();
			});

			Mage.EventManager.On("session.unset", (sender, reason) => {
				actorId = null;
				sessionKey = null;
			});

			Mage.CommandCenter.preSerialiseHook += (commandBatch) => {
				if (!string.IsNullOrEmpty(sessionKey))
				{
					Dictionary<string, string> sessionHeader = new Dictionary<string, string>();
					sessionHeader.Add("name", "mage.session");
					sessionHeader.Add("key", sessionKey);

					commandBatch.BatchHeaders.Add(sessionHeader);
				}
			};
		}

		//
		public string GetSessionKey()
		{
			return sessionKey;
		}

		//
		public string GetActorId()
		{
			return actorId;
		}
	}
}
