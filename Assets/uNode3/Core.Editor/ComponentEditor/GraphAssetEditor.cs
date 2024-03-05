using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors {
    public class GraphAssetEditor : Editor {
		public override void OnInspectorGUI() {
			var monoScript = uNodeEditorUtility.GetMonoScript(target);
			if(monoScript != null) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), true);
				EditorGUI.EndDisabledGroup();
			}
			DrawGUI(true);
		}

		public virtual void DrawOpenGraph() {
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(new GUIContent("Edit Graph", ""))) {
				uNodeEditor.Open(target as GraphAsset);
			}
			EditorGUILayout.EndHorizontal();
		}

		public virtual void DrawExecutionMode() {
			GraphAsset asset = target as GraphAsset;
			if(uNodePreference.preferenceData.generatorData.compilationMethod == CompilationMethod.Unity) {
				var type = asset.GetFullGraphName().ToType(false);
				if(type != null) {
					EditorGUILayout.HelpBox("This graph is Run using Native C#", MessageType.Info);
				}
				else {
					EditorGUILayout.HelpBox("This graph is Run using Reflection", MessageType.Info);
				}
			}
			else {
				if(!GenerationUtility.IsGraphCompiled(asset)) {
					EditorGUILayout.HelpBox("This graph is Run using Reflection.", MessageType.Info);
				}
				else if(GenerationUtility.IsGraphUpToDate(asset)) {
					EditorGUILayout.HelpBox("This graph is Run using Native C#", MessageType.Info);
				}
				else {
					var boxRect = EditorGUILayout.BeginVertical();
					EditorGUILayout.HelpBox("This graph is Run using Native C# but script is outdated.\n[Click To Recompile]", MessageType.Warning);
					EditorGUILayout.EndVertical();
					if(Event.current.clickCount == 1 && Event.current.button == 0 && boxRect.Contains(Event.current.mousePosition)) {
						GraphUtility.SaveAllGraph();
						GenerationUtility.GenerateCSharpScript();
						Event.current.Use();
					}
				}
			}
		}

		public virtual void DrawGUI(bool isInspector) {
			if(target is IAttributeSystem attributeSystem) {
				var att = AttributeTargets.Class;
				if(target is IClassGraph typeGraph) {
					if(typeGraph.IsStruct) {
						att = AttributeTargets.Struct;
					}
					else if(typeGraph.IsInterface) {
						att = AttributeTargets.Interface;
					}
				}
				else if(target is IScriptInterface) {
					att = AttributeTargets.Interface;
				}
				uNodeGUI.DrawAttribute(attributeSystem.Attributes, target, null, att);
			}
			if(target is IInterfaceSystem) {
				uNodeGUI.DrawInterfaces(target as IInterfaceSystem);
			}
			DrawGraphLayout();
		}

		protected void DrawGraphLayout() {
			var graph = target as IGraph;
			if(graph != null) {
				uNodeGUIUtility.EditValueLayouted(nameof(graph.GraphData.graphLayout), graph.GraphData, val => {
					graph.GraphData.graphLayout = (GraphLayout)val;
					UGraphView.ClearCache(graph);
					uNodeEditor.RefreshEditor(true);
					//uNodeGUIUtility.GUIChangedMajor(target);
				});
			}
		}
	}
}