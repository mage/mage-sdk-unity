using System;

namespace Wizcorp.MageSDK.MageClient.Message.Client {
	public enum TransportType {
		SHORTPOLLING,
		LONGPOLLING
	}

	public abstract class TransportClient {
		protected bool _running;
		public bool running { get { return _running; } }

		public abstract void stop();
		public abstract void start();
	}
}
