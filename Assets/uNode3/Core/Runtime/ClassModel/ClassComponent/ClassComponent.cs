using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode {
    [AddComponentMenu("uNode/Class Component")]
    public class ClassComponent : BaseRuntimeBehaviour, IRuntimeComponent, IRuntimeGraphWrapper {
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
		public RuntimeBehaviour nativeInstance { get; private set; }
		public override string uniqueIdentifier => target != null ? target.uniqueIdentifier : string.Empty;

		bool IRuntimeClassContainer.IsInitialized => Instance != null || nativeInstance != null;
		IRuntimeClass IRuntimeClassContainer.RuntimeClass {
			get {
				EnsureInitialized();
				if(nativeInstance != null) {
					return nativeInstance;
				}
				else {
					return this;
				}
			}
		}

		IEnumerable<Type> IRuntimeInterface.GetInterfaces() {
			if(target is IInterfaceSystem iface) {
				return iface.Interfaces.Select(i => i.type);
			}
			return Type.EmptyTypes;
		}
		IGraph IInstancedGraph.OriginalGraph => target;
		List<VariableData> IRuntimeGraphWrapper.WrappedVariables => variables;

		private GraphInstance m_instance;
		public GraphInstance Instance => m_instance;
		#endregion

		#region Initialization
		public void EnsureInitialized() {
			if(Instance != null || nativeInstance != null)
				return;
			if(!Application.isPlaying) {
				throw new System.Exception("Can't initialize graph instance when not playing.");
			} else if(target == null) {
				throw new Exception("Target graph can't be null");
			}
			var type = target.GeneratedTypeName.ToType(false);
			if(type != null) {
				//Instance native c# graph, native graph will call Awake immediately
				var instance = gameObject.AddComponent(type) as RuntimeBehaviour;
				instance.hideFlags = HideFlags.HideInInspector;

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
				instance.enabled = enabled;
			} else {
				//Instance reflection graph
				m_instance = RuntimeGraphUtility.InitializeComponentGraph(target, this, variables);
				m_instance.eventData.onAwake?.Invoke(Instance);
			}
		}

		void IRuntimeClassContainer.ResetInitialization() {
			m_instance = null;
			nativeInstance = null;
		}
		#endregion

		void Awake() {
			m_instance = null;
			EnsureInitialized();
		}

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
			} else if(m_instance != null) {
				m_instance.eventData.onEnable?.Invoke(Instance);
			}
		}

		void OnDisable() {
			if(nativeInstance != null) {
				nativeInstance.enabled = false;
			} else if(m_instance != null) {
				m_instance.eventData.onDisable?.Invoke(Instance);
			}
		}

		void OnDrawGizmos() {
			if(target != null) {
				RuntimeGraphUtility.DrawGizmos(ref m_instance, target, this, variables);
			}
		}

		void OnDrawGizmosSelected() {
			if(target != null) {
				RuntimeGraphUtility.DrawGizmosSelected(ref m_instance, target, this, variables);
			}
		}

		public override object InvokeFunction(string name, object[] values) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.InvokeFunction(name, values);
			}
			return uNodeHelper.InvokeFunction(this, name, values);
		}

		public override object InvokeFunction(string name, Type[] parameters, object[] values) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.InvokeFunction(name, parameters, values);
			}
			return uNodeHelper.InvokeFunction(this, name, parameters, values);
		}

		public override void SetVariable(string name, object value) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetVariable(name, value);
			} else
				uNodeHelper.SetVariable(this, name, value);
		}

		public override void SetVariable(string name, object value, char @operator) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetVariable(name, value, @operator);
			} else
				uNodeHelper.SetVariable(this, name, value, @operator);
		}

		public override object GetVariable(string name) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.GetVariable(name);
			}
			return uNodeHelper.GetVariable(this, name);
		}

		public override void SetProperty(string name, object value) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetProperty(name, value);
			} else
				uNodeHelper.SetProperty(this, name, value);
		}

		public override void SetProperty(string name, object value, char @operator) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetProperty(name, value, @operator);
			} else {
				uNodeHelper.SetProperty(this, name, value, @operator);
			}
		}

		public override object GetProperty(string name) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.GetProperty(name);
			}
			return uNodeHelper.GetProperty(this, name);
		}
	}
}