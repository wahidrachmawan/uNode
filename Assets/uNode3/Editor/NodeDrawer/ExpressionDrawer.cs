using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MaxyGames.UNode.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MaxyGames.UNode.Editors.Drawer {
    public class ExpressionDrawer : NodeDrawer<Nodes.ExpressionNode> {
		private static FilterAttribute filter = new FilterAttribute() {
			DisplayGeneratedRuntimeType = false,
		};

		private static void AnalizeExpression(ExpressionNode node) {
			var oldInputs = node.inputs.ToArray();
			node.inputs.Clear();
			var syntaxTree = RoslynUtility.GetSyntaxTree(node.expression);
			if(syntaxTree.Members.Count == 1 && syntaxTree.Members[0] is GlobalStatementSyntax globalStatement) {
				var expressionStatement = globalStatement.Statement as ExpressionStatementSyntax;
				if(expressionStatement != null) {
					var expression = expressionStatement.Expression;
					var walker = new ExpressionWalker();
					walker.DoVisit(expression, syntax => {
						if(syntax is IdentifierNameSyntax identifier) {
							if(identifier.Parent is MemberAccessExpressionSyntax memberAccess) {
								if(memberAccess.Expression != identifier) {
									return;
								}
							}
							var identifierValue = identifier.Identifier.ValueText;
							if(string.IsNullOrEmpty(identifierValue) == false) {
								if(node.inputs.Any(item => item.name == identifierValue))
									return;
								var oldInput = oldInputs.FirstOrDefault(item => item.name == identifierValue);
								if(oldInput != null) {
									node.inputs.Add(oldInput);
								}
								else {
									node.inputs.Add(new ExpressionNode.InputData() {
										name = identifierValue,
									});
								}
							}
						}
					});
				}
			}
			else {

			}
		}

		class ExpressionWalker : CSharpSyntaxWalker {
			private Action<SyntaxNode> action;

			public void DoVisit(SyntaxNode token, Action<SyntaxNode> action) {
				this.action = action;
				Visit(token);
			}

			public override void VisitIdentifierName(IdentifierNameSyntax node) {
				action?.Invoke(node);
			}
		}

		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField("Expression:");
			string formulaCode = EditorGUILayout.TextArea(node.expression);
			if(formulaCode != node.expression) {
				node.rawAssembly = new byte[0];
				node.expression = formulaCode;
				AnalizeExpression(node);
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
					EditorGUI.LabelField(position, node.inputs[index].name);
					position.y += EditorGUIUtility.singleLineHeight;
					position = EditorGUI.PrefixLabel(position, new GUIContent("Type"));
					uNodeGUIUtility.DrawTypeDrawer(position, node.inputs[index].type, GUIContent.none, (type) => {
						node.inputs[index].type = type;
						uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
					}, targetObject: option.unityObject, filter: filter);
				},
				add: null,
				remove: null,
				reorder: (list, oldIndex, newIndex) => {
					uNodeUtility.ReorderList(node.inputs, newIndex, oldIndex);
					option.RegisterUndo();
					uNodeUtility.ReorderList(node.inputs, oldIndex, newIndex);
					uNodeGUIUtility.GUIChanged(node, UIChangeType.Average);
				},
				elementHeight: (index) => EditorGUIUtility.singleLineHeight * 2);

			if(GUILayout.Button(new GUIContent("Compile"))) {
				string expression = node.expression;
				string className = Nodes.FormulaNode.FormulaType;
				string methodName = Nodes.FormulaNode.FormulaMethod;
				string returnType = node.ReturnType().PrettyName(true);
				var parameters = node.inputs.Select(p => {
					if(p.type?.type != null) {
						return p.type.type.PrettyName(true) + " " + p.name;
					}
					else {
						return returnType + " " + p.name;
					}
				});
				var usingNamespaces = node.nodeObject.graphContainer.GetUsingNamespaces();
				string namespaces = null;
				foreach(var ns in usingNamespaces) {
					namespaces += "using " + ns + ";\n";
				}
				string contents = $"return {expression};";

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
				EditorGUILayout.HelpBox("Expression is not compiled.\nCompile is required only on running with reflection mode.", MessageType.Warning);
			}
		}
	}
}