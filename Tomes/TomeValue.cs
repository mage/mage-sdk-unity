using System;

using Newtonsoft.Json.Linq;

namespace Wizcorp.MageSDK.Tomes
{
	public class TomeValue : JValue
	{
		//
		public Tome.OnChanged OnChanged;
		public Tome.OnDestroy OnDestroy;

		//
		private JToken root;

		//
		public TomeValue(JValue value, JToken root) : base(value)
		{
			//
			this.root = root;
			if (this.root == null)
			{
				this.root = this;
			}

			//
			OnChanged += EmitToParents;
		}

		//
		private void EmitToParents(JToken oldValue)
		{
			if (!Equals(this, root))
			{
				Tome.EmitParentChange(Parent);
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
				if (OnDestroy != null)
				{
					OnDestroy.Invoke();
				}

				OnChanged = null;
				OnDestroy = null;
			}
		}

		//
		public void ApplyOperation(string op, JToken val)
		{
			lock ((object)this)
			{
				switch (op)
				{
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
