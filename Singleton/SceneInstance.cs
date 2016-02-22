using UnityEngine;

public class SceneInstance<T> : MonoBehaviour where T : class, new() {
	//
	protected static T _Instance = null;
	public static T Instance { get { return _Instance; } }

	// Use this for initialization before any start methods are called
	protected virtual void Awake () {
		_Instance = (T)(object)this;
	}

	// Use this for destruction
	protected virtual void OnDestroy() {
		_Instance = null;
	}
}
