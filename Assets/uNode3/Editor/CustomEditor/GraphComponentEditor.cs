using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(GraphComponent), true)]
    public class GraphComponentEditor : GraphAssetEditor {
		public override void DrawGUI(bool isInspector) {
			var asset = target as GraphComponent;
			uNodeGUIUtility.ShowField(new GUIContent("Name"), "graphName", asset);
			if(isInspector == false) {
				uNodeGUI.DrawNamespace("Using Namespaces", asset.usingNamespaces, asset, (arr) => {
					asset.usingNamespaces = arr as List<string> ?? arr.ToList();
					uNodeEditorUtility.MarkDirty(asset);
				});
				if(asset is IInterfaceSystem) {
					uNodeGUI.DrawInterfaces(asset as IInterfaceSystem);
				}
				DrawGraphLayout();
			}
			else {
				if(!Application.isPlaying) {
					uNodeGUI.DrawGraphVariables(asset.GraphData, this);
				}
				else if(asset.nativeInstance != null) {
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
				if(!Application.isPlaying) {
					var type = asset.GeneratedTypeName.ToType(false);
					if(type != null) {
						EditorGUILayout.HelpBox("Run using Native C#", MessageType.Info);
					}
					else {
						EditorGUILayout.HelpBox("Run using Reflection", MessageType.Info);
					}
				}
				//if(!Application.isPlaying || asset.nativeInstance == null) 
				{
					EditorGUILayout.BeginHorizontal();
					if(GUILayout.Button(new GUIContent("Edit Graph", ""), EditorStyles.toolbarButton)) {
						uNodeEditor.Open(asset);
					}
					EditorGUILayout.EndHorizontal();
				}
			}
		}
	}
}