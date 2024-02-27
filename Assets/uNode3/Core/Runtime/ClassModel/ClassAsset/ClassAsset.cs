using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
    public class ClassAsset : BaseRuntimeAsset, IRuntimeAsset, IIcon, IRuntimeGraphWrapper, IRefreshable {
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
		public RuntimeAsset nativeInstance { get; private set; }
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
			if (!Application.isPlaying) {
				throw new Exception("Can't initialize graph instance when not playing.");
			}
			if (target == null) {
				throw new Exception("Target graph can't be null");
			}
			var type = target.GeneratedTypeName.ToType(false);
			if (type != null) {
				//Instance native c# graph, native graph will call Awake immediately
				var instance = ScriptableObject.CreateInstance(type) as RuntimeAsset;
#if UNITY_EDITOR
				instance.name = name;
#endif
				nativeInstance = instance;
				//Initialize the references
				var references = target.scriptData.unityObjects;
				for(int i = 0; i < references.Count; i++) {
					SetVariable(references[i].name, references[i].value);
				}
				//Initialize the variable
				uNodeHelper.RuntimeUtility.InitializeVariables(this, target, variables);
				//Call awake
				instance.OnAwake();
			}
			else {
				//Instance reflection graph
				Instance = RuntimeGraphUtility.InitializeAssetGraph(target, this, variables);
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

		public void Refresh() {
			if (!Application.isPlaying) {
				Instance = null;
				nativeInstance = null;
			}
		}
	}
}