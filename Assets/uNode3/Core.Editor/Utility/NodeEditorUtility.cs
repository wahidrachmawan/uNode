using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public static class NodeEditorUtility {
		#region Add
		public static void AddNewFunction(FunctionContainer container, string name, Type returnType, string[] parameterName, Type[] parameterType, string[] genericParameter = null, Action<Function> action = null) {
			if(parameterName.Length != parameterType.Length)
				throw new Exception("Parameter Name & Parameter Type length must same.");
			AddNewObject<Function>(name, container, func => {
				func.name = name;
				func.returnType = returnType;
				{//Init start node

				}
				if(parameterName.Length > 0) {
					for(int i = 0; i < parameterName.Length; i++) {
						func.parameters.Add(new ParameterData(parameterName[i], parameterType[i]));
					}
				}
				if(genericParameter != null && genericParameter.Length > 0) {
					for(int i = 0; i < genericParameter.Length; i++) {
						ArrayUtility.Add(ref func.genericParameters, new GenericParameterData(genericParameter[i]));
					}
				}
				if(action != null) {
					action(func);
				}
				Undo.SetCurrentGroupName("New Function : " + name);
			});
		}

		public static void AddNewFunction(FunctionContainer container, string name, Type returnType, IList<ParameterData> parameters, string[] genericParameter = null, Action<Function> action = null) {
			AddNewObject<Function>(name, container, func => {
				func.name = name;
				func.returnType = returnType;
				{//Init start node

				}
				if(parameters.Count > 0) {
					for(int i = 0; i < parameters.Count; i++) {
						func.parameters.Add(parameters[i]);
					}
				}
				if(genericParameter != null && genericParameter.Length > 0) {
					for(int i = 0; i < genericParameter.Length; i++) {
						ArrayUtility.Add(ref func.genericParameters, new GenericParameterData(genericParameter[i]));
					}
				}
				if(action != null) {
					action(func);
				}
				Undo.SetCurrentGroupName("New Function : " + name);
			});
		}

		public static void AddNewFunction(FunctionContainer container, string name, Type returnType, Action<Function> action = null) {
			string fName = name;
			var functions = container.collections;
			if(functions.Count > 0) {
				int index = 0;
				while(true) {
					index++;
					bool found = false;
					foreach(var f in functions) {
						if(f != null && f.name.Equals(name) && f.Parameters.Count == 0) {
							found = true;
							break;
						}
					}
					if(found) {
						name = fName + index;
					}
					else {
						break;
					}
				}
			}
			AddNewObject<Function>(name, container, func => {
				func.name = name;
				func.returnType = returnType;
				{//Init start node

				}
				if(action != null) {
					action(func);
				}
				Undo.SetCurrentGroupName("New Function : " + name);
			});
		}

		public static void AddNewObject<T>(string name, UGraphElement parent, Action<T> action = null) where T : UGraphElement, new() {
			if(parent == null)
				throw new ArgumentNullException(nameof(parent));
			var value = new T();
			value.name = name;
			value.SetParent(parent);
			action?.Invoke(value);
		}

		public static void AddNewVariable(VariableContainer container, string name, Type type, Action<Variable> action = null) {
			if(string.IsNullOrEmpty(name)) {
				name = "newVariable";
			}
			if(type.IsByRef) {
				type = type.GetElementType();
			}
			var ListVariable = container.collections;
			Variable variable = new Variable();
			variable.type = type;
			variable.name = name;
			variable.resetOnEnter = container.GetObjectInParent<ILocalVariableSystem>() != null;
			if(uNodePreference.preferenceData.newVariableAccessor == uNodePreference.DefaultAccessor.Private) {
				variable.modifier.SetPrivate();
			}
			if(ReflectionUtils.CanCreateInstance(type)) {
				if(type == typeof(object)) {
					variable.defaultValue = "";
				}
				else {
					variable.defaultValue = ReflectionUtils.CreateInstance(type);
				}
			}
			int i = 1;
			while(ListVariable.Count > 0) {
				bool correct = true;
				foreach(var var in ListVariable) {
					if(var.name == variable.name) {
						variable.name = name + (ListVariable.Count + i++);
						correct = false;
						break;
					}
				}
				if(correct) {
					break;
				}
			}
			variable.SetParent(container);
			action?.Invoke(variable);
		}

		public static void AddNewProperty(PropertyContainer container, string name, Type type, Action<Property> action = null) {
			if(string.IsNullOrEmpty(name)) {
				name = "newProperty";
			}
			if(type.IsByRef) {
				type = type.GetElementType();
			}
			string fName = name;
			var properties = container.collections;
			if(properties.Count > 0) {
				int index = 0;
				while(true) {
					index++;
					bool found = false;
					foreach(var f in properties) {
						if(f != null && f.name.Equals(name)) {
							found = true;
							break;
						}
					}
					if(found) {
						name = fName + index;
					}
					else {
						break;
					}
				}
			}
			AddNewObject<Property>(name, container, property => {
				property.name = name;
				property.type = type;
				if(action != null) {
					action(property);
				}
				Undo.SetCurrentGroupName("New Property : " + name);
			});
		}

		/// <summary>
		/// Add a new constructor.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="name"></param>
		/// <param name="action"></param>
		public static void AddNewConstructor(ConstructorContainer container, string name, Action<Constructor> action = null) {
			AddNewObject<Constructor>(name, container, ctor => {
				ctor.name = name;
				{//Init start node

				}
				if(action != null) {
					action(ctor);
				}
				Undo.SetCurrentGroupName("New Constructor : " + name);
			});
			NodeGraph.RefreshOpenedGraph();
		}
		#endregion

		#region Ports
		public static T GetPort<T>(NodeObject node) where T : UPort {
			if(typeof(T) == typeof(ValueInput)) {
				return node.ValueInputs.FirstOrDefault() as T;
			}
			if(typeof(T) == typeof(ValueOutput)) {
				if(node.primaryValueOutput != null)
					return node.primaryValueOutput as T;
				return node.ValueOutputs.FirstOrDefault() as T;
			}
			if(typeof(T) == typeof(FlowInput)) {
				if(node.primaryFlowInput != null)
					return node.primaryFlowInput as T;
				return node.FlowInputs.FirstOrDefault() as T;
			}
			if(typeof(T) == typeof(FlowOutput)) {
				if(node.primaryFlowOutput != null)
					return node.primaryFlowOutput as T;
				return node.FlowOutputs.FirstOrDefault() as T;
			}
			throw new InvalidOperationException();
		}

		public static void RegisterPort(Node node, FlowInput port) {
			if(node.nodeObject.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.GetType()}");
			}
			node.nodeObject.RegisterPort(port);
		}

		public static void RegisterPort(Node node, FlowOutput port) {
			if(node.nodeObject.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.GetType()}");
			}
			node.nodeObject.RegisterPort(port);
		}

		public static void RegisterPort(Node node, ValueInput port) {
			if(node.nodeObject.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.GetType()}");
			}
			node.nodeObject.RegisterPort(port);
		}

		public static void RegisterPort(Node node, ValueOutput port) {
			if(node.nodeObject.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.GetType()}");
			}
			node.nodeObject.RegisterPort(port);
		}

		public static void RegisterPort(NodeObject node, FlowInput port) {
			if(node.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.node.GetType()}");
			}
			node.RegisterPort(port);
		}

		public static void RegisterPort(NodeObject node, FlowOutput port) {
			if(node.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.node.GetType()}");
			}
			node.RegisterPort(port);
		}

		public static void RegisterPort(NodeObject node, ValueInput port) {
			if(node.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.node.GetType()}");
			}
			node.RegisterPort(port);
		}

		public static void RegisterPort(NodeObject node, ValueOutput port) {
			if(node.FlowInputs.Any(p => p.id == port.id)) {
				throw new ArgumentException($"Duplicate port for '{port.id}' in {node.node.GetType()}");
			}
			node.RegisterPort(port);
		}

		public static void ConnectPort(FlowInput input, FlowOutput output) {
			FlowConnection.CreateAndConnect(input, output);
		}

		public static void ConnectPort(ValueInput input, ValueOutput output) {
			ValueConnection.CreateAndConnect(input, output);
		}
		#endregion

		#region Add Nodes
		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="graphData"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(GraphEditorData graphData, Vector2 position, Action<T> action = null) where T : Node {
			AddNewNode(graphData, null, typeof(T), position, action);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="graphData"></param>
		/// <param name="position"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(GraphEditorData graphData, Type type, Vector2 position, Action<T> action = null) where T : Node {
			AddNewNode(graphData, null, type, position, action);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="graphData"></param>
		/// <param name="name"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(GraphEditorData graphData, string name, Vector2 position, Action<T> action = null) where T : Node {
			AddNewNode(graphData, name, typeof(T), position, action);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="graphData"></param>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="position"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(GraphEditorData graphData, string name, Type type, Vector2 position, Action<T> action = null) where T : Node {
			if(string.IsNullOrEmpty(name)) {
				name = "Node";
			}
			bool isPrefab = uNodeEditorUtility.IsPrefab(graphData.owner);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			else {
				Undo.RegisterCompleteObjectUndo(graphData.owner, "New Node : " + name);
			}
			AddNewNode<T>(graphData.currentCanvas, name, type, position, action);
			uNodeEditorUtility.MarkDirty(graphData.owner);
			graphData.Refresh();
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="graph"></param>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="position"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static void AddNewNode<T>(UGraphElement parent, Vector2 position, Action<T> action = null) where T : Node {
			AddNewNode<T>(parent, null, typeof(T), position, action, setUndoGroup: false);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="graph"></param>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="position"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static void AddNewNode<T>(UGraphElement parent, string name, Vector2 position, Action<T> action = null) where T : Node {
			AddNewNode<T>(parent, name, typeof(T), position, action, setUndoGroup: false);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="graph"></param>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="position"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static void AddNewNode<T>(UGraphElement parent, string name, Type type, Vector2 position, Action<T> action = null, bool setUndoGroup = true, bool registerNode = true, bool autoAssignPorts = true) where T : Node {
			if(parent is null) {
				throw new ArgumentNullException(nameof(parent));
			}

			if(string.IsNullOrEmpty(name)) {
				name = "Node";
			}
			if(type == null)
				type = typeof(T);
			AddNewObject<NodeObject>(name + " " + parent.childCount, parent, node => {
				if(type != null) {
					if(type.IsCastableTo(typeof(Node))) {
						node.node = ReflectionUtils.CreateInstance(type) as Node;
					}
					else {
						var nod = new Nodes.HLNode();
						nod.type = type;
						node.node = nod;
					}
				}
				//Set the node position
				node.position = new Rect(position.x, position.y, 170, 100);
				//Register the node for port initialization
				if(registerNode) {
					node.EnsureRegistered();
					//Mark node to unregistered so that future EnsureRegister should call Register.
					node.isRegistered = false;
				}
				if(setUndoGroup) {
					//Set the undo group name
					Undo.SetCurrentGroupName("New Node : " + name);
				}
				if(action != null) {
					//Call an action callback
					action(node.node as T);
				}
				if(autoAssignPorts) {
					//Auto assign ports.
					AutoAssignNodePorts(node);
				}
			});
		}

		public static void AutoRerouteAndProxy(Connection connection, UGraphElement canvas) {
			if(connection == null || connection.Input == null || connection.Output == null) return;
			if(connection.Input.node.graph.graphLayout == GraphLayout.Vertical) {
				if(connection is ValueConnection valueConnection) {
					if(uNodePreference.preferenceData.autoCreateReroute && connection.Input.GetNode() is not Nodes.NodeReroute) {
						if(!connection.isProxy) {
							if(connection.Output.node.position.xMin > connection.Input.node.position.xMin) {
								var inPortPosition = connection.Input.node.position;
								NodeEditorUtility.AddNewNode(canvas,
									new Vector2(
										inPortPosition.x - 100,
										inPortPosition.y),
									(Nodes.NodeReroute n) => {
										n.kind = Nodes.NodeReroute.RerouteKind.Value;
										n.Register();
										n.input.ConnectTo(connection.Output);
										connection.Input.ConnectTo(n.output);
										if(uNodePreference.preferenceData.autoProxyConnection) {
											n.input.connections[0].isProxy = true;
										}
									}
								);
							}
						}
					}
					else if(uNodePreference.preferenceData.autoProxyConnection) {
						if(!connection.isProxy) {
							if(connection.Output.node.position.xMin > connection.Input.node.position.xMin) {
								valueConnection.isProxy = true;
							}
						}
					}
				}
			}
		}

		public static bool AutoConnectPortToTarget(UPort source, NodeObject target, UGraphElement canvas, bool autoConvert = true) {
			if(source is FlowInput flowInput) {
				if(target.primaryFlowOutput != null && target.primaryFlowOutput.hasValidConnections == false) {
					Connection.CreateAndConnect(flowInput, target.primaryFlowOutput);
					return true;
				}
				else {
					var ports = target.FlowOutputs;
					foreach(var port in ports) {
						if(port != null && port.hasValidConnections == false) {
							Connection.CreateAndConnect(flowInput, port);
							return true;
						}
					}
				}
			}
			else if(source is FlowOutput flowOutput) {
				if(target.primaryFlowInput != null) {
					Connection.CreateAndConnect(flowOutput, target.primaryFlowInput);
					return true;
				}
				else {
					var ports = target.FlowInputs;
					foreach(var port in ports) {
						if(port != null) {
							Connection.CreateAndConnect(flowOutput, port);
							return true;
						}
					}
				}
			}
			else if(source is ValueInput valueInput) {
				var type = valueInput.type;
				var ports = target.ValueOutputs;
				foreach(var port in ports) {
					if(port.type.IsCastableTo(type)) {
						var con = Connection.CreateAndConnect(valueInput, port);
						AutoRerouteAndProxy(con, canvas);
						return true;
					}
				}
				if(autoConvert) {
					foreach(var port in ports) {
						if(AutoConvertPort(port.type, type, port, valueInput, (val) => {
							var con = Connection.CreateAndConnect(valueInput, val.nodeObject.primaryValueOutput);
							AutoRerouteAndProxy(con, canvas);
						}, canvas, new FilterAttribute(port.type))) {
							return true;
						}
					}
				}
			}
			else if(source is ValueOutput valueOutput) {
				var type = valueOutput.type;
				var ports = target.ValueInputs;
				foreach(var port in ports) {
					if(type.IsCastableTo(port.type)) {
						var con = Connection.CreateAndConnect(port, valueOutput);
						AutoRerouteAndProxy(con, canvas);
						return true;
					}
				}
				if(autoConvert) {
					foreach(var port in ports) {
						if(AutoConvertPort(type, port.type, valueOutput, port, (val) => {
							var con = Connection.CreateAndConnect(port, val.nodeObject.primaryValueOutput);
							AutoRerouteAndProxy(con, canvas);
						}, canvas, new FilterAttribute(port.type))) {
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool CanAutoConvertType(Type leftType, Type rightType,
			ValueOutput outputPort, ValueInput inputPort,
			UGraphElement canvas,
			FilterAttribute filter = null,
			bool forceConvert = false) {
			if(leftType == null || rightType == null) return false;

			if(leftType is RuntimeType) {
				if(rightType is not RuntimeType && uNodeDatabase.nativeGraphTypes.Contains(rightType)) {
					rightType = ReflectionUtils.GetRuntimeType(rightType);
				}
			}
			else if(rightType is RuntimeType) {
				if(leftType is not RuntimeType && uNodeDatabase.nativeGraphTypes.Contains(leftType)) {
					leftType = ReflectionUtils.GetRuntimeType(leftType);
				}
			}

			if(rightType != typeof(object)) {
				var autoConverts = NodeEditorUtility.FindAutoConvertPorts();
				foreach(var c in autoConverts) {
					c.filter = filter;
					c.leftType = leftType;
					c.rightType = rightType;
					c.canvas = canvas;
					c.input = inputPort;
					c.output = outputPort;
					c.position = inputPort.node.position.position;
					c.force = forceConvert;
					if(c.IsValid()) {
						return true;
					}
				}
			}
			return false;
		}

		internal static bool CanAutoConvertInput(Type inputType, FilterAttribute outputFilter) {
			if(inputType.IsByRef) {
				inputType = inputType.GetElementType();
			}
			if(outputFilter.IsValidTypeSimple(inputType)) {
				if(outputFilter.Types != null && outputFilter.Types.Count > 0) {
					foreach(var typeInFilter in outputFilter.Types) {
						if(CanAutoConvertType(typeInFilter, inputType)) {
							return true;
						}
					}
					return false;
				}
				return true;
			}
			return false;
			
		}

		internal static bool CanAutoConvertOuput(Type outputType, FilterAttribute inputFilter) {
			if(outputType.IsByRef) {
				outputType = outputType.GetElementType();
			}
			if(inputFilter.IsValidTypeSimple(outputType)) {
				if(inputFilter.Types != null && inputFilter.Types.Count > 0) {
					foreach(var typeInFilter in inputFilter.Types) {
						if(CanAutoConvertType(outputType, typeInFilter)) {
							return true;
						}
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public static bool CanAutoConvertType(Type leftType, Type rightType) {
			if(leftType == typeof(string)) {
				if(rightType == typeof(float) ||
					rightType == typeof(int) ||
					rightType == typeof(double) ||
					rightType == typeof(decimal) ||
					rightType == typeof(short) ||
					rightType == typeof(ushort) ||
					rightType == typeof(uint) ||
					rightType == typeof(long) ||
					rightType == typeof(byte) ||
					rightType == typeof(sbyte)) {
					return true;
				}
			}
			if(leftType.IsCastableTo(rightType)) {
				return true;
			}
			var autoConverts = NodeEditorUtility.FindAutoConvertPorts();
			foreach(var c in autoConverts) {
				c.leftType = leftType;
				c.rightType = rightType;
				c.filter = null;
				c.canvas = null;
				c.input = null;
				c.output = null;
				c.position = Vector2.zero;
				c.force = false;
				if(c.IsValid()) {
					return true;
				}
			}
			return false;
		}

		public static bool AutoConvertPort(Type leftType, Type rightType,
			ValueOutput output, ValueInput input,
			Action<Node> action,
			UGraphElement canvas,
			FilterAttribute filter = null,
			bool forceConvert = false) {

			if(leftType is RuntimeType) {
				if(rightType is not RuntimeType && uNodeDatabase.nativeGraphTypes.Contains(rightType)) {
					rightType = ReflectionUtils.GetRuntimeType(rightType);
				}
			}
			else if(rightType is RuntimeType) {
				if(leftType is not RuntimeType && uNodeDatabase.nativeGraphTypes.Contains(leftType)) {
					leftType = ReflectionUtils.GetRuntimeType(leftType);
				}
			}

			if(rightType != typeof(object)) {
				var autoConverts = NodeEditorUtility.FindAutoConvertPorts();
				foreach(var c in autoConverts) {
					c.leftType = leftType;
					c.rightType = rightType;
					c.filter = filter;
					c.canvas = canvas;
					c.input = input;
					c.output = output;
					c.position = input.node.position.position;
					c.force = forceConvert;
					if(c.IsValid()) {
						if(c.CreateNode(action)) {
							return true;
						}
						return false;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Return true if node can be inserted into <paramref name="parent"/>
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static bool CanAddNode(UGraphElement parent) {
			if(parent is NodeContainer) {
				return true;
			}
			else if(parent is NodeObject nodeObject) {
				return nodeObject.node is ISuperNode;
			}
			return parent is ISuperNode;
		}

		/// <summary>
		/// Auto assign input value ports.
		/// </summary>
		/// <param name="nodeObject"></param>
		public static void AutoAssignNodePorts(NodeObject nodeObject) {
			if(nodeObject == null || nodeObject.node == null) return;

			var graph = nodeObject.graphContainer;
			var graphType = graph.GetGraphType();

			bool AssignNodePort(ValueInput port, Type type, FilterAttribute filter) {
				if(port == null || type == null)
					return false;
				if(filter == null)
					filter = FilterAttribute.DefaultTypeFilter;
				if(type != typeof(object) && type.IsPrimitive == false) {
					if(graphType.IsCastableTo(type)) {
						port.AssignToDefault(MemberData.This(graph));
						return true;
					}
				}
				if(type.IsSubclassOf(typeof(Delegate))) {
					if(CanAddNode(nodeObject.parent)) {
						AddNewNode<Nodes.NodeLambda>(nodeObject.parent, "Lambda", null, new Vector2(nodeObject.position.x - 150, nodeObject.position.y), (nod) => {
							nod.delegateType = type;
							nod.Register();
							port.ConnectTo(nod.output);
						}, setUndoGroup: false);
						return true;
					}
				}
				else if(filter.IsValidTypeForValueConstant(type) && ReflectionUtils.CanCreateInstance(type)) {
					port.AssignToDefault(MemberData.CreateValueFromType(type));
					return true;
				}
				else if(graphType != null && (graphType == type || type.IsSubclassOf(graphType))) {
					port.AssignToDefault(MemberData.This(graph));
					return true;
				}
				//else if(CanAutoConvertType(graphType, type)) {
				//	AddNewNode<MultipurposeNode>(nodeObject.parent, new Vector2(nodeObject.position.x - 200, nodeObject.position.y), thisNode => {
				//		thisNode.target = MemberData.This(graph);
				//		thisNode.Register();

				//		AutoConvertPort(graphType, type, thisNode.output, port, convertNode => {
				//			port.ConnectTo(GetPort<ValueOutput>(convertNode));
				//		}, nodeObject.parent);
				//	});
				//	return true;
				//}
				return false;
			}

			foreach(var port in nodeObject.ValueInputs) {
				if(!port.isAssigned && port.CanGetValue()) {
					var type = port.type;
					if(port.filter != null) {
						if(port.filter.SetMember) {
							continue;
						}
						var types = port.filter.Types;
						if(types.Count > 0) {
							for(int i = 0; i < types.Count; i++) {
								if(AssignNodePort(port, types[i], port.filter)) {
									continue;
								}
							}
						}
					}
					AssignNodePort(port, type, port.filter);
				}
			}
		}
		#endregion

		#region Selections
		/// <summary>
		/// Select a node for the graph.
		/// </summary>
		/// <param name="graphData"></param>
		/// <param name="node"></param>
		/// <param name="clearSelectedNodes"></param>
		public static void SelectNode(uNodeEditor.TabData graphData, NodeObject node, bool clearSelectedNodes = true) {
			if(clearSelectedNodes)
				graphData.selectedGraphData.ClearSelection();
			graphData.selectedGraphData.AddToSelection(node);
		}

		/// <summary>
		/// Select a node for the graph.
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="root"></param>
		public static void SelectRoot(uNodeEditor.TabData editorData, NodeContainer root) {
			editorData.selectedGraphData.GetPosition(root);
			editorData.selectedGraphData.AddToSelection(root);
			editorData.selectedGraphData.currentCanvas = root;
		}
		#endregion

		#region Drawer
		private static List<INodeDrawer> _nodeDrawers;
		/// <summary>
		/// Find command menu on create node.
		/// </summary>
		/// <returns></returns>
		public static List<INodeDrawer> FindNodeDrawers() {
			if(_nodeDrawers == null) {
				_nodeDrawers = EditorReflectionUtility.GetListOfType<INodeDrawer>();
				_nodeDrawers.Sort((x, y) => {
					return string.CompareOrdinal(x.order.ToString(), y.order.ToString());
				});
			}
			return _nodeDrawers;
		}

		private static Dictionary<Type, INodeDrawer> _nodeDrawerMap = new Dictionary<Type, INodeDrawer>();
		/// <summary>
		/// Find node drawer for specific node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static INodeDrawer FindNodeDrawer(Node node) {
			return FindNodeDrawer(node.GetType());
		}

		/// <summary>
		/// Find node drawer for specific node.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static INodeDrawer FindNodeDrawer(Type type) {
			INodeDrawer drawer;
			if(!_nodeDrawerMap.TryGetValue(type, out drawer)) {
				var drawers = FindNodeDrawers();
				if(drawers.Count > 0) {
					for(int i = 0; i < drawers.Count; i++) {
						if(drawers[i].IsValid(type)) {
							drawer = drawers[i];
							break;
						}
					}
				}
				_nodeDrawerMap[type] = drawer;
			}
			return drawer;
		}
		#endregion

		#region Menu
		private static List<TransitionMenu> _transitionMenus;
		/// <summary>
		/// Find all transition menu.
		/// </summary>
		/// <returns></returns>
		public static List<TransitionMenu> FindTransitionMenu() {
			if(_transitionMenus == null) {
				_transitionMenus = new List<TransitionMenu>();
				foreach(Assembly assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.GetCustomAttributes(typeof(TransitionMenu), false).Length > 0) {
								TransitionMenu menuItem = (TransitionMenu)type.GetCustomAttributes(typeof(TransitionMenu), false)[0];
								menuItem.type = type;
								_transitionMenus.Add(menuItem);
							}
						}
					}
					catch { continue; }
				}

				_transitionMenus.Sort((x, y) => string.Compare(x.path, y.path, StringComparison.OrdinalIgnoreCase));
			}
			return _transitionMenus;
		}

		private static List<NodeMenu> _nodeMenus;
		/// <summary>
		/// Find all node menu.
		/// </summary>
		/// <returns></returns>
		public static List<NodeMenu> FindNodeMenu() {
			if(_nodeMenus == null) {
				_nodeMenus = new List<NodeMenu>();
				foreach(Assembly assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsDefined(typeof(NodeMenu), false)) {
								NodeMenu menuItem = (NodeMenu)type.GetCustomAttributes(typeof(NodeMenu), false)[0];
								menuItem.type = type;

								if(type.IsCastableTo(typeof(FlowNode))) {
									menuItem.hasFlowInput = true;
									menuItem.hasFlowOutput = true;
								}
								else if(type.IsCastableTo(typeof(BaseFlowNode))) {
									menuItem.hasFlowInput = true;
								}

								if(string.IsNullOrEmpty(menuItem.tooltip) && type.IsDefined(typeof(DescriptionAttribute), false)) {
									var des = (DescriptionAttribute)type.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
									menuItem.tooltip = des.description;
								}
								_nodeMenus.Add(menuItem);
							}
						}
					}
					catch { continue; }
				}
				_nodeMenus.Sort((x, y) => {
					int result = string.Compare(x.category, y.category, StringComparison.OrdinalIgnoreCase);
					if(result == 0) {
						result = CompareUtility.Compare(x.order, y.order);
						if(result == 0) {
							return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
						}
					}
					return result;
				});
			}
			return _nodeMenus;
		}

		private static List<EventMenuAttribute> _eventMenus;
		/// <summary>
		/// Find all node menu.
		/// </summary>
		/// <returns></returns>
		public static List<EventMenuAttribute> FindEventMenu() {
			if(_eventMenus == null) {
				_eventMenus = new List<EventMenuAttribute>();
				foreach(Assembly assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsDefined(typeof(EventMenuAttribute), false) && type.IsCastableTo(typeof(BaseGraphEvent))) {
								EventMenuAttribute menuItem = (EventMenuAttribute)type.GetCustomAttributes(typeof(EventMenuAttribute), false)[0];
								menuItem.type = type;
								//if(string.IsNullOrEmpty(menuItem.tooltip) && type.IsDefined(typeof(DescriptionAttribute), false)) {
								//	var des = (DescriptionAttribute)type.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
								//	menuItem.tooltip = des.description;
								//}
								_eventMenus.Add(menuItem);
							}
						}
					}
					catch { continue; }
				}
				_eventMenus.Sort((x, y) => {
					int result = string.Compare(x.category, y.category, StringComparison.OrdinalIgnoreCase);
					if(result == 0) {
						result = CompareUtility.Compare(x.order, y.order);
						if(result == 0) {
							return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
						}
					}
					return result;
				});
			}
			return _eventMenus;
		}

		private static List<AutoConvertPort> _convertPorts;
		/// <summary>
		/// Find command pin menu.
		/// </summary>
		/// <returns></returns>
		public static List<AutoConvertPort> FindAutoConvertPorts() {
			if(_convertPorts == null) {
				_convertPorts = EditorReflectionUtility.GetListOfType<AutoConvertPort>();
				_convertPorts.Sort((x, y) => {
					return string.CompareOrdinal(x.order.ToString(), y.order.ToString());
				});
			}
			return _convertPorts;
		}

		private static List<NodeMenuCommand> _customNodeCommands;
		public static List<NodeMenuCommand> FindNodeCommands() {
			if(_customNodeCommands == null) {
				_customNodeCommands = EditorReflectionUtility.GetListOfType<NodeMenuCommand>();
				_customNodeCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _customNodeCommands;
		}

		private static List<PortMenuCommand> _customPortCommands;
		/// <summary>
		/// Find command pin menu.
		/// </summary>
		/// <returns></returns>
		public static List<PortMenuCommand> FindPortCommands() {
			if(_customPortCommands == null) {
				_customPortCommands = EditorReflectionUtility.GetListOfType<PortMenuCommand>();
				_customPortCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _customPortCommands;
		}

		private static List<GraphManipulator> _graphManipulators;
		/// <summary>
		/// Find command pin menu.
		/// </summary>
		/// <returns></returns>
		public static List<GraphManipulator> FindGraphManipulators() {
			if(_graphManipulators == null) {
				_graphManipulators = EditorReflectionUtility.GetListOfType<GraphManipulator>();
				_graphManipulators.Sort((x, y) => CompareUtility.Compare(x.order, y.order));
			}
			return _graphManipulators;
		}

		private static List<CustomInputPortItem> _customInputPortItems;
		public static List<CustomInputPortItem> FindCustomInputPortItems() {
			if(_customInputPortItems == null) {
				_customInputPortItems = EditorReflectionUtility.GetListOfType<CustomInputPortItem>();
				_customInputPortItems.Sort((x, y) => {
					return CompareUtility.Compare(x.order, y.order);
				});
			}
			return _customInputPortItems;
		}

		private static List<GraphMenuCommand> _customGraphCommands;
		public static List<GraphMenuCommand> FindGraphCommands() {
			if(_customGraphCommands == null) {
				_customGraphCommands = EditorReflectionUtility.GetListOfType<GraphMenuCommand>();
				_customGraphCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _customGraphCommands;
		}

		private static List<INodeItemCommand> _createNodeMenuCommands;
		/// <summary>
		/// Find command menu on create node.
		/// </summary>
		/// <returns></returns>
		public static List<INodeItemCommand> FindCreateNodeCommands() {
			if(_createNodeMenuCommands == null) {
				_createNodeMenuCommands = EditorReflectionUtility.GetListOfType<INodeItemCommand>();
				_createNodeMenuCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _createNodeMenuCommands;
		}

		private static List<GraphAnalyzer> analizers;
		private static List<GraphAnalyzer> GetAnalizers() {
			if(analizers == null) {
				analizers = EditorReflectionUtility.GetListOfType<GraphAnalyzer>();
				analizers.Sort((x, y) => {
					return CompareUtility.Compare(x.order, y.order);
				});
			}
			return analizers;
		}

		private static Dictionary<Type, List<GraphAnalyzer>> analizerForGraphs = new();
		public static List<GraphAnalyzer> GetGraphAnalizers(Type type) {
			if(analizerForGraphs.TryGetValue(type, out var result) == false) {
				var analizers = GetAnalizers();
				result = new List<GraphAnalyzer>(analizers);
				result.RemoveAll(a => a.IsValidAnalyzerForGraph(type) == false);
				analizerForGraphs[type] = result;
			}
			return result;
		}

		private static Dictionary<Type, List<GraphAnalyzer>> analizerForGraphElements = new();
		public static List<GraphAnalyzer> GetGraphElementAnalizers(Type type) {
			if(analizerForGraphElements.TryGetValue(type, out var result) == false) {
				var analizers = GetAnalizers();
				result = new List<GraphAnalyzer>(analizers);
				result.RemoveAll(a => a.IsValidAnalyzerForElement(type) == false);
				analizerForGraphElements[type] = result;
			}
			return result;
		}

		private static Dictionary<Type, List<GraphAnalyzer>> analizerForNodes = new();
		public static List<GraphAnalyzer> GetNodeAnalizers(Type type) {
			if(analizerForNodes.TryGetValue(type, out var result) == false) {
				var analizers = GetAnalizers();
				result = new List<GraphAnalyzer>(analizers);
				result.RemoveAll(a => a.IsValidAnalyzerForNode(type) == false);
				analizerForNodes[type] = result;
			}
			return result;
		}
		#endregion

		#region MoveNodes
		/// <summary>
		/// Move the node to position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="nodes"></param>
		public static void MoveNodes(Vector2 position, params NodeObject[] nodes) {
			if(nodes.Length == 0)
				throw new ArgumentNullException();
			MoveNodes(nodes, position);
		}

		/// <summary>
		/// Move the node to position
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="position"></param>
		public static void MoveNodes(IEnumerable<NodeObject> nodes, Vector2 position) {
			foreach(var node in nodes) {
				node.position.x += position.x;
				node.position.y += position.y;
			}
		}
		#endregion

		#region TeleportNodes
		/// <summary>
		/// Teleport the node to position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="nodes"></param>
		public static void TeleportNodes(Vector2 position, params NodeObject[] nodes) {
			TeleportNodes(nodes, position);
		}

		/// <summary>
		/// Teleport the node to position
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="position"></param>
		public static void TeleportNodes(IList<NodeObject> nodes, Vector2 position) {
			Vector2 center = Vector2.zero;
			foreach(var node in nodes) {
				center.x += node.position.x;
				center.y += node.position.y;
			}
			center /= nodes.Count;
			foreach(var node in nodes) {
				node.position.x = (node.position.x - center.x) + position.x;
				node.position.y = (node.position.y - center.y) + position.y;
			}
		}
		#endregion

		#region GetNodeRect
		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="node"></param>
		/// <param name="position"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(NodeObject node, Vector2 position, Vector2 size = new Vector2()) {
			return new Rect(node.position.x + position.x, (node.position.y + position.y) - 17, size.x, size.y);
		}

		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(params NodeObject[] nodes) {
			return GetNodeRect(nodes.ToList());
		}

		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(IList<NodeObject> nodes) {
			if(nodes == null || nodes.Count == 0)
				return Rect.zero;
			if(nodes.Count == 1) {
				return nodes[0].position;
			}
			Rect rect = nodes[0].position;
			foreach(var node in nodes) {
				if(rect.width < node.position.x + node.position.width) {
					rect.width = node.position.x + node.position.width;
				}
				if(rect.height < node.position.y + node.position.height) {
					rect.height = node.position.y + node.position.height;
				}
				if(rect.x > node.position.x) {
					rect.x = node.position.x;
				}
				if(rect.y > node.position.y) {
					rect.y = node.position.y;
				}
			}
			rect.width -= rect.x;
			rect.height -= rect.y;
			return rect;
		}

		public static List<NodeObject> GetNodeFromRect(Rect rect, IList<NodeObject> nodes) {
			List<NodeObject> list = new List<NodeObject>();
			foreach(var n in nodes) {
				if(n != null && rect.Overlaps(n.position)) {
					list.Add(n);
				}
			}
			return list;
		}
		#endregion

		#region Others
		public static bool IsValidMenu(NodeMenu menuItem, Type sourceType, NodeFilter nodeFilter, GraphEditorData graphData) {
			//bool isFlowNode = GraphUtility.HasFlowInput(menuItem.type);
			//if(!flowNodes && isFlowNode)
			//	return false;
			//if(isFlowNode && filter.SetMember)
			//	return false;
			//if(graphData.isInMainGraph) {
			//	if(menuItem.includedScopes.Contains(NodeScope.NotFlowGraph)) {
			//		return false;
			//	}
			//}
			if(graphData.selectedRoot is MainGraphContainer) {
				var graph = graphData.graph;
				if(graph is IStateGraph) {
					if(!menuItem.IsValidScope(NodeScope.StateGraph)) {
						return false;
					}
				}
				else if(graph is IMacroGraph) {
					if(menuItem.IsExcludedScope(NodeScope.Macro)) {
						return false;
					}
				}
				else if(graph is ICustomMainGraph mainGraph) {
					var scope = mainGraph.MainGraphScope;
					if(!menuItem.IsValidScope(scope)) {
						return false;
					}
					if(menuItem.IsCoroutine && !mainGraph.AllowCoroutine) {
						return false;
					}
				}
				else if(graphData.currentCanvas == graphData.selectedRoot) {
					return false;
				}
			}
			else {
				if(!menuItem.IsValidScope(NodeScope.Function)) {
					if(menuItem.IsCoroutine == false || menuItem.IsValidScope(NodeScope.Coroutine) == false) {
						return false;
					}
				}
			}
			if(menuItem.IsCoroutine && !graphData.supportCoroutine) {
				return false;
			}
			if(nodeFilter == NodeFilter.None)
				return true;
			if(nodeFilter.HasFlags(NodeFilter.FlowInput)) {
				if(menuItem.hasFlowInput == false)
					return false;
			}
			if(nodeFilter.HasFlags(NodeFilter.FlowOutput)) {
				if(menuItem.hasFlowOutput == false)
					return false;
			}
			if(nodeFilter.HasFlags(NodeFilter.ValueInput)) {
				if(menuItem.outputs != null) {
					foreach(var type in menuItem.outputs) {
						if(type == typeof(object) || type.IsCastableTo(sourceType)) {
							return true;
						}
					}
				}
				return false;
			}
			if(nodeFilter.HasFlags(NodeFilter.ValueOutput)) {
				if(menuItem.inputs != null) {
					foreach(var type in menuItem.inputs) {
						if(type == typeof(object) || sourceType.IsCastableTo(type)) {
							return true;
						}
					}
				}
				return false;
			}
			return true;
		}

		public static Vector2 SnapTo(Vector2 vec, float snap) {
			return new Vector2(SnapTo(vec.x, snap), SnapTo(vec.y, snap));
		}

		public static float SnapTo(float a, float snap) {
			return Mathf.Round(a / snap) * snap;
		}
		#endregion

		#region Place Fits
		public class PlaceFit {
			class PlaceFitData {
				public NodeObject node;

				public List<PlaceFitData> inputs = new();
				public List<PlaceFitData> outputs = new();
				public List<PlaceFitData> flows = new();

				public List<NodeObject> GetNodes() {
					var list = new List<NodeObject>() { node };
					foreach(var data in inputs) {
						list.AddRange(data.GetNodes());
					}
					foreach(var data in outputs) {
						list.AddRange(data.GetNodes());
					}
					foreach(var data in flows) {
						list.AddRange(data.GetNodes());
					}
					return list;
				}
			}

			private static Vector2 flowSpacing = new Vector2(20, 45);
			private static Vector2 valueSpacing = new Vector2(25, 25);

			public static void PlaceFitNodes(NodeObject node) {
				var nodes = CG.Nodes.FindAllConnections(node, true, true, false, true);
				foreach(var n in nodes) {
					if(n.position.width == 0) {
						n.position.width = 200;
					}
					if(n.position.height == 0) {
						n.position.height = 100;
					}
				}
				var exceptionNodes = CG.Nodes.FindAllConnections(node, false, false, true, false);
				exceptionNodes.Remove(node);
				foreach(var n in exceptionNodes) {
					if(nodes.Contains(n)) {
						nodes.Remove(n);
					}
				}
				var data = CreateData(node, nodes, nodes, nodes);
				DoPlaceFit(data);
			}

			private static Rect GetNodeRect(IList<NodeObject> nodes) {
				Rect rect = Rect.zero;
				if(nodes.Count > 0) {
					rect = nodes[0].position;
				}
				foreach(var data in nodes) {
					rect = Encompass(rect, data.position);
				}
				return rect;
			}

			private static Rect Encompass(Rect a, Rect b) {
				Rect result = default(Rect);
				result.xMin = Math.Min(a.xMin, b.xMin);
				result.yMin = Math.Min(a.yMin, b.yMin);
				result.xMax = Math.Max(a.xMax, b.xMax);
				result.yMax = Math.Max(a.yMax, b.yMax);
				return result;
			}

			private static void TeleportNodes(IList<NodeObject> nodes, Vector2 position, bool fromCenter = true) {
				if(fromCenter) {
					Vector2 center = Vector2.zero;
					foreach(var node in nodes) {
						center.x += node.position.x;
						center.y += node.position.y;
					}
					center /= nodes.Count;
					foreach(var node in nodes) {
						node.position.x = (node.position.x - center.x) + position.x;
						node.position.y = (node.position.y - center.y) + position.y;
					}
				}
				else {
					Vector2 pos = Vector2.zero;
					if(nodes.Count > 0) {
						pos = nodes[0].position.position;
					}
					foreach(var node in nodes) {
						var p = node.position;
						if(pos.x > p.x) {
							pos.x = p.x;
						}
						if(pos.y > p.y) {
							pos.y = p.y;
						}
					}
					foreach(var node in nodes) {
						node.position.x = (node.position.x - pos.x) + position.x;
						node.position.y = (node.position.y - pos.y) + position.y;
					}
				}
			}

			private static void DoPlaceFit(PlaceFitData tree) {
				if(tree.inputs.Count > 0) {
					var parentPos = tree.node.position;
					List<NodeObject> listNodes = new List<NodeObject>();
					float offset = 0;
					foreach(var childTree in tree.inputs) {
						DoPlaceFit(childTree);
						var nodes = childTree.GetNodes();
						var totalRect = GetNodeRect(nodes);

						TeleportNodes(nodes, new Vector2(parentPos.x - totalRect.width - valueSpacing.x, parentPos.y + offset));
						offset += totalRect.height + valueSpacing.y;
						listNodes.AddRange(nodes);
					}
					if(tree.inputs.Count == 1) {
						var rect = GetNodeRect(listNodes);
						var sourcePosition = GetNodeRect(new[] { tree.inputs[0].node });
						MoveNodes(listNodes, new Vector2(0, -(sourcePosition.y - rect.y) + (parentPos.height - sourcePosition.height) / 2));
					}
					else {
						MoveNodes(listNodes, new Vector2(0, ((parentPos.height - GetNodeRect(listNodes).height) / 2)));
					}
				}
				if(tree.outputs.Count > 0) {
					var parentPos = tree.node.position;
					List<NodeObject> listNodes = new List<NodeObject>();
					float offset = 0;
					foreach(var childTree in tree.outputs) {
						DoPlaceFit(childTree);
						var nodes = childTree.GetNodes();
						var totalRect = GetNodeRect(nodes);

						TeleportNodes(nodes, new Vector2(parentPos.x + totalRect.width + valueSpacing.x, parentPos.y + offset), false);
						offset += totalRect.height + valueSpacing.y;
						listNodes.AddRange(nodes);
					}
					if(tree.outputs.Count == 1) {
						var rect = GetNodeRect(listNodes);
						var sourcePosition = GetNodeRect(new[] { tree.outputs[0].node } );
						MoveNodes(listNodes, new Vector2(0, -(sourcePosition.y - rect.y) + (parentPos.height - sourcePosition.height) / 2));
					}
					else {
						MoveNodes(listNodes, new Vector2(0, ((parentPos.height - GetNodeRect(listNodes).height) / 2)));
					}
				}
				if(tree.flows.Count > 0) {
					var parentPos = tree.node.position;
					float parentY = parentPos.y + parentPos.height;
					{
						List<NodeObject> nodeViews = new List<NodeObject>();
						if(tree.inputs.Count > 0) {
							foreach(var childTree in tree.inputs) {
								nodeViews.AddRange(childTree.GetNodes());
							}
						}
						if(tree.outputs.Count > 0) {
							foreach(var childTree in tree.outputs) {
								nodeViews.AddRange(childTree.GetNodes());
							}
						}
						if(nodeViews.Count > 0) {
							foreach(var n in nodeViews) {
								var rect = GetNodeRect(new[] { n });
								if(rect.y + rect.height > parentY) {
									parentY = rect.y + rect.height;
								}
							}
						}
					}
					if(tree.flows.Count > 0) {
						List<NodeObject> listNodes = new List<NodeObject>();
						float offset = 0;
						foreach(var childTree in tree.flows) {
							DoPlaceFit(childTree);
							var nodes = childTree.GetNodes();
							var totalRect = GetNodeRect(nodes);
							var dist = Mathf.Abs(GetNodeRect(nodes).width - totalRect.width);

							TeleportNodes(nodes, new Vector2(parentPos.x + offset + dist, parentY + flowSpacing.y), false);
							offset += totalRect.width + flowSpacing.x + dist;
							listNodes.AddRange(nodes);
						}
						if(tree.flows.Count == 1) {
							var rect = GetNodeRect(listNodes);
							var sourcePosition = GetNodeRect(new[] { tree.flows[0].node });
							//var parentPosition = GetNodeRect(tree.node);
							//TeleportNodes(listNodes, new Vector2(parentPosition.x - (sourcePosition.x - rect.x) - ((sourcePosition.width - parentPos.width) / 2), rect.y), false);
							MoveNodes(listNodes, new Vector2(-(sourcePosition.x - rect.x) + (parentPos.width - sourcePosition.width) / 2, 0));
						}
						else {
							var rect = GetNodeRect(listNodes);
							MoveNodes(listNodes, new Vector2(((parentPos.width - rect.width) / 2), 0));
						}
					}
				}
			}

			private static PlaceFitData CreateData(NodeObject node, HashSet<NodeObject> inputs, HashSet<NodeObject> outputs, HashSet<NodeObject> flows) {
				var data = new PlaceFitData() {
					node = node,
				};
				foreach(var port in node.ValueInputs) {
					var targets = port.connections.Where(c => c.isProxy == false).Select(c => c.output.node);
					foreach(var target in targets) {
						if(inputs.Remove(target)) {
							data.inputs.Add(CreateData(target, inputs, outputs, flows));
						}
					}
				}
				foreach(var port in node.ValueOutputs) {
					var targets = port.connections.Where(c => c.isProxy == false).Select(c => c.input.node);
					foreach(var target in targets) {
						if(outputs.Remove(target)) {
							data.outputs.Add(CreateData(target, inputs, outputs, flows));
						}
					}
				}
				foreach(var port in node.FlowOutputs) {
					var targets = port.connections.Where(c => c.isProxy == false).Select(c => c.input.node);
					foreach(var target in targets) {
						if(outputs.Remove(target)) {
							data.flows.Add(CreateData(target, inputs, outputs, flows));
						}
					}
				}
				return data;
			}
		}
		#endregion
	}
}