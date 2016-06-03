using UnityEngine;

namespace Wizcorp.MageSDK.Utils
{
	public class MonoSingleton<T> : MonoBehaviour
		where T : MonoBehaviour
	{
		public static T Instance
		{
			get
			{
				if (_Instance == null)
				{
					Instantiate();
				}

				return _Instance;
			}
		}

		// Instance functions
		protected static T _Instance;

		// Instantiation function if you need to pre-instantiate rather than on demand
		public static void Instantiate()
		{
			if (_Instance != null)
			{
				return;
			}

			var newObject = new GameObject(typeof(T).Name);
			DontDestroyOnLoad(newObject);

			_Instance = newObject.AddComponent<T>();
		}

		// Use this for initialization before any start methods are called
		protected virtual void Awake()
		{
			if (_Instance != null)
			{
				DestroyImmediate(gameObject);
				return;
			}

			_Instance = (T)(object)this;
			DontDestroyOnLoad(gameObject);
		}
	}
}