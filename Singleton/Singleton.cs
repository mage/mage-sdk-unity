using UnityEngine;

public class Singleton<T> where T : class, new() {
	// Instance functions
	private static T _instance;
	public static T instance {
		get {
			if (_instance == null) {
				_instance = new T();
			}
			
			return _instance;
		}
	}

	// Hack which makes sure the _instance property is set during the T class constructor
	public Singleton () {
		_instance = (T)(object)this;
	}
}
