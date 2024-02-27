using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeConvert))]
	public class NodeConvertView : BaseNodeView {
		protected override void InitializeView() {
			var node = targetNode as Nodes.NodeConvert;
			if(!node.compactDisplay) {
				base.InitializeView();
				return;
			}
			InitializePrimaryPort();
			ConstructCompactStyle(false);
			AddInputValuePort(new ValueInputData(node.target)).AddToClassList("hide-image");
			if (UIElementUtility.Theme.coloredNodeBorder) {
				//Set border color
				Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
				c.a = 0.8f;
				border.style.SetBorderColor(c);
			}
		}
	}
}