// IF WE ARE NOT USING UNITY OR WE HAVE DEFINED THE
// MAGE_USE_WEBREQUEST MACROS, USE THIS VERSION
#if MAGE_USE_WEBREQUEST

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
		private HttpWebRequest _request;
		private Action<Exception, string> finalCb;

		private Timer _timeoutTimer;

		/// <summary>
		/// Timeout setting for request
		/// </summary>
		private long _timeout = 100 * 1000;
		public long Timeout
		{
			get
			{
				return _timeout;
			}
			set
			{
				_timeout = value;

				if (_timeoutTimer != null) {
					_timeoutTimer.Dispose();
					_timeoutTimer = null;
				}

				_timeoutTimer = new Timer((object state) => {
					Abort();
					FinalCbMainThread(new HttpRequestException("Request timed out", 0), null);
				}, null, Timeout, System.Threading.Timeout.Infinite);
			}
		}


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
			Timeout = _timeout;

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
			finalCb = cb;
			_request = httpRequest;

			// Initiate response
			if (postData == null)
			{
				ReadResponseData(FinalCbMainThread);
			}
			else
			{
				WritePostData(postData, (requestError) => {
					if (requestError != null)
					{
						FinalCbMainThread(requestError, null);
						return;
					}

					// Process the response
					ReadResponseData(FinalCbMainThread);
				});
			}
		}


		// Write post data to request buffer asynchronously
		private void WritePostData(byte[] postData, Action<Exception> cb)
		{
			_request.BeginGetRequestStream((callbackResult) => {
				try
				{
					Stream postStream = _request.EndGetRequestStream(callbackResult);
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

		// Read response data from request buffer asynchronously
		private void ReadResponseData(Action<Exception, string> cb)
		{
			// Begin waiting for a response
			_request.BeginGetResponse((callbackResult) => {
				// Cleanup timeout
				_timeoutTimer.Dispose();
				_timeoutTimer = null;

				// Process response
				string responseString = null;
				try
				{
					HttpWebResponse response = (HttpWebResponse)_request.EndGetResponse(callbackResult);

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
			}, null);
		}

		// Calls the final callback on the main thread
		private void FinalCbMainThread(Exception error, string response)
		{
			HttpRequestManager.Queue(FinalCbMainThreadEnumerator(error, response));
		}

		// Coroutine enumerator to achieve the above
		private IEnumerator FinalCbMainThreadEnumerator(Exception error, string response)
		{
			finalCb(error, response);
			yield break;
		}

		/// <summary>
		/// Abort the request
		/// </summary>
		public void Abort()
		{
			if (_request != null)
			{
				HttpWebRequest _request = this._request;
				this._request = null;
				_request.Abort();
			}

			if (_timeoutTimer != null)
			{
				_timeoutTimer.Dispose();
				_timeoutTimer = null;
			}
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
		public static HttpRequest Post(string url, string contentType, string postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
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
		public static HttpRequest Post(string url, string contentType, byte[] postData, Dictionary<string, string> headers, CookieContainer cookies, Action<Exception, string> cb)
		{
			return new HttpRequest(url, contentType, postData, headers, cookies, cb);
		}
	}
}
#endif
