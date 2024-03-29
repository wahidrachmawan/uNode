using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeSetValue))]
	public class SetNodeView : BaseNodeView {
		protected override void InitializeView() {
			var node = targetNode as Nodes.NodeSetValue;
			InitializePrimaryPort();
			AddInputValuePort(new ValueInputData(node.target));
			AddInputValuePort(new ValueInputData(node.value));
			if(uNodeUtility.preferredDisplay != DisplayKind.Full && owner.graphLayout == GraphLayout.Vertical) {
				ConstructCompactStyle();
			}
		}
	}
}
