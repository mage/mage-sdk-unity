using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.Event;
using Wizcorp.MageSDK.Log;

namespace Wizcorp.MageSDK.MageClient
{
	public class Archivist : EventEmitter<VaultValue>
	{
		private Mage Mage
		{
			get { return Mage.Instance; }
		}

		private Logger Logger
		{
			get { return Mage.Logger("Archivist"); }
		}

		// Local cache of all retrieved vault values
		private Dictionary<string, VaultValue> cache = new Dictionary<string, VaultValue>();


		// Constructor
		public Archivist()
		{
			// Set data to vault value when set event received
			Mage.EventManager.On("archivist:set", (sender, info) => {
				var topic = (string)info["key"]["topic"];
				var index = (JObject)info["key"]["index"];
				JToken data = info["value"]["data"];
				var mediaType = (string)info["value"]["mediaType"];
				var expirationTime = (int?)info["expirationTime"];
				ValueSet(topic, index, data, mediaType, expirationTime);
			});

			// Del data inside vault value when del event received
			Mage.EventManager.On("archivist:del", (sender, info) => {
				var topic = (string)info["key"]["topic"];
				var index = (JObject)info["key"]["index"];
				ValueDel(topic, index);
			});

			// Touch vault value expiry when touch event received
			Mage.EventManager.On("archivist:touch", (sender, info) => {
				var topic = (string)info["key"]["topic"];
				var index = (JObject)info["key"]["index"];
				var expirationTime = (int?)info["expirationTime"];
				ValueTouch(topic, index, expirationTime);
			});

			// Apply changes to vault value when applyDiff event is received
			Mage.EventManager.On("archivist:applyDiff", (sender, info) => {
				var topic = (string)info["key"]["topic"];
				var index = (JObject)info["key"]["index"];
				var diff = (JArray)info["diff"];
				var expirationTime = (int?)info["expirationTime"];
				ValueApplyDiff(topic, index, diff, expirationTime);
			});
		}


		////////////////////////////////////////////
		//           Cache Manipulation           //
		////////////////////////////////////////////

		// Returns string id of a vault value for given topic and index
		private static string CreateCacheKey(string topic, JObject index)
		{
			// Sort the keys so order of index is always the same
			var indexKeys = new List<string>();
			foreach (KeyValuePair<string, JToken> property in index)
			{
				indexKeys.Add(property.Key);
			}
			indexKeys.Sort();

			// Construct cache key list with correct ordering
			var cacheKeys = new List<string>();
			cacheKeys.Add(topic);

			foreach (string indexKey in indexKeys)
			{
				cacheKeys.Add(indexKey + "=" + index[indexKey]);
			}

			// Join the cache key list into final key string
			return string.Join(":", cacheKeys.ToArray());
		}


		// Returns cache value if it exists and has not passed max allowed age
		private VaultValue GetCacheValue(string cacheKeyName, int? maxAge = null)
		{
			lock ((object)cache)
			{
				if (!cache.ContainsKey(cacheKeyName))
				{
					return null;
				}

				VaultValue value = cache[cacheKeyName];
				double timespan = (DateTime.UtcNow - value.WrittenAt).TotalMilliseconds;
				if (maxAge != null && timespan > maxAge * 1000)
				{
					return null;
				}

				return value;
			}
		}


		// Return cache dictionary
		public Dictionary<string, VaultValue> GetCache()
		{
			return cache;
		}


		// Clear out the cache entirely
		public void ClearCache()
		{
			lock ((object)cache)
			{
				cache.Clear();
			}
		}


		// Remove a vault value from the cache by it's topic and index
		public void DeleteCacheItem(string topic, JObject index)
		{
			DeleteCacheItem(CreateCacheKey(topic, index));
		}


		// Remove a vault value from the cache by it's cache key name
		public void DeleteCacheItem(string cacheKeyName)
		{
			lock ((object)cache)
			{
				Logger.Debug("Deleting cache item: " + cacheKeyName);
				if (!cache.ContainsKey(cacheKeyName))
				{
					return;
				}

				cache.Remove(cacheKeyName);
			}
		}


		////////////////////////////////////////////
		//        Vault Value Manipulation        //
		////////////////////////////////////////////
		private void ValueSetOrDelete(JObject info)
		{
			var topic = (string)info["key"]["topic"];
			var index = (JObject)info["key"]["index"];
			var rawValue = (JObject)info["value"];

			if (rawValue != null)
			{
				ValueSet(topic, index, rawValue["data"], (string)rawValue["mediaType"], (int?)info["expirationTime"]);
			}
			else
			{
				ValueDel(topic, index);
			}
		}

		private void ValueSet(string topic, JObject index, JToken data, string mediaType, int? expirationTime)
		{
			string cacheKeyName = CreateCacheKey(topic, index);
			VaultValue cacheValue;

			// NOTE: even though some of these operations lock already, we put them inside this
			// lock to ensure there is no time inconsistencies if things happen too fast.
			lock ((object)cache)
			{
				cacheValue = GetCacheValue(cacheKeyName);
				if (cacheValue == null)
				{
					// If it doesn't exist, create a new vault value
					cacheValue = new VaultValue(topic, index);
					cache.Add(cacheKeyName, cacheValue);
				}
				else
				{
					// If it exists delete existing value in preparation for set
					cacheValue.Del();
				}

				// Set data to vault value
				cacheValue.SetData(mediaType, data);
				cacheValue.Touch(expirationTime);
			}

			// Emit set event
			Emit(topic + ":set", cacheValue);
		}

		private void ValueAdd(string topic, JObject index, JToken data, string mediaType, int? expirationTime)
		{
			string cacheKeyName = CreateCacheKey(topic, index);
			VaultValue cacheValue;

			// NOTE: even though some of these operations lock already, we put them inside this
			// lock to ensure there is no time inconsistencies if things happen too fast.
			lock ((object)cache)
			{
				// Check if value already exists
				cacheValue = GetCacheValue(cacheKeyName);
				if (cacheValue != null)
				{
					Logger.Error("Could not add value (already exists): " + cacheKeyName);
					return;
				}

				// Create new vault value
				cacheValue = new VaultValue(topic, index);
				cache.Add(cacheKeyName, cacheValue);

				// Set data to vault value
				cacheValue.SetData(mediaType, data);
				cacheValue.Touch(expirationTime);
			}

			// Emit add event
			Emit(topic + ":add", cacheValue);
		}

		private void ValueDel(string topic, JObject index)
		{
			// Check if value already exists
			string cacheKeyName = CreateCacheKey(topic, index);
			VaultValue cacheValue = GetCacheValue(cacheKeyName);
			if (cacheValue == null)
			{
				Logger.Warning("Could not delete value (doesn't exist): " + cacheKeyName);
				return;
			}

			// Do delete
			cacheValue.Del();

			// Emit touch event
			Emit(topic + ":del", cacheValue);
		}

		private void ValueTouch(string topic, JObject index, int? expirationTime)
		{
			// Check if value already exists
			string cacheKeyName = CreateCacheKey(topic, index);
			VaultValue cacheValue = GetCacheValue(cacheKeyName);
			if (cacheValue == null)
			{
				Logger.Warning("Could not touch value (doesn't exist): " + cacheKeyName);
				return;
			}

			// Do touch
			cacheValue.Touch(expirationTime);

			// Emit touch event
			Emit(topic + ":touch", cacheValue);
		}

		private void ValueApplyDiff(string topic, JObject index, JArray diff, int? expirationTime)
		{
			// Make sure value exists
			string cacheKeyName = CreateCacheKey(topic, index);
			VaultValue cacheValue = GetCacheValue(cacheKeyName);
			if (cacheValue == null)
			{
				Logger.Warning("Got a diff for a non-existent value:" + cacheKeyName);
				return;
			}

			// Apply diff
			cacheValue.ApplyDiff(diff);
			cacheValue.Touch(expirationTime);

			// Emit applyDiff event
			Emit(topic + ":applyDiff", cacheValue);
		}


		////////////////////////////////////////////
		//            Raw Communication           //
		////////////////////////////////////////////
		private void RawGet(string topic, JObject index, Action<Exception, JToken> cb)
		{
			var parameters = new JObject();
			parameters.Add("topic", topic);
			parameters.Add("index", index);

			Mage.CommandCenter.SendCommand("archivist.rawGet", parameters, cb);
		}

		private void RawMGet(JToken queries, JObject options, Action<Exception, JToken> cb)
		{
			var parameters = new JObject();
			parameters.Add(new JProperty("queries", queries));
			parameters.Add("options", options);

			Mage.CommandCenter.SendCommand("archivist.rawMGet", parameters, cb);
		}

		private void RawList(string topic, JObject partialIndex, JObject options, Action<Exception, JToken> cb)
		{
			var parameters = new JObject();
			parameters.Add("topic", new JValue(topic));
			parameters.Add("partialIndex", partialIndex);
			parameters.Add("options", options);

			Mage.CommandCenter.SendCommand("archivist.rawList", parameters, cb);
		}

		private void RawSet(string topic, JObject index, JToken data, string mediaType, string encoding, string expirationTime, Action<Exception, JToken> cb)
		{
			var parameters = new JObject();
			parameters.Add("topic", new JValue(topic));
			parameters.Add("index", index);
			parameters.Add(new JProperty("data", data));
			parameters.Add("mediaType", new JValue(mediaType));
			parameters.Add("encoding", new JValue(encoding));
			parameters.Add("expirationTime", new JValue(expirationTime));

			Mage.CommandCenter.SendCommand("archivist.rawSet", parameters, cb);
		}

		private void RawDel(string topic, JObject index, Action<Exception, JToken> cb)
		{
			var parameters = new JObject();
			parameters.Add("topic", new JValue(topic));
			parameters.Add("index", index);

			Mage.CommandCenter.SendCommand("archivist.rawDel", parameters, cb);
		}


		////////////////////////////////////////////
		//           Exposed Operations           //
		////////////////////////////////////////////
		public void Get(string topic, JObject index, JObject options, Action<Exception, JToken> cb)
		{
			// Default options
			options = (options != null) ? options : new JObject();
			if (options["optional"] == null)
			{
				options.Add("optional", new JValue(false));
			}


			// Check cache
			string cacheKeyName = CreateCacheKey(topic, index);
			VaultValue cacheValue = GetCacheValue(cacheKeyName, (int?)options["maxAge"]);
			if (cacheValue != null)
			{
				cb(null, cacheValue.Data);
				return;
			}


			// Get data from server
			RawGet(topic, index, (error, result) => {
				if (error != null)
				{
					cb(error, null);
					return;
				}

				// Parse value
				try
				{
					ValueSetOrDelete((JObject)result);
				}
				catch (Exception cacheError)
				{
					cb(cacheError, null);
					return;
				}

				// Return result
				cb(null, GetCacheValue(cacheKeyName).Data);
			});
		}

		public void MGet(JArray queries, JObject options, Action<Exception, JToken> cb)
		{
			// Default options
			options = (options != null) ? options : new JObject();
			if (options["optional"] == null)
			{
				options.Add("optional", new JValue(false));
			}


			// Keep track of actual data we need from server
			var realQueries = new JArray();
			var realQueryKeys = new Dictionary<string, int>();
			var responseArray = new JArray();


			// Check cache
			foreach (JObject query in queries)
			{
				var topic = (string)query["topic"];
				var index = (JObject)query["index"];
				string cacheKeyName = CreateCacheKey(topic, index);
				VaultValue cacheValue = GetCacheValue(cacheKeyName, (int?)options["maxAge"]);
				if (cacheValue != null)
				{
					responseArray.Add(cacheValue.Data);
				}
				else
				{
					realQueryKeys.Add(cacheKeyName, responseArray.Count);
					responseArray.Add(null);
					realQueries.Add(query);
				}
			}


			// Check if any real queries exist
			if (realQueries.Count == 0)
			{
				cb(null, responseArray);
				return;
			}


			// Get data from server
			RawMGet(realQueries, options, (error, results) => {
				if (error != null)
				{
					cb(error, null);
					return;
				}

				try
				{
					var resultsArray = results as JArray;
					if (resultsArray == null)
					{
						throw new Exception("RawMGet returned non Array results: " + results);
					}

					foreach (JObject topicValue in resultsArray)
					{
						// Determine value cacheKeyName
						var topic = (string)topicValue["key"]["topic"];
						var index = (JObject)topicValue["key"]["index"];
						string cacheKeyName = CreateCacheKey(topic, index);

						// Set value to cache
						ValueSetOrDelete(topicValue);

						// Add value to response
						int responseKey = realQueryKeys[cacheKeyName];
						responseArray[responseKey].Replace(GetCacheValue(cacheKeyName).Data);
					}
				}
				catch (Exception cacheError)
				{
					cb(cacheError, null);
					return;
				}

				// Return result
				cb(null, responseArray);
			});
		}

		public void MGet(JObject queries, JObject options, Action<Exception, JToken> cb)
		{
			// Default options
			options = (options != null) ? options : new JObject();
			if (options["optional"] == null)
			{
				options.Add("optional", new JValue(false));
			}


			// Keep track of actual data we need from server
			var realQueries = new JObject();
			var realQueryKeys = new Dictionary<string, string>();
			var responseObject = new JObject();


			// Check cache
			foreach (KeyValuePair<string, JToken> query in queries)
			{
				string cacheKeyName = CreateCacheKey(query.Value["topic"].ToString(), query.Value["index"] as JObject);
				VaultValue cacheValue = GetCacheValue(cacheKeyName, (int?)options["maxAge"]);
				if (cacheValue != null)
				{
					responseObject.Add(query.Key, cacheValue.Data);
				}
				else
				{
					realQueryKeys.Add(cacheKeyName, query.Key);
					realQueries.Add(query.Key, query.Value);
				}
			}


			// Check if any real queries exist
			if (realQueries.Count == 0)
			{
				cb(null, responseObject);
				return;
			}


			// Get data from server
			RawMGet(realQueries, options, (error, results) => {
				if (error != null)
				{
					cb(error, null);
					return;
				}

				try
				{
					var resultsArray = results as JArray;
					if (resultsArray == null)
					{
						throw new Exception("RawMGet returned non Array results: " + results);
					}

					foreach (JObject topicValue in resultsArray)
					{
						// Determine value cacheKeyName
						string valueTopic = topicValue["key"]["topic"].ToString();
						var valueIndex = (JObject)topicValue["key"]["index"];
						string cacheKeyName = CreateCacheKey(valueTopic, valueIndex);

						// Set value to cache
						ValueSetOrDelete(topicValue);

						// Add value to response
						string responseKey = realQueryKeys[cacheKeyName];
						responseObject.Add(responseKey, GetCacheValue(cacheKeyName).Data);
					}
				}
				catch (Exception cacheError)
				{
					cb(cacheError, null);
					return;
				}

				// Return result
				cb(null, responseObject);
			});
		}

		public void List(string topic, JObject partialIndex, JObject options, Action<Exception, JToken> cb)
		{
			RawList(topic, partialIndex, options, cb);
		}
	}
}
