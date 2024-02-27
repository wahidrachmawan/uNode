using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	public class RuntimeGraphObject : UnityEngine.Object, IRuntimeClass, IInstancedGraph, IGetValue {
		private IInstancedGraph _instanced;
		private IRuntimeClass _target;
		public IRuntimeClass target {
			get => _target;
			set {
				_target = value;
				if(value is IInstancedGraph _instanced) {
					this._instanced = _instanced;
				}
			}
		}

		public RuntimeGraphObject(IRuntimeClass target) {
			this.target = target;
		}

		public string uniqueIdentifier => target.uniqueIdentifier;

		/// <summary>
		/// Make sure to redirect the value to target, use for `this` node to get the correct instance.
		/// </summary>
		/// <returns></returns>
		object IGetValue.Get() => target;

		public IGraph OriginalGraph {
			get {
				if(target is IInstancedGraph instancedGraph) {
					return instancedGraph.OriginalGraph;
				}
				return null;
			}
		}

		public GraphInstance Instance => _instanced?.Instance;

		public object GetProperty(string name) {
			return target.GetProperty(name);
		}

		public object GetVariable(string name) {
			return target.GetVariable(name);
		}

		public object InvokeFunction(string name, object[] values) {
			return target.InvokeFunction(name, values);
		}

		public object InvokeFunction(string name, Type[] parameters, object[] values) {
			return target.InvokeFunction(name, parameters, values);
		}

		public void SetProperty(string name, object value, char @operator) {
			target.SetProperty(name, value, @operator);
		}

		public void SetProperty(string name, object value) {
			target.SetProperty(name, value);
		}

		public void SetVariable(string name, object value, char @operator) {
			target.SetVariable(name, value, @operator);
		}

		public void SetVariable(string name, object value) {
			target.SetVariable(name, value);
		}
	}

	public class ClassObject : BaseRuntimeObject, IIcon, IRuntimeGraphWrapper {
		/// <summary>
		/// The target reference object
		/// </summary>
		[Tooltip("The target reference object")]
		public ClassDefinition target;

		/// <summary>
		/// The list of variable.
		/// </summary>
		[HideInInspector]
		public List<VariableData> variables = new List<VariableData>();

		#region Properties
		public RuntimeObject nativeInstance { get; private set; }
		public override string uniqueIdentifier => target != null ? target.FullGraphName : string.Empty;

		bool IRuntimeClassContainer.IsInitialized => Instance != null || nativeInstance != null;
		IRuntimeClass IRuntimeClassContainer.RuntimeClass {
			get {
				EnsureInitialized();
				if(nativeInstance != null) {
					return nativeInstance;
				} else {
					return this;
				}
			}
		}

		IEnumerable<Type> IRuntimeInterface.GetInterfaces() {
			if (target is IInterfaceSystem iface) {
				return iface.Interfaces.Select(i => i.type);
			}
			return Type.EmptyTypes;
		}

		IGraph IInstancedGraph.OriginalGraph => target;
		List<VariableData> IRuntimeGraphWrapper.WrappedVariables => variables;
		public GraphInstance Instance { get; private set; }
		#endregion

		#region Initialization
		public void EnsureInitialized() {
			if (Instance != null || nativeInstance != null)
				return;
			if (target == null) {
				throw new Exception("Target graph can't be null");
			}
			var type = target.GeneratedTypeName.ToType(false);
			if (type != null) {
				//Instance native c# graph
				var instance = Activator.CreateInstance(type) as RuntimeObject;
				nativeInstance = instance;
				//Initialize the references
				var references = target.scriptData.unityObjects;
				for(int i = 0; i < references.Count; i++) {
					SetVariable(references[i].name, references[i].value);
				}
				//Initialize the variable
				foreach(var v in target.GetAllVariables()) {
					object value = v.defaultValue;
					for(int x = 0; x < variables.Count; x++) {
						if(v.name.Equals(variables[x].name)) {
							value = variables[x].Get();
							break;
						}
					}
					SetVariable(v.name, value);
				}
			}
			else {
				//Instance reflection graph
				Instance = RuntimeGraphUtility.InitializeObjectGraph(target, this, variables);
			}
		}

		void IRuntimeClassContainer.ResetInitialization() {
			Instance = null;
			nativeInstance = null;
		}
		#endregion

		public override object InvokeFunction(string name, object[] values) {
			EnsureInitialized();
			if (nativeInstance != null) {
				return nativeInstance.InvokeFunction(name, values);
			}
			return uNodeHelper.InvokeFunction(this, name, values);
		}

		public override object InvokeFunction(string name, Type[] parameters, object[] values) {
			EnsureInitialized();
			if (nativeInstance != null) {
				return nativeInstance.InvokeFunction(name, parameters, values);
			}
			return uNodeHelper.InvokeFunction(this, name, parameters, values);
		}

		public override void SetVariable(string name, object value) {
			EnsureInitialized();
			if (nativeInstance != null) {
				nativeInstance.SetVariable(name, value);
			}
			else
				uNodeHelper.SetVariable(this, name, value);
		}

		public override void SetVariable(string name, object value, char @operator) {
			EnsureInitialized();
			if (nativeInstance != null) {
				nativeInstance.SetVariable(name, value, @operator);
			}
			else
				uNodeHelper.SetVariable(this, name, value, @operator);
		}

		public override object GetVariable(string name) {
			EnsureInitialized();
			if (nativeInstance != null) {
				return nativeInstance.GetVariable(name);
			}
			return uNodeHelper.GetVariable(this, name);
		}

		public override void SetProperty(string name, object value) {
			EnsureInitialized();
			if (nativeInstance != null) {
				nativeInstance.SetProperty(name, value);
			}
			else
				uNodeHelper.SetProperty(this, name, value);
		}

		public override void SetProperty(string name, object value, char @operator) {
			EnsureInitialized();
			if (nativeInstance != null) {
				nativeInstance.SetProperty(name, value, @operator);
			}
			else {
				uNodeHelper.SetProperty(this, name, value, @operator);
			}
		}

		public override object GetProperty(string name) {
			EnsureInitialized();
			if (nativeInstance != null) {
				return nativeInstance.GetProperty(name);
			}
			return uNodeHelper.GetProperty(this, name);
		}

		public Type GetIcon() {
			return target?.GetIcon() ?? typeof(TypeIcons.RuntimeTypeIcon);
		}
	}
}