using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(ClassDefinition), true)]
    class ClassDefinitionEditor : GraphAssetEditor {
		public override void DrawGUI(bool isInspector) {
			var asset = target as ClassDefinition;
			//EditorGUI.BeginDisabledGroup(isInspector);
			uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
			uNodeGUI.DrawClassDefinitionModel(asset.GetModel(), model => asset.model = model);
			uNodeGUIUtility.ShowField(nameof(asset.@namespace), asset, asset);
			uNodeGUI.DrawNamespace("Using Namespaces", asset.usingNamespaces, asset, (arr) => {
				asset.usingNamespaces = arr as List<string> ?? arr.ToList();
				uNodeEditorUtility.MarkDirty(asset);
			});
			base.DrawGUI(isInspector);
			//EditorGUI.EndDisabledGroup();

			if(isInspector) {
				uNodeGUIUtility.ShowField(new GUIContent("Compile to C#", "If true, the graph will be compiled to C# to run using native c# performance on build or in editor using ( Generate C# Scripts ) menu."), nameof(asset.scriptData.compileToScript), asset.scriptData, asset);
				if(asset.scriptData.compileToScript == false && uNodePreference.preferenceData.generatorData.generationMode != GenerationKind.Compatibility) {
					EditorGUILayout.HelpBox("You're not using compatibility generation mode, therefore interacting between this graph and compiled runtime graph might cause issue. Please change generation mode to `Compatibility` in preference if you have any issue.", MessageType.Warning);
				}

				DrawExecutionMode();
				DrawOpenGraph();
			}
		}
	}
}