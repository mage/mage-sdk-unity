using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.MageClient;

namespace Wizcorp.MageSDK.Tomes
{
	public class TomeObject : JObject
	{
		//
		public Tome.OnChanged OnChanged;
		public Tome.OnDestroy OnDestroy;
		public Tome.OnAdd OnAdd;
		public Tome.OnDel OnDel;

		//
		private JToken root;


		//
		public TomeObject(JObject data, JToken root)
		{
			//
			this.root = root;
			if (this.root == null)
			{
				this.root = this;
			}

			//
			foreach (JProperty property in data.Properties())
			{
				Add(property.Name, Tome.Conjure(property.Value, root));
			}

			//
			OnChanged += EmitToParents;
			OnAdd += EmitChanged;
			OnDel += EmitChanged;
		}

		//
		private void EmitToParents(JToken oldValue)
		{
			if (this != root)
			{
				Tome.EmitParentChange(Parent);
			}
		}

		//
		private void EmitChanged(JToken key)
		{
			if (OnChanged != null)
			{
				OnChanged.Invoke(null);
			}
		}


		//
		public void Assign(JToken newValue)
		{
			lock ((object)this)
			{
				switch (newValue.Type)
				{
					case JTokenType.Array:
						var newTomeArray = new TomeArray((JArray)newValue, root);
						Replace(newTomeArray);

						if (Parent == null)
						{
							// If replace was successfuly move over event handlers and call new OnChanged handler
							// The instance in which replace would not be successful, is when the old and new values are the same
							OnChanged -= EmitToParents;
							OnChanged += newTomeArray.OnChanged;
							newTomeArray.OnChanged = OnChanged;
							newTomeArray.OnDestroy = OnDestroy;
							OnAdd -= EmitChanged;
							OnAdd += newTomeArray.OnAdd;
							newTomeArray.OnAdd = OnAdd;
							OnDel -= EmitChanged;
							OnDel += newTomeArray.OnDel;
							newTomeArray.OnDel = OnDel;

							if (newTomeArray.OnChanged != null)
							{
								newTomeArray.OnChanged.Invoke(null);
							}
						}
						else
						{
							// Otherwise call original OnChanged handler
							if (OnChanged != null)
							{
								OnChanged.Invoke(null);
							}
						}
						break;
					case JTokenType.Object:
						var newTomeObject = new TomeObject((JObject)newValue, root);
						Replace(newTomeObject);

						if (Parent == null)
						{
							// If replace was successfuly move over event handlers and call new OnChanged handler
							// The instance in which replace would not be successful, is when the old and new values are the same
							OnChanged -= EmitToParents;
							OnChanged += newTomeObject.OnChanged;
							newTomeObject.OnChanged = OnChanged;
							newTomeObject.OnDestroy = OnDestroy;
							OnAdd -= EmitChanged;
							OnAdd += newTomeObject.OnAdd;
							newTomeObject.OnAdd = OnAdd;
							OnDel -= EmitChanged;
							OnDel += newTomeObject.OnDel;
							newTomeObject.OnDel = OnDel;

							if (newTomeObject.OnChanged != null)
							{
								newTomeObject.OnChanged.Invoke(null);
							}
						}
						else
						{
							// Otherwise call original OnChanged handler
							if (OnChanged != null)
							{
								OnChanged.Invoke(null);
							}
						}
						break;
					default:
						var newTomeValue = new TomeValue((JValue)newValue, root);
						Replace(newTomeValue);

						if (Parent == null)
						{
							// If replace was successfuly move over event handlers and call new OnChanged handler
							// The instance in which replace would not be successful, is when the old and new values are the same
							OnChanged -= EmitToParents;
							OnChanged += newTomeValue.OnChanged;
							newTomeValue.OnChanged = OnChanged;
							newTomeValue.OnDestroy = OnDestroy;

							if (newTomeValue.OnChanged != null)
							{
								newTomeValue.OnChanged.Invoke(null);
							}
						}
						else
						{
							// Otherwise call original OnChanged handler
							if (OnChanged != null)
							{
								OnChanged.Invoke(null);
							}
						}
						break;
				}
			}
		}

		//
		public void Destroy()
		{
			lock ((object)this)
			{
				foreach (KeyValuePair<string, JToken> property in this)
				{
					Tome.Destroy(property.Value);
				}

				if (OnDestroy != null)
				{
					OnDestroy.Invoke();
				}

				OnChanged = null;
				OnDestroy = null;
				OnAdd = null;
				OnDel = null;
			}
		}

		//
		public void Set(string propertyName, JToken value)
		{
			lock ((object)this)
			{
				// Make sure the property exists
				if (this[propertyName] == null)
				{
					Add(propertyName, Tome.Conjure(value, root));
					if (OnAdd != null)
					{
						OnAdd.Invoke(propertyName);
					}
					return;
				}

				// Assign the property
				JToken property = this[propertyName];
				switch (property.Type)
				{
					case JTokenType.Array:
						((TomeArray)property).Assign(value);
						break;
					case JTokenType.Object:
						((TomeObject)property).Assign(value);
						break;
					default:
						var tomeValue = property as TomeValue;
						if (tomeValue == null)
						{
							Mage.Instance.Logger("Tomes").Data(property).Error("property is not a tome value: " + propertyName);
							UnityEngine.Debug.Log(this);
						}
						else
						{
							tomeValue.Assign(value);
						}
						break;
				}
			}
		}

		//
		public void Del(string propertyName)
		{
			lock ((object)this)
			{
				JToken property = this[propertyName];
				switch (property.Type)
				{
					case JTokenType.Array:
						((TomeArray)property).Destroy();
						break;
					case JTokenType.Object:
						((TomeObject)property).Destroy();
						break;
					default:
						var tomeValue = property as TomeValue;
						if (tomeValue == null)
						{
							Mage.Instance.Logger("Tomes").Data(property).Error("property is not a tome value: " + propertyName);
							UnityEngine.Debug.Log(this);
						}
						else
						{
							tomeValue.Destroy();
						}
						break;
				}

				Remove(propertyName);
				if (OnDel != null)
				{
					OnDel.Invoke(propertyName);
				}
			}
		}

		//
		public void Move(string fromKey, JToken toParent, JToken toKey)
		{
			lock ((object)this)
			{
				if (toParent.Type == JTokenType.Array)
				{
					((TomeArray)toParent).Set((int)toKey, this[fromKey]);
				}
				else
				{
					((TomeObject)toParent).Set((string)toKey, this[fromKey]);
				}

				Del(fromKey);
			}
		}

		//
		public void Rename(string wasKey, string isKey)
		{
			lock ((object)this)
			{
				JToken wasValue = this[wasKey];
				Del(wasKey);
				Set(isKey, wasValue);
			}
		}

		//
		public void Swap(string firstKey, JToken secondParent, JToken secondKey)
		{
			lock ((object)this)
			{
				JToken secondValue;
				if (secondParent.Type == JTokenType.Array)
				{
					secondValue = secondParent[(int)secondKey];
					secondParent[(int)secondKey].Replace(this[firstKey]);
				}
				else
				{
					secondValue = secondParent[(string)secondKey];
					secondParent[(string)secondKey].Replace(this[firstKey]);
				}

				this[firstKey].Replace(secondValue);
				if (OnChanged != null)
				{
					OnChanged.Invoke(null);
				}
			}
		}


		//
		public void ApplyOperation(string op, JToken val, JToken root)
		{
			lock ((object)this)
			{
				switch (op)
				{
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
						var fromKey = (string)val["key"];
						JToken toParent = Tome.PathValue(root, val["newParent"] as JArray);
						JToken toKey = (val["newKey"] != null) ? val["newKey"] : new JValue(fromKey);
						Move(fromKey, toParent, toKey);
						break;

					case "rename":
						foreach (KeyValuePair<string, JToken> property in (JObject)val)
						{
							string wasKey = property.Key;
							var isKey = (string)property.Value;
							Rename(wasKey, isKey);
						}
						break;

					case "swap":
						var firstKey = (string)val["key"];
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
}
