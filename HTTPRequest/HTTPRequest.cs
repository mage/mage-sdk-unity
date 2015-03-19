using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;

public class HTTPRequest {
	public static void Get(string url, Dictionary<string, string> headers, Action<Exception, string> cb) {
		// Initialize request instance
		HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
		httpRequest.Method = WebRequestMethods.Http.Get;
		
		// Set request headers
		if (headers != null) {
			foreach (KeyValuePair<string, string> entry in headers) {
				httpRequest.Headers.Add(entry.Key, entry.Value);
			}
		}

		// Process the response
		ReadResponseData(httpRequest, cb);
	}

	public static void Post(string url, string contentType, string postData, Dictionary<string, string> headers, Action<Exception, string> cb) {
		byte[] binaryPostData = Encoding.UTF8.GetBytes(postData);
		Post(url, contentType, binaryPostData, headers, cb);
	}

	public static void Post(string url, string contentType, byte[] postData, Dictionary<string, string> headers, Action<Exception, string> cb) {
		// Initialize request instance
		HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
		httpRequest.Method = WebRequestMethods.Http.Post;

		// Set content type if provided
		if (contentType != null) {
			httpRequest.ContentType = contentType;
		}
		
		// Set request headers
		if (headers != null) {
			foreach (KeyValuePair<string, string> entry in headers) {
				httpRequest.Headers.Add(entry.Key, entry.Value);
			}
		}
		
		// Make a connection and send the request
		WritePostData (httpRequest, postData, (Exception requestError) => {
			if (requestError != null) {
				cb(requestError, null);
				return;
			}
			
			// Process the response
			ReadResponseData(httpRequest, cb);
		});
	}

	private static void WritePostData (HttpWebRequest httpRequest, byte[] postData, Action<Exception> cb) {
		httpRequest.BeginGetRequestStream ((IAsyncResult callbackResult) => {
			try {
				Stream postStream = httpRequest.EndGetRequestStream(callbackResult);
				postStream.Write(postData, 0, postData.Length);
				postStream.Close();
			} catch (Exception error) {
				cb(error);
				return;
			}
			cb(null);
		}, null);
	}

	private static void ReadResponseData (HttpWebRequest httpRequest, Action<Exception, string> cb) {
		httpRequest.BeginGetResponse(new AsyncCallback((IAsyncResult callbackResult) => {
			string responseString;
			try {
				HttpWebResponse response = (HttpWebResponse)httpRequest.EndGetResponse(callbackResult);
				
				using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream())) {
					responseString = httpWebStreamReader.ReadToEnd();
				}
			} catch (Exception error) {
				cb(error, null);
				return;
			}
			
			cb(null, responseString);
		}), null);
	}
}
