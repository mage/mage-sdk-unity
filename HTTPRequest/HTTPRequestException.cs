using System;

namespace Wizcorp.MageSDK.Network.Http
{
	public class HttpRequestException : Exception
	{
		public int Status;

		public HttpRequestException(string message, int status) : base(message)
		{
			Status = status;
		}
	}
}
