using System;
using System.Collections.Generic;
using System.ComponentModel;

public class EventEmitter<T> {
	//
	private Dictionary<string, object> eventTags = new Dictionary<string, object>();
	private EventHandlerList eventsList = new EventHandlerList();

	//
	public void on(string eventTag, Action<object, T> handler)
	{
		if (!eventTags.ContainsKey(eventTag)) {
			eventTags.Add(eventTag, new object());
		}

		eventsList.AddHandler(eventTags[eventTag], handler);
	}

	//
	public void once(string eventTag, Action<object, T> handler)
	{
		Action<object, T> handlerWrapper;
		handlerWrapper = (object obj, T arguments) => {
			eventsList.RemoveHandler(eventTags[eventTag], handlerWrapper);
			handler(obj, arguments);
		};

		on(eventTag, handlerWrapper);
	}

	//
	public void emit(string eventTag, object sender, T arguments)
	{
		if (!eventTags.ContainsKey(eventTag)) {
			return;
		}

		Action<object, T> execEventList = (Action<object, T>)eventsList[eventTags[eventTag]];
		execEventList(sender, arguments);
	}
	
	public void emit(string eventTag, T arguments)
	{
		emit(eventTag, null, arguments);
	}

	//
	public void off(string eventTag, Action<object, T> handler)
	{
		eventsList.RemoveHandler(eventTags[eventTag], handler);
	}

	//
	public void removeAllListeners()
	{
		// Destroy all event handlers
		eventsList.Dispose();
		eventsList = new EventHandlerList();

		// Destroy all event tags
		eventTags = new Dictionary<string, object>();
	}
}