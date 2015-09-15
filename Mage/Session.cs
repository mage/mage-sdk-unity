using Newtonsoft.Json.Linq;

public class Session {
	private Mage mage { get { return Mage.Instance; }}

	//
	private string sessionKey;
	private string actorId;

	//
	public Session () {
		mage.eventManager.on ("session.set", (object sender, JToken session) => {
			actorId = session["actorId"].ToString();
			sessionKey = session["key"].ToString();
		});
		
		mage.eventManager.on ("session.unset", (object sender, JToken reason) => {
			actorId = null;
			sessionKey = null;
		});
	}

	//
	public string GetSessionKey() {
		return sessionKey;
	}

	//
	public string GetActorId() {
		return actorId;
	}
}
