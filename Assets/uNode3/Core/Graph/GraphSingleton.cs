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
		inherithFrom =typeof(RuntimeBehaviour),
		generationKind = GenerationKind.Performance)]
	public class GraphSingleton : GraphAsset, IClassGraph, ISingletonGraph, IGraphWithVariables, IGraphWithProperties, IGraphWithFunctions, IIndependentGraph, IClassIdentifier, IReflectionType {
		public string @namespace;
		public List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections", "System.Collections.Generic" };

		[Tooltip(
@"True: the graph will be persistence mean that the graph will not be destroyed on Load a scene this is useful for global settings so the value is persistence between scenes.

False: The graph will be destroyed on Loading a scene, this usefull for Scene Management so every enter new scene the value will be reset to default value")]
		public bool persistence = true;

		[HideInInspector, SerializeField]
		public GeneratedScriptData scriptData = new GeneratedScriptData();

		#region Properties
		public string GeneratedTypeName => scriptData?.typeName;

		public virtual string GraphName {
			get {
				var nm = GraphData.name;
				//return !string.IsNullOrEmpty(Name) ? Name.Replace(' ', '_') : "_" + Mathf.Abs(GetHashCode());
				if(string.IsNullOrEmpty(nm)) {
					if(string.IsNullOrEmpty(scriptData.fileName) && uNodeUtility.IsInMainThread) {
						try {
							scriptData.fileName = this.name;
						}
						//Ensure to skip any error regarding `GetName` is not allowed to be called during serialization.
						catch { }
					}
					return scriptData.fileName;
				}
				return nm;
			}
		}

		public virtual string FullGraphName {
			get {
				string ns = Namespace;
				if(!string.IsNullOrEmpty(ns)) {
					return ns + "." + GraphName;
				}
				else {
					return GraphName;
				}
			}
		}

		public string Namespace {
			get {
				return @namespace;
			}
			set => @namespace = value;
		}

		public Type InheritType => typeof(MonoBehaviour);

		public List<string> UsingNamespaces {
			get => usingNamespaces;
			set => usingNamespaces = value;
		}

		public string uniqueIdentifier => FullGraphName;

		GeneratedScriptData ITypeWithScriptData.ScriptData => scriptData;

		RuntimeType _runtimeType;
		public RuntimeType ReflectionType {
			get {
				if(_runtimeType == null) {
					_runtimeType = new RuntimeGraphType(this);
				}
				return _runtimeType;
			}
		}

		bool ISingletonGraph.IsPersistence => persistence;
		IEnumerable<Type> IRuntimeInterface.GetInterfaces() {
			return Type.EmptyTypes;
		}
		IRuntimeClass ISingletonGraph.Instance => m_instance;
		#endregion

		#region Initialization
		[NonSerialized]
		private BaseRuntimeBehaviour m_instance;
		public BaseRuntimeBehaviour runtimeInstance => m_instance;

		internal void EnsureInitialized() {
			if(m_instance != null) {
				return;
			}
			if(!Application.isPlaying) {
				throw new Exception("Can't initialize graph instance when not playing.");
			}
			var gameObject = new GameObject("#Singleton => " + FullGraphName);
			if(persistence) {
				//gameObject.hideFlags = HideFlags.DontSaveInEditor;
				DontDestroyOnLoad(gameObject);
			}

			var type = GeneratedTypeName.ToType(false);
			if(type != null) {
				//Instance native c# graph, native graph will call Awake immediately
				var instance = gameObject.AddComponent(type) as RuntimeBehaviour;
				
				m_instance = instance;
				//Initialize the references
				var references = scriptData.unityObjects;
				for(int i = 0; i < references.Count; i++) {
					SetVariable(references[i].name, references[i].value);
				}
				//Initialize the variable
				foreach(var v in this.GetVariables()) {
					SetVariable(v.name, SerializerUtility.Duplicate(v.defaultValue));
				}
				//Call awake
				instance.OnAwake();
				instance.enabled = true;
			}
			else {
				var instance = gameObject.AddComponent<RuntimeInstancedGraph>();
				m_instance = instance;
				instance.Initialize(this);
			}
		}
		#endregion

		#region Unity Events
		private void OnValidate() {
			scriptData.fileName = this.name;
		}
		#endregion

		#region Functions
		public void ExecuteFunction(string name) {
			EnsureInitialized();
			InvokeFunction(name, null);
		}

		public object GetProperty(string name) {
			EnsureInitialized();
			return m_instance.GetProperty(name);
		}

		public object GetVariable(string name) {
			EnsureInitialized();
			return m_instance.GetVariable(name);
		}

		public object InvokeFunction(string name, object[] values) {
			EnsureInitialized();
			return m_instance.InvokeFunction(name, values);
		}

		public object InvokeFunction(string name, Type[] parameters, object[] values) {
			EnsureInitialized();
			return m_instance.InvokeFunction(name, parameters, values);
		}

		public void SetProperty(string name, object value, char @operator) {
			EnsureInitialized();
			m_instance.SetProperty(name, value, @operator);
		}

		public void SetProperty(string name, object value) {
			EnsureInitialized();
			m_instance.SetProperty(name, value);
		}

		public void SetVariable(string name, object value, char @operator) {
			EnsureInitialized();
			m_instance.SetVariable(name, value, @operator);
		}

		public void SetVariable(string name, object value) {
			EnsureInitialized();
			m_instance.SetVariable(name, value);
		}
		#endregion

		#region Utility

		private static Dictionary<Type, BaseRuntimeBehaviour> instanceMaps = new Dictionary<Type, BaseRuntimeBehaviour>();
		/// <summary>
		/// Get the singleton instance
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetInstance<T>() where T : BaseRuntimeBehaviour {
			if(!instanceMaps.TryGetValue(typeof(T), out var instance) || instance == null) {
				var objs = GameObject.FindObjectsOfType<T>();
				T result = objs.FirstOrDefault();
				if(result == null) {
					var db = uNodeUtility.GetDatabase()?.GetGraphDatabase<T>();
					if(db != null && db.asset != null) {
						if(db.asset is GraphSingleton singleton) {
							singleton.EnsureInitialized();
							if(singleton.m_instance is T) {
								instanceMaps[typeof(T)] = singleton.m_instance;
								return singleton.m_instance as T;
							}
						}
						else {
							throw new Exception("The graph is not singleton, cannot get the singleton instance.");
						}
					}
					else {
						throw new NullReferenceException($"The graph database:{typeof(T).FullName} was not found.  Please update database in menu 'Tools > uNode > Update Graph Database' to fix this.");
					}
				}
				instanceMaps[typeof(T)] = result;
				return result;
			}
			return instance as T;
		}

		/// <summary>
		/// Get the graph singleton instance by its unique identifier
		/// </summary>
		/// <param name="uniqueIdentifier"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetInstance(string uniqueIdentifier) {
			var db = uNodeUtility.GetDatabase()?.GetGraphDatabase(uniqueIdentifier);
			if(db != null) {
				var graph = db.asset as GraphSingleton;
				if(graph != null) {
					graph.EnsureInitialized();
					return graph.m_instance;
				}
				else {
					Debug.LogError("The graph reference is missing. Please update database in menu 'Tools > uNode > Update Graph Database' to fix this.");
				}
			}
			else {
				Debug.LogError("The graph database was not found. Please update database in menu 'Tools > uNode > Update Graph Database' to fix this.");
			}
			return null;
		}
		#endregion
	}
}