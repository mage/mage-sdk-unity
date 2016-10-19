using UnityEngine;

namespace Wizcorp.MageSDK.Utils {
	public class Singleton<T> where T : class, new() {
		// Instance functions
		private static T _Instance;
		public static T Instance {
			get {
				if (_Instance == null) {
					_Instance = new T();
				}

				return _Instance;
			}
		}

		// Hack which makes sure the _instance property is set during the T class constructor
		public Singleton() {
			_Instance = (T)(object)this;
		}
	}
}
