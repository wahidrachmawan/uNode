using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
    [NodeCustomEditor(typeof(Nodes.MultiANDNode))]
	public class AndNodeView : BaseNodeView {
		protected override void OnReloadView() {
			base.OnReloadView();
			if(uNodeUtility.preferredDisplay != DisplayKind.Full) {
				ConstructCompactStyle();
			}
		}
	}
}