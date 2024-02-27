using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.MultiArithmeticNode))]
	public class ArithmeticNodeView : BaseNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			if(uNodeUtility.preferredDisplay != DisplayKind.Full) {
				ConstructCompactStyle();
			}
		}
	}
}