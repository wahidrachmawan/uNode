using System;
using System.Collections.Generic;
using UnityEngine;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Commands {
	class CustomSetItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(ValueOutput source) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(ItemSelector.CustomItem.Create("Set Value", () => {
				NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (NodeSetValue n) => {
					n.Register();
					n.target.ConnectTo(source);
					var type = source.type;
					if(type != null) {
						if(type.IsSubclassOf(typeof(System.MulticastDelegate))) {
							NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas - new Vector2(100, 0), delegate (NodeLambda node) {
								node.Register();
								Connection.Connect(n.value, node.output);
								n.setType = SetType.Add;
								node.delegateType = type;
							});
						} else {
							n.value.AssignToDefault(MemberData.Default(type));
						}
					}
					graph.Refresh();
				});
			}, "Flow", icon: uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
			return items;
		}

		public override bool IsValidPort(ValueOutput source, PortAccessibility accessibility) {
			return accessibility.CanSet();
		}
	}

	class CustomInputArithmaticItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(ValueOutput source) {
			var type = source.type;
			var items = new List<ItemSelector.CustomItem>();
			if(type.IsPrimitive && type != typeof(bool) && type != typeof(char)) {
				string typeName = type.PrettyName();
				items.Add(ItemSelector.CustomItem.Create(string.Format("Add ({0}, {0})", typeName), () => {
					NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
						n.EnsureRegistered();
						n.inputs[0].type = type;
						n.inputs[0].port.ConnectTo(source);
						n.inputs[1].type = type;
						n.inputs[1].port.AssignToDefault(MemberData.Default(type));
						n.operatorKind = ArithmeticType.Add;
						n.Register();
						graph.Refresh();
					});
				}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(type)));
				items.Add(ItemSelector.CustomItem.Create(string.Format("Subtract ({0}, {0})", typeName), () => {
					NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
						n.EnsureRegistered();
						n.inputs[0].type = type;
						n.inputs[0].port.ConnectTo(source);
						n.inputs[1].type = type;
						n.inputs[1].port.AssignToDefault(MemberData.Default(type));
						n.operatorKind = ArithmeticType.Subtract;
						n.Register();
						graph.Refresh();
					});
				}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(type)));
				items.Add(ItemSelector.CustomItem.Create(string.Format("Divide ({0}, {0})", typeName), () => {
					NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
						n.EnsureRegistered();
						n.inputs[0].type = type;
						n.inputs[0].port.ConnectTo(source);
						n.inputs[1].type = type;
						n.inputs[1].port.AssignToDefault(MemberData.Default(type));
						n.operatorKind = ArithmeticType.Divide;
						n.Register();
						graph.Refresh();
					});
				}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(type)));
				items.Add(ItemSelector.CustomItem.Create(string.Format("Multiply ({0}, {0})", typeName), () => {
					NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
						n.EnsureRegistered();
						n.inputs[0].type = type;
						n.inputs[0].port.ConnectTo(source);
						n.inputs[1].type = type;
						n.inputs[1].port.AssignToDefault(MemberData.Default(type));
						n.operatorKind = ArithmeticType.Multiply;
						n.Register();
						graph.Refresh();
					});
				}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(type)));
				items.Add(ItemSelector.CustomItem.Create(string.Format("Modulo ({0}, {0})", typeName), () => {
					NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
						n.EnsureRegistered();
						n.inputs[0].type = type;
						n.inputs[0].port.ConnectTo(source);
						n.inputs[1].type = type;
						n.inputs[1].port.AssignToDefault(MemberData.Default(type));
						n.operatorKind = ArithmeticType.Modulo;
						graph.Refresh();
					});
				}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(type)));
			}

			var preference = uNodePreference.GetPreference();
			var assemblies = EditorReflectionUtility.GetAssemblies();
			var includedAssemblies = uNodePreference.GetIncludedAssemblies();
			var ns = graph.graphData.GetNamespaces();
			foreach(var assembly in assemblies) {
				if(!includedAssemblies.Contains(assembly.GetName().Name)) {
					continue;
				}
				var operators = EditorReflectionUtility.GetOperators(assembly, (op) => {
					return ns.Contains(op.DeclaringType.Namespace);
				});
				if(operators.Count > 0) {
					foreach(var op in operators) {
						switch(op.Name) {
							case "op_Addition": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ArithmeticType.Add));
								break;
							}
							case "op_Subtraction": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ArithmeticType.Subtract));
								break;
							}
							case "op_Division": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ArithmeticType.Divide));
								break;
							}
							case "op_Multiply": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ArithmeticType.Multiply));
								break;
							}
							case "op_Modulus": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ArithmeticType.Modulo));
								break;
							}
						}
					}
				}
			}
			items.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
			return items;
		}

		private ItemSelector.CustomItem GetItem(Type type, Type param1, Type param2, Type returnType, ValueOutput source, ArithmeticType operatorType) {
			return ItemSelector.CustomItem.Create(string.Format(operatorType.ToString() + " ({0}, {1})", param1.PrettyName(), param2.PrettyName()), () => {
				NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
					n.EnsureRegistered();
					if(param1.IsCastableTo(type)) {
						n.inputs[0].type = param1;
						n.inputs[0].port.ConnectTo(source);
						n.inputs[1].type = param2;
						n.inputs[1].port.AssignToDefault(MemberData.Default(param2));
					} else {
						n.inputs[0].type = param1;
						n.inputs[0].port.AssignToDefault(MemberData.Default(param1));
						n.inputs[1].type = param2;
						n.inputs[1].port.ConnectTo(source);
					}
					n.operatorKind = operatorType;
					graph.Refresh();
				});
			}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(returnType));
		}

		public override bool IsValidPort(ValueOutput source, PortAccessibility accessibility) {
			return true;
		}
	}

    class CustomInputComparisonItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(ValueOutput source) {
			var type = source.type;
			var items = new List<ItemSelector.CustomItem>();
			if(type.IsPrimitive && type != typeof(bool) && type != typeof(char)) {//Primitives
				items.AddRange(GetCustomItemForPrimitives(type, source));
			} else {
				items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.Equal));
				items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.NotEqual));
			}
			var preference = uNodePreference.GetPreference();
			var assemblies = EditorReflectionUtility.GetAssemblies();
			var includedAssemblies = uNodePreference.GetIncludedAssemblies();
			var ns = graph.graphData.GetNamespaces();
			foreach(var assembly in assemblies) {
				if(!includedAssemblies.Contains(assembly.GetName().Name)) {
					continue;
				}
				var operators = EditorReflectionUtility.GetOperators(assembly, (op) => {
					return ns.Contains(op.DeclaringType.Namespace);
				});
				if(operators.Count > 0) {
					foreach(var op in operators) {
						switch(op.Name) {
							//case "op_Equality": {
							//	var parameters = op.GetParameters();
							//	if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
							//		break;
							//	items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, data, ComparisonType.Equal));
							//	break;
							//}
							//case "op_Inequality": {
							//	var parameters = op.GetParameters();
							//	if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
							//		break;
							//	items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, data, ComparisonType.NotEqual));
							//	break;
							//}
							case "op_LessThan": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ComparisonType.LessThan));
								break;
							}
							case "op_GreaterThan": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ComparisonType.GreaterThan));
								break;
							}
							case "op_LessThanOrEqual": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ComparisonType.LessThanOrEqual));
								break;
							}
							case "op_GreaterThanOrEqual": {
								var parameters = op.GetParameters();
								if(parameters[0].ParameterType != type && parameters[1].ParameterType != type)
									break;
								items.Add(GetItem(type, parameters[0].ParameterType, parameters[1].ParameterType, op.ReturnType, source, ComparisonType.GreaterThanOrEqual));
								break;
							}
						}
					}
				}
			}
			items.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
			return items;
		}

		private List<ItemSelector.CustomItem> GetCustomItemForPrimitives(Type type, ValueOutput source) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.Equal));
			items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.GreaterThan));
			items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.GreaterThanOrEqual));
			items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.LessThan));
			items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.LessThanOrEqual));
			items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.NotEqual));
			return items;
		}

		private ItemSelector.CustomItem GetItem(Type type, Type param1, Type param2, Type returnType, ValueOutput source, ComparisonType operatorType) {
			return ItemSelector.CustomItem.Create(string.Format(operatorType.ToString() + " ({0}, {1})", param1.PrettyName(), param2.PrettyName()), () => {
				NodeEditorUtility.AddNewNode(graph.graphData, null, null, mousePositionOnCanvas, (ComparisonNode n) => {
					n.EnsureRegistered();
					n.operatorKind = operatorType;
					if(param1 == param2) {
						n.inputType = param1;
					}
					if(param1.IsCastableTo(type)) {
						n.inputA.ConnectTo(source);
						n.inputB.AssignToDefault(MemberData.Default(param1));
					} else {
						n.inputA.AssignToDefault(MemberData.Default(param2));
						n.inputB.ConnectTo(source);
					}
					n.Register();
					graph.Refresh();
				});
			}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(returnType));
		}
	}
}