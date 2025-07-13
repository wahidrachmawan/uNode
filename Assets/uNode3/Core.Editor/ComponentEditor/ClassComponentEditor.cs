using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(ClassComponent), true)]
    public class ClassComponentEditor : Editor {
		public override void OnInspectorGUI() {
			var asset = target as ClassComponent;
			var position = uNodeGUIUtility.GetRect();
			var bPos = position;
			bPos.x += position.width - 20;
			bPos.width = 20;
			if(GUI.Button(bPos, "", EditorStyles.label)) {
				var items = ItemSelector.MakeCustomItemsForInstancedType(
					new System.Type[] { typeof(ClassDefinition) }, 
					(val) => {
						asset.target = val as ClassDefinition;
						uNodeEditorUtility.MarkDirty(asset);
					}, 
					uNodeEditorUtility.IsSceneObject(asset),
					obj => {
						if(obj is ClassDefinition definition && definition.model is ClassComponentModel) {
							return true;
						}
						return false;
					});
				ItemSelector.ShowWindow(null, null, null, items).ChangePosition(bPos.ToScreenRect()).displayDefaultItem = false;
				Event.current.Use();
			}
			EditorGUI.BeginChangeCheck();
			var targetVal = EditorGUI.ObjectField(position, new GUIContent("Graph", "The target graph reference"), asset.target, typeof(ClassDefinition), false);
			if(EditorGUI.EndChangeCheck()) {
				asset.target = targetVal as ClassDefinition;
				uNodeEditorUtility.MarkDirty(asset);
			}

			if(asset.target != null) {
				if(!Application.isPlaying) {
					EditorGUI.BeginChangeCheck();
					uNodeGUI.DrawLinkedVariables(asset.variables, asset.target, unityObject: asset);
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.MarkDirty(asset);
					}
				} else if(asset.nativeInstance != null) {
					Editor editor = CustomInspector.GetEditor(asset.nativeInstance);
					if(editor != null) {
						EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Runtime Component");
						editor.OnInspectorGUI();
						//if(Event.current.type == EventType.Repaint) {
						//	editor.Repaint();
						//}
					} else {
						uNodeGUIUtility.ShowFields(asset.nativeInstance, asset.nativeInstance);
					}
				} else {
					EditorGUI.BeginChangeCheck();
					if(asset.Instance != null) {
						uNodeGUI.DrawRuntimeGraphVariables(asset.Instance);
					}
					else {
						uNodeGUI.DrawLinkedVariables(asset.variables, asset.target, unityObject: asset);
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.MarkDirty(asset);
					}
				}
				if(asset.target is IClassGraph) {
					if(!Application.isPlaying) {
						if(uNodePreference.preferenceData.generatorData.compilationMethod == CompilationMethod.Unity) {
							var type = asset.target.GeneratedTypeName.ToType(false);
							if(type != null) {
								EditorGUILayout.HelpBox("Run using Native C#", MessageType.Info);
							}
							else {
								EditorGUILayout.HelpBox("Run using Reflection", MessageType.Info);
							}
						}
						else {
							if(!GenerationUtility.IsGraphCompiled(asset.target)) {
								EditorGUILayout.HelpBox("Run using Reflection.", MessageType.Info);
							}
							else if(GenerationUtility.IsGraphUpToDate(asset.target)) {
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
				}
				else {
					EditorGUILayout.HelpBox("The target graph is not supported.", MessageType.Warning);
				}
				if(!Application.isPlaying || asset.nativeInstance == null) {
					EditorGUILayout.BeginHorizontal();
					if(GUILayout.Button(new GUIContent("Edit Graph", ""), EditorStyles.toolbarButton)) {
						uNodeEditor.Open(asset.target);
					}
					EditorGUILayout.EndHorizontal();
				}
			} else {
				EditorGUILayout.HelpBox("Please assign the target graph or create a New", MessageType.Error);

				if(GUILayout.Button("New Graph")) {
					GraphCreatorWindow.ShowWindow(new ClassDefinitionCreator(), true, obj => {
						var definition = obj as ClassDefinition;
						asset.target = definition;
					});
				}
			}
		}

		public override bool RequiresConstantRepaint() {
			return Application.isPlaying;
		}
	}
}