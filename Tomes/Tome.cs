using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.Tomes
{
	public class Tome
	{
		public delegate void OnAdd(JToken key);

		public delegate void OnChanged(JToken oldValue);

		public delegate void OnDel(JToken key);

		public delegate void OnDestroy();

		//
		public static JToken Conjure(JToken data, JToken root = null)
		{
			switch (data.Type)
			{
				case JTokenType.Array:
					return new TomeArray((JArray)data, root);
				case JTokenType.Object:
					return new TomeObject((JObject)data, root);
				default:
					return new TomeValue((JValue)data, root);
			}
		}

		//
		public static void Destroy(JToken data)
		{
			switch (data.Type)
			{
				case JTokenType.Array:
					var tomeArray = data as TomeArray;
					if (tomeArray != null)
					{
						tomeArray.Destroy();
					}
					break;
				case JTokenType.Object:
					var tomeObject = data as TomeObject;
					if (tomeObject != null)
					{
						tomeObject.Destroy();
					}
					break;
				default:
					var tomeValue = data as TomeValue;
					if (tomeValue != null)
					{
						tomeValue.Destroy();
					}
					break;
			}
		}

		//
		public static JToken PathValue(JToken value, JArray paths)
		{
			foreach (JToken path in paths)
			{
				value = PathValue(value, path);
			}

			return value;
		}

		//
		public static JToken PathValue(JToken value, List<string> paths)
		{
			foreach (string path in paths)
			{
				value = PathValue(value, path);
			}

			return value;
		}

		//
		public static JToken PathValue(JToken value, JToken key)
		{
			if (value.Type == JTokenType.Array)
			{
				return value[(int)key];
			}

			return value[(string)key];
		}

		//
		public static JToken PathValue(JToken value, string key)
		{
			if (value.Type == JTokenType.Array)
			{
				return value[int.Parse(key)];
			}

			return value[key];
		}

		//
		public static void EmitParentChange(JToken parent)
		{
			switch (parent.Type)
			{
				case JTokenType.Array:
					var parentArray = parent as TomeArray;
					if (parentArray != null && parentArray.OnChanged != null)
					{
						parentArray.OnChanged.Invoke(null);
					}
					break;
				case JTokenType.Object:
					var parentObject = parent as TomeObject;
					if (parentObject != null && parentObject.OnChanged != null)
					{
						parentObject.OnChanged.Invoke(null);
					}
					break;
				case JTokenType.Property:
					EmitParentChange(parent.Parent);
					break;
				default:
					throw new Exception(parent.Type.ToString() + " cannot be a parent!");
			}
		}

		//
		public static void ApplyDiff(JToken root, JArray operations)
		{
			foreach (var jToken in operations)
			{
				var operation = (JObject)jToken;
				try
				{
					JToken value = PathValue(root, (JArray)operation["chain"]);

					string op = operation["op"].ToString();
					JToken val = operation["val"];

					switch (value.Type)
					{
						case JTokenType.Array:
							var tomeArray = value as TomeArray;
							if (tomeArray != null)
							{
								tomeArray.ApplyOperation(op, val, root);
							}
							break;
						case JTokenType.Object:
							var tomeObject = value as TomeObject;
							if (tomeObject != null)
							{
								tomeObject.ApplyOperation(op, val, root);
							}
							break;
						default:
							var tomeValue = value as TomeValue;
							if (tomeValue != null)
							{
								tomeValue.ApplyOperation(op, val);
							}
							break;
					}
				}
				catch (Exception diffError)
				{
					// TODO: NEED TO DECIDE IF TOMES SHOULD BE STANDALONE OR INSIDE MAGE.
					// e.g. should it depend on mage.logger or UnityEngine.Debug for logging?
					UnityEngine.Debug.LogError("Failed to apply diff operation:");
					UnityEngine.Debug.LogError(operation);
					UnityEngine.Debug.LogError(diffError);
					UnityEngine.Debug.LogError(root);
					UnityEngine.Debug.LogError(PathValue(root, (JArray)operation["chain"]));
					throw;
				}
			}
		}
	}
}