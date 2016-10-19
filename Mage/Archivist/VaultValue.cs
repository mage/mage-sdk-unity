using System;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Tomes;

namespace Wizcorp.MageSDK.MageClient {
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

		private int? _expirationTime;
		public int? expirationTime { get { return _expirationTime; } }

		//
		private DateTime _writtenAt;
		public DateTime writtenAt { get { return _writtenAt; } }


		//
		public VaultValue(string topic, JObject index) {
			_topic = topic;
			_index = index;
		}


		// TODO: implement multiple media-types and encoding
		public void SetData(string mediaType, JToken data) {
			lock ((object)this) {
				// Detect media type
				_mediaType = mediaType;

				// Set data based on media type
				_data = Tome.Conjure(JToken.Parse((string)data));

				// Bump the last written time
				_writtenAt = DateTime.UtcNow;
			}
		}


		//
		public void Del() {
			lock ((object)this) {
				// Bump the last written time and check if we have data to destroy
				_writtenAt = DateTime.UtcNow;
				if (_data == null) {
					return;
				}

				// Cleanup data
				Tome.Destroy(_data);
				_data = null;
				_mediaType = null;

				// Clear expiration time
				Touch(null);
			}
		}


		// TODO: the actual implementation of this requires the MAGE time module,
		// also we have a timer to clear the value once expired.
		public void Touch(int? expirationTime) {
			lock ((object)this) {
				_expirationTime = expirationTime;
			}
		}


		//
		public void ApplyDiff(JArray diff) {
			lock ((object)this) {
				if (diff == null || _data == null) {
					return;
				}

				// Apply diff to data
				Tome.ApplyDiff(_data, diff);

				// Bump the last written time
				_writtenAt = DateTime.UtcNow;
			}
		}
	}
}
