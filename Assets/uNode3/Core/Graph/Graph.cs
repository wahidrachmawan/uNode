using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Scripting;

namespace MaxyGames.UNode {
	[GraphElement]
	[Serializable]
	public class Graph : UGraphElement {
		#region Fields
		public GraphLayout graphLayout = GraphLayout.Vertical;
		public List<AttributeData> attributes;

		[NonSerialized]
		public IGraph owner;
		[NonSerialized]
		public int version;
		#endregion

		#region Containers
		private VariableContainer _variableContainer;
		public VariableContainer variableContainer {
			get {
				if(_variableContainer == null) {
					_variableContainer = GetObjectInChildren<VariableContainer>();
					if(_variableContainer == null) {
						_variableContainer = AddChild(new VariableContainer());
					}
				}
				return _variableContainer;
			}
		}

		private ConstructorContainer _constructorContainer;
		public ConstructorContainer constructorContainer {
			get {
				if(_constructorContainer == null) {
					_constructorContainer = GetObjectInChildren<ConstructorContainer>();
					if(_constructorContainer == null) {
						_constructorContainer = AddChild(new ConstructorContainer());
					}
				}
				return _constructorContainer;
			}
		}

		private PropertyContainer _propertyContainer;
		public PropertyContainer propertyContainer {
			get {
				if(_propertyContainer == null) {
					_propertyContainer = GetObjectInChildren<PropertyContainer>();
					if(_propertyContainer == null) {
						_propertyContainer = AddChild(new PropertyContainer());
					}
				}
				return _propertyContainer;
			}
		}

		private FunctionContainer _functionContainer;
		public FunctionContainer functionContainer {
			get {
				if(_functionContainer == null) {
					_functionContainer = GetObjectInChildren<FunctionContainer>();
					if(_functionContainer == null) {
						_functionContainer = AddChild(new FunctionContainer());
					}
				}
				return _functionContainer;
			}
		}

		private MainGraphContainer _mainContainer;
		public MainGraphContainer mainGraphContainer {
			get {
				if(_mainContainer == null) {
					_mainContainer = functionContainer.GetObjectInChildren<MainGraphContainer>();
					if(_mainContainer == null) {
						_mainContainer = functionContainer.InsertChild(0, new MainGraphContainer());
					}
				}
				return _mainContainer;
			}
		}
		#endregion

		[SerializeField]
		private int m_UniqueID;

		/// <summary>
		/// Acquire new UID
		/// </summary>
		/// <returns></returns>
		internal int GetNewUniqueID() {
			while(GetElementByID(++m_UniqueID) != null) { }
			return m_UniqueID;
		}

		[field: NonSerialized]
		internal UGraphElement linkedOwner { get; private set; }
		/// <summary>
		/// Set the graph as linked graph ( macros )
		/// </summary>
		/// <param name="parent"></param>
		internal void SetAsLinkedGraph(UGraphElement parent) {
			linkedOwner = parent;
		}

		Dictionary<int, UGraphElement> _cachedElements;
		private Dictionary<int, UGraphElement> cachedElements {
			get {
				if(_cachedElements == null)
					_cachedElements = new Dictionary<int, UGraphElement>();
				return _cachedElements;
			}
		}

		public UGraphElement GetElementByID(int id) {
			if(cachedElements.TryGetValue(id, out var e)) {
				if(e != null)
					return e;
			}
			var ret = GetObjectInChildren<UGraphElement>(element => element.id == id, true);
			if(ret != null) {
				cachedElements[id] = ret;
			}
			return ret;
		}

		public T GetElementByID<T>(int id) where T : UGraphElement {
			if(cachedElements.TryGetValue(id, out var e)) {
				if(e != null)
					return e as T;
				//else
				//	return null;
			}
			//var ee = GetObjectInChildren<T>(element => element.id == id, true);
			//if(ee != null) {
			//	cachedElements[id] = ee;
			//}
			//return ee;
			return null;
		}

		public override bool CanChangeParent() => false;

		protected override void OnChildAdded(UGraphElement element) {
			cachedElements[element.id] = element;
			base.OnChildAdded(element);
		}

		protected override void OnChildRemoved(UGraphElement element) {
			cachedElements.Remove(element.id);
			base.OnChildRemoved(element);
		}

		public void InitializeElement() {
			this.ForeachInChildrens(element => {
				if(element is NodeObject node) {
					node.EnsureRegistered();
				}
			}, true);
		}

		internal void OnDeserialized() {
			this.ForeachInChildrens(element => {
				if(element == null) return;
				cachedElements[element.id] = element;
			}, true);
		}
	}

	[Serializable]
	public sealed class SerializedGraph : ISerializationCallbackReceiver {
		[SerializeReference]
		private GraphData serializedGraph;
		[SerializeField]
		private UnityEngine.Object owner;

		[SerializeField]
		private OdinSerializedData serializedData;
		[NonSerialized]
		private Graph graph;
		[NonSerialized]
		private bool successDeserialize;
		[NonSerialized]
		private bool hasInitialize;

		[Serializable]
		public class PortData {
			[SerializeReference]
			public UPort port;
			[SerializeReference]
			public NodeObject nodeObject;
		}

		[Serializable]
		public class GraphData {
			[SerializeReference]
			public Graph graph;
		}

		public OdinSerializedData SerializedData => serializedData;

		public Graph Graph {
			get {
				if(graph == null) {
					OnAfterDeserialize();
					if(graph == null)
						SerializeGraph();
				}
				return graph;
			}
		}

		public Graph GetGraph(IGraph container) {
			try {
				if(graph.IsValidElement() == false) {
					OnAfterDeserialize();
					if(graph == null) {
						if(serializedData == null || !serializedData.isFilled) {
							graph = new Graph();
							SerializeGraph();
						}
					}
					graph.owner = container;
				}
				else {
					graph.owner = container;
				}
				if(object.ReferenceEquals(owner, container) == false) {
					owner = container as UnityEngine.Object;
				}
				if(!hasInitialize) {
					hasInitialize = true;
					graph.InitializeElement();
				}
			}
			catch {
				Debug.LogError("Error on trying getting graph", container as UnityEngine.Object);
				throw;
			}
			return graph;
		}

		public SerializedGraph() { }

		public SerializedGraph(Graph graph) {
			this.graph = graph;
		}

		public void OnAfterDeserialize() {
			if(graph == null || !successDeserialize) {
				DeserializeGraph();
				if(!object.ReferenceEquals(graph, null)) {
					successDeserialize = true;
				}
			}
			else {
				var oldGraph = graph;
				var newGraph = DoDeserialize();
#if UNITY_EDITOR
				if(hasInitialize) {
					if(object.ReferenceEquals(newGraph, null) == false) {
						newGraph.version = oldGraph.version + 1;
					}
					hasInitialize = false;
				}
#endif
				newGraph?.OnDeserialized();
				graph = newGraph;
				if(oldGraph != null) {
					if(oldGraph != graph) {
						//Mark the old graph to invalid so all reference is redirected to new graph.
						oldGraph.MarkInvalid();
					}
					//if(oldGraph == graph) {
					//	oldGraph.ForeachInChildrens(element => {
					//		if(element is ISerializationCallbackReceiver) {
					//			(element as ISerializationCallbackReceiver).OnAfterDeserialize();
					//		}
					//	}, true);
					//}
				}
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			if(graph == null)
				return;
#if UNITY_EDITOR
			//This is to prevent lag on big graph when selecting graph asset
			//The lag causes by unity inspector that's constantly check modified value
			//and checking tries serializing the whole value
			//TODO: fix error caused by UnityEditor.Selection.activeObject regarding to domain backup
			try {
				if(graph.graphContainer != null) {
					if(uNodeThreadUtility.frame > 3000 && !UnityEditor.EditorApplication.isUpdating && !UnityEditor.EditorApplication.isCompiling && object.ReferenceEquals(UnityEditor.Selection.activeObject, graph.graphContainer)) {
						if(Event.current == null)
							return;
					}
					if(owner is Component) {
						//This to prevent Trimming graph when the graph is Component
						SerializeGraph();
						return;
					}
				}
			}
			catch { }
#if UNODE_TRIM_ON_BUILD && UNODE_PRO
			if(UnityEditor.BuildPipeline.isBuildingPlayer) {
				var tempGraph = this.graph;
				tempGraph = uNodeUtility.ProBinding.GetTrimmedGraph(tempGraph);
				serializedData = Serialize(tempGraph, OdinSerializer.DataFormat.Binary);
				serializedGraph = null;
				return;
			}
#endif
#endif
			SerializeGraph();
		}

		public void SerializeGraph() {
			if(graph == null)
				return;
#if UNITY_EDITOR
			try {
				if(owner is Component) {
					if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(owner)) {
						//This to prevent graph being modified if the graph is from prefab instance.
						//By preventing it, we can ensure the graph is up to date.
						return;
					}
					//This to serialize the graph using Odin instead when the graph is a Component.
					//This also prevent trimming features
					serializedData = Serialize(graph, OdinSerializer.DataFormat.Binary);
					//Make sure Unity doesn't serialize the graph.
					serializedGraph = null;
					return;
				}
			}
			catch(Exception ex) {
				Debug.LogException(ex);
			}
#endif
			if(serializedGraph == null) {
				serializedGraph = new GraphData();
			}
			serializedGraph.graph = graph;
			serializedData = null;
			//serializedData = Serialize(graph);
		}

		public void DeserializeGraph() {
			graph = DoDeserialize();
			graph?.OnDeserialized();
		}

		internal Graph MakeCopy() {
			var data = Serialize(graph);
			return Deserialize(data);
		}

		private Graph DoDeserialize() {
			if(serializedGraph != null) {
				return serializedGraph.graph;
			}
			return Deserialize(serializedData);
		}

		public static void Copy(SerializedGraph source, SerializedGraph destination, IGraph sourceReference = null, IGraph destinationReference = null) {
			destination.serializedData = new OdinSerializedData();
			destination.serializedData.CopyFrom(source.serializedData);
			if(sourceReference != null && destinationReference != null) {
				var references = destination.serializedData.references;
				for(int i = 0; i < references.Count; i++) {
					if(object.ReferenceEquals(references[i], sourceReference)) {
						references[i] = destinationReference as UnityEngine.Object;
					}
				}
			}
		}

		public static OdinSerializedData Serialize(Graph graph) {
			using(var cache = SerializerUtility.UnitySerializationContext) {
#if UNITY_EDITOR
				return SerializerUtility.SerializeValue(graph, OdinSerializer.DataFormat.Nodes, cache);
#else
				return SerializerUtility.SerializeValue(graph, cache);
#endif
			}
		}

		public static OdinSerializedData Serialize(Graph graph, OdinSerializer.DataFormat format) {
			using(var cache = SerializerUtility.UnitySerializationContext) {
				return SerializerUtility.SerializeValue(graph, format, cache);
			}
		}

		public static Graph Deserialize(OdinSerializedData serializedData) {
			using(var cache = SerializerUtility.UnityDeserializationContext) {
				return SerializerUtility.Deserialize<Graph>(serializedData, cache);
			}
		}
	}
}