using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode {
    [AddComponentMenu("")]
    public sealed class RuntimeInstancedGraph : BaseRuntimeBehaviour, IRuntimeGraph, IInstancedGraph {
		#region Properties
		public override string uniqueIdentifier => GetHashCode().ToString();

		public RuntimeBehaviour nativeInstance { get; private set; }

		public IGraph OriginalGraph { get; private set; }
		public Graph GraphData => OriginalGraph?.GraphData;

		private GraphInstance m_instance;
		public GraphInstance Instance => m_instance;
		#endregion

		#region Initialization
		public void Initialize(IGraph graph) {
			this.hideFlags = HideFlags.DontSave;
			OriginalGraph = graph;
			EnsureInitialized();
		}

		private bool hasInitialize;
		void EnsureInitialized() {
			if(hasInitialize || OriginalGraph == null) {
				return;
			}
			hasInitialize = true;
			if(!Application.isPlaying) {
				throw new System.Exception("Can't initialize graph instance when not playing.");
			}
			//Instance reflection graph
			m_instance = RuntimeGraphUtility.InitializeComponentGraph(OriginalGraph, this);
			m_instance.eventData.onAwake?.Invoke(Instance);
		}
		#endregion

		#region Unity Events
		void Start() {
			EnsureInitialized();
			if(m_instance != null) {
				m_instance.eventData.onStart?.Invoke(Instance);
			}
		}

		void OnEnable() {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.enabled = true;
				nativeInstance.OnBehaviourEnable();
			}
			else if(m_instance != null) {
				m_instance.eventData.onEnable?.Invoke(Instance);
			}
		}

		void OnDisable() {
			//if(target is uNodeComponentSingleton singleton && singleton.IsPersistence) {
			//	//If the target graph is a persistence singleton then we don't need to disable it as it might cause a bugs.
			//	return;
			//}
			if(nativeInstance != null) {
				nativeInstance.enabled = false;
			}
			else if(m_instance != null) {
				m_instance.eventData.onDisable?.Invoke(Instance);
			}
		}
		#endregion

		#region Functions
		public override object GetProperty(string name) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.GetProperty(name);
			}
			var reference = Instance.graph.GetProperty(name);
			if(reference == null) {
				throw new Exception($"Property: {name} was not found on graph: {OriginalGraph}");
			}
			return reference.Get(Instance);
		}

		public override object GetVariable(string name) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.GetVariable(name);
			}
			var reference = Instance.graph.GetVariable(name);
			if(reference == null) {
				throw new Exception($"Variable: {name} was not found on graph: {OriginalGraph}");
			}
			return reference.Get(Instance);
		}

		public override object InvokeFunction(string name, object[] values) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.InvokeFunction(name, values);
			}
			Type[] parameters = new Type[values != null ? values.Length : 0];
			if(values != null) {
				for(int i = 0; i < parameters.Length; i++) {
					parameters[i] = values[i] != null ? values[i].GetType() : typeof(object);
				}
				for(int i = 0; i < values.Length; i++) {
					values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
				}
			}
			var reference = Instance.graph.GetFunction(name, parameters);
			if(reference == null) {
				throw new Exception($"Function: {name} was not found on graph: {OriginalGraph}");
			}
			return reference.Invoke(Instance, values);
		}

		public override object InvokeFunction(string name, Type[] parameters, object[] values) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.InvokeFunction(name, parameters, values);
			}
			if(values != null) {
				for(int i = 0; i < values.Length; i++) {
					values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
				}
			}
			var reference = Instance.graph.GetFunction(name, parameters);
			if(reference == null) {
				throw new Exception($"Function: {name} was not found on graph: {OriginalGraph}");
			}
			return reference.Invoke(Instance, values);
		}

		public override void SetProperty(string name, object value, char @operator) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetProperty(name, value, @operator);
			}
			else {
				var reference = Instance.graph.GetProperty(name);
				if(reference == null) {
					throw new Exception($"Property: {name} was not found on graph: {OriginalGraph}");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				switch(@operator) {
					case '+':
					case '-':
					case '/':
					case '*':
					case '%':
						var val = reference.Get(Instance);
						value = uNodeHelper.ArithmeticOperator(val, value, @operator, reference.type, value?.GetType());
						break;
				}
				reference.Set(Instance, value);
			}
		}

		public override void SetProperty(string name, object value) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetProperty(name, value);
			}
			else {
				var reference = Instance.graph.GetProperty(name);
				if(reference == null) {
					throw new Exception($"Property: {name} was not found on graph: {OriginalGraph}");
				}
				reference.Set(Instance, uNodeHelper.GetActualRuntimeValue(value));
			}
		}

		public override void SetVariable(string name, object value, char @operator) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetVariable(name, value, @operator);
			}
			else {
				var reference = Instance.graph.GetVariable(name);
				if(reference == null) {
					throw new Exception($"Variable: {name} was not found on graph: {OriginalGraph}");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				switch(@operator) {
					case '+':
					case '-':
					case '/':
					case '*':
					case '%':
						var val = reference.Get(Instance);
						value = uNodeHelper.ArithmeticOperator(val, value, @operator, reference.type, value?.GetType());
						break;
				}
				reference.Set(Instance, value);
			}
		}

		public override void SetVariable(string name, object value) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetVariable(name, value);
			}
			else {
				var reference = Instance.graph.GetVariable(name);
				if(reference == null) {
					throw new Exception($"Variable: {name} was not found on graph: {OriginalGraph}");
				}
				reference.Set(Instance, uNodeHelper.GetActualRuntimeValue(value));
			}
		}
		#endregion
	}
}