using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class MultiArithmeticDrawer : NodeDrawer<Nodes.MultiArithmeticNode> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			UInspector.Draw(option.property[nameof(node.operatorKind)]);

			uNodeGUI.DrawCustomList(node.inputs, "Inputs",
				drawElement: (position, index, value) => {
					position = EditorGUI.PrefixLabel(position, new GUIContent("Type"));
					uNodeGUIUtility.DrawTypeDrawer(position, node.inputs[index].type, GUIContent.none, (type) => {
						node.inputs[index].type = type;
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}, targetObject: option.unityObject);
				},
				add: (position) => {
					ItemSelector.ShowType(null, null, member => {
						option.RegisterUndo();
						var type = member.startType;
						node.inputs.Add(new Nodes.MultiArithmeticNode.PortData() {
							type = type,
						});
						node.Register();
						node.inputs.Last().port.AssignToDefault(MemberData.Default(type));
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}).ChangePosition(GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
				},
				remove: (index) => {
					if(node.inputs.Count > 2) {
						option.RegisterUndo();
						node.inputs.RemoveAt(index);
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
						//Re-register the node for fix errors on showing inputs port summaries.
						node.Register();
					}
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.inputs, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.inputs, oldIndex, newIndex);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				});
			if(GUILayout.Button(new GUIContent("Change Operator"))) {
				var customItems = new List<ItemSelector.CustomItem>();
				{//Primitives
					customItems.AddRange(GetCustomItemForPrimitives(node, typeof(int)));
					customItems.AddRange(GetCustomItemForPrimitives(node, typeof(float)));
				}
				var ns = node.nodeObject.graphContainer.GetUsingNamespaces();
				var preference = uNodePreference.GetPreference();
				var assemblies = EditorReflectionUtility.GetAssemblies();
				var includedAssemblies = uNodePreference.GetIncludedAssemblies();
				foreach(var assembly in assemblies) {
					if(!includedAssemblies.Contains(assembly.GetName().Name)) {
						continue;
					}
					var operators = EditorReflectionUtility.GetOperators(assembly, (op) => {
						return ns == null || ns.Contains(op.DeclaringType.Namespace);
					});
					if(operators.Count > 0) {
						foreach(var op in operators) {
							switch(op.Name) {
								case "op_Addition": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Add));
									break;
								}
								case "op_Subtraction": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Subtract));
									break;
								}
								case "op_Division": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Divide));
									break;
								}
								case "op_Multiply": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Multiply));
									break;
								}
								case "op_Modulus": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Modulo));
									break;
								}
							}
						}
					}
				}
				customItems.Sort((x, y) => {
					if(x.category == y.category) {
						return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
					}
					return string.Compare(x.category, y.category, StringComparison.OrdinalIgnoreCase);
				});
				if(customItems.Count > 0) {
					ItemSelector.ShowWindow(null, null, null, customItems).
						ChangePosition(
							GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect())
						).displayDefaultItem = false;
				}
			}
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}

		private static List<ItemSelector.CustomItem> GetCustomItemForPrimitives(Nodes.MultiArithmeticNode source, Type type) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Add));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Divide));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Modulo));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Multiply));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Subtract));
			return items;
		}

		private static ItemSelector.CustomItem GetCustomItem(Nodes.MultiArithmeticNode source, Type param1, Type param2, Type declaredType, Type returnType, ArithmeticType operatorType) {
			return ItemSelector.CustomItem.Create(string.Format(operatorType.ToString() + " ({0}, {1})", param1.PrettyName(), param2.PrettyName()), () => {
				uNodeEditorUtility.RegisterUndo(source.nodeObject.GetUnityObject());
				source.operatorKind = operatorType;
				while(source.inputs.Count > 2) {
					source.inputs.RemoveAt(source.inputs.Count - 1);
				}
				source.inputs[0].type = param1;
				source.inputs[1].type = param2;
				source.EnsureRegistered();
				source.inputs[0].port.AssignToDefault(ReflectionUtils.CreateInstance(param1));
				source.inputs[1].port.AssignToDefault(ReflectionUtils.CreateInstance(param2));
				uNodeGUIUtility.GUIChanged(source, UIChangeType.Average);
			}, declaredType.PrettyName() + " : Operator", icon: uNodeEditorUtility.GetTypeIcon(returnType));
		}
	}
}