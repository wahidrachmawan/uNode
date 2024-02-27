using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.FunctionEntryNode))]
	public class FunctionEntryNodeView : BaseNodeView {
		protected override void OnSetup() {
			base.OnSetup();
			this.AddToClassList(ussClassEntryNode);
			var func = nodeObject.GetObjectInParent<Function>();
			if (func != null) {
				var icon = new Image() { name = "title-icon" };
				icon.image = uNodeEditorUtility.GetTypeIcon(func.ReturnType());
				titleContainer.Add(icon);
				icon.BringToFront();
			}
		}
	}
}