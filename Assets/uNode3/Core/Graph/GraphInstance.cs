using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class ValueOutputData {
		public readonly string id;

		public object value;

		public ValueOutputData(string id) {
			this.id = id;
		}
	}

	public class RuntimeLocalValue {
		public readonly GraphInstance instance;
		public readonly UGraphElementRef owner;

		public object value;

		private readonly Dictionary<string, ValueOutputData> outputDatas = new Dictionary<string, ValueOutputData>();

		public ValueOutputData GetOutputData(string id) {
			if(!outputDatas.TryGetValue(id, out var data)) {
				data = new ValueOutputData(id);
				outputDatas[id] = data;
			}
			return data;
		}

		public void SetOutputData(string id, object value) {
			if(!outputDatas.TryGetValue(id, out var data)) {
				data = new ValueOutputData(id);
				outputDatas[id] = data;
			}
			data.value = value;
		}

		public RuntimeLocalValue(GraphInstance instance, UGraphElement owner) {
			this.instance = instance;
			this.owner = new UGraphElementRef(owner);
		}
	}

	internal readonly struct RuntimeGraphID {
		public readonly int graphID;
		public readonly int elementID;
		public readonly int objectID;

		public override string ToString() {
			return $"({graphID}, {elementID}, {objectID})";
		}

		public override bool Equals(object obj) {
			return obj is RuntimeGraphID iD &&
				   graphID == iD.graphID &&
				   elementID == iD.elementID &&
				   objectID == iD.objectID;
		}

		public override int GetHashCode() {
			return HashCode.Combine(graphID, elementID, objectID);
		}

		public static bool operator ==(RuntimeGraphID lhs, RuntimeGraphID rhs) {
			return lhs.graphID == rhs.graphID && lhs.elementID == rhs.elementID && lhs.objectID == rhs.objectID;
		}

		public static bool operator !=(RuntimeGraphID lhs, RuntimeGraphID rhs) {
			return !(lhs == rhs);
		}

		public RuntimeGraphID(int graphID, int elementID) {
			this.graphID = graphID;
			this.elementID = elementID;
			objectID = 0;
		}

		public RuntimeGraphID(int graphID, int elementID, int objectID) {
			this.graphID = graphID;
			this.elementID = elementID;
			this.objectID = objectID;
		}
	}

	public class RuntimeGraphEventData {
		public Action<GraphInstance> onAwake;
		public Action<GraphInstance> onStart;
		public Action<GraphInstance> onDestroy;
		public Action<GraphInstance> onDisable;
		public Action<GraphInstance> onEnable;
		public Action<GraphInstance> onDrawGizmos;
		public Action<GraphInstance> onDrawGizmosSelected;

		private Dictionary<string, Action<GraphInstance>> customEvents;

		public void RegisterCustomEvent(string name, Action<GraphInstance> action) {
			if(customEvents == null) {
				customEvents = new Dictionary<string, Action<GraphInstance>>();
			}
			customEvents[name] = action;
		}

		public void ExecuteCustomEvent(string name, GraphInstance instance) {
			if(customEvents != null) {
				if(customEvents.TryGetValue(name, out var act)) {
					act(instance);
					return;
				}
			}
			throw new Exception($"there's no Custom Event with name: {name}");
		}

		public bool HasCustomEvent(string name) {
			if(customEvents != null) {
				return customEvents.ContainsKey(name);
			}
			return false;
		}
	}

	public class RuntimePortData {
		[NonSerialized]
		public JumpStatement jumpStatement;

		/// <summary>
		/// Get/Set the current state
		/// </summary>
		[NonSerialized]
		public StateType state;
		/// <summary>
		/// Are this event is finished
		/// </summary>
		[NonSerialized]
		protected bool finished;
		/// <summary>
		/// Are this event has called
		/// </summary>
		[NonSerialized]
		protected bool hasCalled;
		public StateType currentState {
			get {
				if(!finished && hasCalled) {
					return StateType.Running;
				}
				return state;
			}
		}
	}

	public sealed class GraphInstance {
        public readonly object target;
		public readonly IGraph graph;
		public readonly RuntimeGraphEventData eventData;
		public readonly MemberReferenceTree members = new MemberReferenceTree();
		public readonly StateGraphRunner stateRunner;

		public Flow defaultFlow => stateRunner.defaultFlow;

		public int version;

		public GraphInstance(object target, IGraph graph, RuntimeGraphEventData eventData) {
            this.target = target;
			this.graph = graph;
			this.eventData = eventData;
			stateRunner = new StateGraphRunner(this);
		}

		public void Initialize() {
			InitializeGraphData(graph.GraphData);
		}

		void InitializeGraphData(Graph graphData) {
			var baseGraph = RuntimeGraphUtility.GetInheritedGraph(graphData.graphContainer);
			if(baseGraph != null) {
				InitializeGraphData(baseGraph.GraphData);
			}
			graphData.InitializeElement();
			graphData.ForeachInChildrens(element => {
				try {
					element.OnRuntimeInitialize(this);
				} catch(Exception ex) {
					Debug.LogException(new GraphException(ex, element));
				}
			}, true);
		}

		public class MemberReferenceTree {
			public Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
			public Dictionary<string, Property> properties = new Dictionary<string, Property>();
			public Dictionary<string, List<Function>> functions = new Dictionary<string, List<Function>>();

			public MemberReferenceTree baseMember;
		}

        private Dictionary<RuntimeGraphID, RuntimeLocalValue> elementDatas = new Dictionary<RuntimeGraphID, RuntimeLocalValue>();
        private Dictionary<RuntimeGraphID, object> customDatas = new Dictionary<RuntimeGraphID, object>();
        private Dictionary<(RuntimeGraphID, object), object> customDatas2 = new Dictionary<(RuntimeGraphID, object), object>();

		#region Coroutines
		//private Dictionary<object, List<Coroutine>> routineMap = new Dictionary<object, List<Coroutine>>();
		/// <summary>
		/// Start a coroutine.
		/// </summary>
		/// <param name="routine"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public Coroutine StartCoroutine(IEnumerator routine, object owner) {
			if(target is MonoBehaviour mb) {
				var result = mb.StartCoroutine(routine);
				//if(!routineMap.ContainsKey(owner)) {
				//	routineMap[owner] = new List<Coroutine>();
				//}
				//routineMap[owner].Add(result);
				return result;
			}
			else {
				throw new Exception("The target must inherit from MonoBehaviour or it's sub classes");
			}
		}

		public void StopCoroutine(Coroutine coroutine) {
			if(target is MonoBehaviour mb) {
				mb.StopCoroutine(coroutine);
			}
			else {
				throw new Exception("The target must inherit from MonoBehaviour or it's sub classes");
			}
		}

		///// <summary>
		///// Stop all coroutines running on owner.
		///// </summary>
		///// <param name="owner"></param>
		//public void StopAllCoroutines(object owner) {
		//	if(target is MonoBehaviour mb) {
		//		List<Coroutine> coroutineList;
		//		if(routineMap.TryGetValue(owner, out coroutineList)) {
		//			foreach(var routine in coroutineList) {
		//				if(routine != null) {
		//					mb.StopCoroutine(routine);
		//				}
		//			}
		//			//Clear after stoping all coroutine.
		//			coroutineList.Clear();
		//		}
		//	}
		//	else {
		//		throw new Exception("The target must inherit from MonoBehaviour or it's sub classes");
		//	}
		//}
		#endregion

		#region Custom Data
		public void SetUserData(UGraphElement owner, object value) {
			customDatas[owner.runtimeID] = value;
		}

		public object GetUserData(UGraphElement owner) {
			if(!customDatas.TryGetValue(owner.runtimeID, out var data)) {
				customDatas[owner.runtimeID] = data;
			}
			return data;
		}

		public void SetUserData(UGraphElement owner, object key, object value) {
			RuntimeGraphID id;
			if(object.ReferenceEquals(owner, null) == false)
				id = owner.runtimeID;
			else
				id = default;
			customDatas2[(id, key)] = value;
		}

		public object GetUserData(UGraphElement owner, object key) {
			RuntimeGraphID id;
			if(object.ReferenceEquals(owner, null) == false)
				id = owner.runtimeID;
			else
				id = default;
			if(!customDatas2.TryGetValue((id, key), out var data)) {
				customDatas2[(id, key)] = data;
			}
			return data;
		}
		#endregion

		#region Members
		//public Function GetFunction(string name, Type[] types) {

		//}
		#endregion

		#region Delegates
		private Dictionary<Function, Delegate> delegate_maps;
		internal Delegate GetDelegate(Function function) {
			if(delegate_maps == null)
				delegate_maps = new();
			if(delegate_maps.TryGetValue(function, out var result) == false) {
				var method = function.graphContainer.GetGraphType().GetMethod(function.name, MemberData.flags, null, function.ParameterTypes, null);
				if(method != null) {
					if(method.ReturnType == typeof(void)) {
						result = CustomDelegate.CreateActionDelegate(values => {
							function.Invoke(this, values);
						}, function.ParameterTypes);
					}
					else {
						result = CustomDelegate.CreateFuncDelegate(values => {
							return function.Invoke(this, values);
						}, function.ParameterTypes.Append(function.ReturnType()).ToArray());
					}
					delegate_maps[function] = result;
				}
				else {
					throw new Exception($"Cannot create delegate, the graph: {function.graphContainer.GetType()} is not support for delegate creation.");
				}
			}
			return result;
		}

		#endregion

		#region Utility
		public StateFlow RunState(FlowOutput output) {
#if UNITY_EDITOR
			if(GraphDebug.useDebug && output != null) {
				var node = output.node;
				GraphDebug.Flow(target, node.graphContainer.GetGraphID(), node.id, output.id);
			}
#endif
			var input = output.GetTargetFlow();
			if(input != null) {
				var data = GetStateData(input);
				data.Run();
				return data;
			}
			return null;
		}

		public void StopState(FlowOutput output) {
			var input = output.GetTargetFlow();
			if(input != null) {
				var data = GetStateData(input);
				data.Stop();
			}
		}

		public void StopState(FlowInput input) {
			var data = GetStateData(input);
			data.Stop();
		}

		public StateFlow GetStateData(FlowInput port) {
			return stateRunner.GetStateData(port);
		}

		public StateFlow GetStateData(FlowPort port) {
			if(port is FlowInput input) {
				return GetStateData(input);
			}
			else if(port is FlowOutput output) {
				return GetStateData(output.GetTargetFlow());
			}
			throw null;
		}

		/// <summary>
		/// Get the valid element ( used for support live editing )
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="element"></param>
		/// <returns></returns>
		public T GetValidElement<T>(T element) where T : UGraphElement {
			if(element.IsValid == false) {
				if(elementDatas.TryGetValue(element.runtimeID, out var data)) {
					return data.owner.reference as T;
				}
				var graphData = element.graphContainer.GraphData;
				if(graphData != null) {
					var validElement = graphData.GetElementByID(element.id);
					if(validElement is T result) {
						//For any subsequent operation is using lookup.
						GetOrCreateElementDataValue(validElement);
						//Return the result
						return result;
					}
				}
				return null;
			}
			return element;
		}

		/// <summary>
		/// Get the valid node ( used for support live editing )
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="element"></param>
		/// <returns></returns>
		public T GetValidNode<T>(T element) where T : Node {
			if(element.IsValid == false) {
				if(elementDatas.TryGetValue(element.nodeObject.runtimeID, out var data)) {
					if(data.owner.reference is NodeObject node && node is T result) {
						return result;
					}
				}
				var graphData = element.nodeObject.graphContainer.GraphData;
				if(graphData != null) {
					var validElement = graphData.GetElementByID(element.id);
					if(validElement is NodeObject node && node.node is T result) {
						//For any subsequent operation is using lookup.
						GetOrCreateElementDataValue(validElement);
						//Return the result
						return result;
					}
				}
				return null;
			}
			return element;
		}

		/// <summary>
		/// Get the valid port ( used for support live editing )
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public FlowOutput GetValidPort(FlowOutput port) {
			if(port.node.IsValid == false) {
				var nodeObject = GetValidElement<NodeObject>(port.node);
				if(nodeObject != null) {
					return nodeObject.GetFlowOutput(port.id);
				}
				return null;
			}
			return port;
		}

		/// <summary>
		/// Get the valid port ( used for support live editing )
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public FlowInput GetValidPort(FlowInput port) {
			if(port.node.IsValid == false) {
				var nodeObject = GetValidElement<NodeObject>(port.node);
				if(nodeObject != null) {
					return nodeObject.GetFlowInput(port.id);
				}
				return null;
			}
			return port;
		}

		/// <summary>
		/// Get the valid port ( used for support live editing )
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public ValueOutput GetValidPort(ValueOutput port) {
			if(port.node.IsValid == false) {
				if(elementDatas.TryGetValue(port.node.runtimeID, out var data)) {
					if(data.owner.reference is NodeObject node) {
						return node.GetValueOutput(port.id);
					}
				}
				var graphData = graph.GraphData;
				if(graphData != null) {
					var validElement = graphData.GetElementByID(port.node.id);
					if(validElement is NodeObject node) {
						return node.GetValueOutput(port.id);
					}
				}
				return null;
			}
			return port;
		}

		/// <summary>
		/// Get the valid port ( used for support live editing )
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public ValueInput GetValidPort(ValueInput port) {
			if(port.node.IsValid == false) {
				if(elementDatas.TryGetValue(port.node.runtimeID, out var data)) {
					if(data.owner.reference is NodeObject node) {
						return node.GetValueInput(port.id);
					}
				}
				var graphData = graph.GraphData;
				if(graphData != null) {
					var validElement = graphData.GetElementByID(port.node.id);
					if(validElement is NodeObject node) {
						return node.GetValueInput(port.id);
					}
				}
				return null;
			}
			return port;
		}

		private RuntimeLocalValue GetOrCreateElementDataValue(UGraphElement owner) {
			var id = owner.runtimeID;
			if(!elementDatas.TryGetValue(id, out var data)) {
				data = new RuntimeLocalValue(this, owner);
				elementDatas[id] = data;
			}
			return data;
		}
		#endregion

		#region Element Data
		public ref object GetElementDataByRef(UGraphElement owner) {
			var data = GetOrCreateElementDataValue(owner);
			return ref data.value;
		}

		public object GetElementData(UGraphElement owner) {
			var data = GetOrCreateElementDataValue(owner);
			return data.value;
		}

		public T GetElementData<T>(UGraphElement owner) {
			var data = GetOrCreateElementDataValue(owner);
			if(object.ReferenceEquals(data.value, null))
				return default;
			return (T)data.value;
		}

		public void SetElementData(UGraphElement owner, object value) {
			var data = GetOrCreateElementDataValue(owner);
			data.value = value;
		}

		public bool HasElementData(UGraphElement owner) {
			return elementDatas.TryGetValue(owner.runtimeID, out var _);
		}

		public T GetOrCreateElementData<T>(UGraphElement owner) where T : new() {
			var id = owner.runtimeID;
			if(!elementDatas.TryGetValue(id, out var data)) {
				data = new RuntimeLocalValue(this, owner);
				data.value = new T();
				elementDatas[id] = data;
			}
			if(object.ReferenceEquals(data.value, null)) {
				data.value = new T();
			}
			return (T)data.value;
		}
		#endregion
	}
}