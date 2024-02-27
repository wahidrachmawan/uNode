using System;
using UnityEngine;

namespace MaxyGames.UNode {
	#region Base Classes
	public abstract class BaseRuntimeBehaviour : MonoBehaviour, IRuntimeClass {
		public abstract string uniqueIdentifier { get; }

		public abstract object GetProperty(string name);
		public abstract object GetVariable(string name);
		public virtual T GetProperty<T>(string name) {
			var value = GetProperty(name);
			if(!object.ReferenceEquals(value, null)) {
				return (T)value;
			}
			return default;
		}
		public virtual T GetVariable<T>(string name) {
			var value = GetVariable(name);
			if(!object.ReferenceEquals(value, null)) {
				return (T)value;
			}
			return default;
		}

		/// <summary>
		/// Execute function by name without parameter
		/// </summary>
		/// <param name="name"></param>
		public void ExecuteFunction(string name) {
			InvokeFunction(name, Array.Empty<object>());
		}

		public abstract object InvokeFunction(string name, object[] values);
		public abstract object InvokeFunction(string name, Type[] parameters, object[] values);

		public abstract void SetProperty(string name, object value);
		public abstract void SetProperty(string name, object value, char @operator);

		public abstract void SetVariable(string name, object value);
		public abstract void SetVariable(string name, object value, char @operator);
		public override bool Equals(object other) {
			if(other is BaseRuntimeBehaviour runtime) {
				return uNodeHelper.CompareRuntimeObject(this, runtime);
			}
			return base.Equals(other);
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public static bool operator ==(BaseRuntimeBehaviour x, BaseRuntimeBehaviour y) {
			return uNodeHelper.CompareRuntimeObject(x, y);
		}

		public static bool operator !=(BaseRuntimeBehaviour x, BaseRuntimeBehaviour y) {
			return !uNodeHelper.CompareRuntimeObject(x, y);
		}
	}
	#endregion

	public abstract class RuntimeBehaviour : BaseRuntimeBehaviour {
		private string m_uniqueIdentifier;
		public override string uniqueIdentifier {
			get {
				if(m_uniqueIdentifier == null) {
					m_uniqueIdentifier = this.GetType().FullName;
				}
				return m_uniqueIdentifier;
			}
		}

		public virtual void OnAwake() { }

		public virtual void OnBehaviourEnable() { }

		/// <summary>
		/// Execute function without parameters
		/// </summary>
		/// <param name="name"></param>
		public void InvokeFunction(string name) {
			var func = this.GetType().GetMethod(name, Type.EmptyTypes);
			if(func != null) {
				func.InvokeOptimized(this, null);
			}
		}

		public override void SetVariable(string name, object value) {
			uNodeHelper.RuntimeUtility.SetVariable(this, name, value);
		}

		public override void SetVariable(string name, object value, char @operator) {
			uNodeHelper.RuntimeUtility.SetVariable(this, name, value, @operator);
		}

		public override object GetVariable(string name) {
			return uNodeHelper.RuntimeUtility.GetVariable(this, name);
		}

		public override object GetProperty(string name) {
			return uNodeHelper.RuntimeUtility.GetProperty(this, name);
		}

		public override void SetProperty(string name, object value) {
			uNodeHelper.RuntimeUtility.SetProperty(this, name, value);
		}
		
		public override void SetProperty(string name, object value, char @operator) {
			uNodeHelper.RuntimeUtility.SetProperty(this, name, value, @operator);
		}

		public override object InvokeFunction(string name, object[] values) {
			return uNodeHelper.RuntimeUtility.InvokeFunction(this, name, values);
		}

		public override object InvokeFunction(string name, Type[] parameters, object[] values) {
			return uNodeHelper.RuntimeUtility.InvokeFunction(this, name, parameters, values);
		}
	}
}