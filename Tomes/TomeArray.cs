using System;
using Newtonsoft.Json.Linq;

public class TomeArray : JArray {
	//
	public Tome.OnChanged onChanged;
	public Tome.OnDestroy onDestroy;
	public Tome.OnAdd onAdd;
	public Tome.OnDel onDel;
	
	//
	private JToken root;


	//
	public TomeArray(JArray data, JToken _root) {
		//
		root = _root;
		if (root == null) {
			root = this;
		}

		//
		for (int i = 0; i < data.Count; i += 1) {
			this.Add(Tome.Conjure(data[i], root));
		}
		
		//
		onChanged += EmitToParents;
		onAdd += EmitChanged;
		onDel += EmitChanged;
	}
	
	//
	private void EmitToParents(JToken oldValue) {
		if (this != root) {
			Tome.EmitParentChange(Parent.Parent);
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
		switch (newValue.Type) {
		case JTokenType.Array:
			TomeArray newTomeArray = new TomeArray((JArray)newValue, root);
			this.Replace(newTomeArray);

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
			break;
		case JTokenType.Object:
			TomeObject newTomeObject = new TomeObject((JObject)newValue, root);
			this.Replace(newTomeObject);

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
			break;
		default:
			TomeValue newTomeValue = new TomeValue((JValue)newValue, root);
			this.Replace(newTomeValue);

			onChanged -= EmitToParents;
			onChanged += newTomeValue.onChanged;
			newTomeValue.onChanged = onChanged;
			newTomeValue.onDestroy = onDestroy;

			if (newTomeValue.onChanged != null) {
				newTomeValue.onChanged.Invoke(null);
			}
			break;
		}
	}
	
	//
	public void Destroy() {
		if (onDestroy != null) {
			onDestroy.Invoke();
		}
	}
	
	//
	public void Set(int index, JToken value) {
		// Make sure the property exists, filling in missing indexes
		if (this.Count <= index) {
			while (this.Count < index) {
				this.Add(Tome.Conjure(JValue.CreateNull(), root));
			}

			this.Add(Tome.Conjure(value, root));
			if (onAdd != null) {
				onAdd.Invoke(index);
			}
			return;
		}

		// Assign the property
		JToken property = this[index];
		switch (property.Type) {
		case JTokenType.Array:
			(property as TomeArray).Assign(value);
			break;
		case JTokenType.Object:
			(property as TomeObject).Assign(value);
			break;
		default:
			(property as TomeValue).Assign(value);
			break;
		}
	}
	
	//
	public void Del(int index) {
		JToken item = this[index];
		switch (item.Type) {
		case JTokenType.Array:
			(item as TomeArray).Destroy();
			break;
		case JTokenType.Object:
			(item as TomeObject).Destroy();
			break;
		default:
			(item as TomeValue).Destroy();
			break;
		}

		this[index].Replace(JValue.CreateNull());
		if (onDel != null) {
			onDel.Invoke(index);
		}
	}
	
	//
	public void Move(int fromKey, JToken newParent, JToken newKey) {
		if (newParent.Type == JTokenType.Array) {
			(newParent as TomeArray).Set((int)newKey, this[fromKey]);
		} else {
			(newParent as TomeObject).Set((string)newKey, this[fromKey]);
		}
		
		Del(fromKey);
	}
	
	//
	public void Rename(int wasKey, int isKey) {
		JToken wasValue = this[wasKey];
		Del(wasKey);
		Set(isKey, wasValue);
	}
	
	//
	public void Swap(int firstKey, JToken secondParent, JToken secondKey) {
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

	//
	public void Push(JToken item) {
		this.Set(this.Count, Tome.Conjure(item, root));
	}

	//
	public void Pop() {
		this.Del(this.Count - 1);
		this.Last.Remove();
	}

	//
	public void Shift() {
		this.Del(0);
		this.First.Remove();
	}

	//
	public void UnShift(JToken item) {
		this.AddFirst(Tome.Conjure(item, root));
		if (onAdd != null) {
			onAdd.Invoke(0);
		}
	}

	//
	public void Reverse() {
		JArray oldOrder = new JArray(this as JArray);
		for (int i = oldOrder.Count; i > 0; i -= 1) {
			this[oldOrder.Count - i].Replace(oldOrder[i - 1]);
		}

		if (onChanged != null) {
			onChanged.Invoke(null);
		}
	}

	//
	public void Splice(int index, int deleteCount, JArray insertItems) {
		// Delete given item count starting at given index
		for (int delI = index + deleteCount - 1; delI >= index; delI -= 1) {
			if (delI > this.Count - 1) {
				continue;
			}

			Del(delI);
			this[delI].Remove();
		}

		// Insert given items starting at given index
		for (int addI = 0; addI < insertItems.Count; addI += 1) {
			int insertI = index + addI;
			this.Insert(insertI, Tome.Conjure(insertItems[addI]));
			if (onAdd != null) {
				onAdd.Invoke(insertI);
			}
		}
	}


	// We implement this as when using JArray.IndexOf(JToken) it compares the reference but not the value.
	public int IndexOf(string lookFor) {
		for (int i = 0; i < this.Count; i += 1) {
			JToken value = this[i];
			if (value.Type == JTokenType.String && (string)value == lookFor) {
				return i;
			}
		}

		return -1;
	}


	//
	public void ApplyOperation(string op, JToken val, JToken root) {
		switch (op) {
		case "assign":
			Assign(val);
			break;

		case "set":
			Set((int)val["key"], val["val"]);
			break;

		case "del":
			Del((int)val);
			break;

		case "move":
			int fromKey = (int)val["key"];
			JToken newParent = Tome.PathValue(root, val["newParent"] as JArray);
			JToken toKey = (val["newKey"] != null) ? val["newKey"] : new JValue(fromKey);
			Move(fromKey, newParent, toKey);
			break;

		case "rename":
			foreach (var property in val as JObject) {
				int wasKey = int.Parse(property.Key);
				int isKey = (int)property.Value;
				Rename(wasKey, isKey);
			}
			break;

		case "swap":
			int firstKey = (int)val["key"];
			JToken secondParent = Tome.PathValue(root, val["newParent"] as JArray);
			JToken secondKey = (val["newKey"] != null) ? val["newKey"] : new JValue(firstKey);
			Swap(firstKey, secondParent, secondKey);
			break;

		case "push":
			foreach (JToken item in val as JArray) {
				Push(item);
			}
			break;

		case "pop":
			Pop();
			break;

		case "shift":
			Shift();
			break;

		case "unshift":
			JArray unshiftItems = val as JArray;
			for (int i = unshiftItems.Count; i > 0; i -= 1) {
				UnShift(unshiftItems[i - 1]);
			}
			break;

		case "reverse":
			Reverse();
			break;

		case "splice":
			int index = (int)val[0];
			int deleteCount = (int)val[1];

			JArray items = new JArray(val as JArray);
			items.First.Remove();
			items.First.Remove();

			Splice(index, deleteCount, items);
			break;

		default:
			throw new Exception("TomeArray - Unsupported operation: " + op);
		}
	}
}