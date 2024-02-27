using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.MultiORNode))]
	public class OrNodeView : BaseNodeView {
		protected override void InitializeView() {
			InitializePrimaryPort();
			var node = targetNode as Nodes.MultiORNode;
			for(int x = 0; x < node.inputs.Count; x++) {
				int index = x;
				AddInputValuePort(new ValueInputData(node.inputs[index].port));
			}
			if(uNodeUtility.preferredDisplay != DisplayKind.Full) {
				ConstructCompactStyle();
			}
		}
	}
}