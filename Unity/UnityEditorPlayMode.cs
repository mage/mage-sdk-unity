#if UNITY_EDITOR

using UnityEditor;

namespace Wizcorp.MageSDK.Unity
{
	[InitializeOnLoad]
	public static class UnityEditorPlayMode
	{
		private static EditorPlayModeState currentState = EditorPlayModeState.Stopped;
		public delegate void EditorModeChanged(EditorPlayModeState newState);
		public static EditorModeChanged OnEditorModeChanged;

		static UnityEditorPlayMode()
		{
			EditorApplication.playmodeStateChanged += OnUnityPlayModeChanged;
			if (EditorApplication.isPaused)
			{
				currentState = EditorPlayModeState.Paused;
			}
		}

		private static void OnUnityPlayModeChanged()
		{
			var newState = EditorPlayModeState.Stopped;
			switch (currentState)
			{
				case EditorPlayModeState.Stopped:
					if (EditorApplication.isPlayingOrWillChangePlaymode)
					{
						newState = EditorPlayModeState.Playing;
					}
					else
					{
						newState = EditorPlayModeState.Paused;
					}
					break;
				case EditorPlayModeState.Playing:
					if (EditorApplication.isPaused)
					{
						newState = EditorPlayModeState.Paused;
					}
					else if (EditorApplication.isPlaying)
					{
						newState = EditorPlayModeState.Playing;
					}
					else
					{
						newState = EditorPlayModeState.Stopped;
					}
					break;
				case EditorPlayModeState.Paused:
					if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused)
					{
						newState = EditorPlayModeState.Playing;
					}
					else if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPaused)
					{
						newState = EditorPlayModeState.Paused;
					}
					break;
			}

			if (OnEditorModeChanged != null)
			{
				OnEditorModeChanged.Invoke(newState);
			}

			currentState = newState;
		}
	}
}

#endif
