using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : class, new() {
	// Instance functions
	private static T _Instance;
	public static T Instance { get { return _Instance; } }

	// Use this for initialization before any start methods are called
	protected void Awake() {
		if (_Instance != null) {
			GameObject.DestroyImmediate(gameObject);
			return;
		}

		_Instance = (T)(object)this;
		GameObject.DontDestroyOnLoad(gameObject);
	}
}
