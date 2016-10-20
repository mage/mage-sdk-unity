using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Wizcorp.MageSDK.MageClient;

namespace Wizcorp.MageSDK.Tomes
{
	public class TomeArray : JArray
	{
		//
		public Tome.OnChanged OnChanged;
		public Tome.OnDestroy OnDestroy;
		public Tome.OnAdd OnAdd;
		public Tome.OnDel OnDel;

		//
		private JToken root;

		//
		public TomeArray(JArray data, JToken root)
		{
			//
			this.root = root;
			if (this.root == null)
			{
				this.root = this;
			}

			//
			for (var i = 0; i < data.Count; i += 1)
			{
				Add(Tome.Conjure(data[i], root));
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
				foreach (JToken value in this)
				{
					Tome.Destroy(value);
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
		public void Set(int index, JToken value)
		{
			lock ((object)this)
			{
				// Make sure the property exists, filling in missing indexes
				if (Count <= index)
				{
					while (Count < index)
					{
						Add(Tome.Conjure(JValue.CreateNull(), root));
					}

					Add(Tome.Conjure(value, root));
					if (OnAdd != null)
					{
						OnAdd.Invoke(index);
					}
					return;
				}

				// Assign the property
				JToken property = this[index];
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
							Mage.Instance.Logger("Tomes").Data(property).Error("property is not a tome value: " + index.ToString());
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
		public void Del(int index)
		{
			lock ((object)this)
			{
				JToken property = this[index];
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
							Mage.Instance.Logger("Tomes").Data(property).Error("property is not a tome value:" + index.ToString());
							UnityEngine.Debug.Log(this);
						}
						else
						{
							tomeValue.Destroy();
						}
						break;
				}

				this[index].Replace(JValue.CreateNull());
				if (OnDel != null)
				{
					OnDel.Invoke(index);
				}
			}
		}

		//
		public void Move(int fromKey, JToken newParent, JToken newKey)
		{
			lock ((object)this)
			{
				if (newParent.Type == JTokenType.Array)
				{
					((TomeArray)newParent).Set((int)newKey, this[fromKey]);
				}
				else
				{
					((TomeObject)newParent).Set((string)newKey, this[fromKey]);
				}

				Del(fromKey);
			}
		}

		//
		public void Rename(int wasKey, int isKey)
		{
			lock ((object)this)
			{
				JToken wasValue = this[wasKey];
				Del(wasKey);
				Set(isKey, wasValue);
			}
		}

		//
		public void Swap(int firstKey, JToken secondParent, JToken secondKey)
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
		public void Push(JToken item)
		{
			lock ((object)this)
			{
				Set(Count, item);
			}
		}

		// NOTE: Tome behavior for a del operation is to replace values with null.
		// However a pop operation does in fact remove the item as well as
		// firing the del event. Thus we do both below.
		public JToken Pop()
		{
			lock ((object)this)
			{
				JToken last = this[Count - 1];
				Del(Count - 1);
				Last.Remove();
				return last;
			}
		}

		// NOTE: Tome behavior for a del operation is to replace values with null.
		// However a shift operation does in fact remove the item as well as
		// firing the del event. Thus we do both below.
		public JToken Shift()
		{
			lock ((object)this)
			{
				JToken first = this[0];
				Del(0);
				First.Remove();
				return first;
			}
		}

		//
		public void UnShift(JToken item)
		{
			lock ((object)this)
			{
				AddFirst(Tome.Conjure(item, root));
				if (OnAdd != null)
				{
					OnAdd.Invoke(0);
				}
			}
		}

		//
		public void Reverse()
		{
			lock ((object)this)
			{
				var oldOrder = new JArray(this);
				for (int i = oldOrder.Count; i > 0; i -= 1)
				{
					this[oldOrder.Count - i].Replace(oldOrder[i - 1]);
				}

				if (OnChanged != null)
				{
					OnChanged.Invoke(null);
				}
			}
		}

		//
		public void Splice(int index, int deleteCount, JArray insertItems)
		{
			lock ((object)this)
			{
				// Delete given item count starting at given index
				for (int delI = index + deleteCount - 1; delI >= index; delI -= 1)
				{
					if (delI > Count - 1)
					{
						continue;
					}

					Del(delI);
					this[delI].Remove();
				}

				// Insert given items starting at given index
				for (var addI = 0; addI < insertItems.Count; addI += 1)
				{
					int insertI = index + addI;
					Insert(insertI, Tome.Conjure(insertItems[addI]));
					if (OnAdd != null)
					{
						OnAdd.Invoke(insertI);
					}
				}
			}
		}


		// We implement this as when using JArray.IndexOf(JToken) it compares the reference but not the value.
		public int IndexOf(string lookFor)
		{
			lock ((object)this)
			{
				for (var i = 0; i < Count; i += 1)
				{
					JToken value = this[i];
					if (value.Type == JTokenType.String && (string)value == lookFor)
					{
						return i;
					}
				}

				return -1;
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
						Set((int)val["key"], val["val"]);
						break;

					case "del":
						Del((int)val);
						break;

					case "move":
						var fromKey = (int)val["key"];
						JToken newParent = Tome.PathValue(root, val["newParent"] as JArray);
						JToken toKey = (val["newKey"] != null) ? val["newKey"] : new JValue(fromKey);
						Move(fromKey, newParent, toKey);
						break;

					case "rename":
						foreach (KeyValuePair<string, JToken> property in (JObject)val)
						{
							int wasKey = int.Parse(property.Key);
							var isKey = (int)property.Value;
							Rename(wasKey, isKey);
						}
						break;

					case "swap":
						var firstKey = (int)val["key"];
						JToken secondParent = Tome.PathValue(root, val["newParent"] as JArray);
						JToken secondKey = (val["newKey"] != null) ? val["newKey"] : new JValue(firstKey);
						Swap(firstKey, secondParent, secondKey);
						break;

					case "push":
						foreach (JToken item in (JArray)val)
						{
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
						var unshiftItems = val as JArray;
						for (int i = unshiftItems.Count; i > 0; i -= 1)
						{
							UnShift(unshiftItems[i - 1]);
						}
						break;

					case "reverse":
						Reverse();
						break;

					case "splice":
						var index = (int)val[0];
						var deleteCount = (int)val[1];

						var items = new JArray(val as JArray);
						items.First.Remove();
						items.First.Remove();

						Splice(index, deleteCount, items);
						break;

					default:
						throw new Exception("TomeArray - Unsupported operation: " + op);
				}
			}
		}
	}
}
