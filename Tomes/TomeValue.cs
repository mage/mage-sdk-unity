using System;

using Newtonsoft.Json.Linq;


namespace Wizcorp.MageSDK.Tomes {
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
			lock ((object)this) {
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
			lock ((object)this) {
				if (onDestroy != null) {
					onDestroy.Invoke();
				}

				onChanged = null;
				onDestroy = null;
			}
		}


		//
		public void ApplyOperation(string op, JToken val) {
			lock ((object)this) {
				switch (op) {
					case "assign":
						Assign(val);
						break;

					default:
						throw new Exception("TomeValue - Unsupported operation: " + op);
				}
			}
		}
	}
}
