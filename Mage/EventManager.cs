using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Event;
using Wizcorp.MageSDK.Log;

namespace Wizcorp.MageSDK.MageClient
{
	public class EventManager : EventEmitter<JToken>
	{
		private Mage Mage
		{
			get { return Mage.Instance; }
		}

		private Logger Logger
		{
			get { return Mage.Logger("EventManager"); }
		}


		public void EmitEventList(JArray events)
		{
			foreach (JToken responseEvent in events)
			{
				string eventTag = null;
				JToken eventData = null;

				// Copy the eventItem for processing
				JArray eventItem = JArray.Parse(responseEvent.ToString());

				// Check that event name is present
				if (eventItem.Count >= 1)
				{
					eventTag = eventItem[0].ToString();
				}

				// Check if any event data is present
				if (eventItem.Count == 2)
				{
					eventData = eventItem[1];
				}

				// Check if there were any errors, log and skip them
				if (eventTag == null || eventItem.Count > 2)
				{
					Logger.Data(eventItem).Error("Invalid event format:");
					continue;
				}

				// Emit the event
				Logger.Debug("Emitting '" + eventTag + "'");
				Mage.EventManager.Emit(eventTag, eventData);
			}
		}
	}
}
