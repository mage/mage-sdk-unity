using System;

using UnityEngine;

namespace Wizcorp.MageSDK.Utils
{
	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
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

		// Instance functions
		private static T instance;

		// Instantiation function if you need to pre-instantiate rather than on demand
		public static void Instantiate()
		{
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

			try
			{
				instance = (T)(object)this;
			}
			catch (InvalidCastException e)
			{
				Debug.LogError(e);
			}

			DontDestroyOnLoad(gameObject);
		}
	}
}