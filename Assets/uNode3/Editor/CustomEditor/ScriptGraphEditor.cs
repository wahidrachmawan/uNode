using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(ScriptGraph), true)]
    public class ScriptGraphEditor : GraphAssetEditor {
		public override void DrawGUI(bool isInspector) {
			var asset = target as ScriptGraph;
			asset.Namespace = EditorGUILayout.DelayedTextField("Namespace", asset.Namespace);
			uNodeGUI.DrawNamespace("Using Namespaces", asset.UsingNamespaces, asset, (arr) => {
				asset.UsingNamespaces = arr as List<string> ?? arr.ToList();
				uNodeEditorUtility.MarkDirty(asset);
			});
			if(isInspector) {
				EditorGUILayout.LabelField("Classes", EditorStyles.centeredGreyMiniLabel);
				using(new EditorGUILayout.VerticalScope("Box")) {
					foreach(IScriptGraphType type in asset.TypeList) {
						if(type == null) continue;
						if(GUILayout.Button(!string.IsNullOrEmpty(type.TypeName) ? asset.name : type.TypeName)) {
							uNodeEditor.Open(type);
						}
					}
				}
			}
		}
	}
}