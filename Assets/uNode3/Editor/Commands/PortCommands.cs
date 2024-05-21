using System;
using UnityEngine;
using UnityEditor;
using MaxyGames.UNode.Nodes;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors.Commands {
	#region Macro Commands
	class CreateMacroFlowInputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "New Macro Port";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, delegate (MacroPortNode n) {
				n.nodeObject.name = data.portName;
				n.kind = PortKind.FlowInput;
				n.position = source.position;
				n.nodeObject.position.y -= 100;
				n.Register();
				n.exit.ConnectTo(data.port);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.FlowInput)
				return false;
			return source.nodeObject.parent is NodeObject parent && parent.node is IMacro || source.nodeObject.graphContainer is IMacroGraph;
		}
	}

	class CreateMacroValueInputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "New Macro Port";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, delegate (MacroPortNode n) {
				n.type = data.portType;
				n.nodeObject.name = data.portName;
				n.kind = PortKind.ValueInput;
				n.position = source.position;
				n.nodeObject.position.x -= 100;
				n.Register();
				n.output.ConnectTo(data.port);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			return source.nodeObject.parent is NodeObject parent && parent.node is IMacro || source.nodeObject.graphContainer is IMacroGraph;
		}
	}

	class CreateMacroFlowOutputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "New Macro Port";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, delegate (MacroPortNode n) {
				n.nodeObject.name = data.portName;
				n.kind = PortKind.FlowOutput;
				n.position = source.position;
				n.nodeObject.position.x -= 100;
				n.Register();
				n.enter.ConnectTo(data.port);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.FlowOutput)
				return false;
			return source.nodeObject.parent is NodeObject parent && parent.node is IMacro || source.nodeObject.graphContainer is IMacroGraph;
		}
	}

	class CreateMacroValueOutputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "New Macro Port";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, delegate (MacroPortNode n) {
				n.type = data.portType;
				n.nodeObject.name = data.portName;
				n.kind = PortKind.ValueOutput;
				n.position = source.position;
				n.nodeObject.position.x -= 100;
				n.Register();
				n.input.ConnectTo(data.port);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueOutput)
				return false;
			return source.nodeObject.parent is NodeObject parent && parent.node is IMacro || source.nodeObject.graphContainer is IMacroGraph;
		}
	}
	#endregion

	class GetInstanceOutputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Get instance";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			Type type = data.portType;
			FilterAttribute filter = new FilterAttribute {
				VoidType = true,
				MaxMethodParam = int.MaxValue,
				Public = true,
				Instance = true,
				Static = false,
				UnityReference = false,
				InvalidTargetType = MemberData.TargetType.Null | MemberData.TargetType.Values,
				// DisplayDefaultStaticType = false
			};
			List<ItemSelector.CustomItem> customItems;
			if(type is RuntimeType) {
				(type as RuntimeType).Update();
			}
			customItems = ItemSelector.MakeCustomItems(type, filter, "Data Members", "Data Members ( Inherited )");
			var usingNamespaces = source.nodeObject.graphContainer.GetUsingNamespaces();
			if(usingNamespaces != null && usingNamespaces.Count > 0) {
				customItems.AddRange(ItemSelector.MakeExtensionItems(type, usingNamespaces, filter, "Extensions"));
			}
			{//Custom input port items.
				if(customItems == null) {
					customItems = new List<ItemSelector.CustomItem>();
				}
				var customInputItems = NodeEditorUtility.FindCustomInputPortItems();
				if(customInputItems != null && customInputItems.Count > 0) {
					foreach(var c in customInputItems) {
						c.graph = graph;
						c.mousePositionOnCanvas = mousePositionOnCanvas;
						if(c.IsValidPort(data.port as ValueOutput, PortAccessibility.ReadOnly)) {
							var items = c.GetItems(data.port as ValueOutput);
							if(items != null) {
								customItems.AddRange(items);
							}
						}
					}
				}
			}
			if(customItems != null) {
				filter.Static = true;
				customItems.Sort((x, y) => {
					if(x.category != y.category) {
						return string.Compare(x.category, y.category, StringComparison.OrdinalIgnoreCase);
					}
					return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
				});
				ItemSelector w = ItemSelector.ShowWindow(source, filter, (MemberData mData) => {
					NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultipurposeNode nod) => {
						nod.target = mData;
						nod.Register();
						if(nod.instance != null) {
							nod.instance.ConnectTo(data.port);
						} else {
							foreach(var p in nod.parameters) {
								if(p.input != null && type.IsCastableTo(p.info.ParameterType)) {
									p.input.ConnectTo(data.port);
									break;
								}
							}
						}
						graph.Refresh();
					});
				}, customItems: customItems).ChangePosition(GUIUtility.GUIToScreenPoint(mousePosition));
				w.displayRecentItem = false;
				w.displayNoneOption = false;
			}
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueOutput)
				return false;
			return data.port == source.nodeObject.primaryValueOutput || source is Node node && node.CanGetValue();
		}
	}

	class SetInstanceOutputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Set instance";
			}
		}

		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			Type type = data.portType;
			if(type.IsSubclassOf(typeof(System.MulticastDelegate))) {
				NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, delegate (EventHook n) {
					n.EnsureRegistered();
					n.target.ConnectTo(data.port);
				});
			} else {
				NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, delegate (NodeSetValue n) {
					n.EnsureRegistered();
					n.target.ConnectTo(data.port);
					if (type.IsSubclassOf(typeof(System.MulticastDelegate))) {
						NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, delegate (NodeLambda node) {
							node.EnsureRegistered();
							n.value.ConnectTo(node.nodeObject.primaryValueOutput);
							n.setType = SetType.Add;
							node.delegateType = type;
						});
					} else {
						n.value.AssignToDefault(MemberData.Default(type));
					}
				});
			}
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueOutput)
				return false;
			return (data.port as ValueOutput).CanSetValue();
		}
	}

	class SetInstanceFieldOutputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Set instance field";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			Type type = data.portType;
			FilterAttribute filter = new FilterAttribute();
			filter.VoidType = false;
			filter.MaxMethodParam = int.MaxValue;
			filter.SetMember = true;
			filter.Public = true;
			filter.Instance = true;
			filter.Static = false;
			filter.DisplayDefaultStaticType = false;
			var customItems = ItemSelector.MakeCustomItems(type, filter, type.PrettyName());
			if(customItems != null) {
				ItemSelector w = ItemSelector.ShowWindow(source, filter, delegate (MemberData mData) {
					NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultipurposeNode n) => {
						n.target = mData;
						n.Register();
						if(n.instance != null) {
							n.instance.ConnectTo(data.port);
						}
						NodeEditorUtility.AddNewNode(graph.graphData, null, null,
							new Vector2(mousePositionOnCanvas.x + n.position.width + 150, mousePositionOnCanvas.y),
							(NodeSetValue SV) => {
								SV.target.ConnectTo(n.output);
							});
					});
					graph.Refresh();
				}, customItems: customItems).ChangePosition(GUIUtility.GUIToScreenPoint(mousePosition));
				w.displayDefaultItem = false;
			}
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueOutput)
				return false;
			return (data.port as ValueOutput).CanSetValue();
		}
	}

	class PromoteToNodeInputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Promote to node";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Promote to node");
				Undo.RegisterCompleteObjectUndo(unityObject, "Promote to node");
			}
			MemberData m = (data.port as ValueInput).defaultValue;
			if(data.portType != null && (!m.isAssigned || m.type == null)) {
				m.CopyFrom(MemberData.Default(data.portType));
			}
			NodeEditorUtility.AddNewNode<MultipurposeNode>(graph.graphData, null, null, new Vector2(mousePositionOnCanvas.x - 100, mousePositionOnCanvas.y), (node) => {
				node.target = new MemberData(m);
				node.Register();
				node.output.ConnectTo(data.port);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			var port = data.port as ValueInput;
			if(port.isAssigned && port.UseDefaultValue) {
				return true;
			}
			return false;
		}
	}

	class ToNodeInputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Assign to value constant";
			}
		}
		public override bool onlyContextMenu => false;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("To value node");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "To value node");
			}
			NodeEditorUtility.AddNewNode<MultipurposeNode>(graph.graphData, null, null, new Vector2(source.position.x - 100, source.position.y), (node) => {
				node.target = MemberData.CreateFromValue(ReflectionUtils.CreateInstance(filter.Types[0]), filter.Types[0]);
				node.Register();
				node.output.ConnectTo(data.port);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			var port = data.port as ValueInput;
			if(port == null || port.type == null)
				return false;
			return filter.IsValidTypeForValueConstant(port.type) && port.type.IsByRef == false && filter.SetMember == false;
		}
	}

	class ToConstructorInputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Assign to contructor node";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Assign to contructor node");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "Assign to contructor node");
			}
			var position = new Vector2(source.position.x - 100, source.position.y);
			var type = filter.GetActualType();
			if(type.IsByRef) {
				type = type.GetElementType();
			}
			if(type.IsArray) {
				NodeEditorUtility.AddNewNode<MakeArrayNode>(graph.graphData, null, null, position, (node) => {
					node.elementType = type.ElementType();
					node.Register();
					node.output.ConnectTo(data.port);
				});
			}
			else {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(graph.graphData, null, null, position, (node) => {
					var ctors = type.GetConstructors(MemberData.flags);
					if(ctors != null && ctors.Length > 0) {
						System.Reflection.ConstructorInfo ctor = null;
						foreach(var c in ctors) {
							if(ctor == null) {
								ctor = c;
							}
							else if(ctor.GetParameters().Length > c.GetParameters().Length) {
								ctor = c;
							}
						}
						node.target = new MemberData(ctor);
					}
					else {
						node.target = new MemberData(type.Name + ".ctor", type, MemberData.TargetType.Constructor);
					}
					node.Register();
					node.output.ConnectTo(data.port);
				});
			}
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			//var port = data.port as ValueInput;
			//if(port.isAssigned) {
			//	return false;
			//} else 
			if(filter != null && filter.Types != null && filter.Types.Count > 0) {
				Type t = filter.GetActualType();
				if(ReflectionUtils.CanCreateInstance(t) && !t.IsPrimitive && t != typeof(string)) {
					return true;
				}
			}
			return false;
		}
	}

	class PromoteToVariableOutputPortCommand : PortMenuCommand {
		Type type;

		public override string name {
			get {
				return "Promote to variable";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Promote to variable");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "Promote to variable");
			}
			var port = data.port as ValueOutput;
			if(type != null) {
				NodeEditorUtility.AddNewVariable(source.nodeObject.graph.variableContainer, "", type, (variable) => {
					if(type.IsByRef) {
						variable.modifier.SetPrivate();
					}
					NodeEditorUtility.AddNewNode(graph.graphData, mousePositionOnCanvas, (NodeSetValue setNode) => {
						setNode.target.AssignToDefault(MemberData.CreateFromValue(variable));
						setNode.value.ConnectTo(port);
					});
				});
			}
			uNodeGUIUtility.GUIChanged(source.nodeObject.graphContainer, UIChangeType.Important);
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueOutput)
				return false;
			var port = data.port as ValueOutput;
			type = filter != null ? filter.GetActualType() : data.portType ?? typeof(object);
			if(type != null) {
				if(type.IsByRef) {
					type = type.GetElementType();
				}
				return true;
			}
			return false;
		}
	}

	class PromoteToVariableInputPortCommand : PortMenuCommand {
		Type type;

		public override string name {
			get {
				return "Promote to variable";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Promote to variable");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "Promote to variable");
			}
			var port = data.port as ValueInput;
			if(type != null) {
				NodeEditorUtility.AddNewVariable(source.nodeObject.graph.variableContainer, "", type.IsByRef ? type.GetElementType() : type, (variable) => {
					var m = port.defaultValue;
					if(m.isAssigned && !type.IsByRef) {
						variable.defaultValue = m.Get(null);
					} else if(type.IsByRef) {
						variable.modifier.SetPrivate();
					}
					port.AssignToDefault(MemberData.CreateFromValue(variable));
				});
			}
			uNodeGUIUtility.GUIChanged(source.nodeObject.graphContainer, UIChangeType.Important);
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			var port = data.port as ValueInput;
			type = filter != null ? filter.GetActualType() : data.portType ?? typeof(object);
			if(port.UseDefaultValue && type != null) {
				if(type.IsByRef) {
					type = type.GetElementType();
				}
				return true;
			}
			return false;
		}
	}

	class PromoteToVariableNodeInputPortCommand : PortMenuCommand {
		Type type;

		public override string name {
			get {
				return "Promote to variable node";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Promote to variable node");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "Promote to variable node");
			}
			var port = data.port as ValueInput;
			if(type != null) {
				NodeEditorUtility.AddNewVariable(source.nodeObject.graph.variableContainer, "", type.IsByRef ? type.GetElementType() : type, (variable) => {
					var m = port.defaultValue;
					if(m.isAssigned && !type.IsByRef) {
						variable.defaultValue = m.Get(null);
					} else if(type.IsByRef) {
						variable.modifier.SetPrivate();
					}
					NodeEditorUtility.AddNewNode<MultipurposeNode>(graph.graphData, null, null, new Vector2(source.position.x - 100, source.position.y), (node) => {
						node.target = MemberData.CreateFromValue(variable);
						node.Register();
						port.ConnectTo(node.output);
					});
				});
				graph.Refresh();
				uNodeGUIUtility.GUIChanged(source.nodeObject.graphContainer, UIChangeType.Important);
			}
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			var port = data.port as ValueInput;
			type = filter != null ? filter.GetActualType() : data.portType ?? typeof(object);
			if(port.UseDefaultValue && type != null) {
				if(type.IsByRef) {
					type = type.GetElementType();
				}
				return true;
			}
			return false;
		}
	}

	class AssignToDelegateInputPortCommand : PortMenuCommand {
		Type type;

		public override string name {
			get {
				return "Assign to anonymous function";
			}
		}

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Assign to anonymous function");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "Assign to anonymous function");
			}
			var pos = mousePositionOnCanvas != Vector2.zero ? mousePositionOnCanvas : new Vector2(source.position.x - 100, source.position.y);
			NodeEditorUtility.AddNewNode<NodeAnonymousFunction>(graph.graphData, null, null,
				pos,
				(node) => {
					var method = type.GetMethod("Invoke");
					if(method != null) {
						node.returnType = method.ReturnType;
						foreach(var p in method.GetParameters()) {
							node.parameters.Add(new NodeAnonymousFunction.ParameterData() {
								name = p.Name,
								type = p.ParameterType,
							});
						}
					}
					node.Register();
					node.output.ConnectTo(data.port);
					graph.Refresh();
				});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			type = filter != null ? filter.GetActualType() : typeof(object);
			if(/*!data.port.isConnected && */type != null && type.IsCastableTo(typeof(Delegate))) {
				if(filter != null && filter.SetMember) {
					return false;
				}
				return true;
			}
			return false;
		}
	}

	class AssignToLambdaInputPortCommand : PortMenuCommand {
		Type type;

		public override string name {
			get {
				return "Assign to lambda";
			}
		}

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Assign to lambda");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "Assign to lambda");
			}
			var pos = mousePositionOnCanvas != Vector2.zero ? mousePositionOnCanvas : new Vector2(source.position.x - 100, source.position.y);
			NodeEditorUtility.AddNewNode<NodeLambda>(graph.graphData, null, null,
				pos,
				(node) => {
					node.delegateType = type;
					node.Register();
					node.output.ConnectTo(data.port);
					graph.Refresh();
				});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			type = filter != null ? filter.GetActualType() : typeof(object);
			if(/*!data.port.isConnected && */type != null && type.IsCastableTo(typeof(Delegate))) {
				if(filter != null && filter.SetMember) {
					return false;
				}
				return true;
			}
			return false;
		}
	}

	class PromoteToLocalVariableInputPortCommand : PortMenuCommand {
		Type type;

		public override string name {
			get {
				return "Promote to local variable";
			}
		}
		public override bool onlyContextMenu => true;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			if(source.nodeObject.graphContainer is UnityEngine.Object unityObject) {
				Undo.SetCurrentGroupName("Promote to local variable");
				Undo.RegisterFullObjectHierarchyUndo(unityObject, "Promote to local variable");
			}
			var port = data.port as ValueInput;
			if(type != null) {
				NodeEditorUtility.AddNewVariable(graph.graphData.selectedRoot.variableContainer, "", type.IsByRef ? type.GetElementType() : type, (variable) => {
					var m = port.defaultValue;
					if(m.isAssigned && !type.IsByRef) {
						variable.defaultValue = m.Get(null);
					}
					else if(type.IsByRef) {
						variable.modifier.SetPrivate();
					}
					port.AssignToDefault(MemberData.CreateFromValue(variable));
				});
			}
			uNodeGUIUtility.GUIChanged(source.nodeObject.graphContainer, UIChangeType.Important);
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueInput)
				return false;
			if(graph.graphData.selectedRoot is ILocalVariableSystem) {
				var port = data.port as ValueInput;
				type = filter != null ? filter.GetActualType() : data.portType ?? typeof(object);
				if(port.UseDefaultValue && type != null) {
					if(type.IsByRef) {
						type = type.GetElementType();
					}
					return true;
				}
			}
			return false;
		}
	}

	class ReroutePortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Reroute";
			}
		}

		public override int order => 100;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode(graph.graphData, mousePositionOnCanvas, (NodeReroute n) => {
				switch(data.portKind) {
					case PortKind.FlowInput:
						n.kind = NodeReroute.RerouteKind.Flow;
						n.Register();
						n.exit.ConnectTo(data.port);
						break;
					case PortKind.FlowOutput:
						n.kind = NodeReroute.RerouteKind.Flow;
						n.Register();
						n.enter.ConnectTo(data.port);
						break;
					case PortKind.ValueInput:
						n.kind = NodeReroute.RerouteKind.Value;
						n.Register();
						n.output.ConnectTo(data.port);
						break;
					case PortKind.ValueOutput:
						n.kind = NodeReroute.RerouteKind.Value;
						n.Register();
						n.input.ConnectTo(data.port);
						if(uNodePreference.preferenceData.autoProxyConnection) {
							if(graph.graphData.graphLayout == GraphLayout.Vertical) {
								if(source.position.center.x > mousePositionOnCanvas.x) {
									n.input.connections[0].isProxy = true;
								}
							}
						}
						break;
				}
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			return true;
		}
	}

	class CacheOutputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Cache output";
			}
		}

		public override int order => 100;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode(graph.graphData, mousePositionOnCanvas, (CacheNode n) => {
				n.target.ConnectTo(data.port);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind != PortKind.ValueOutput)
				return false;
			return (data.port as ValueOutput).CanGetValue();
		}
	}

	class ExposeOutputPortCommand : PortMenuCommand {
		public override string name {
			get {
				return "Expose output";
			}
		}
		public override int order => 100;

		public override void OnClick(Node source, PortCommandData data, Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode(graph.graphData, mousePositionOnCanvas, (ExposedNode n) => {
				n.value.ConnectTo(data.port);
				n.Register();
				n.Refresh(true);
			});
			graph.Refresh();
		}

		public override bool IsValidPort(Node source, PortCommandData data) {
			if(data.portKind == PortKind.ValueOutput) {
				if(data.portType != null && (data.portType.IsPrimitive || data.portType == typeof(string))) {
					return false;
				}
				return (data.port as ValueOutput).CanGetValue();
			}
			return false;
		}
	}
}