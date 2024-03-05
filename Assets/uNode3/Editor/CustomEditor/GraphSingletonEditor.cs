using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(GraphSingleton), true)]
    public class GraphSingletonEditor : GraphAssetEditor {
		public override void DrawGUI(bool isInspector) {
			var asset = target as GraphSingleton;
			if(isInspector == false) {
				uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
				uNodeGUIUtility.ShowField(nameof(asset.@namespace), asset, asset);
				uNodeGUI.DrawNamespace("Using Namespaces", asset.usingNamespaces, asset, (arr) => {
					asset.usingNamespaces = arr as List<string> ?? arr.ToList();
					uNodeEditorUtility.MarkDirty(asset);
				});
				uNodeGUIUtility.ShowField(nameof(asset.persistence), asset, asset);
				DrawGraphLayout();
			}
			else {
				if(!Application.isPlaying) {
					uNodeGUI.DrawGraphVariables(asset.GraphData, null);
				}
				else if(asset.runtimeInstance != null) {
					if(asset.runtimeInstance is RuntimeInstancedGraph instancedGraph) {
						if(instancedGraph.Instance != null) {
							uNodeGUI.DrawRuntimeGraphVariables(instancedGraph.Instance);
						}
						else {
							uNodeGUI.DrawGraphVariables(asset.GraphData, null);
						}
					}
					else {
						Editor editor = CustomInspector.GetEditor(asset.runtimeInstance);
						if(editor != null) {
							EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Runtime Component");
							editor.OnInspectorGUI();
							if(Event.current.type == EventType.Repaint) {
								editor.Repaint();
							}
						}
						else {
							uNodeGUIUtility.ShowFields(asset.runtimeInstance, asset.runtimeInstance);
						}
					}
				}
				else {
					uNodeGUI.DrawGraphVariables(asset.GraphData, null);
				}
				if(!Application.isPlaying) {
					if(uNodePreference.preferenceData.generatorData.compilationMethod == CompilationMethod.Unity) {
						var type = asset.GeneratedTypeName.ToType(false);
						if(type != null) {
							EditorGUILayout.HelpBox("Run using Native C#", MessageType.Info);
						}
						else {
							EditorGUILayout.HelpBox("Run using Reflection", MessageType.Info);
						}
					}
					else {
						if(!GenerationUtility.IsGraphCompiled(asset)) {
							EditorGUILayout.HelpBox("Run using Reflection.", MessageType.Info);
						}
						else if(GenerationUtility.IsGraphUpToDate(asset)) {
							EditorGUILayout.HelpBox("Run using Native C#", MessageType.Info);
						}
						else {
							var boxRect = EditorGUILayout.BeginVertical();
							EditorGUILayout.HelpBox("Run using Native C# but script is outdated.\n[Click To Recompile]", MessageType.Warning);
							EditorGUILayout.EndVertical();
							if(Event.current.clickCount == 1 && Event.current.button == 0 && boxRect.Contains(Event.current.mousePosition)) {
								GraphUtility.SaveAllGraph();
								GenerationUtility.GenerateCSharpScript();
								Event.current.Use();
							}
						}
					}
				}
				if(!Application.isPlaying || asset.runtimeInstance == null) {
					DrawOpenGraph();
				}
			}
		}
	}
}