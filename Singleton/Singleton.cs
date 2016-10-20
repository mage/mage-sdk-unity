namespace Wizcorp.MageSDK.Utils
{
	public class Singleton<T> where T : class, new()
	{
		// Instance functions
		private static T instance;
		public static T Instance
		{
			get { return instance ?? (instance = new T()); }
		}

		// Hack which makes sure the _instance property is set during the T class constructor
		protected Singleton()
		{
			instance = (T)(object)this;
		}
	}
}
