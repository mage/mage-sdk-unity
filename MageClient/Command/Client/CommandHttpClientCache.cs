using System.Collections.Generic;

public class CommandHttpClientCache
{
	public string BatchUrl;
	public string PostData;
	public Dictionary<string, string> Headers;

	public CommandHttpClientCache(string batchUrl, string postData, Dictionary<string, string> headers)
	{
		BatchUrl = batchUrl;
		PostData = postData;
		Headers = headers;
	}
}
