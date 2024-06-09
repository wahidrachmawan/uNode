using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[GraphSystem(
		supportAttribute = false,
		supportGeneric = false,
		supportModifier = false,
		allowAutoCompile = true,
		isScriptGraph = false,
		inherithFrom = typeof(RuntimeBehaviour),
		generationKind = GenerationKind.Compatibility)]
	[AddComponentMenu("uNode/Graph Component")]
	public class GraphComponent : BaseRuntimeBehaviour, IInstancedGraph, IClassGraph, IGraphWithVariables, IGraphWithProperties, IGraphWithFunctions, IStateGraph, IIndependentGraph, IRuntimeGraph {
		[SerializeField]
		protected string graphName;
		public List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections", "System.Collections.Generic" };

		[SerializeField]
		public GeneratedScriptData scriptData = new GeneratedScriptData();

		[SerializeField]
		protected SerializedGraph m_serializedGraph = new SerializedGraph();
		public Graph GraphData => m_serializedGraph.GetGraph(this);
		public SerializedGraph serializedGraph => m_serializedGraph;

		#region Properties
		public string GeneratedTypeName {
			get {
				return scriptData?.typeName;
			}
		}

		public override string uniqueIdentifier => Mathf.Abs(GetHashCode()).ToString();
		private string m_graphName;

		public virtual string GraphName {
			get {
				if(uNodeUtility.IsInMainThread) {
					try {
						GraphData.name = name;
						if(string.IsNullOrEmpty(graphName)) {
							m_graphName = GraphData.name + "_GC" + Mathf.Abs(GetHashCode());
						}
						else {
							m_graphName = graphName + "_GC" + Mathf.Abs(GetHashCode());
						}
						scriptData.fileName = m_graphName;
					}
					//Ensure to skip any error regarding `GetName` is not allowed to be called during serialization.
					catch { }
				}
				if(!string.IsNullOrEmpty(graphName)) {
					return graphName;
				}
				if(!string.IsNullOrEmpty(GraphData.name) || string.IsNullOrEmpty(scriptData.fileName)) {
					return GraphData.name;
				}
				return scriptData.fileName;
			}
		}

		public string FullGraphName {
			get {
				var name = GraphName;
				if(string.IsNullOrEmpty(m_graphName) == false) {
					name = m_graphName;
				}
				string ns = Namespace;
				if(!string.IsNullOrEmpty(ns)) {
					return ns + "." + name;
				}
				else {
					return name;
				}
			}
		}

		public string Namespace {
			get {
				return RuntimeType.RuntimeNamespace;
			}
		}

		public Type InheritType => typeof(MonoBehaviour);
		public bool CanCreateStateGraph => true;

		public List<string> UsingNamespaces {
			get => usingNamespaces;
			set => usingNamespaces = value;
		}

		GeneratedScriptData ITypeWithScriptData.ScriptData => scriptData;

		public RuntimeBehaviour nativeInstance { get; private set; }
		IGraph IInstancedGraph.OriginalGraph => this;

		private GraphInstance m_graphInstance;
		public GraphInstance Instance => m_graphInstance;
		#endregion

		#region Initialization
		private bool hasInitialize;
		void EnsureInitialized() {
			if(hasInitialize) {
				return;
			}
			if(!Application.isPlaying) {
				throw new System.Exception("Can't initialize graph instance when not playing.");
			}
			hasInitialize = true;
			var type = GeneratedTypeName.ToType(false);
			if(type != null) {
				//Instance native c# graph, native graph will call Awake immediately
				var instance = gameObject.AddComponent(type) as RuntimeBehaviour;
				instance.hideFlags = HideFlags.HideInInspector;

				nativeInstance = instance;
				//Initialize the references
				var references = scriptData.unityObjects;
				for(int i = 0; i < references.Count; i++) {
					SetVariable(references[i].name, references[i].value);
				}
				//Initialize the variable
				foreach(var v in this.GetVariables()) {
					var var = v;
					var value = var.defaultValue;
					SetVariable(var.name, value);
				}
				//Call awake
				instance.OnAwake();
				instance.enabled = enabled;
			}
			else {
				//Instance reflection graph
				m_graphInstance = RuntimeGraphUtility.InitializeComponentGraph(this, this);
				m_graphInstance.eventData.onAwake?.Invoke(Instance);
			}
		}
		#endregion

		#region Unity Events
		void Awake() {
			EnsureInitialized();
		}

		void Start() {
			EnsureInitialized();
			if(m_graphInstance != null) {
				m_graphInstance.eventData.onStart?.Invoke(Instance);
			}
		}

		void OnEnable() {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.enabled = true;
				nativeInstance.OnBehaviourEnable();
			}
			else if(m_graphInstance != null) {
				m_graphInstance.eventData.onEnable?.Invoke(Instance);
			}
		}

		void OnDisable() {
			if(nativeInstance != null) {
				nativeInstance.enabled = false;
			}
			else if(m_graphInstance != null) {
				m_graphInstance.eventData.onDisable?.Invoke(Instance);
			}
		}

		void OnDrawGizmos() {
			if(nativeInstance == null || Application.isPlaying == false) {
				RuntimeGraphUtility.DrawGizmos(ref m_graphInstance, this, this, null);
			}
		}

		void OnDrawGizmosSelected() {
			if(nativeInstance == null || Application.isPlaying == false) {
				RuntimeGraphUtility.DrawGizmosSelected(ref m_graphInstance, this, this, null);
			}
		}
		#endregion

		#region Functions
		public override object GetProperty(string name) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.GetProperty(name);
			}
			return uNodeHelper.GetProperty(this, name);
		}

		public override object GetVariable(string name) {
			EnsureInitialized();
			if(nativeInstance != null) {
				return nativeInstance.GetVariable(name);
			}
			return uNodeHelper.GetVariable(this, name);
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

		public override void SetProperty(string name, object value, char @operator) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetProperty(name, value, @operator);
			}
			else {
				uNodeHelper.SetProperty(this, name, value, @operator);
			}
		}

		public override void SetProperty(string name, object value) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetProperty(name, value);
			}
			else
				uNodeHelper.SetProperty(this, name, value);
		}

		public override void SetVariable(string name, object value, char @operator) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetVariable(name, value, @operator);
			}
			else
				uNodeHelper.SetVariable(this, name, value, @operator);
		}

		public override void SetVariable(string name, object value) {
			EnsureInitialized();
			if(nativeInstance != null) {
				nativeInstance.SetVariable(name, value);
			}
			else
				uNodeHelper.SetVariable(this, name, value);
		}
		#endregion
	}
}