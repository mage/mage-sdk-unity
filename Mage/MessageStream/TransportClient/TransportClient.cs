using System;

public enum TransportType {
	SHORTPOLLING,
	LONGPOLLING
}

public abstract class TransportClient {
	public abstract void stop ();
	public abstract void start();
}
