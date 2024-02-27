using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
    [CustomEditor(typeof(InterfaceScript), true)]
    public class InterfaceScriptEditor : GraphAssetEditor {
		public override void DrawGUI(bool isInspector) {
			var asset = target as InterfaceScript;
			uNodeGUIUtility.ShowField(nameof(asset.icon), asset, asset);
			uNodeGUIUtility.ShowField(nameof(asset.modifier), asset, asset);
			base.DrawGUI(isInspector);
		}
	}
}