using UnityEngine;

namespace Wizcorp.MageSDK.Utils
{
	public class SceneInstance<T> : MonoBehaviour where T : class
	{
		//
		protected static T instance;
		public static T Instance
		{
			get { return instance; }
		}

		// Use this for initialization before any start methods are called
		protected virtual void Awake()
		{
			instance = (T)(object)this;
		}

		// Use this for destruction
		protected virtual void OnDestroy()
		{
			instance = null;
		}
	}
}
