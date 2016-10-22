// IF WE ARE USING UNITY AND WE HAVE NOT DEFINED THE
// MAGE_USE_WEBREQUEST MACROS, USE THIS VERSION
#if UNITY_5 && !MAGE_USE_WEBREQUEST

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Reflection;

using UnityEngine;

namespace Wizcorp.MageSDK.Network.Http
{
	public class HttpRequest
	{
		private WWW request;
		private CookieContainer cookies;
		private Action<Exception, string> cb;

		// Timeout setting for request
		public long Timeout = 100 * 1000;
		private Stopwatch timeoutTimer = new Stopwatch();


		// Constructor
		public HttpRequest(string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			// Start timeout timer
			timeoutTimer.Start();

			// Queue constructor for main thread execution
			HttpRequestManager.Queue(Constructor(url, contentType, postData, headers, cookies, cb));
		}


		// Main thread constructor
		private IEnumerator Constructor(string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			Dictionary<string, string> headersCopy = (headers != null) ? new Dictionary<string, string>(headers) : new Dictionary<string, string>();

			// Set content type if provided
			if (contentType != null)
			{
				headersCopy.Add("ContentType", contentType);
			}

			// Set cookies if provided
			if (cookies != null)
			{
				Uri requestUri = new Uri(url);
				string cookieString = cookies.GetCookieHeader(requestUri);
				if (!string.IsNullOrEmpty(cookieString))
				{
					headersCopy.Add("Cookie", cookieString);
				}
			}

			// Setup private properties and fire off the request
			this.cb = cb;
			this.cookies = cookies;
			request = new WWW(url, postData, headersCopy);

			// Initiate response
			HttpRequestManager.Queue(WaitLoop());
			yield break;
		}


		// Wait for www request to complete with timeout checks
		private IEnumerator WaitLoop()
		{
			while (request != null && !request.isDone)
			{
				if (timeoutTimer.ElapsedMilliseconds >= Timeout)
				{
					// Timed out abort the request with timeout error
					Abort();
					cb(new Exception("Request timed out"), null);
					yield break;
				}

				// Otherwise continue to wait
				yield return null;
			}

			// Check if we destroyed the request due to an abort
			if (request == null)
			{
				yield break;
			}

			// Cleanup timeout
			timeoutTimer.Stop();
			timeoutTimer = null;

			// Check if there is a callback
			if (cb == null)
			{
				yield break;
			}

			// Check if there was an error with the request
			if (request.error != null)
			{
				var statusCode = 0;
				if (request.responseHeaders.ContainsKey("STATUS"))
				{
					statusCode = int.Parse(request.responseHeaders["STATUS"].Split(' ')[1]);
				}

				cb(new HttpRequestException(request.error, statusCode), null);
				yield break;
			}

			// Otherwise check for cookies and return the response
			// HACK: we need to use reflection as cookieContainer
			// junks same key cookies as it uses a dictionary. To
			// work around this we use reflection to get the original
			// cookie response text, which isn't exposed, and parse
			// it ourselves
			Uri requestUri = new Uri(request.url);
			PropertyInfo pinfoHeadersString = typeof(WWW).GetProperty("responseHeadersString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (pinfoHeadersString != null)
			{
				string headersString = pinfoHeadersString.GetValue(request, null) as string;
				string[] headerLines = headersString.Split('\n');

				foreach (string headerStr in headerLines)
				{
					if (headerStr.StartsWith("set-cookie:", true, null))
					{
						cookies.SetCookies(requestUri, headerStr.Remove(0, 11));
					}
				}
			}

			cb(null, request.text);
		}


		// Abort request
		public void Abort()
		{
			if (this.request == null)
			{
				return;
			}

			WWW request = this.request;
			this.request = null;

			request.Dispose();
			timeoutTimer.Stop();
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
		public static HttpRequest Post(
			string url, string contentType, string postData, Dictionary<string, string> headers, CookieContainer cookies,
			Action<Exception, string> cb)
		{
			byte[] binaryPostData = Encoding.UTF8.GetBytes(postData);
			return Post(url, contentType, binaryPostData, headers, cookies, cb);
		}

		// Create POST request and return it
		public static HttpRequest Post(
			string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies,
			Action<Exception, string> cb)
		{
			return new HttpRequest(url, contentType, postData, headers, cookies, cb);
		}
	}
}

#endif
