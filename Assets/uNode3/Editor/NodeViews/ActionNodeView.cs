using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeAction))]
	public class ActionNodeView : BlockNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			Nodes.NodeAction node = nodeObject.node as Nodes.NodeAction;
			InitializeBlocks(node.data.container, BlockType.Action);
		}
	}
}