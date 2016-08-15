using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class HTTPRequestManager : MonoSingleton<HTTPRequestManager> {
	//
	private static List<IEnumerator> queued = new List<IEnumerator>();

	//
	public static void Queue(IEnumerator coroutine) {
		lock ((object)queued) {
			queued.Add(coroutine);
		}
	}

	//
	void Update () {
		lock ((object)queued) {
			for (int i = 0; i < queued.Count; i += 1) {
				StartCoroutine(queued[i]);
			}

			queued.Clear();
		}
	}
}