using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Wizcorp.MageSDK.Event
{
	public class EventEmitter<T>
	{
		//
		private Dictionary<string, EventHandlerList> tagListMap = new Dictionary<string, EventHandlerList>();

		//
		public void On(string eventTag, Action<object, T> handler)
		{
			if (!tagListMap.ContainsKey(eventTag))
			{
				tagListMap.Add(eventTag, new EventHandlerList());
			}

			tagListMap[eventTag].AddHandler(null, handler);
		}

		//
		public void Once(string eventTag, Action<object, T> handler)
		{
			Action<object, T> handlerWrapper = null;
			handlerWrapper = (obj, arguments) => {
				tagListMap[eventTag].RemoveHandler(null, handlerWrapper);
				handler(obj, arguments);
			};

			On(eventTag, handlerWrapper);
		}

		//
		public void Emit(string eventTag, object sender, T arguments)
		{
			if (!tagListMap.ContainsKey(eventTag))
			{
				return;
			}

			var execEventList = (Action<object, T>)tagListMap[eventTag][null];
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
			if (!tagListMap.ContainsKey(eventTag))
			{
				return;
			}

			tagListMap[eventTag].RemoveHandler(null, handler);
		}

		//
		public void RemoveTagListeners(string eventTag)
		{
			if (!tagListMap.ContainsKey(eventTag))
			{
				return;
			}

			tagListMap[eventTag].Dispose();
			tagListMap[eventTag] = new EventHandlerList();
		}

		//
		public void RemoveAllListeners()
		{
			foreach (KeyValuePair<string, EventHandlerList> entry in tagListMap)
			{
				entry.Value.Dispose();
			}

			tagListMap = new Dictionary<string, EventHandlerList>();
		}
	}
}
