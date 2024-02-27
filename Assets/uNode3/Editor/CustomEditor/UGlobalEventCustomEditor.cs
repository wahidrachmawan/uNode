using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(UGlobalEventCustom), true)]
    public class UGlobalEventCustomEditor : Editor {
		public override void OnInspectorGUI() {
			var monoScript = uNodeEditorUtility.GetMonoScript(target);
			if(monoScript != null) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), true);
				EditorGUI.EndDisabledGroup();
			}
			var asset = target as UGlobalEventCustom;
			uNodeGUI.DrawCustomList(asset.parameters, "Parameters",
				drawElement: (pos, index, parameter) => {
					var name = EditorGUI.DelayedTextField(new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight), "Name", parameter.name);
					if(name != parameter.name) {
						parameter.name = name;
					}
					uNodeGUIUtility.DrawTypeDrawer(
						new Rect(pos.x, pos.y + EditorGUIUtility.singleLineHeight, pos.width, EditorGUIUtility.singleLineHeight),
						parameter.type,
						new GUIContent("Type"),
						type => {
							uNodeEditorUtility.RegisterUndo(asset);
							parameter.type = type;
						}, null, asset);
				},
				add: position => {
					asset.parameters.Add(new ParameterData("newParameter", typeof(string)));
				},
				remove: index => {
					asset.parameters.RemoveAt(index);
				},
				elementHeight: index => {
					return EditorGUIUtility.singleLineHeight * 2;
				});
		}
	}
}