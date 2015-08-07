using System;

using Newtonsoft.Json.Linq;

public class VaultValue {
	private string _topic;
	private JObject _index;
	
	private JToken _data;
	private string _mediaType;
	private uint _expirationTime;


	//
	public VaultValue(JObject valueObject) {
		// Extract value data
		_topic = valueObject["key"]["topic"].ToString();
		_index = (JObject)valueObject["key"]["index"];

		_data = JToken.Parse(valueObject["value"]["data"].ToString());
		_mediaType = valueObject["value"]["mediaType"].ToString();
		// TODO: handle expiration time
		//_expirationTime = valueObject["value"]["expirationTime"];
	}


	// Property getters
	public string topic { get { return _topic; } }
	public JObject index { get { return _index; } }
	
	public JToken data { get { return _data; } }
	public string mediaType { get { return _mediaType; } }
	public uint expirationTime { get { return _expirationTime; } }


	//
	private JToken getProperty(JToken parent, JToken propertyName) {
		return getProperty (parent, propertyName.ToString());
	}

	private JToken getProperty(JToken parent, string propertyName) {
		if (parent.Type == JTokenType.Object) {
			return parent[propertyName];
		}

		if (parent.Type == JTokenType.Array) {
			return parent[int.Parse(propertyName)];
		}

		// Error
		return null;
	}


	//
	private void setProperty(JToken parent, JToken propertyName, JToken value) {
		setProperty (parent, propertyName.ToString(), value);
	}

	private void setProperty(JToken parent, string propertyName, JToken value) {
		if (parent.Type == JTokenType.Object) {
			parent[propertyName] = value;
			return;
		}

		if (parent.Type == JTokenType.Array) {
			int index = int.Parse(propertyName);
			JArray parentArray = parent as JArray;

			while ((parent as JArray).Count <= index) {
				parentArray.Add(null);
			}

			parentArray[index] = value;
			return;
		}

		// Error
	}


	//
	private void delProperty (JToken parent, JToken propertyName) {
		delProperty (parent, propertyName.ToString());
	}

	private void delProperty (JToken parent, string propertyName) {
		if (parent.Type == JTokenType.Object) {
			(parent as JObject).Remove(propertyName);
			return;
		}
		
		if (parent.Type == JTokenType.Array) {
			getProperty(parent, propertyName).Replace(null);
			return;
		}

		// Error
	}
	
	
	//
	private JToken resolvePath(JArray path) {
		JToken value = _data;
		foreach (JToken key in path) {
			value = getProperty(value, key);
		}
		return value;
	}


	// Applies a diff to the current object
	public void applyDiff(JArray operations) {
		foreach (JObject operation in operations) {
			// Traverse the key chain
			JToken value = resolvePath((JArray)operation["chain"]);

			// Apply the operation
			processOperation(value, operation);
		}
	}


	//
	private void processOperation(JToken value, JObject operation) {
		switch (operation["op"].ToString()) {
		case "assign":
			value.Replace(operation["val"]);
			break;
		case "set":
			setProperty(value, operation["val"]["key"], operation["val"]["val"]);
			break;
		case "del":
			delProperty(value, operation["val"]);
			break;
		case "move":
			JToken fromKey = operation["val"]["key"];
			JToken fromVal = getProperty(value, fromKey);
			
			JToken toParent = resolvePath((JArray)operation["val"]["newParent"]);
			JToken toKey = (operation["val"]["newKey"] != null) ? operation["val"]["newKey"] : operation["val"]["key"];
			
			setProperty(toParent, toKey, fromVal);
			delProperty(value, fromKey);
			break;
		case "rename":
			foreach (var property in operation["val"] as JObject) {
				string wasKey = property.Key;
				JToken wasValue = getProperty(value, wasKey);
				
				JToken isKey = property.Value;
				setProperty(value, isKey, wasValue);
				delProperty(value, wasKey);
			}
			break;
		case "swap":
			JToken firstKey = operation["val"]["key"];
			JToken firstValue = getProperty(value, firstKey);
			
			JToken secondKey = (operation["val"]["newKey"] != null) ? operation["val"]["newKey"] : operation["val"]["key"];
			JToken secondParent = resolvePath((JArray)operation["val"]["newParent"]);
			JToken secondValue = getProperty(secondParent, secondKey);
			
			firstValue.Replace(secondValue);
			secondValue.Replace(firstValue);
			break;
		case "push":
			foreach (JToken item in operation["val"] as JArray) {
				(value as JArray).Add(item);
			}
			break;
		case "pop":
			(value as JArray).Last.Remove();
			break;
		case "shift":
			(value as JArray).First.Remove();
			break;
		case "unshift":
			JArray items = operation["val"] as JArray;
			for (int i = items.Count; i > 0; i -= 1) {
				(value as JArray).AddFirst(items[i - 1]);
			}
			break;
		case "reverse":
			JArray oldOrder = new JArray(value as JArray);
			for (int i = oldOrder.Count; i > 0; i -= 1) {
				value[oldOrder.Count - i].Replace(oldOrder[i - 1]);
			}
			break;
		case "splice":
			int index = int.Parse(operation["val"][0].ToString());
			int deleteCount = int.Parse(operation["val"][1].ToString());
			for (int delI = index + deleteCount - 1; delI >= index; delI -= 1) {
				if (delI > (value as JArray).Count - 1) {
					continue;
				}
				
				(value as JArray).RemoveAt(delI);
			}
			for (int addI = 2; addI < (operation["val"] as JArray).Count; addI += 1) {
				int insertI = index + addI - 2;
				(value as JArray).Insert(insertI, operation["val"][addI]);
			}
			break;
		default:
			throw new Exception("VaultValue.applyDiff - Unsupported operation: " + operation);
			break;
		}

		UnityEngine.Debug.Log (value);
	}
}
 