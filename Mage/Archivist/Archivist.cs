using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

public class Archivist : EventEmitter<JObject> {
	private Mage mage { get { return Mage.instance; } }
	private Logger logger { get { return mage.logger("archivist"); } }

	// Local cache of all retrieved vault values
	private Dictionary<string, VaultValue> _cache = new Dictionary<string, VaultValue>();


	// Constructor
	public Archivist () {
		// Apply changes to cached vault values when applyDiff event is fired
		mage.eventManager.on ("archivist:applyDiff", (object sender, JToken diff) => {
			string topic = diff["key"]["topic"].ToString();
			JObject index = (JObject)diff["key"]["index"];
			string cacheKeyName = getCacheKey(topic, index);

			// Check if cache contains value
			if (!_cache.ContainsKey(cacheKeyName)) {
				logger.debug("Can't apply diff, value doesn't exist: " + cacheKeyName);
				return;
			}

			logger.data(diff["diff"]).verbose("Applying diff to vault value: " + cacheKeyName);
			_cache[cacheKeyName].ApplyDiff((JArray)diff["diff"]);
		});
	}


	// Returns string id of a vault value for given topic and index
	private string getCacheKey (string topic, JObject index) {
		// Sort the keys so order of index is always the same
		List<string> indexKeys = new List<string> ();
		foreach (var property in index) {
			indexKeys.Add(property.Key);
		}
		indexKeys.Sort ();

		// Construct cache key list with correct ordering
		List<string> cacheKeys = new List<string> ();
		cacheKeys.Add (topic);

		foreach (string indexKey in indexKeys) {
			cacheKeys.Add (indexKey + "=" + index[indexKey].ToString());
		}

		// Join the cache key list into final key string
		return string.Join(":", cacheKeys.ToArray());
	}


	////////////////////////////////////////////
	//       Raw Communication Functions      //
	////////////////////////////////////////////
	private void rawGet(string topic, JObject index, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add("topic", topic);
		parameters.Add("index", index);
		
		mage.rpcClient.call ("archivist.rawGet", parameters, cb);
	}
	
	private void rawMGet(JToken queries, JObject options, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add(new JProperty ("queries", queries));
		parameters.Add("options", options);

		mage.rpcClient.call ("archivist.rawMGet", parameters, cb);
	}
	
	private void rawList(string topic, JObject partialIndex, JObject options, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add("topic", new JValue(topic));
		parameters.Add("partialIndex", partialIndex);
		parameters.Add("options", options);
		
		mage.rpcClient.call ("archivist.rawList", parameters, cb);
	}
	
	private void rawSet(string topic, JObject index, JToken data, string mediaType, string encoding, string expirationTime, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add ("topic", new JValue(topic));
		parameters.Add ("index", index);
		parameters.Add(new JProperty ("data", data));
		parameters.Add ("mediaType", new JValue(mediaType));
		parameters.Add ("encoding", new JValue(encoding));
		parameters.Add ("expirationTime", new JValue(expirationTime));
		
		mage.rpcClient.call ("archivist.rawSet", parameters, cb);
	}
	
	private void rawDel(string topic, JObject index, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add ("topic", new JValue(topic));
		parameters.Add ("index", index);

		mage.rpcClient.call ("archivist.rawDel", parameters, cb);
	}
	
	
	////////////////////////////////////////////
	//           Exposed Operations           //
	////////////////////////////////////////////
	public void get(string topic, JObject index, JObject options, Action<Exception, JToken> cb) {
		// Check cache
		string cacheKeyName = getCacheKey (topic, index);
		if (_cache.ContainsKey(cacheKeyName)) {
			cb(null, _cache[cacheKeyName].data);
			return;
		}
		
		// Default options
		if (options == null) {
			options = new JObject();
		}

		if (options["optional"] == null) {
			options.Add ("optional", new JValue(false));
		}
	
		// Get data from server
		rawGet (topic, index, (Exception error, JToken result) => {
			if (error != null) {
				cb (error, null);
				return;
			}

			//if (result == null && !options ["optional"]) {
			//	return cb (new Exception ("ValueNotFound"), null);
			//}

			VaultValue newValue;
			try {
				// Create vault value
				newValue = new VaultValue((JObject)result);
				
				// Add value to cache
				_cache.Add (cacheKeyName, newValue);
			} catch (Exception cacheError) {
				cb(cacheError, null);
				return;
			}

			// Return result
			cb (null, newValue.data);
		});
	}

	public void mget(JArray queries, JObject options, Action<Exception, JToken> cb) {
		JArray realQueries = new JArray();
		Dictionary<string, int> realQueryKeys = new Dictionary<string, int>();
		JArray responseArray = new JArray();


		// Check cache
		foreach (JObject query in queries) {
			string cacheKeyName = getCacheKey (query["topic"].ToString(), query["index"] as JObject);
			if (_cache.ContainsKey(cacheKeyName)) {
				responseArray.Add(_cache[cacheKeyName].data);
			} else {
				realQueryKeys.Add(cacheKeyName, responseArray.Count);
				responseArray.Add(null);
				realQueries.Add(query);
			}
		}


		// Check if any real queries exist
		if (realQueries.Count == 0) {
			cb (null, responseArray);
			return;
		}


		// Default options


		// Get data from server
		rawMGet (realQueries, options, (Exception error, JToken result) => {
			if (error != null) {
				cb (error, null);
				return;
			}

			try {
				JArray topicValues = (JArray)result;
				foreach (JObject topicValue in topicValues) {
					// Determine value cacheKeyName
					string valueTopic = topicValue["key"]["topic"].ToString();
					JObject valueIndex = (JObject)topicValue["key"]["index"];
					string cacheKeyName = getCacheKey(valueTopic, valueIndex);

					// Create vault value
					VaultValue newValue = new VaultValue(topicValue);

					// Add value to cache
					_cache.Add (cacheKeyName, newValue);

					// Add value to response
					int responseKey = realQueryKeys[cacheKeyName];
					responseArray[responseKey].Replace((JToken)newValue.data);
				}
			} catch (Exception cacheError) {
				cb(cacheError, null);
				return;
			}
			
			// Return result
			cb (null, responseArray);
		});
	}
	
	public void mget(JObject queries, JObject options, Action<Exception, JToken> cb) {
		JObject realQueries = new JObject();
		Dictionary<string, string> realQueryKeys = new Dictionary<string, string>();
		JObject responseObject = new JObject();


		// Check cache
		foreach (var query in queries) {
			string cacheKeyName = getCacheKey (query.Value["topic"].ToString(), query.Value["index"] as JObject);
			if (_cache.ContainsKey(cacheKeyName)) {
				responseObject.Add(query.Key, _cache[cacheKeyName].data);
			} else {
				realQueryKeys.Add(cacheKeyName, query.Key);
				realQueries.Add(query.Key, query.Value);
			}
		}


		// Check if any real queries exist
		if (realQueries.Count == 0) {
			cb (null, responseObject);
			return;
		}

		
		// Default options


		// Get data from server
		rawMGet (realQueries, options, (Exception error, JToken result) => {
			if (error != null) {
				cb (error, null);
				return;
			}

			try {
				JArray topicValues = (JArray)result;
				foreach (JObject topicValue in topicValues) {
					// Determine value cacheKeyName
					string valueTopic = topicValue["key"]["topic"].ToString();
					JObject valueIndex = (JObject)topicValue["key"]["index"];
					string cacheKeyName = getCacheKey(valueTopic, valueIndex);

					// Create vault value
					VaultValue newValue = new VaultValue(topicValue);

					// Add value to cache
					_cache.Add (cacheKeyName, newValue);

					// Add value to response
					string responseKey = realQueryKeys[cacheKeyName];
					responseObject.Add(responseKey, (JToken)newValue.data);
				}
			} catch (Exception cacheError) {
				cb(cacheError, null);
				return;
			}
			
			// Return result
			cb (null, responseObject);
		});
	}
	
	public void list(string topic, JObject partialIndex, JObject options, Action<Exception, JToken> cb) {
		rawList (topic, partialIndex, options, cb);
	}
}
