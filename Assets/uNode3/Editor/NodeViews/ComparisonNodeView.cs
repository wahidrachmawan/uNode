using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
    [NodeCustomEditor(typeof(Nodes.ComparisonNode))]
	public class ComparisonNodeView : BaseNodeView {
		protected override void InitializeView() {
			var node = nodeObject.node as Nodes.ComparisonNode;
			AddPrimaryOutputValue();
			AddInputValuePort(new ValueInputData(node.inputA));
			AddInputValuePort(new ValueInputData(node.inputB));

			if(uNodeUtility.preferredDisplay != DisplayKind.Full) {
				ConstructCompactStyle();
			}
		}
	}
}