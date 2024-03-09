using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MaxyGames.UNode.Editors.Drawer {
    public class FormulaDrawer : NodeDrawer<Nodes.FormulaNode> {
		private static FilterAttribute filter = new FilterAttribute() {
			DisplayGeneratedRuntimeType = false,
			ValidTargetType = MemberData.TargetType.Null,
		};

		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			EditorGUI.BeginChangeCheck();
			UInspector.Draw(option.property[nameof(node.kind)]);
			if(EditorGUI.EndChangeCheck()) {
				node.rawAssembly = new byte[0];
				uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
			}

			EditorGUILayout.LabelField("Formula:");
			string formulaCode = EditorGUILayout.TextArea(node.formula);
			if(formulaCode != node.formula) {
				node.rawAssembly = new byte[0];
				node.formula = formulaCode;
				uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
			}

			GUILayout.Space(5);
			EditorGUI.BeginChangeCheck();
			UInspector.Draw(option.property[nameof(node.outputType)]);
			if(EditorGUI.EndChangeCheck()) {
				node.rawAssembly = new byte[0];
				uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
			}

			uNodeGUI.DrawCustomList(node.inputs, "Inputs",
				drawElement: (position, index, value) => {
					position.height = EditorGUIUtility.singleLineHeight;
					uNodeGUIUtility.EditValue(position, new GUIContent("name"), value.name, typeof(string), val => {
						node.inputs[index].name = val as string;
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					});
					position.y += EditorGUIUtility.singleLineHeight;
					position = EditorGUI.PrefixLabel(position, new GUIContent("Type"));
					uNodeGUIUtility.DrawTypeDrawer(position, node.inputs[index].type, GUIContent.none, (type) => {
						node.inputs[index].type = type;
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}, targetObject: option.unityObject, filter: filter);
				},
				add: (position) => {
					ItemSelector.ShowType(null, null, member => {
						option.RegisterUndo();
						var type = member.startType;
						node.inputs.Add(new Nodes.FormulaNode.InputData() {
							name = "input" + node.inputs.Count,
						});
						node.Register();
						node.inputs.Last().port.AssignToDefault(MemberData.Default(type));
						node.inputs.Last().type = type;
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}).ChangePosition(GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
				},
				remove: (index) => {
					option.RegisterUndo();
					node.inputs.RemoveAt(index);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					//Re-register the node for fix errors on showing inputs port summaries.
					node.Register();
				},
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.inputs, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.inputs, oldIndex, newIndex);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				elementHeight: (index) => EditorGUIUtility.singleLineHeight * 2);

			GUILayout.Space(5);

			if(GUILayout.Button(new GUIContent("Compile"))) {
				string formula = node.formula;
				string className = Nodes.FormulaNode.FormulaType;
				string methodName = Nodes.FormulaNode.FormulaMethod;
				string returnType = node.ReturnType().PrettyName(true);
				var usingNamespaces = node.nodeObject.graphContainer.GetUsingNamespaces();
				var parameters = node.inputs.Select(p => p.type.type.PrettyName(true) + " " + p.name);
				string namespaces = null;
				foreach(var ns in usingNamespaces) {
					namespaces += "using " + ns + ";\n";
				}
				string contents;
				if(node.kind == Nodes.FormulaNode.FormulaKind.Simple) {
					if(node.ReturnType() == typeof(void)) {
						contents = $"{formula};";
					}
					else {
						contents = $"return {formula};";
					}
				}
				else {
					contents = formula.Replace("\n", "\n\t");
				}

				string code = 
@$"#pragma warning disable
{namespaces}
public static class {className} {{
public static {returnType} {methodName}({string.Join(", ", parameters)}) {{

	{contents}

}}
}}";

				var compileResult = RoslynUtility.CompileScript("Generated_" + System.IO.Path.GetRandomFileName() + uNodeUtility.GenerateUID(), new string[] { code });
				if(compileResult.isSuccess) {
					node.rawAssembly = compileResult.rawAssembly;
				}
				else {
					var strs = code.Split('\n');
					var errors = string.Empty;
					for(int i = 0; i < strs.Length; i++) {
						errors += $"\n{i + 1}.  {strs[i]}";
					}
					Debug.LogError("Error compiling formula =>\n" + errors);
					compileResult.LogErrors();
				}
			}

			UInspector.Draw(option.property[nameof(node.displayError)]);
			
			uNodeGUI.DrawHeader("Advanced");
			UInspector.Draw(option.property[nameof(node.optionUseStatic)], label: new GUIContent("Static Function"));

			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
			if(node.displayError == false && node.IsCompiled == false && node.nodeObject.graphContainer is not IScriptGraphType) {
				EditorGUILayout.HelpBox("Formula is not compiled.\nCompile is required only on running with reflection mode.", MessageType.Warning);
			}
		}
	}
}