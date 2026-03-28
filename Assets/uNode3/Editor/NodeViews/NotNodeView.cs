using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NotNode))]
	public class NotNodeView : BaseNodeView {
		protected override void OnReloadView() {
			var node = targetNode as Nodes.NotNode;
			InitializePrimaryPort();
			AddInputValuePort(new ValueInputData(node.target));
			ConstructCompactStyle(minimalize: true, hidePortIcon: true);

			if (UIElementUtility.Theme.coloredNodeBorder) {
				//Set border color
				Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
				c.a = 0.8f;
				border.style.SetBorderColor(c);
			}
		}
	}
}