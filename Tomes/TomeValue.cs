using System;
using Newtonsoft.Json.Linq;

public class TomeValue : JValue {
	//
	public Tome.OnChanged onChanged;
	public Tome.OnDestroy onDestroy;

	//
	private JToken root;


	//
	public TomeValue(JValue value, JToken _root) : base(value) {
		//
		root = _root;
		if (root == null) {
			root = this;
		}
		
		//
		onChanged += EmitToParents;
	}

	//
	private void EmitToParents(JToken oldValue) {
		if (this != root) {
			Tome.EmitParentChange(Parent);
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
	public void ApplyOperation(string op, JToken val) {
		switch (op) {
		case "assign":
			Assign(val);
			break;

		default:
			throw new Exception("TomeValue - Unsupported operation: " + op);
		}
	}
}