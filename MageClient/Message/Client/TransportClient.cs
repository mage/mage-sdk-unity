namespace Wizcorp.MageSDK.MageClient.Message.Client
{
	public abstract class TransportClient
	{
		protected bool _running;
		public bool running { get { return _running; } }

		public abstract void Stop();
		public abstract void Start();
	}
}
