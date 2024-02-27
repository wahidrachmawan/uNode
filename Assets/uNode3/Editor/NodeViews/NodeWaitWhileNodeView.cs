using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeWaitWhile))]
	public class NodeWaitWhileNodeView : BlockNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			Nodes.NodeWaitWhile node = targetNode as Nodes.NodeWaitWhile;
			InitializeBlocks(node.data.container, BlockType.Condition);
		}
	}
}