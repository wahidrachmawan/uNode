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
			EditorGUI.BeginChangeCheck();
			uNodeGUI.DrawLinkedVariables(asset.variables, asset.target, unityObject: asset);
			if(EditorGUI.EndChangeCheck()) {
				uNodeEditorUtility.MarkDirty(asset);
			}
			if(!Application.isPlaying) {
				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button(new GUIContent("Edit Graph", ""), EditorStyles.toolbarButton)) {
					uNodeEditor.Open(asset.target);
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.HelpBox("This will spawn singleton ( if not spawned ) and change singleton variable at Awake", MessageType.Info);
			}
		}
	}
}