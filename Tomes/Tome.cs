using System;
using Newtonsoft.Json.Linq;

public class Tome {
	public delegate void OnChanged(JToken oldValue);
	public delegate void OnDestroy();
	public delegate void OnAdd(JToken key);
	public delegate void OnDel(JToken key);

	//
	public static JToken Conjure(JToken data, JToken root = null) {
		switch (data.Type) {
		case JTokenType.Array:
			return new TomeArray((JArray)data, root);
		case JTokenType.Object:
			return new TomeObject((JObject)data, root);
		default:
			return new TomeValue((JValue)data, root);
		}
	}

	//
	public static JToken PathValue(JToken data, JArray paths) {
		JToken value = data;
		foreach (JToken path in paths) {
			if (value.Type == JTokenType.Array) {
				value = value[(int)path];
				continue;
			}

			value = value[(string)path];
		}

		return value;
	}

	//
	public static void EmitParentChange(JToken parent) {
		switch (parent.Type) {
		case JTokenType.Array:
			TomeArray parentArray = parent as TomeArray;
			if (parentArray.onChanged != null) {
				parentArray.onChanged.Invoke(null);
			}
			break;
		case JTokenType.Object:
			TomeObject parentObject = parent as TomeObject;
			if (parentObject.onChanged != null) {
				parentObject.onChanged.Invoke(null);
			}
			break;
		default:
			throw new Exception("TomeValue cannot be a parent!");
			break;
		}
	}

	//
	public static void ApplyDiff(JToken root, JArray operations) {
		foreach (JObject operation in operations) {
			JToken value = PathValue(root, (JArray)operation["chain"]);

			string op = operation["op"].ToString();
			JToken val = operation["val"];

			try {
				switch (value.Type) {
				case JTokenType.Array:
					(value as TomeArray).ApplyOperation(op, val, root);
					break;
				case JTokenType.Object:
					(value as TomeObject).ApplyOperation(op, val, root);
					break;
				default:
					(value as TomeValue).ApplyOperation(op, val);
					break;
				}
			} catch (Exception diffError) {
				// TODO: NEED TO DECIDE IF TOMES SHOULD BE STANDALONE OR INSIDE MAGE.
				// e.g. should it depend on mage.logger or UnityEngine.Debug for logging?
				UnityEngine.Debug.LogError("Failed to apply diff operation:");
				UnityEngine.Debug.LogError(operation);
				UnityEngine.Debug.LogError(diffError);
				throw diffError;
			}
		}
	}
}
