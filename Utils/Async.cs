using System;
using System.Collections.Generic;

namespace Wizcorp.MageSDK.Utils
{
	/// <summary>
	/// Async is a utility class which provides straight-forward, powerful functions for working with asynchronous C#.
	/// 
	/// Async provides around 20 functions that include the usual 'functional' suspects (map, reduce, filter, each…) as well as
	/// some common patterns for asynchronous control flow (parallel, series, waterfall…). All these functions assume you follow
	/// the convention of providing a single callback as the last argument of your async function.
	/// </summary>
	public static class Async
	{
		public static void Each<T>(List<T> items, Action<T, Action<Exception>> fn, Action<Exception> cb)
		{
			if (items == null || items.Count == 0)
			{
				cb(null);
				return;
			}

			var currentItemI = 0;
			Action iterate = null;
			iterate = () => {
				if (currentItemI >= items.Count)
				{
					cb(null);
					return;
				}

				// Execute the given function on this item
				fn(
					items[currentItemI],
					error => {
						// Stop iteration if there was an error
						if (error != null)
						{
							cb(error);
							return;
						}

						// Continue to next item
						currentItemI++;
						iterate();
					});
			};

			// Begin the iteration
			iterate();
		}

		public static void Series(List<Action<Action<Exception>>> actionItems, Action<Exception> cb)
		{
			bool isEmpty = actionItems == null || actionItems.Count == 0;
			if (isEmpty)
			{
				cb(null);
				return;
			}

			var currentItemI = 0;
			Action iterate = null;
			iterate = () => {
				if (currentItemI >= actionItems.Count)
				{
					cb(null);
					return;
				}

				// Shift an element from the list
				Action<Action<Exception>> actionItem = actionItems[currentItemI];

				// Execute the given function on this item
				actionItem(
					error => {
						// Stop iteration if there was an error
						if (error != null)
						{
							cb(error);
							return;
						}

						// Continue to next item
						currentItemI++;
						iterate();
					});
			};

			// Begin the iteration
			iterate();
		}
	}
}