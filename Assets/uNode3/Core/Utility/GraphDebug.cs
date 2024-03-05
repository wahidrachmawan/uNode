using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;

namespace MaxyGames.UNode {
	public static class GraphDebug {
		#region Classes
		public struct DebugValue {
			public bool isSet;
			public float time;
			public object value;

			public bool isValid => time > 0;
		}
		public class DebugFlow {
			private StateType _state;
			public StateType nodeState {
				get {
					if(customCondition != null) {
						return customCondition();
					}
					return _state;
				}
				set {
					_state = value;
				}
			}
			public float calledTime;
			public float breakpointTimes;
			public bool isTransitionRunning;
			public Func<StateType> customCondition;
			public object nodeValue;
			public bool isValid => calledTime > 0;
		}

		public class DebugMessage {
			public Dictionary<int, DebugMessageData> datas = new Dictionary<int, DebugMessageData>();


			public string GetMessage(Connection connection) {
				if(connection == null || connection.isValid == false) return null;

				var input = connection.Input;
				var output = connection.Output;
				var graph = input.node.graphContainer;

				var graphID = uNodeUtility.GetObjectID(graph as UnityEngine.Object);
				bool isValue = connection is ValueConnection;

				if(datas.TryGetValue(graphID, out var messageData)) {
					if(isValue) {
						if(messageData.valueMessages.TryGetValue(input.node.id, out var map)) {
							if(map.TryGetValue(input.id, out var message)) {
								return message;
							}
						}
					}
					else {
						if(messageData.flowMessages.TryGetValue(output.node.id, out var map)) {
							if(map.TryGetValue(output.id, out var message)) {
								return message;
							}
						}
					}
				}
				return null;
			}

			public bool HasMessage(Connection connection) {
				return GetMessage(connection) != null;
			}

			public void SetMessage(Connection connection, string message) {
				if(connection == null || connection.isValid == false) return;

				var input = connection.Input;
				var output = connection.Output;
				var graph = input.node.graphContainer;

				var graphID = uNodeUtility.GetObjectID(graph as UnityEngine.Object);
				bool isValue = connection is ValueConnection;

				if(datas.TryGetValue(graphID, out var messageData) == false) {
					datas[graphID] = messageData = new DebugMessageData();
				}
				if(isValue) {
					if(messageData.valueMessages.TryGetValue(input.node.id, out var map) == false) {
						if(message == null) return;
						messageData.valueMessages[input.node.id] = map = new Dictionary<string, string>();
					}
					if(message == null) {
						map.Remove(input.id);
					}
					else {
						map[input.id] = message;
					}
				}
				else {
					if(messageData.flowMessages.TryGetValue(output.node.id, out var map) == false) {
						if(message == null) return;
						messageData.flowMessages[output.node.id] = map = new Dictionary<string, string>();
					}
					if(message == null) {
						map.Remove(output.id);
					}
					else {
						map[output.id] = message;
					}
				}
			}

			private void Update() {
				try {
					foreach(var (id, message) in datas) {
						if(debugData.TryGetValue(id, out var map)) {
							foreach(var (_, data) in map) {
								data.messageData = message;
							}
						}
					}
				}
				catch { }
			}

			public void Save() {
#if UNITY_EDITOR && UNODE_PRO
				var bytes = SerializerUtility.Serialize(debugMessage);
				UnityEditor.SessionState.SetString("UNODE_DATA_DEBUG_MESSAGE", Convert.ToBase64String(bytes));
#endif
				Update();
			}
		}

		public class DebugMessageData {
			public Dictionary<int, Dictionary<string, string>> flowMessages = new Dictionary<int, Dictionary<string, string>>();
			public Dictionary<int, Dictionary<string, string>> valueMessages = new Dictionary<int, Dictionary<string, string>>();
		}

		static DebugMessage _debugMessage;
		internal static DebugMessage debugMessage {
			get {
				if(_debugMessage == null) {
#if UNITY_EDITOR && UNODE_PRO
					var str = UnityEditor.SessionState.GetString("UNODE_DATA_DEBUG_MESSAGE", string.Empty);
					if(!string.IsNullOrEmpty(str)) {
						var bytes = Convert.FromBase64String(str);
						_debugMessage = SerializerUtility.Deserialize<DebugMessage>(bytes);
					}
#endif
					if(_debugMessage == null)
						_debugMessage = new DebugMessage();
				}
				return _debugMessage;
			}
		}

		/// <summary>
		/// Class that contains Debug data
		/// </summary>
		public class DebugData {
			public DebugMessageData messageData;

			public Dictionary<int, DebugFlow> nodeDebug = new Dictionary<int, DebugFlow>();
			public Dictionary<int, Dictionary<string, DebugFlow>> flowDebug = new Dictionary<int, Dictionary<string, DebugFlow>>();

			public Dictionary<string, DebugValue> flowConnectionDebug = new Dictionary<string, DebugValue>();
			public Dictionary<string, DebugValue> valueConnectionDebug = new Dictionary<string, DebugValue>();

			public DebugValue GetDebugValue(FlowOutput port) {
				if(port.isAssigned) {
					flowConnectionDebug.TryGetValue(port.node.id + port.id, out var value);
					return value;
				}
				return default;
			}

			public DebugFlow GetDebugValue(FlowInput port) {
				if(port.isPrimaryPort) {
					nodeDebug.TryGetValue(port.node.id, out var result);
					return result;
				} else {
					flowDebug.TryGetValue(port.node.id, out var map);
					if(map != null && map.TryGetValue(port.id, out var result)) {
						return result;
					}
				}
				return default;
			}

			public DebugValue GetDebugValue(ValueInput port) {
				if(port.isAssigned) {
					valueConnectionDebug.TryGetValue(port.node.id + port.id, out var value);
					return value;
				}
				return default;
			}
		}
		#endregion

		public static Dictionary<int, ConditionalWeakTable<object, DebugData>> debugData = new Dictionary<int, ConditionalWeakTable<object, DebugData>>();
		/// <summary>
		/// Are debug mode is on.
		/// </summary>
		public static bool useDebug = true;
		/// <summary>
		/// The timer for debug.
		/// </summary>
		public static float debugLinesTimer;

		/// <summary>
		/// The debug time, increased every time.
		/// Note: only work inside unity editor.
		/// </summary>
		public static float debugTime => (float)debugTimeAsDouble;

		public static double debugTimeAsDouble;

		public static float transitionSpeed = 0.5f;

		private static int m_lastDebugID;
		private static ConditionalWeakTable<object, string> m_debugIDs = new ConditionalWeakTable<object, string>();

		internal static string GetDebugID(object obj) {
			if(obj is UnityEngine.Object) {
				return obj.GetHashCode().ToString();
			}
			else {
				if(!m_debugIDs.TryGetValue(obj, out var result)) {
					result = "@" + (++m_lastDebugID);
					m_debugIDs.AddOrUpdate(obj, result);
				}
				return result;
			}
		}

		internal static object GetDebugObject(string id) {
			if(string.IsNullOrEmpty(id))
				return null;
			if(id[0] == '@') {
				foreach(var (obj, debugID) in m_debugIDs) {
					if(id == debugID) {
						return obj;
					}
				}
			}
#if UNITY_EDITOR
			else if(int.TryParse(id, out var result)) {
				return UnityEditor.EditorUtility.InstanceIDToObject(result);
			}
#endif
			return null;
		}

		public static class Breakpoint {
			/// <summary>
			/// This will filled from uNodeEditorInitializer.
			/// </summary>
			internal static Func<Dictionary<int, HashSet<int>>> getBreakpoints;
			/// <summary>
			/// This will filled from uNodeEditorInitializer.
			/// </summary>
			internal static event Action onBreakpointChanged;

			/// <summary>
			/// Get all breakpoints
			/// </summary>
			public static Dictionary<int, HashSet<int>> Get => getBreakpoints?.Invoke();

			/// <summary>
			/// Are the node has breakpoint.
			/// </summary>
			/// <param name="graphID"></param>
			/// <param name="nodeID"></param>
			/// <returns></returns>
			public static bool HasBreakpoint(int graphID, int nodeID) {
				if(getBreakpoints == null) {
#if UNITY_EDITOR
					throw new Exception("uNode is not initialized");
#else
				return false;
#endif
				}
				if(Get.TryGetValue(graphID, out var hash)) {
					return hash.Contains(nodeID);
				}
				return false;
			}

			/// <summary>
			/// Are the node has breakpoint.
			/// </summary>
			/// <param name="element"></param>
			/// <returns></returns>
			public static bool HasBreakpoint(UGraphElement element) {
				if(element != null) {
					return HasBreakpoint(element.graphContainer.GetGraphID(), element.id);
				}
				return false;
			}

			/// <summary>
			/// Add breakpoint to node.
			/// </summary>
			/// <param name="graphID"></param>
			/// <param name="nodeID"></param>
			public static void AddBreakpoint(int graphID, int nodeID) {
				if(getBreakpoints == null) {
#if UNITY_EDITOR
					throw new Exception("uNode is not initialized");
#else
				return;
#endif
				}
				if(graphID == 0)
					return;
				if(!Get.TryGetValue(graphID, out var hash)) {
					hash = new HashSet<int>();
					Get[graphID] = hash;
				}
				if(!hash.Contains(nodeID)) {
					hash.Add(nodeID);
					onBreakpointChanged?.Invoke();
				}
			}

			/// <summary>
			/// Remove breakpoint from node.
			/// </summary>
			/// <param name="graphID"></param>
			/// <param name="nodeID"></param>
			public static void RemoveBreakpoint(int graphID, int nodeID) {
				if(getBreakpoints == null) {
#if UNITY_EDITOR
					throw new Exception("uNode is not initialized");
#else
				return;
#endif
				}
				if(graphID == 0)
					return;
				if(!Get.TryGetValue(graphID, out var hash)) {
					hash = new HashSet<int>();
					Get[graphID] = hash;
				}
				if(hash.Contains(nodeID)) {
					hash.Remove(nodeID);
					onBreakpointChanged?.Invoke();
				}
			}

			/// <summary>
			/// Remove breakpoint from node.
			/// </summary>
			/// <param name="graphID"></param>
			/// <param name="nodeID"></param>
			public static void ClearBreakpoints() {
				if(getBreakpoints == null) {
#if UNITY_EDITOR
					throw new Exception("uNode is not initialized");
#else
				return;
#endif
				}
				Get.Clear();
				onBreakpointChanged?.Invoke();
			}
		}

		/// <summary>
		/// Call this function to debug EventNode that using value node.
		/// </summary>
		public static T Value<T>(T value, object owner, int objectUID, int nodeUID, string portID, bool isSet = false) {
			if(!useDebug || uNodeUtility.isPlaying == false)
				return value;
			if(owner is ValueType) {
				//If the owner of value is struct then we use the type instead
				owner = owner.GetType();
			}
			if(!debugData.TryGetValue(objectUID, out var debugMap)) {
				debugMap = new ConditionalWeakTable<object, DebugData>();
				debugData[objectUID] = debugMap;
			}
			if(!debugMap.TryGetValue(owner, out var data)) {
				data = new DebugData();
				debugMap.AddOrUpdate(owner, data);

#if UNODE_PRO
				if(debugMessage.datas.TryGetValue(objectUID, out var messageData)) {
					data.messageData = messageData;
				}
#endif
			}
			var id = nodeUID + portID;
			data.valueConnectionDebug[id] = new DebugValue() {
				time = debugTime,
				value = value,
				isSet = isSet,
			};
#if UNODE_PRO
			if(data.messageData != null) {
				if(data.messageData.valueMessages.TryGetValue(nodeUID, out var map)) {
					if(map.TryGetValue(portID, out var message)) {
						var graphID = objectUID;
						var valueMessage = value == null ? "null" : value.ToString();
						if(string.IsNullOrEmpty(message)) {
							Debug.Log(GraphException.GetMessage(valueMessage, graphID, nodeUID, owner));
						}
						else {
							Debug.Log(GraphException.GetMessage(message + ": " + valueMessage, graphID, nodeUID, owner));
						}
					}
				}
			}
#endif
			return value;
		}

		public static void Flow(object owner, int objectUID, int nodeUID, string portID) {
			if(!useDebug || uNodeUtility.isPlaying == false)
				return;
			if(owner is ValueType) {
				//If the owner of value is struct then we use the type instead
				owner = owner.GetType();
			}
			if(!debugData.TryGetValue(objectUID, out var debugMap)) {
				debugMap = new ConditionalWeakTable<object, DebugData>();
				debugData[objectUID] = debugMap;
			}
			if(!debugMap.TryGetValue(owner, out var data)) {
				data = new DebugData();
				debugMap.AddOrUpdate(owner, data);

#if UNODE_PRO
				if(debugMessage.datas.TryGetValue(objectUID, out var messageData)) {
					data.messageData = messageData;
				}
#endif
			}
			var id = nodeUID + portID;
			data.flowConnectionDebug[id] = new DebugValue() {
				time = debugTime,
			};
#if UNODE_PRO
			if(data.messageData != null) {
				if(data.messageData.flowMessages.TryGetValue(nodeUID, out var map)) {
					if(map.TryGetValue(portID, out var message)) {
						var graphID = objectUID;
						Debug.Log(GraphException.GetMessage(message, graphID, nodeUID, owner));
					}
				}
			}
#endif
		}

		/// <summary>
		/// Call this function to debug the flow port.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="objectUID"></param>
		/// <param name="nodeUID"></param>
		/// <param name="state">true : success, false : failure, null : running</param>
		public static void FlowNode(object owner, int objectUID, int nodeUID, bool? state) {
			FlowNode(owner, objectUID, nodeUID, null, state);
		}

		/// <summary>
		/// Call this function to debug the flow port.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="objectUID"></param>
		/// <param name="nodeUID"></param>
		/// <param name="state">true : success, false : failure, null : running</param>
		public static void FlowNode(object owner, int objectUID, int nodeUID, string portID, bool? state) {
			if(!useDebug || uNodeUtility.isPlaying == false)
				return;
			if(owner is ValueType) {
				//If the owner of value is struct then we use the type instead
				owner = owner.GetType();
			}
			var s = state == null ? StateType.Running : (state.Value ? StateType.Success : StateType.Failure);
			if(!debugData.TryGetValue(objectUID, out var debugMap)) {
				debugMap = new ConditionalWeakTable<object, DebugData>();
				debugData[objectUID] = debugMap;
			}
			if(!debugMap.TryGetValue(owner, out var data)) {
				data = new DebugData();
				debugMap.AddOrUpdate(owner, data);
			}
			if(portID == null) {
				if(!data.nodeDebug.TryGetValue(nodeUID, out var nodeDebug)) {
					nodeDebug = new DebugFlow();
					data.nodeDebug[nodeUID] = nodeDebug;
				}
				nodeDebug.calledTime = debugTime;
				nodeDebug.nodeState = s;
				if(Breakpoint.HasBreakpoint(objectUID, nodeUID)) {
					nodeDebug.breakpointTimes = debugTime;
					Debug.Break();
				}
			} else {
				if(!data.flowDebug.TryGetValue(nodeUID, out var flowData)) {
					flowData = new Dictionary<string, DebugFlow>();
					data.flowDebug[nodeUID] = flowData;
				}
				if(!flowData.TryGetValue(portID, out var nodeDebug)) {
					nodeDebug = new DebugFlow();
					flowData[portID] = nodeDebug;
				}
				nodeDebug.calledTime = debugTime;
				nodeDebug.nodeState = s;
				if(Breakpoint.HasBreakpoint(objectUID, nodeUID)) {
					nodeDebug.breakpointTimes = debugTime;
					Debug.Break();
				}
			}
		}
	}
}