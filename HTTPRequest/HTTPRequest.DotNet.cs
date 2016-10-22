// IF WE ARE NOT USING UNITY OR WE HAVE DEFINED THE
// MAGE_USE_WEBREQUEST MACROS, USE THIS VERSION
#if !UNITY_5 || MAGE_USE_WEBREQUEST

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;

namespace Wizcorp.MageSDK.Network.Http
{
	public class HttpRequest
	{
		private HttpWebRequest request;
		private CookieContainer cookies;
		private Action<Exception, string> cb;
		private Timer timeoutTimer;

		// Timeout setting for request
		private long _timeout = 100 * 1000;
		public long timeout
		{
			get
			{
				return _timeout;
			}
			set
			{
				_timeout = value;

				timeoutTimer.Dispose();
				timeoutTimer = new Timer((object state) => {
					this.Abort();
					cb(new Exception("Request timed out"), null);
				}, null, timeout, Timeout.Infinite);
			}
		}


		// Constructor
		public HttpRequest(string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			// Start timeout timer
			timeoutTimer = new Timer((object state) => {
				this.Abort();
				cb(new Exception("Request timed out"), null);
			}, null, timeout, Timeout.Infinite);

			// Initialize request instance
			HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
			httpRequest.Method = (postData != null) ? WebRequestMethods.Http.Post : WebRequestMethods.Http.Get;

			// Set request headers
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> entry in headers)
				{
					httpRequest.Headers.Add(entry.Key, entry.Value);
				}
			}

			// Set content type if provided
			if (contentType != null)
			{
				httpRequest.ContentType = contentType;
			}

			// Set cookies if provided
			if (cookies != null)
			{
				httpRequest.CookieContainer = cookies;
			}

			// Setup private properties and fire off the request
			this.cb = cb;
			this.cookies = cookies;
			this.request = httpRequest;

			// Initiate response
			if (postData == null)
			{
				ReadResponseData(cb);
				return;
			}
			else
			{
				WritePostData(postData, (Exception requestError) => {
					if (requestError != null)
					{
						cb(requestError, null);
						return;
					}

					// Process the response
					ReadResponseData(cb);
				});
				return;
			}
		}


		// Write post data to request buffer
		private void WritePostData(byte[] postData, Action<Exception> cb)
		{
			request.BeginGetRequestStream((IAsyncResult callbackResult) => {
				try
				{
					Stream postStream = request.EndGetRequestStream(callbackResult);
					postStream.Write(postData, 0, postData.Length);
					postStream.Close();
				}
				catch (Exception error)
				{
					cb(error);
					return;
				}
				cb(null);
			}, null);
		}

		// Read response data from request buffer
		private void ReadResponseData(Action<Exception, string> cb)
		{
			// Begin waiting for a response
			request.BeginGetResponse(new AsyncCallback((IAsyncResult callbackResult) => {
				// Cleanup timeout
				timeoutTimer.Dispose();
				timeoutTimer = null;

				// Process response
				string responseString = null;
				try
				{
					HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);

					using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream()))
					{
						responseString = httpWebStreamReader.ReadToEnd();
					}

					response.Close();
				}
				catch (Exception error)
				{
					cb(error, null);
					return;
				}

				cb(null, responseString);
			}), null);
		}


		// Abort request
		public void Abort()
		{
			if (request == null)
			{
				return;
			}

			HttpWebRequest _request = request;
			request = null;

			_request.Abort();
			timeoutTimer.Dispose();
			timeoutTimer = null;
		}


		// Create GET request and return it
		public static HttpRequest Get(string url, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			// Create request and return it
			// The callback will be called when the request is complete
			return new HttpRequest(url, null, null, headers, cookies, cb);
		}

		// Create POST request and return it
		public static HttpRequest Post(string url, string contentType, string postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			byte[] binaryPostData = Encoding.UTF8.GetBytes(postData);
			return Post(url, contentType, binaryPostData, headers, cookies, cb);
		}

		// Create POST request and return it
		public static HttpRequest Post(string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			return new HttpRequest(url, contentType, postData, headers, cookies, cb);
		}
	}
}
#endif
