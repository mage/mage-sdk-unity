using System;

public class HTTPRequestException : Exception {
	public int Status = 0;

	public HTTPRequestException(string Message, int Status) : base(Message) {
		this.Status = Status;
	}
}