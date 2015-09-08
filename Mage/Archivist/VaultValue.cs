using System;

using Newtonsoft.Json.Linq;

public class VaultValue {
	//
	private string _topic;
	public string topic { get { return _topic; } }

	private JObject _index;
	public JObject index { get { return _index; } }
	
	private JToken _data;
	public JToken data { get { return _data; } }

	private string _mediaType;
	public string mediaType { get { return _mediaType; } }

	//private uint _expirationTime;
	//public uint expirationTime { get { return _expirationTime; } }


	//
	public VaultValue(JObject valueObject) {
		// Extract value data
		_topic = valueObject["key"]["topic"].ToString();
		_index = (JObject)valueObject["key"]["index"];

		_mediaType = valueObject["value"]["mediaType"].ToString();

		// TODO: implement multiple media-types
		_data = Tome.Conjure(JToken.Parse(valueObject["value"]["data"].ToString()));

		// TODO: handle expiration time
		//_expirationTime = valueObject["value"]["expirationTime"];
	}


	//
	public void ApplyDiff(JArray operations) {
		Tome.ApplyDiff(_data, operations);
	}
}
 