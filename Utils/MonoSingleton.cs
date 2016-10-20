using UnityEngine;

namespace Wizcorp.MageSDK.Utils
{
	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		// Instance functions
		protected static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					Instantiate();
				}

				return instance;
			}
		}

		// Instantiation function if you need to pre-instantiate rather than on demand
		public static void Instantiate()
		{
			if (instance != null)
			{
				return;
			}

			var newObject = new GameObject(typeof(T).Name);
			DontDestroyOnLoad(newObject);

			instance = newObject.AddComponent<T>();
		}

		// Use this for initialization before any start methods are called
		protected virtual void Awake()
		{
			if (instance != null)
			{
				DestroyImmediate(gameObject);
				return;
			}

			instance = (T)(object)this;
			DontDestroyOnLoad(gameObject);
		}
	}
}
