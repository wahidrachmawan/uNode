using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(ScriptGraph), true)]
    public class ScriptGraphEditor : Editor {
		public override void OnInspectorGUI() {
			var monoScript = uNodeEditorUtility.GetMonoScript(target);
			if(monoScript != null) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), true);
				EditorGUI.EndDisabledGroup();
			}
			var asset = target as ScriptGraph;
			asset.Namespace = EditorGUILayout.DelayedTextField("Namespace", asset.Namespace);
			uNodeGUI.DrawNamespace("Using Namespaces", asset.UsingNamespaces, asset, (arr) => {
				asset.UsingNamespaces = arr as List<string> ?? arr.ToList();
				uNodeEditorUtility.MarkDirty(asset);
			});
		}
	}
}