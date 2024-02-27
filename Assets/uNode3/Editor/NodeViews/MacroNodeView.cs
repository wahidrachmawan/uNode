using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {

	[NodeCustomEditor(typeof(Nodes.MacroNode))]
	public class MacroNodeView : BaseNodeView {
		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			evt.menu.AppendAction("Open Macro", (e) => {
				owner.graph.graphData.currentCanvas = targetNode.nodeObject;
				owner.graph.Refresh();
				owner.graph.UpdatePosition();
			}, DropdownMenuAction.AlwaysEnabled);
			base.BuildContextualMenu(evt);
		}

		protected override void InitializeView() {
			base.InitializeView();
			titleContainer.RegisterCallback<MouseDownEvent>(e => {
				if(e.button == 0 && e.clickCount == 2) {
					owner.graph.graphData.currentCanvas = targetNode.nodeObject;
					owner.graph.Refresh();
					owner.graph.UpdatePosition();
				}
			});
		}
	}
}