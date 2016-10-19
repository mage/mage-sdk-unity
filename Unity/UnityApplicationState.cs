using UnityEngine;
using System.Collections;

using Wizcorp.MageSDK.Utils;

public class UnityApplicationState : MonoSingleton<UnityApplicationState> {
	public delegate void OnAppStateChanged(bool pauseStatus);
	public OnAppStateChanged onAppStateChanged;

	// What should happen when the application is put into the background
	void OnApplicationPause(bool pauseStatus) {
		if (onAppStateChanged != null) {
			onAppStateChanged.Invoke(pauseStatus);
		}
	}
}
