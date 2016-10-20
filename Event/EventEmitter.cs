using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Wizcorp.MageSDK.Event
{
	public class EventEmitter<T>
	{
		//
		private Dictionary<string, object> eventTags = new Dictionary<string, object>();
		private EventHandlerList eventsList = new EventHandlerList();

		//
		public void On(string eventTag, Action<object, T> handler)
		{
			if (!eventTags.ContainsKey(eventTag))
			{
				eventTags.Add(eventTag, new object());
			}

			eventsList.AddHandler(eventTags[eventTag], handler);
		}

		//
		public void Once(string eventTag, Action<object, T> handler)
		{
			Action<object, T> handlerWrapper = null;
			handlerWrapper = (obj, arguments) => {
				eventsList.RemoveHandler(eventTags[eventTag], handlerWrapper);
				handler(obj, arguments);
			};

			On(eventTag, handlerWrapper);
		}

		//
		public void Emit(string eventTag, object sender, T arguments)
		{
			if (!eventTags.ContainsKey(eventTag))
			{
				return;
			}

			var execEventList = (Action<object, T>)eventsList[eventTags[eventTag]];
			if (execEventList != null)
			{
				execEventList(sender, arguments);
			}
		}

		public void Emit(string eventTag, T arguments)
		{
			Emit(eventTag, null, arguments);
		}

		//
		public void Off(string eventTag, Action<object, T> handler)
		{
			if (!eventTags.ContainsKey(eventTag))
			{
				return;
			}

			eventsList.RemoveHandler(eventTags[eventTag], handler);
		}

		//
		public void RemoveAllListeners()
		{
			// Destroy all event handlers
			eventsList.Dispose();
			eventsList = new EventHandlerList();

			// Destroy all event tags
			eventTags = new Dictionary<string, object>();
		}
	}
}
