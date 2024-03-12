using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeReroute))]
	public class NodeRerouteView : BaseNodeView {
		protected override void InitializeView() {
			var node = targetNode as Nodes.NodeReroute;
			InitializePrimaryPort();
			if(node.IsFlowNode()) {
				title = "";
				this.AddToClassList("Reroute-flow");
				if(graphData.graphLayout == GraphLayout.Horizontal) {
					ConstructCompactStyle(minimalize: true, hidePortIcon: true);
				}
			} else {
				this.AddToClassList("Reroute-value");
				ConstructCompactStyle(false);
				var inPort = AddInputValuePort(new ValueInputData(node.input));
				inPort.AddToClassList(ussClassHidePortIcon);
				inPort.SetName("");
				if(UIElementUtility.Theme.coloredNodeBorder) {
					//Set border color
					Color c = uNodePreference.GetColorForType(primaryOutputValue.GetPortType());
					c.a = 0.8f;
					border.style.SetBorderColor(c);
				}
			}
		}
	}
}