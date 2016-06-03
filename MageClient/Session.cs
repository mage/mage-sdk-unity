namespace Wizcorp.MageSDK.MageClient
{
	public class Session
	{
		private Mage mage
		{
			get { return Mage.Instance; }
		}

		private string actorId;

		//
		private string sessionKey;

		//
		public Session()
		{
			mage.EventManager.On("session.set", (sender, session) => {
				actorId = session["actorId"].ToString();
				sessionKey = session["key"].ToString();
			});

			mage.EventManager.On("session.unset", (sender, reason) => {
				actorId = null;
				sessionKey = null;
			});
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