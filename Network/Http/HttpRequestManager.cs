using System.Collections;
using System.Collections.Generic;

using Wizcorp.MageSDK.Utils;

namespace Wizcorp.MageSDK.Network.Http
{
	public class HttpRequestManager : MonoSingleton<HttpRequestManager>
	{
		//
		private static List<IEnumerator> queued = new List<IEnumerator>();

		//
		public static void Queue(IEnumerator coroutine)
		{
			lock ((object)queued)
			{
				queued.Add(coroutine);
			}
		}

		//
		void Update()
		{
			lock ((object)queued)
			{
				for (var i = 0; i < queued.Count; i += 1)
				{
					StartCoroutine(queued[i]);
				}

				queued.Clear();
			}
		}
	}
}
