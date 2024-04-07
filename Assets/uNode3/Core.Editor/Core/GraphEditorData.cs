using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	//public enum EditorSelectionType {
	//	None,
	//	Graph,
	//	GraphElements,
	//	Other,
	//}

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
		}

		public GraphEditorData(GraphEditorData editorData, params UGraphElement[] selections) {
			if(editorData == null)
				return;
			_owner = editorData._owner;
			_graph = editorData._graph;
			_serializedSelecteds = new List<BaseReference>(editorData.serializedSelecteds);
			foreach(var ele in selections) {
				serializedSelecteds.Add(new UGraphElementRef(ele));
			}
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

		public Graph graphData => graph?.GraphData;

		public GraphLayout graphLayout => graphData.graphLayout;

		public bool isValidGraph => owner is IGraph && graph != null;

		public GraphSystemAttribute graphSystem => GraphUtility.GetGraphSystem(_owner);
		//}

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
				} else {
					_selectedCanvas = new UGraphElementRef(value);
				}
			}
		}

		/// <summary>
		/// True if the current canvas is macro editing
		/// </summary>
		public bool isInMacro {
			get {
				if(currentCanvas is NodeContainer && graph is IMacroGraph)
					return true;
				return currentCanvas is NodeObject nodeObject && nodeObject.node is IMacro;
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

		public bool canAddNode {
			get {
				if(isValidGraph) {
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

		public bool supportCoroutine {
			get {
				if(selectedRoot == graphData.mainGraphContainer && (graph is IMacroGraph || graph is IStateGraph state && state.CanCreateStateGraph)) {
					return true;
				} else if(selectedGroup?.node is ISuperNode superNode) {
					if(superNode.AllowCoroutine()) {
						return true;
					}
				}
				if(selectedRoot.IsValidElement()) {
					return selectedRoot.AllowCoroutine();
				}
				return false;
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

		public NodeObject selectedGroup {
			get {
				return currentCanvas as NodeObject;
			}
		}

		public bool isInMainGraph {
			get {
				return selectedRoot is MainGraphContainer;
			}
		}

		public bool isSupportMainGraph {
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

		public string mainGraphTitle {
			get {
				if(graph is IStateGraph) {
					return "STATE GRAPH";
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

		public void AddToSelection(BaseReference reference) {
			if(reference.ReferenceValue is not NodeObject) {
				serializedSelecteds.Clear();
			}
			serializedSelecteds.RemoveAll(r => r is not UGraphElementRef);
			serializedSelecteds.Add(reference);
		}

		public void RemoveFromSelection(UGraphElement element) {
			for(int i=0;i< serializedSelecteds.Count;i++) {
				if(serializedSelecteds[i].ReferenceValue as UGraphElement == element) {
					serializedSelecteds.RemoveAt(i);
				}
			}
		}

		public void ClearSelection() {
			serializedSelecteds.Clear();
		}

		public bool hasSelection => serializedSelecteds.Count > 0;

		public IEnumerable<object> selecteds {
			get {
				foreach(var ele in serializedSelecteds) {
					yield return ele.ReferenceValue;
				}
			}
		}

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
		#endregion

		#region Canvas
		/// <summary>
		/// True if the owner can be edited by graph editor.
		/// </summary>
		public bool isGraphOpen {
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
				if (graphCanvas.position != Vector2.zero) {
					return graphCanvas;
				}
			} else {
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
			} else if(obj is NodeObject nodeObject) {
				if(nodeObject.node is ISuperNode) {
					ISuperNode superNode = nodeObject.node as ISuperNode;
					foreach(var n in superNode.nestedFlowNodes) {
						if(n != null) {
							graphCanvas.position = new Vector2(n.position.x - 200, n.position.y - 200);
							break;
						}
					}
				} else {
					graphCanvas.position = new Vector2(nodeObject.position.x - 200, nodeObject.position.y - 200);
				}
			}
			if(graphCanvas.position == Vector2.zero) {
				if(obj is NodeObject) {
					var nodes = obj.GetObjectsInChildren<NodeObject>();
					var n = nodes.FirstOrDefault();
					if (n != null) {
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
			if(graph is IRefreshable) {
				(graph as IRefreshable).Refresh();
			}
		}

		public HashSet<string> GetNamespaces() {
			return graph.GetUsingNamespaces();
		}
	}
}