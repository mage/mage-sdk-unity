using UnityEngine;
using System.Collections;

using Wizcorp.MageSDK.Utils;

namespace Wizcorp.MageSDK.Unity
{
	public class UnityApplicationState : MonoSingleton<UnityApplicationState>
	{
		public delegate void AppStateChanged(bool pauseStatus);
		public AppStateChanged OnAppStateChanged;

		// What should happen when the application is put into the background
		void OnApplicationPause(bool pauseStatus)
		{
			if (OnAppStateChanged != null)
			{
				OnAppStateChanged.Invoke(pauseStatus);
			}
		}
	}
}
