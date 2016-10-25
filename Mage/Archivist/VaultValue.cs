using System;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Tomes;

namespace Wizcorp.MageSDK.MageClient
{
	public class VaultValue
	{
		//
		public string Topic { get; private set; }
		public JObject Index { get; private set; }
		public JToken Data { get; private set; }
		public string MediaType { get; private set; }
		public int? ExpirationTime { get; private set; }
		public DateTime WrittenAt { get; private set; }

		//
		public VaultValue(string topic, JObject index)
		{
			Topic = topic;
			Index = index;
		}

		// TODO: implement multiple media-types and encoding
		public void SetData(string mediaType, JToken data)
		{
			lock ((object)this)
			{
				// Detect media type
				MediaType = mediaType;

				// Set data based on media type
				Data = Tome.Conjure(JToken.Parse((string)data));

				// Bump the last written time
				WrittenAt = DateTime.UtcNow;
			}
		}

		//
		public void Del()
		{
			lock ((object)this)
			{
				// Bump the last written time and check if we have data to destroy
				WrittenAt = DateTime.UtcNow;
				if (Data == null)
				{
					return;
				}

				// Cleanup data
				Tome.Destroy(Data);
				Data = null;
				MediaType = null;

				// Clear expiration time
				Touch(null);
			}
		}

		// TODO: the actual implementation of this requires the MAGE time module,
		// also we have a timer to clear the value once expired.
		public void Touch(int? expirationTime)
		{
			lock ((object)this)
			{
				ExpirationTime = expirationTime;
			}
		}

		//
		public void ApplyDiff(JArray diff)
		{
			lock ((object)this)
			{
				if (diff == null || Data == null)
				{
					return;
				}

				// Apply diff to data
				Tome.ApplyDiff(Data, diff);

				// Bump the last written time
				WrittenAt = DateTime.UtcNow;
			}
		}
	}
}
