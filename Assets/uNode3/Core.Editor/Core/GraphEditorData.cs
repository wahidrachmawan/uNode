#pragma warning disable CS0618
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// The graph editor data
	/// </summary>
	[System.Serializable]
	public class GraphEditorData {
		#region Fields
		[SerializeField]
		private UnityEngine.Object _owner;
		[SerializeField]
		private UnityEngine.Object _graph;
		[SerializeField]
		private UGraphElementRef _selectedCanvas;
		#endregion

		#region Constructors
		public GraphEditorData() {

		}

		public GraphEditorData(GraphEditorData editorData) {
			if(editorData == null)
				return;
			_owner = editorData._owner;
			_graph = editorData._graph;
			_serializedSelecteds = new List<BaseReference>(editorData.serializedSelecteds);
			currentCanvas = editorData.currentCanvas;
		}

		public GraphEditorData(GraphEditorData editorData, params UGraphElement[] selections) {
			if(editorData == null)
				return;
			_owner = editorData._owner;
			_graph = editorData._graph;
			_serializedSelecteds = new List<BaseReference>(editorData.serializedSelecteds);
			if(selections.Length > 0) {
				_serializedSelecteds.Clear();
				foreach(var ele in selections) {
					serializedSelecteds.Add(new UGraphElementRef(ele));
				}
			}
			currentCanvas = editorData.currentCanvas;
		}

		public GraphEditorData(Object graph) {
			SetOwner(graph);
		}

		public GraphEditorData(Object graph, params UGraphElement[] selections) {
			SetOwner(graph);
			foreach(var ele in selections) {
				serializedSelecteds.Add(new UGraphElementRef(ele));
			}
		}

		public GraphEditorData(UGraphElement graphElement) {
			var obj = graphElement.graphContainer as UnityEngine.Object;
			SetOwner(obj);
			AddToSelection(graphElement);
		}
		#endregion

		/// <summary>
		/// Set owner of the data
		/// </summary>
		/// <param name="owner"></param>
		public void SetOwner(Object owner) {
			if(owner is IGraph) {
				_owner = owner;
				_graph = owner;
			}
			else {
				_owner = owner;
			}
		}

		/// <summary>
		/// The UnityEngine.Object of the graph
		/// </summary>
		public Object owner {
			get {
				return _owner;
			}
		}

		/// <summary>
		/// The root owner of the graph.
		/// </summary>
		public Object RootOwner {
			get {
				if(owner is IScriptGraphType scriptGraphType) {
					return scriptGraphType.ScriptTypeData.scriptGraphReference;
				}
				return owner;
			}
		}

		/// <summary>
		/// The selected target root.
		/// </summary>
		public IGraph graph {
			get {
				if(_graph == null) {
					return null;
				}
				return _graph as IGraph;
			}
		}

		public UBind bindGraph => UBind.FromObject(owner);

		/// <summary>
		/// The actual graph data
		/// </summary>
		public Graph graphData => graph?.GraphData;

		/// <summary>
		/// The graph layout
		/// </summary>
		public GraphLayout graphLayout => graphData.graphLayout;

		/// <summary>
		/// True if the data is valid
		/// </summary>
		public bool IsValidGraph => owner is IGraph && graph != null;

		/// <summary>
		/// Get the graph system
		/// </summary>
		public GraphSystemAttribute graphSystem => GraphUtility.GetGraphSystem(_owner);

		/// <summary>
		/// The current scope of graph editing.
		/// A value must be a group node, a graph root (function, property, constructor), or a graph itseft.
		/// </summary>
		/// <value></value>
		public UGraphElement currentCanvas {
			get {
				return _selectedCanvas?.reference ?? graphData?.mainGraphContainer;
			}
			set {
				if(value == null) {
					_selectedCanvas = null;
				}
				else {
					_selectedCanvas = new UGraphElementRef(value);
				}
				UpdateScopes(value);
			}
		}

		void UpdateScopes(UGraphElement canvas) {
			if(m_scopes == null) {
				m_scopes = new();
			}
			m_scopes.Clear();
			if(canvas is NodeObject) {
				if((canvas as NodeObject).node is ISuperNode superNode) {
					NodeScope.ApplyScopes(superNode.SupportedScope, m_scopes, null, out _);
				}
				else {
					m_scopes.Add(NodeScope.FlowGraph);
				}
			}
			else {
				var root = selectedRoot;
				if(root != null) {
					if(root is MainGraphContainer) {
						if(graph is IStateGraph) {
							m_scopes.Add(NodeScope.StateGraph);
							m_scopes.Add(NodeScope.FlowGraph);
							m_scopes.Add(NodeScope.Coroutine);
						}
						else if(graph is ICustomMainGraph mainGraph) {
							NodeScope.ApplyScopes(mainGraph.MainGraphScope, m_scopes, null, out _);
							if(mainGraph.AllowCoroutine) {
								m_scopes.Add(NodeScope.Coroutine);
							}
						}
					}
					else if(root is IEventGraphCanvas) {
						var canvass = root as IEventGraphCanvas;
						NodeScope.ApplyScopes(canvass.Scope, m_scopes, null, out _);
					}
					if(root is BaseFunction) {
						m_scopes.Add(NodeScope.Function);
						m_scopes.Add(NodeScope.FlowGraph);
					}
					if(root.AllowCoroutine()) {
						m_scopes.Add(NodeScope.Coroutine);
					}
				}
				if(IsInMacro) {
					m_scopes.Add(NodeScope.Macro);
					m_scopes.Add(NodeScope.FlowGraph);
				}
			}
			//For in case entry was deleted.
			if(canvas is NodeContainerWithEntry containerWithEntry && containerWithEntry.Entry != null) { }
		}

		[System.NonSerialized]
		private HashSet<string> m_scopes;
		/// <summary>
		/// The scope of the graph
		/// </summary>
		public HashSet<string> scopes {
			get {
				if(m_scopes == null) {
					UpdateScopes(currentCanvas);
				}
				return m_scopes;
			}
		}

		/// <summary>
		/// The all nodes in current canvas
		/// </summary>
		public IEnumerable<NodeObject> nodes {
			get {
				if(currentCanvas.IsValidElement()) {
					return currentCanvas.GetObjectsInChildren<NodeObject>();
				}
				return Enumerable.Empty<NodeObject>();
			}
		}

		/// <summary>
		/// Return true, if can add node to the <see cref="currentCanvas"/>
		/// </summary>
		public bool CanAddNode {
			get {
				if(IsValidGraph) {
					if(currentCanvas is MainGraphContainer) {
						if(graph is IMacroGraph) {
							return true;
						}
						else if(graph is IStateGraph stateGraph) {
							return stateGraph.CanCreateStateGraph;
						}
						else if(graph is ICustomMainGraph mainGraph) {
							return mainGraph.CanCreateOnMainGraph;
						}
					}
					else if(graph is IClassGraph classGraph && classGraph.InheritType == null) {
						//In case it is interface and the current graph is Runtime Graphs
						return false;
					}
					else {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Return true if the current canvas is supporting coroutine
		/// </summary>
		public bool SupportCoroutine {
			get {
				if(selectedGroup != null) {
					if(selectedGroup?.node is ISuperNode superNode) {
						if(superNode.AllowCoroutine()) {
							return true;
						}
					}
					return false;
				}
				if(selectedRoot.IsValidElement()) {
					if(selectedRoot is MainGraphContainer) {
						if(graph is IStateGraph) {
							return true;
						}
						if(graph is ICustomMainGraph mainGraph) {
							return mainGraph.AllowCoroutine;
						}
					}
					return selectedRoot.AllowCoroutine();
				}
				return false;
			}
		}

		/// <summary>
		/// True if the current canvas is supported to add flow nodes
		/// </summary>
		public bool SupportFlowNode {
			get {
				return scopes.Count == 0 || scopes.Contains(NodeScope.FlowGraph);
			}
		}

		#region Selection
		/// <summary>
		/// The selected uNode root
		/// </summary>
		public NodeContainer selectedRoot {
			get {
				return currentCanvas?.GetObjectInParent<NodeContainer>();
			}
		}

		/// <summary>
		/// Return the selected nested node
		/// </summary>
		public NodeObject selectedGroup {
			get {
				return currentCanvas as NodeObject;
			}
		}

		/// <summary>
		/// Return true if has selected elements
		/// </summary>
		public bool hasSelection => serializedSelecteds.Count > 0;

		/// <summary>
		/// All selection objects
		/// </summary>
		public IEnumerable<object> selecteds {
			get {
				foreach(var ele in serializedSelecteds) {
					yield return ele.ReferenceValue;
				}
			}
		}

		/// <summary>
		/// All selecteds nodes
		/// </summary>
		public IEnumerable<NodeObject> selectedNodes {
			get {
				foreach(var ele in serializedSelecteds) {
					if(ele.ReferenceValue is NodeObject node) {
						yield return node;
					}
				}
			}
		}

		public int selectedCount => serializedSelecteds.Count;

		/// <summary>
		/// Return true if it is currently in Main Graph
		/// </summary>
		public bool IsInMainGraph {
			get {
				return selectedRoot is MainGraphContainer;
			}
		}

		/// <summary>
		/// Return true if the graph is support main graph
		/// </summary>
		public bool IsSupportMainGraph {
			get {
				if(graph is IStateGraph stateGraph) {
					return stateGraph.CanCreateStateGraph;
				}
				else if(graph is IMacroGraph) {
					return true;
				}
				else if(graph is ICustomMainGraph) {
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// True if the current canvas is macro editing
		/// </summary>
		public bool IsInMacro {
			get {
				if(currentCanvas is NodeContainer && graph is IMacroGraph)
					return true;
				return currentCanvas is NodeObject nodeObject && nodeObject.node is IMacro;
			}
		}

		/// <summary>
		/// The title of main graph
		/// </summary>
		public string MainGraphTitle {
			get {
				if(graph is IStateGraph) {
					return "EVENT GRAPH";
				}
				else if(graph is IMacroGraph) {
					return "MACRO";
				}
				else if(graph is ICustomMainGraph mainGraph) {
					return mainGraph.MainGraphTitle;
				}
				else {
					return "NO ROOT";
				}
			}
		}

		[SerializeReference]
		private List<BaseReference> _serializedSelecteds;
		private List<BaseReference> serializedSelecteds {
			get {
				if(_serializedSelecteds == null)
					_serializedSelecteds = new List<BaseReference>();
				return _serializedSelecteds;
			}
		}

		/// <summary>
		/// Add element to selection
		/// </summary>
		/// <param name="element"></param>
		public void AddToSelection(UGraphElement element) {
			if(element is not NodeObject) {
				serializedSelecteds.Clear();
				if(element is Graph) {
					return;
				}
			}
			serializedSelecteds.RemoveAll(r => r is not UGraphElementRef);
			serializedSelecteds.Add(new UGraphElementRef(element));
		}

		/// <summary>
		/// Add element to selection
		/// </summary>
		/// <param name="reference"></param>
		public void AddToSelection(BaseReference reference) {
			if(reference.ReferenceValue is not NodeObject) {
				serializedSelecteds.Clear();
			}
			serializedSelecteds.RemoveAll(r => r is not UGraphElementRef);
			serializedSelecteds.Add(reference);
		}

		/// <summary>
		/// Add element to selection
		/// </summary>
		/// <param name="element"></param>
		public void AddToSelection(IEnumerable<UGraphElement> elements) {
			if(elements != null) {
				foreach(var e in elements) {
					if(e == null) continue;
					AddToSelection(e);
				}
			}
		}

		/// <summary>
		/// Remove element from selection
		/// </summary>
		/// <param name="element"></param>
		public void RemoveFromSelection(UGraphElement element) {
			for(int i = 0; i < serializedSelecteds.Count; i++) {
				if(serializedSelecteds[i].ReferenceValue as UGraphElement == element) {
					serializedSelecteds.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Clear selections
		/// </summary>
		public void ClearSelection() {
			serializedSelecteds.Clear();
		}
		#endregion

		#region Canvas
		/// <summary>
		/// True if the owner can be edited by graph editor.
		/// </summary>
		public bool IsGraphOpen {
			get {
				return selectedRoot.IsValidElement() || selectedGroup.IsValidElement() || graph is IMacroGraph || graph is IStateGraph state && state.CanCreateStateGraph;
			}
		}

		[System.Serializable]
		public class GraphCanvas {
			[SerializeField]
			private UGraphElementRef graphElementRef;
			public UGraphElement graphElement {
				get {
					return graphElementRef?.reference;
				}
			}
			public Vector2 position;
			public float zoomScale = 1;
			public bool hasFocused;

			public GraphCanvas(UGraphElement graphElement) {
				graphElementRef = new UGraphElementRef(graphElement);
			}
		}
		[SerializeField]
		private List<GraphCanvas> canvasDatas = new List<GraphCanvas>();

		public Vector2 GetPosition(UGraphElement obj) {
			return GetGraphPosition(obj).position;
		}

		public void SetPosition(UGraphElement obj, Vector2 position) {
			GetGraphPosition(obj).position = position;
		}

		public GraphCanvas GetCurrentCanvasData() {
			return GetGraphPosition(currentCanvas, false);
		}

		private GraphCanvas GetGraphPosition(UGraphElement obj, bool focusCanvas = true) {
			if(obj == null) {
				obj = graphData?.mainGraphContainer;
			}
			GraphCanvas graphCanvas = canvasDatas.FirstOrDefault(p => p.graphElement == obj);
			if(graphCanvas != null) {
				if(graphCanvas.position != Vector2.zero) {
					return graphCanvas;
				}
			}
			else {
				graphCanvas = new GraphCanvas(obj);
				canvasDatas.Add(graphCanvas);
			}
			if(!focusCanvas) {
				return graphCanvas;
			}
			if(obj is NodeContainer) {
				if(obj is NodeContainerWithEntry containerWithEntry && containerWithEntry.Entry != null) {
					graphCanvas.position = new Vector2(containerWithEntry.Entry.position.x - 200, containerWithEntry.Entry.position.y - 200);
				}
			}
			else if(obj is NodeObject nodeObject) {
				if(nodeObject.node is ISuperNode) {
					ISuperNode superNode = nodeObject.node as ISuperNode;
					foreach(var n in superNode.NestedFlowNodes) {
						if(n != null) {
							graphCanvas.position = new Vector2(n.position.x - 200, n.position.y - 200);
							break;
						}
					}
				}
				else {
					graphCanvas.position = new Vector2(nodeObject.position.x - 200, nodeObject.position.y - 200);
				}
			}
			if(graphCanvas.position == Vector2.zero) {
				if(obj is NodeObject) {
					var nodes = obj.GetObjectsInChildren<NodeObject>();
					var n = nodes.FirstOrDefault();
					if(n != null) {
						graphCanvas.position = new Vector2(n.position.x - 200, n.position.y - 200);
					}
				}
			}
			return graphCanvas;
		}

		public bool HasPosition(UGraphElement obj) {
			return canvasDatas.Any(p => p.graphElement == obj);
		}

		public Vector2 position {
			get {
				return GetPosition(currentCanvas);
			}
			set {
				SetPosition(currentCanvas, value);
			}
		}

		public void ResetPositionData() {
			canvasDatas.Clear();
		}
		#endregion

		#region Debug
		private object _debugObject;
		private object oldTargetDebug;
		public UnityEngine.Object debugObject;
		public bool debugAnyScript = true, debugSelf = true;
		/// <summary>
		/// The debug object.
		/// </summary>
		public object debugTarget {
			get {
				if(oldTargetDebug != graph) {
					_debugObject = null;
					oldTargetDebug = graph;
				}
				if(_debugObject != null) {
					if(_debugObject is UnityEngine.Object o && o == null) {
						_debugObject = null;
					}
					else {
						return _debugObject;
					}
				}
				if(debugObject == null && !object.ReferenceEquals(debugObject, null)) {
					//This is for fix null reference after exiting playmode
					var obj = UnityEditor.EditorUtility.InstanceIDToObject(debugObject.GetHashCode());
					if(obj != null) {
						debugObject = obj;
					}
				}
				if(debugObject != null) {
					return debugObject;
				}
				else if(debugAnyScript) {
					if(Application.isPlaying) {
						if(GraphDebug.debugData.TryGetValue(graph.GetGraphID(), out var debugMap)) {
							foreach(var (key, _) in debugMap) {
								if(key != null && key != graph) {
									if(key is UnityEngine.Object o && o == null) {
										continue;
									}
									_debugObject = key;
									break;
								}
							}
						}
						//Find instance from sub class
						if(_debugObject == null && graph is IReflectionType) {
							var db = uNodeDatabase.instance;
							foreach(var (id, map) in GraphDebug.debugData) {
								var other = db.graphDatabases.FirstOrDefault(data => data.fileUniqueID == id)?.asset;
								if(other != null) {
									IGraph inherited = null;
									for(int i = 0; i < 100; i++) {
										inherited = RuntimeGraphUtility.GetInheritedGraph(other);
										if(inherited == null)
											break;
										if(inherited == graph) {
											foreach(var (key, _) in map) {
												if(key != null && key != graph) {
													if(key is UnityEngine.Object o && o == null) {
														continue;
													}
													_debugObject = key;
													break;
												}
											}
											break;
										}
										if(i == 100) {
											Debug.LogException(new GraphException($"The graph: {inherited.GetFullGraphName()} has cyclic inherited which is not allowed", inherited.GraphData));
										}
									}
								}
							}
						}
					}
					if(debugSelf && graph is IRuntimeClass) {
						return graph;
					}
					return _debugObject;
				}
				if(debugSelf && graph is IRuntimeClass) {
					return graph;
				}
				return null;
			}
			set {
				if(value is UnityEngine.Object) {
					debugObject = value as UnityEngine.Object;
					debugAnyScript = false;
				}
				else if(value is bool) {
					debugAnyScript = (bool)value;
				}
				else {
					debugAnyScript = false;
				}
				_debugObject = value;
				if(value == null) {
					debugObject = null;
				}
				debugSelf = true;
			}
		}

		public void SetAutoDebugTarget(object target) {
			debugTarget = target;
			debugAnyScript = true;
		}
		#endregion

		/// <summary>
		/// Refresh editor data.
		/// </summary>
		public void Refresh() {
			if(graph == null)
				return;
			UpdateScopes(currentCanvas);
			if(graph is IRefreshable) {
				(graph as IRefreshable).Refresh();
			}
		}

		/// <summary>
		/// Get using namespace of the graph
		/// </summary>
		/// <returns></returns>
		public HashSet<string> GetUsingNamespaces() {
			return graph.GetUsingNamespaces();
		}
	}
}