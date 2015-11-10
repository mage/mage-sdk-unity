using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class TomeObject : JObject {
	//
	public Tome.OnChanged onChanged;
	public Tome.OnDestroy onDestroy;
	public Tome.OnAdd onAdd;
	public Tome.OnDel onDel;
	
	//
	private JToken root;


	//
	public TomeObject(JObject data, JToken _root) {
		//
		root = _root;
		if (root == null) {
			root = this;
		}
		
		//
		foreach (JProperty property in data.Properties()) {
			this.Add(property.Name, Tome.Conjure(property.Value, root));
		}

		//
		onChanged += EmitToParents;
		onAdd += EmitChanged;
		onDel += EmitChanged;
	}
	
	//
	private void EmitToParents(JToken oldValue) {
		if (this != root) {
			Tome.EmitParentChange(Parent);
		}
	}
	
	//
	private void EmitChanged(JToken key) {
		if (onChanged != null) {
			onChanged.Invoke(null);
		}
	}


	//
	public void Assign(JToken newValue) {
		lock((object)this) {
			switch (newValue.Type) {
			case JTokenType.Array:
				TomeArray newTomeArray = new TomeArray((JArray)newValue, root);
				this.Replace(newTomeArray);

				if (this.Parent == null) {
					// If replace was successfuly move over event handlers and call new onChanged handler
					// The instance in which replace would not be successful, is when the old and new values are the same
					onChanged -= EmitToParents;
					onChanged += newTomeArray.onChanged;
					newTomeArray.onChanged = onChanged;
					newTomeArray.onDestroy = onDestroy;
					onAdd -= EmitChanged;
					onAdd += newTomeArray.onAdd;
					newTomeArray.onAdd = onAdd;
					onDel -= EmitChanged;
					onDel += newTomeArray.onDel;
					newTomeArray.onDel = onDel;

					if (newTomeArray.onChanged != null) {
						newTomeArray.onChanged.Invoke(null);
					}
				} else {
					// Otherwise call original onChanged handler
					if (onChanged != null) {
						onChanged.Invoke(null);
					}
				}
				break;
			case JTokenType.Object:
				TomeObject newTomeObject = new TomeObject((JObject)newValue, root);
				this.Replace(newTomeObject);

				if (this.Parent == null) {
					// If replace was successfuly move over event handlers and call new onChanged handler
					// The instance in which replace would not be successful, is when the old and new values are the same
					onChanged -= EmitToParents;
					onChanged += newTomeObject.onChanged;
					newTomeObject.onChanged = onChanged;
					newTomeObject.onDestroy = onDestroy;
					onAdd -= EmitChanged;
					onAdd += newTomeObject.onAdd;
					newTomeObject.onAdd = onAdd;
					onDel -= EmitChanged;
					onDel += newTomeObject.onDel;
					newTomeObject.onDel = onDel;

					if (newTomeObject.onChanged != null) {
						newTomeObject.onChanged.Invoke(null);
					}
				} else {
					// Otherwise call original onChanged handler
					if (onChanged != null) {
						onChanged.Invoke(null);
					}
				}
				break;
			default:
				TomeValue newTomeValue = new TomeValue((JValue)newValue, root);
				this.Replace(newTomeValue);

				if (this.Parent == null) {
					// If replace was successfuly move over event handlers and call new onChanged handler
					// The instance in which replace would not be successful, is when the old and new values are the same
					onChanged -= EmitToParents;
					onChanged += newTomeValue.onChanged;
					newTomeValue.onChanged = onChanged;
					newTomeValue.onDestroy = onDestroy;

					if (newTomeValue.onChanged != null) {
						newTomeValue.onChanged.Invoke(null);
					}
				} else {
					// Otherwise call original onChanged handler
					if (onChanged != null) {
						onChanged.Invoke(null);
					}
				}
				break;
			}
		}
	}
	
	//
	public void Destroy() {
		lock((object)this) {
			foreach (var property in this) {
				Tome.Destroy(property.Value);
			}

			if (onDestroy != null) {
				onDestroy.Invoke();
			}

			onChanged = null;
			onDestroy = null;
			onAdd = null;
			onDel = null;
		}
	}
	
	//
	public void Set(string propertyName, JToken value) {
		lock((object)this) {
			// Make sure the property exists
			if (this[propertyName] == null) {
				this.Add(propertyName, Tome.Conjure(value, root));
				if (onAdd != null) {
					onAdd.Invoke(propertyName);
				}
				return;
			}

			// Assign the property
			JToken property = this[propertyName];
			switch (property.Type) {
			case JTokenType.Array:
				(property as TomeArray).Assign(value);
				break;
			case JTokenType.Object:
				(property as TomeObject).Assign(value);
				break;
			default:
				if ((property as TomeValue) == null) {
					Mage.Instance.logger("Tomes").data(property).error("property is not a tome value: " + propertyName.ToString());
					UnityEngine.Debug.Log(this);
				}
				(property as TomeValue).Assign(value);
				break;
			}
		}
	}

	//
	public void Del(string propertyName) {
		lock((object)this) {
			JToken property = this[propertyName];
			switch (property.Type) {
			case JTokenType.Array:
				(property as TomeArray).Destroy();
				break;
			case JTokenType.Object:
				(property as TomeObject).Destroy();
				break;
			default:
				if ((property as TomeValue) == null) {
					Mage.Instance.logger("Tomes").data(property).error("property is not a tome value: " + propertyName.ToString());
					UnityEngine.Debug.Log(this);
				}
				(property as TomeValue).Destroy();
				break;
			}

			this.Remove(propertyName);
			if (onDel != null) {
				onDel.Invoke(propertyName);
			}
		}
	}

	//
	public void Move(string fromKey, JToken toParent, JToken toKey) {
		lock((object)this) {
			if (toParent.Type == JTokenType.Array) {
				(toParent as TomeArray).Set((int)toKey, this[fromKey]);
			} else {
				(toParent as TomeObject).Set((string)toKey, this[fromKey]);
			}

			Del(fromKey);
		}
	}

	//
	public void Rename(string wasKey, string isKey) {
		lock((object)this) {
			JToken wasValue = this[wasKey];
			Del(wasKey);
			Set(isKey, wasValue);
		}
	}

	//
	public void Swap(string firstKey, JToken secondParent, JToken secondKey) {
		lock((object)this) {
			JToken secondValue;
			if (secondParent.Type == JTokenType.Array) {
				secondValue = secondParent[(int)secondKey];
				secondParent[(int)secondKey].Replace(this[firstKey]);
			} else {
				secondValue = secondParent[(string)secondKey];
				secondParent[(string)secondKey].Replace(this[firstKey]);
			}

			this[firstKey].Replace(secondValue);
			if (onChanged != null) {
				onChanged.Invoke(null);
			}
		}
	}


	//
	public void ApplyOperation(string op, JToken val, JToken root) {
		lock((object)this) {
			switch (op) {
			case "assign":
				Assign(val);
				break;

			case "set":
				Set((string)val["key"], val["val"]);
				break;

			case "del":
				Del((string)val);
				break;

			case "move":
				string fromKey = (string)val["key"];
				JToken toParent = Tome.PathValue(root, val["newParent"] as JArray);
				JToken toKey = (val["newKey"] != null) ? val["newKey"] : new JValue(fromKey);
				Move(fromKey, toParent, toKey);
				break;

			case "rename":
				foreach (var property in val as JObject) {
					string wasKey = property.Key;
					string isKey = (string)property.Value;
					Rename(wasKey, isKey);
				}
				break;

			case "swap":
				string firstKey = (string)val["key"];
				JToken secondParent = Tome.PathValue(root, val["newParent"] as JArray);
				JToken secondKey = (val["newKey"] != null) ? val["newKey"] : new JValue(firstKey);
				Swap(firstKey, secondParent, secondKey);
				break;

			default:
				throw new Exception("TomeObject - Unsupported operation: " + op);
			}
		}
	}
}