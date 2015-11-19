using System.Collections.Generic;


public class ServerWriter : LogWriter {
	private List<string> config;

	public ServerWriter(List<string> logLevels) {
		config = logLevels;
	}

	public override void Dispose() {
	}
}