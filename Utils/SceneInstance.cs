using UnityEngine;

namespace Wizcorp.MageSDK.Utils
{
	public class SceneInstance<T> : MonoBehaviour where T : class
	{
		public static T Instance
		{
			get { return instance; }
		}

		//
		private static T instance;

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