using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.CacheNode))]
	public class CacheNodeView : BaseNodeView {
		protected override void InitializeView() {
			var node = targetNode as Nodes.CacheNode;
			InitializePrimaryPort();
			if(node.compactView) {
				ConstructCompactStyle(true);
				AddInputValuePort(new ValueInputData(node.target)).AddToClassList("hide-image");
			}
			else {
				AddInputValuePort(new ValueInputData(node.target));
			}
			if (UIElementUtility.Theme.coloredNodeBorder) {
				//Set border color
				Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
				c.a = 0.8f;
				border.style.SetBorderColor(c);
			}
		}
	}
}