#if UNITY_EDITOR

using UnityEditor;

namespace Wizcorp.MageSDK.Editor {
	public enum EditorPlayModeState {
		Stopped,
		Playing,
		Paused
	}

	[InitializeOnLoad]
	public class UnityEditorPlayMode {
		private static EditorPlayModeState currentState = EditorPlayModeState.Stopped;
		public delegate void OnEditorModeChanged(EditorPlayModeState newState);
		public static OnEditorModeChanged onEditorModeChanged;

		static UnityEditorPlayMode() {
			EditorApplication.playmodeStateChanged += OnUnityPlayModeChanged;
			if (EditorApplication.isPaused) {
				currentState = EditorPlayModeState.Paused;
			}
		}

		private static void OnUnityPlayModeChanged() {
			EditorPlayModeState newState = EditorPlayModeState.Stopped;
			switch (currentState) {
				case EditorPlayModeState.Stopped:
					if (EditorApplication.isPlayingOrWillChangePlaymode) {
						newState = EditorPlayModeState.Playing;
					} else {
						newState = EditorPlayModeState.Paused;
					}
					break;
				case EditorPlayModeState.Playing:
					if (EditorApplication.isPaused) {
						newState = EditorPlayModeState.Paused;
					} else if (EditorApplication.isPlaying) {
						newState = EditorPlayModeState.Playing;
					} else {
						newState = EditorPlayModeState.Stopped;
					}
					break;
				case EditorPlayModeState.Paused:
					if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused) {
						newState = EditorPlayModeState.Playing;
					} else if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPaused) {
						newState = EditorPlayModeState.Paused;
					}
					break;
			}

			if (onEditorModeChanged != null) {
				onEditorModeChanged.Invoke(newState);
			}

			currentState = newState;
		}
	}
}
#endif
