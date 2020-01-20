// IF WE ARE USING UNITY AND WE HAVE NOT DEFINED THE
// MAGE_USE_WEBREQUEST MACROS, USE THIS VERSION
#if !MAGE_USE_WEBREQUEST

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Wizcorp.MageSDK.Network.Http
{
	public class HttpRequest
	{
		private WWW _request;
		private CookieContainer _cookies;
		private Action<Exception, string> finalCb;

		/// <summary>
		/// Timeout setting for request
		/// </summary>
		private Stopwatch _timeoutTimer = new Stopwatch();
		public long Timeout = 100 * 1000;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="url"></param>
		/// <param name="contentType"></param>
		/// <param name="postData"></param>
		/// <param name="headers"></param>
		/// <param name="cookies"></param>
		/// <param name="cb"></param>
		public HttpRequest(string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			// Start timeout timer
			_timeoutTimer.Start();

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
			finalCb = cb;
			_cookies = cookies;
			_request = new WWW(url, postData, headersCopy);

			// Initiate response
			HttpRequestManager.Queue(WaitLoop());
			yield break;
		}

		// Wait for www request to complete with timeout checks
		private IEnumerator WaitLoop()
		{
			while (_request != null && !_request.isDone)
			{
				if (_timeoutTimer.ElapsedMilliseconds >= Timeout)
				{
					// Timed out abort the request with timeout error
					Abort();
					finalCb(new HttpRequestException("Request timed out", 0), null);
					yield break;
				}

				// Otherwise continue to wait
				yield return null;
			}

			// Check if we destroyed the request due to an abort
			if (_request == null)
			{
				yield break;
			}

			// Cleanup timeout
			_timeoutTimer.Stop();
			_timeoutTimer = null;

			// Check if there is a callback
			if (finalCb == null) {
				yield break;
			}

			// Check if there was an error with the request
			if (_request.error != null)
			{
				var statusCode = 0;
				if (_request.responseHeaders != null && _request.responseHeaders.ContainsKey("STATUS"))
				{
					statusCode = int.Parse(_request.responseHeaders["STATUS"].Split(' ')[1]);
				}

				finalCb(new HttpRequestException(_request.error, statusCode), _request.text);
				yield break;
			}

			// Otherwise check for cookies and return the response
			// NOTE: this does not support multiple cookies as the WWW class headers are stored in a
			// dictionary. Previously we used reflection to extract the raw headers string and passed
			// the set-cookie headers out, however since change of the Unity WWW class to use
			// UnityWebReuqest internally, we no longer have access to this raw string and have no
			// means to resolve this issue.
			string cookieHeader;
			_request.responseHeaders.TryGetValue("Set-Cookie", out cookieHeader);

			if (cookieHeader != null)
			{
				Uri requestUri = new Uri(_request.url);
				_cookies.SetCookies(requestUri, cookieHeader);
			}

			finalCb(null, _request.text);
		}

		/// <summary>
		/// Abort the request
		/// </summary>
		public void Abort()
		{
			if (_request != null)
			{
				DisposeWWWInBackground(_request);
				_request = null;
			}

			if (_timeoutTimer != null)
			{
				_timeoutTimer.Stop();
				_timeoutTimer = null;
			}
		}

		// Hack : I Feel bad for that ... I'm sorry but it's the faster way we found to fix an android bug
		private void DisposeWWWInBackground(WWW www)
		{
			#if UNITY_ANDROID
			new Thread(() => {
				www.Dispose();
			}).Start();
			#else
			www.Dispose();
			#endif
		}


		/// <summary>
		/// Create GET request and return it
		/// </summary>
		/// <param name="url"></param>
		/// <param name="headers"></param>
		/// <param name="cookies"></param>
		/// <param name="cb"></param>
		/// <returns></returns>
		public static HttpRequest Get(string url, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			// Create request and return it
			// The callback will be called when the request is complete
			return new HttpRequest(url, null, null, headers, cookies, cb);
		}

		/// <summary>
		/// Create POST request and return it
		/// </summary>
		/// <param name="url"></param>
		/// <param name="contentType"></param>
		/// <param name="postData"></param>
		/// <param name="headers"></param>
		/// <param name="cookies"></param>
		/// <param name="cb"></param>
		/// <returns></returns>
		public static HttpRequest Post(
			string url, string contentType, string postData, Dictionary<string, string> headers, CookieContainer cookies,
			Action<Exception, string> cb)
		{
			byte[] binaryPostData = Encoding.UTF8.GetBytes(postData);
			return Post(url, contentType, binaryPostData, headers, cookies, cb);
		}

		/// <summary>
		/// Create POST request and return it
		/// </summary>
		/// <param name="url"></param>
		/// <param name="contentType"></param>
		/// <param name="postData"></param>
		/// <param name="headers"></param>
		/// <param name="cookies"></param>
		/// <param name="cb"></param>
		/// <returns></returns>
		public static HttpRequest Post(
			string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies,
			Action<Exception, string> cb)
		{
			return new HttpRequest(url, contentType, postData, headers, cookies, cb);
		}
	}
}
#endif
