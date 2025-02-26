using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
	[CustomEditor(typeof(SingletonInitializer), true)]
	public class SingletonInitializerEditor : Editor {
		public override void OnInspectorGUI() {
			var asset = target as SingletonInitializer;
			{
				serializedObject.UpdateIfRequiredOrScript();
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(asset.target)), new GUIContent("Graph", "The target graph reference"));
				serializedObject.ApplyModifiedProperties();
			}
			if(!Application.isPlaying) {
				EditorGUI.BeginChangeCheck();
				uNodeGUI.DrawLinkedVariables(asset.variables, asset.target, unityObject: asset);
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.MarkDirty(asset);
				}
				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button(new GUIContent("Edit Graph", ""), EditorStyles.toolbarButton)) {
					uNodeEditor.Open(asset.target);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.HelpBox("This will spawn singleton ( if not spawned ) and change singleton variable at Awake", MessageType.Info);
			}
			else {
				if(asset.target != null) {
					var owner = asset.target;
					if(owner.runtimeInstance != null) {
						if(owner.runtimeInstance is RuntimeInstancedGraph instancedGraph) {
							if(instancedGraph.Instance != null) {
								uNodeGUI.DrawRuntimeGraphVariables(instancedGraph.Instance);
							}
							else {
								uNodeGUI.DrawGraphVariables(owner.GraphData, owner);
							}
						}
						else {
							Editor editor = CustomInspector.GetEditor(owner.runtimeInstance);
							if(editor != null) {
								EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Runtime Component");
								editor.OnInspectorGUI();
								if(Event.current.type == EventType.Repaint) {
									editor.Repaint();
								}
							}
							else {
								uNodeGUIUtility.ShowFields(owner.runtimeInstance, owner.runtimeInstance);
							}
						}
					}
				}
				else {
					EditorGUILayout.HelpBox("Singleton initializer is not initialized because the target is null.", MessageType.Info);
				}
			}
		}
	}
}