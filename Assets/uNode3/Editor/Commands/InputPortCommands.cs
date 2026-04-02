using System;
using System.Collections.Generic;
using UnityEngine;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Commands {
	class CustomInputArithmaticItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(ValueOutput source) {
			var type = source.type;
			var items = new List<ItemSelector.CustomItem>();
			bool canSetValue = source.CanSetValue();
			if(type.IsPrimitive && type != typeof(bool) && type != typeof(char)) {
				string typeName = type.PrettyName();
				items.Add(GetItem(type, type, type, type, source, ArithmeticType.Add));
				items.Add(GetItem(type, type, type, type, source, ArithmeticType.Subtract));
				items.Add(GetItem(type, type, type, type, source, ArithmeticType.Divide));
				items.Add(GetItem(type, type, type, type, source, ArithmeticType.Multiply));
				//items.Add(ItemSelector.CustomItem.Create(string.Format("Modulo ({0} % {0})", typeName), () => {
				//	NodeEditorUtility.AddNewNode(graphEditor.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
				//		n.EnsureRegistered();
				//		n.inputs[0].type = type;
				//		n.inputs[0].port.ConnectTo(source);
				//		n.inputs[1].type = type;
				//		n.inputs[1].port.AssignToDefault(MemberData.Default(type));
				//		n.operatorKind = ArithmeticType.Modulo;
				//		graphEditor.Refresh();
				//	});
				//}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(type)));

				if(canSetValue) {
					items.Add(GetFlowItem(source, SetType.Add));
					items.Add(GetFlowItem(source, SetType.Subtract));
					items.Add(GetFlowItem(source, SetType.Divide));
					items.Add(GetFlowItem(source, SetType.Multiply));
					//items.Add(GetFlowItem(source, SetType.Modulo));
				}
			}

			var preference = uNodePreference.GetPreference();
			var assemblies = EditorReflectionUtility.GetAssemblies();
			var includedAssemblies = uNodePreference.GetIncludedAssemblies();
			var ns = graphEditor.graphData.GetUsingNamespaces();
			foreach(var assembly in assemblies) {
				if(!includedAssemblies.Contains(assembly.GetName().Name)) {
					continue;
				}
				var operators = EditorReflectionUtility.GetOperators(assembly, (op) => {
					return ns.Contains(op.DeclaringType.Namespace) || op.DeclaringType == type;
				});
				if(operators != null) {
					foreach(var op in operators) {
						switch(op.Name) {
							case "op_Addition": {
								var parameters = op.GetParameters();
								var paramType = new [] {
									parameters[0].ParameterType.IsByRef ? parameters[0].ParameterType.GetElementType() : parameters[0].ParameterType,
									parameters[1].ParameterType.IsByRef ? parameters[1].ParameterType.GetElementType() : parameters[1].ParameterType};
								if(paramType[0] != type && paramType[1] != type)
									break;
								items.Add(GetItem(type, paramType[0], paramType[1], op.ReturnType, source, ArithmeticType.Add));
								if(canSetValue && paramType[0] == paramType[1]) {
									items.Add(GetFlowItem(source, SetType.Add));
								}
								break;
							}
							case "op_Subtraction": {
								var parameters = op.GetParameters();
								var paramType = new [] {
									parameters[0].ParameterType.IsByRef ? parameters[0].ParameterType.GetElementType() : parameters[0].ParameterType,
									parameters[1].ParameterType.IsByRef ? parameters[1].ParameterType.GetElementType() : parameters[1].ParameterType};
								if(paramType[0] != type && paramType[1] != type)
									break;
								items.Add(GetItem(type, paramType[0], paramType[1], op.ReturnType, source, ArithmeticType.Subtract));
								if(canSetValue && paramType[0] == paramType[1]) {
									items.Add(GetFlowItem(source, SetType.Subtract));
								}
								break;
							}
							case "op_Division": {
								var parameters = op.GetParameters();
								var paramType = new [] {
									parameters[0].ParameterType.IsByRef ? parameters[0].ParameterType.GetElementType() : parameters[0].ParameterType,
									parameters[1].ParameterType.IsByRef ? parameters[1].ParameterType.GetElementType() : parameters[1].ParameterType};
								if(paramType[0] != type && paramType[1] != type)
									break;
								items.Add(GetItem(type, paramType[0], paramType[1], op.ReturnType, source, ArithmeticType.Divide));
								if(canSetValue && paramType[0] == paramType[1]) {
									items.Add(GetFlowItem(source, SetType.Divide));
								}
								break;
							}
							case "op_Multiply": {
								var parameters = op.GetParameters();
								var paramType = new [] {
									parameters[0].ParameterType.IsByRef ? parameters[0].ParameterType.GetElementType() : parameters[0].ParameterType,
									parameters[1].ParameterType.IsByRef ? parameters[1].ParameterType.GetElementType() : parameters[1].ParameterType};
								if(paramType[0] != type && paramType[1] != type)
									break;
								items.Add(GetItem(type, paramType[0], paramType[1], op.ReturnType, source, ArithmeticType.Multiply));
								if(canSetValue && paramType[0] == paramType[1]) {
									items.Add(GetFlowItem(source, SetType.Multiply));
								}
								break;
							}
							//case "op_Modulus": {
							//	var parameters = op.GetParameters();
							//	var paramType = new [] {
							//		parameters[0].ParameterType.IsByRef ? parameters[0].ParameterType.GetElementType() : parameters[0].ParameterType,
							//		parameters[1].ParameterType.IsByRef ? parameters[1].ParameterType.GetElementType() : parameters[1].ParameterType};
							//	if(paramType[0] != type && paramType[1] != type)
							//		break;
							//	items.Add(GetItem(type, paramType[0], paramType[1], op.ReturnType, source, ArithmeticType.Modulo));
							//	if(canSetValue && paramType[0] == paramType[1]) {
							//		items.Add(GetFlowItem(source, SetType.Modulo));
							//	}
							//	break;
							//}
						}
					}
				}
			}
			items.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
			return items;
		}

		private ItemSelector.CustomItem GetItem(Type type, Type param1, Type param2, Type returnType, ValueOutput source, ArithmeticType operatorType) {
			return ItemSelector.CustomItem.Create(operatorType.ToString() + $" ({param1.PrettyName()} {uNodeUtility.GetNicelyDisplayName(operatorType)} {param2.PrettyName()})", () => {
				NodeEditorUtility.AddNewNode(graphEditor.graphData, null, null, mousePositionOnCanvas, (MultiArithmeticNode n) => {
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
					graphEditor.Refresh();
				});
			}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(returnType));
		}

		private ItemSelector.CustomItem GetFlowItem(ValueOutput source, SetType operatorType) {
			return ItemSelector.CustomItem.Create(operatorType.ToString() + $" ({uNodeUtility.GetNicelyDisplayName(operatorType)})", () => {
				NodeEditorUtility.AddNewNode(graphEditor.graphData, null, null, mousePositionOnCanvas, (NodeSetValue n) => {
					n.EnsureRegistered();
					n.target.ConnectTo(source);
					n.setType = operatorType;
					graphEditor.Refresh();
				});
			}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon)));
		}

		public override bool IsValidPort(ValueOutput source, PortAccessibility accessibility) {
			return true;
		}
	}

    class CustomInputComparisonItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(ValueOutput source) {
			var type = source.type;
			var items = new List<ItemSelector.CustomItem>();
			if(type.IsPrimitive && type != typeof(bool) && type != typeof(char) && type != typeof(float)) {//Primitives
				items.AddRange(GetCustomItemForPrimitives(type, source));
			} else {
				items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.Equal));
				items.Add(GetItem(type, type, type, typeof(bool), source, ComparisonType.NotEqual));
			}
			var preference = uNodePreference.GetPreference();
			var assemblies = EditorReflectionUtility.GetAssemblies();
			var includedAssemblies = uNodePreference.GetIncludedAssemblies();
			var ns = graphEditor.graphData.GetUsingNamespaces();
			foreach(var assembly in assemblies) {
				if(!includedAssemblies.Contains(assembly.GetName().Name)) {
					continue;
				}
				var operators = EditorReflectionUtility.GetOperators(assembly, (op) => {
					return ns.Contains(op.DeclaringType.Namespace) || op.DeclaringType == type;
				});
				if(operators != null) {
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
			return ItemSelector.CustomItem.Create(operatorType.ToString() + $" ({param1.PrettyName()} {uNodeUtility.GetDisplayName(operatorType)} {param2.PrettyName()})", () => {
				NodeEditorUtility.AddNewNode(graphEditor.graphData, null, null, mousePositionOnCanvas, (ComparisonNode n) => {
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
					graphEditor.Refresh();
				});
			}, "Operator", icon: uNodeEditorUtility.GetTypeIcon(returnType));
		}
	}
}