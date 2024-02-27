using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(RuntimeInstancedGraph), true)]
    public class RuntimeInstancedGraphEditor : Editor {
		public override void OnInspectorGUI() {
			var asset = target as RuntimeInstancedGraph;
			if(asset.nativeInstance != null) {
				Editor editor = CustomInspector.GetEditor(asset.nativeInstance);
				if(editor != null) {
					EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Runtime Component");
					editor.OnInspectorGUI();
					if(Event.current.type == EventType.Repaint) {
						editor.Repaint();
					}
				}
				else {
					uNodeGUIUtility.ShowFields(asset.nativeInstance, asset.nativeInstance);
				}
			}
			else {
				if(asset.Instance != null) {
					uNodeGUI.DrawRuntimeGraphVariables(asset.Instance);
				}
				else {
					uNodeGUI.DrawGraphVariables(asset.GraphData, this);
				}
			}
			//if(!Application.isPlaying || asset.nativeInstance == null) 
			{
				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button(new GUIContent("Edit Graph", ""), EditorStyles.toolbarButton)) {
					uNodeEditor.Open(asset.OriginalGraph);
				}
				EditorGUILayout.EndHorizontal();
			}
		}
	}
}