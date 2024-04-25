using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(EnumScript), true)]
    public class EnumScriptEditor : Editor {
		public override void OnInspectorGUI() {
			var monoScript = uNodeEditorUtility.GetMonoScript(target);
			if(monoScript != null) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), true);
				EditorGUI.EndDisabledGroup();
			}
			var asset = target as EnumScript;
			uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
			uNodeGUIUtility.ShowField(nameof(asset.modifier), asset, asset);

			uNodeGUI.DrawCustomList(asset.enumerators, "Enumerators",
				drawElement: (position, index, value) => {
					var enumName = EditorGUI.DelayedTextField(position, "Element " + index, value.name);
					if(enumName != value.name) {
						uNodeEditorUtility.RegisterUndo(asset, "Rename enum element");
						value.name = uNodeUtility.AutoCorrectName(enumName);
					}
				},
				add: (position) => {
					uNodeEditorUtility.RegisterUndo(asset, "Add enum element");
					asset.enumerators.Add(new EnumScript.Enumerator() { name = "New" });
				},
				remove: (index) => {
					uNodeEditorUtility.RegisterUndo(asset, "Remove enum element");
					asset.enumerators.RemoveAt(index);
				});

			if(asset is IAttributeSystem) {
				uNodeGUI.DrawAttribute((asset as IAttributeSystem).Attributes, asset, null, AttributeTargets.Enum);
			}
		}
	}
}