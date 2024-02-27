using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(GraphInterface), true)]
    class GraphInterfaceEditor : GraphAssetEditor {
		public override void DrawGUI(bool isInspector) {
			var asset = target as GraphInterface;
			uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
			uNodeGUIUtility.ShowField(nameof(asset.@namespace), asset, asset);
			uNodeGUI.DrawNamespace("Using Namespaces", asset.usingNamespaces, asset, (arr) => {
				asset.usingNamespaces = arr as List<string> ?? arr.ToList();
				uNodeEditorUtility.MarkDirty(asset);
			});
			base.DrawGUI(isInspector);
		}
	}
}