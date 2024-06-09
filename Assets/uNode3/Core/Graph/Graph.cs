using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[GraphElement]
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
						_mainContainer = functionContainer.InsertChild(new MainGraphContainer(), 0);
					}
				}
				return _mainContainer;
			}
		}
		#endregion

		[SerializeField]
		private int m_UniqueID;

		internal int GetNewUniqueID() {
			while(GetElementByID(++m_UniqueID) != null) { }
			return m_UniqueID;
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
			}
			var ee = GetObjectInChildren<T>(element => element.id == id, true);
			if(ee != null) {
				cachedElements[id] = ee;
			}
			return ee;
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
	}

	[Serializable]
	public sealed class SerializedGraph : ISerializationCallbackReceiver {
		[SerializeField]
		private OdinSerializedData serializedData;
		[NonSerialized]
		private Graph graph;
		[NonSerialized]
		private bool successDeserialize;
		[NonSerialized]
		private bool hasInitialize;

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
				var newGraph = Deserialize(serializedData);
#if UNITY_EDITOR
				if(hasInitialize) {
					if(object.ReferenceEquals(newGraph, null) == false) {
						newGraph.version = oldGraph.version + 1;
					}
					hasInitialize = false;
				}
#endif
				graph = newGraph;
				if(oldGraph != null) {
					//Mark the old graph to invalid so all reference is redirected to new graph.
					oldGraph.MarkInvalid();
				}

				//Testing
				//if(graph != null) {
				//	var node = graph.GetNodeInChildren<MultipurposeNode>(true);
				//	if(node != null) {
				//		node.EnsureRegistered();
				//		Debug.Log(node.parameters[0].input.defaultValue.GetNicelyDisplayName());
				//	}
				//}
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
				if(graph.graphContainer != null && uNodeThreadUtility.frame > 100 && object.ReferenceEquals(UnityEditor.Selection.activeObject, graph.graphContainer)) {
					if(Event.current == null)
						return;
				}
			}
			catch { }
			if(UnityEditor.BuildPipeline.isBuildingPlayer) {
				var tempGraph = this.graph;
#if UNODE_TRIM_ON_BUILD && UNODE_PRO
				tempGraph = uNodeUtility.ProBinding.GetTrimmedGraph(tempGraph);
#endif
				serializedData = Serialize(tempGraph, OdinSerializer.DataFormat.Binary);
				return;
			}
#endif
			SerializeGraph();
		}

		public void SerializeGraph() {
			if(graph == null)
				return;
			serializedData = Serialize(graph);
		}

		public void DeserializeGraph() {
			graph = Deserialize(serializedData);
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