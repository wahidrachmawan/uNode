﻿using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeConvert))]
	public class NodeConvertView : BaseNodeView {
		protected override void OnReloadView() {
			var node = targetNode as Nodes.NodeConvert;
			if(!node.compactDisplay) {
				base.OnReloadView();
				return;
			}
			InitializePrimaryPort();
			//ConstructCompactStyle(false);
			titleIcon.RemoveFromHierarchy();
			ConstructCompactTitle(AddInputValuePort(new ValueInputData(node.target)), minimalize: true);
			title = "To: " + uNodeUtility.WrapTextWithTypeColor(node.type.isFilled ? node.type.prettyName : node.output.type.PrettyName());
			if (UIElementUtility.Theme.coloredNodeBorder) {
				//Set border color
				Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
				c.a = 0.8f;
				border.style.SetBorderColor(c);
			}
		}
	}
}