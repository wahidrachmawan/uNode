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
			uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
			uNodeGUI.DrawClassDefinitionModel(asset.GetModel(), model => asset.model = model);
			uNodeGUIUtility.ShowField(nameof(asset.@namespace), asset, asset);
			uNodeGUI.DrawNamespace("Using Namespaces", asset.usingNamespaces, asset, (arr) => {
				asset.usingNamespaces = arr as List<string> ?? arr.ToList();
				uNodeEditorUtility.MarkDirty(asset);
			});
			base.DrawGUI(isInspector);
		}
	}
}