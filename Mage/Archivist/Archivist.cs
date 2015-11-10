using System;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

public class Archivist : EventEmitter<VaultValue> {
	private Mage mage { get { return Mage.Instance; } }
	private Logger logger { get { return mage.logger("archivist"); } }

	// Local cache of all retrieved vault values
	private Dictionary<string, VaultValue> _cache = new Dictionary<string, VaultValue>();


	// Constructor
	public Archivist () {
		// Set data to vault value when set event received
		mage.eventManager.on("archivist:set", (object sender, JToken info) => {
			string topic = (string)info["key"]["topic"];
			JObject index = (JObject)info["key"]["index"];
			JToken data = info["value"]["data"];
			string mediaType = (string)info["value"]["mediaType"];
			int? expirationTime = (int?)info["expirationTime"];
			ValueSet(topic, index, data, mediaType, expirationTime);
		});

		// Del data inside vault value when del event received
		mage.eventManager.on("archivist:del", (object sender, JToken info) => {
			string topic = (string)info["key"]["topic"];
			JObject index = (JObject)info["key"]["index"];
			ValueDel(topic, index);
		});

		// Touch vault value expiry when touch event received
		mage.eventManager.on("archivist:touch", (object sender, JToken info) => {
			string topic = (string)info["key"]["topic"];
			JObject index = (JObject)info["key"]["index"];
			int? expirationTime = (int?)info["expirationTime"];
			ValueTouch(topic, index, expirationTime);
		});

		// Apply changes to vault value when applyDiff event is received
		mage.eventManager.on("archivist:applyDiff", (object sender, JToken info) => {
			string topic = (string)info["key"]["topic"];
			JObject index = (JObject)info["key"]["index"];
			JArray diff = (JArray)info["diff"];
			int? expirationTime = (int?)info["expirationTime"];
			ValueApplyDiff(topic, index, diff, expirationTime);
		});
	}


	////////////////////////////////////////////
	//           Cache Manipulation           //
	////////////////////////////////////////////

	// Returns string id of a vault value for given topic and index
	private string GetCacheKey (string topic, JObject index) {
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


	// Returns cache value if it exists and has not passed max allowed age
	private VaultValue GetCacheValue(string cacheKeyName, int? maxAge = null) {
		lock ((object)this) {
			if (!_cache.ContainsKey(cacheKeyName)) {
				return null;
			}

			VaultValue value = _cache[cacheKeyName];
			double timespan = (DateTime.Now - value.writtenAt).TotalMilliseconds;
			if (maxAge != null && timespan > maxAge * 1000) {
				return null;
			}

			return value;
		}
	}
	

	// Ensures a cache value exists then sets it based on info provided
	private VaultValue CreateCacheValue(string topic, JObject index) {
		lock ((object)this) {
			VaultValue cacheValue = new VaultValue(topic, index);

			string cacheKeyName = GetCacheKey(topic, index);
			_cache.Add(cacheKeyName, cacheValue);

			return cacheValue;
		}
	}


	// Return cache dictionary
	public Dictionary<string, VaultValue> GetCache() {
		return _cache;
	}


	// Clear out the cache entirely
	// NOTE: we create a new instance as the Clear() function is not thread safe.
	public void ClearCache() {
		lock ((object)this) {
			_cache.Clear();
		}
	}


	// Remove a vault value from the cache by it's topic and index
	public void DeleteCacheItem(string topic, JObject index) {
		DeleteCacheItem(GetCacheKey(topic, index));
	}


	// Remove a vault value from the cache by it's cache key name
	public void DeleteCacheItem(string cacheKeyName) {
		lock ((object)this) {
			logger.debug("Deleteing cache item: " + cacheKeyName);
			if (!_cache.ContainsKey(cacheKeyName)) {
				return;
			}

			_cache.Remove(cacheKeyName);
		}
	}


	////////////////////////////////////////////
	//        Vault Value Manipulation        //
	////////////////////////////////////////////
	private void ValueSetOrDelete(JObject info) {
		string topic = (string)info["key"]["topic"];
		JObject index = (JObject)info["key"]["index"];
		JObject rawValue = (JObject)info["value"];

		if (rawValue != null) {
			ValueSet(topic, index, rawValue["data"], (string)rawValue["mediaType"], (int?)info["expirationTime"]);
		} else {
			ValueDel(topic, index);
		}
	}

	private void ValueSet(string topic, JObject index, JToken data, string mediaType, int? expirationTime) {
		// Try and get cache value. If it exists delete existing value
		// in preparation for set. Otherwise create a new vault value.
		string cacheKeyName = GetCacheKey(topic, index);
		VaultValue cacheValue = GetCacheValue(cacheKeyName);
		if (cacheValue == null) {
			cacheValue = CreateCacheValue(topic, index);
		} else {
			cacheValue.Del();
		}

		// Set data to vault value
		cacheValue.SetData(mediaType, data);
		cacheValue.Touch(expirationTime);

		// Emit set event
		this.emit(topic + ":set", cacheValue);
	}

	private void ValueAdd(string topic, JObject index, JToken data, string mediaType, int? expirationTime) {
		// Check if value already exists
		string cacheKeyName = GetCacheKey(topic, index);
		VaultValue cacheValue = GetCacheValue(cacheKeyName);
		if (cacheValue != null) {
			logger.error("Could not add value (already exists): " + cacheKeyName);
			return;
		}

		// Create new vault value
		cacheValue = CreateCacheValue(topic, index);

		// Set data to vault value
		cacheValue.SetData(mediaType, data);
		cacheValue.Touch(expirationTime);
		
		// Emit add event
		this.emit(topic + ":add", cacheValue);
	}

	private void ValueDel(string topic, JObject index) {
		// Check if value already exists
		string cacheKeyName = GetCacheKey(topic, index);
		VaultValue cacheValue = GetCacheValue(cacheKeyName);
		if (cacheValue == null) {
			logger.error("Could not delete value (doesn't exist): " + cacheKeyName);
			return;
		}
		
		// Do delete
		cacheValue.Del();
		
		// Emit touch event
		this.emit(topic + ":del", cacheValue);
	}

	private void ValueTouch(string topic, JObject index, int? expirationTime) {
		// Check if value already exists
		string cacheKeyName = GetCacheKey(topic, index);
		VaultValue cacheValue = GetCacheValue(cacheKeyName);
		if (cacheValue == null) {
			logger.error("Could not touch value (doesn't exist): " + cacheKeyName);
			return;
		}

		// Do touch
		cacheValue.Touch(expirationTime);
		
		// Emit touch event
		this.emit(topic + ":touch", cacheValue);
	}

	private void ValueApplyDiff(string topic, JObject index, JArray diff, int? expirationTime) {
		// Make sure value exists
		string cacheKeyName = GetCacheKey(topic, index);
		VaultValue cacheValue = GetCacheValue(cacheKeyName);
		if (cacheValue == null) {
			logger.warning("Got a diff for a non-existent value:" + cacheKeyName);
			return;
		}
		
		// Apply diff
		cacheValue.ApplyDiff(diff);
		cacheValue.Touch(expirationTime);
		
		// Emit applyDiff event
		this.emit(topic + ":applyDiff", cacheValue);
	}


	////////////////////////////////////////////
	//            Raw Communication           //
	////////////////////////////////////////////
	private void rawGet(string topic, JObject index, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add("topic", topic);
		parameters.Add("index", index);
		
		mage.commandCenter.SendCommand("archivist.rawGet", parameters, cb);
	}
	
	private void rawMGet(JToken queries, JObject options, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add(new JProperty ("queries", queries));
		parameters.Add("options", options);

		mage.commandCenter.SendCommand("archivist.rawMGet", parameters, cb);
	}
	
	private void rawList(string topic, JObject partialIndex, JObject options, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add("topic", new JValue(topic));
		parameters.Add("partialIndex", partialIndex);
		parameters.Add("options", options);
		
		mage.commandCenter.SendCommand("archivist.rawList", parameters, cb);
	}
	
	private void rawSet(string topic, JObject index, JToken data, string mediaType, string encoding, string expirationTime, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add ("topic", new JValue(topic));
		parameters.Add ("index", index);
		parameters.Add(new JProperty ("data", data));
		parameters.Add ("mediaType", new JValue(mediaType));
		parameters.Add ("encoding", new JValue(encoding));
		parameters.Add ("expirationTime", new JValue(expirationTime));
		
		mage.commandCenter.SendCommand("archivist.rawSet", parameters, cb);
	}
	
	private void rawDel(string topic, JObject index, Action<Exception, JToken> cb) {
		JObject parameters = new JObject();
		parameters.Add ("topic", new JValue(topic));
		parameters.Add ("index", index);

		mage.commandCenter.SendCommand("archivist.rawDel", parameters, cb);
	}
	
	
	////////////////////////////////////////////
	//           Exposed Operations           //
	////////////////////////////////////////////
	public void get(string topic, JObject index, JObject options, Action<Exception, JToken> cb) {
		// Default options
		options = (options != null) ? options : new JObject();
		if (options["optional"] == null) {
			options.Add("optional", new JValue(false)); 
		}


		// Check cache
		string cacheKeyName = GetCacheKey(topic, index);
		VaultValue cacheValue = GetCacheValue(cacheKeyName, (int?)options["maxAge"]);
		if (cacheValue != null) {
			cb(null, cacheValue.data);
			return;
		}
	

		// Get data from server
		rawGet (topic, index, (Exception error, JToken result) => {
			if (error != null) {
				cb (error, null);
				return;
			}

			// Parse value
			try {
				ValueSetOrDelete((JObject)result);
			} catch (Exception cacheError) {
				cb(cacheError, null);
				return;
			}

			// Return result
			cb (null, GetCacheValue(cacheKeyName).data);
		});
	}

	public void mget(JArray queries, JObject options, Action<Exception, JToken> cb) {
		// Default options
		options = (options != null) ? options : new JObject();
		if (options["optional"] == null) {
			options.Add("optional", new JValue(false)); 
		}


		// Keep track of actual data we need from server
		JArray realQueries = new JArray();
		Dictionary<string, int> realQueryKeys = new Dictionary<string, int>();
		JArray responseArray = new JArray();


		// Check cache
		foreach (JObject query in queries) {
			string topic = (string)query["topic"];
			JObject index = (JObject)query["index"];
			string cacheKeyName = GetCacheKey(topic, index);
			VaultValue cacheValue = GetCacheValue(cacheKeyName, (int?)options["maxAge"]);
			if (cacheValue != null) {
				responseArray.Add(cacheValue.data);
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


		// Get data from server
		rawMGet (realQueries, options, (Exception error, JToken results) => {
			if (error != null) {
				cb (error, null);
				return;
			}

			try {
				foreach (JObject topicValue in results as JArray) {
					// Determine value cacheKeyName
					string topic = (string)topicValue["key"]["topic"];
					JObject index = (JObject)topicValue["key"]["index"];
					string cacheKeyName = GetCacheKey(topic, index);

					// Set value to cache
					ValueSetOrDelete(topicValue);

					// Add value to response
					int responseKey = realQueryKeys[cacheKeyName];
					responseArray[responseKey].Replace(GetCacheValue(cacheKeyName).data);
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
		// Default options
		options = (options != null) ? options : new JObject();
		if (options["optional"] == null) {
			options.Add("optional", new JValue(false)); 
		}


		// Keep track of actual data we need from server
		JObject realQueries = new JObject();
		Dictionary<string, string> realQueryKeys = new Dictionary<string, string>();
		JObject responseObject = new JObject();


		// Check cache
		foreach (var query in queries) {
			string cacheKeyName = GetCacheKey(query.Value["topic"].ToString(), query.Value["index"] as JObject);
			VaultValue cacheValue = GetCacheValue(cacheKeyName, (int?)options["maxAge"]);
			if (cacheValue != null) {
				responseObject.Add(query.Key, cacheValue.data);
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


		// Get data from server
		rawMGet (realQueries, options, (Exception error, JToken results) => {
			if (error != null) {
				cb (error, null);
				return;
			}

			try {
				foreach (JObject topicValue in results as JArray) {
					// Determine value cacheKeyName
					string valueTopic = topicValue["key"]["topic"].ToString();
					JObject valueIndex = (JObject)topicValue["key"]["index"];
					string cacheKeyName = GetCacheKey(valueTopic, valueIndex);

					// Set value to cache
					ValueSetOrDelete(topicValue);

					// Add value to response
					string responseKey = realQueryKeys[cacheKeyName];
					responseObject.Add(responseKey, GetCacheValue(cacheKeyName).data);
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
