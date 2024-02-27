using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.NodeValidation))]
	public class ValidationNodeView : BlockNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			Nodes.NodeValidation node = targetNode as Nodes.NodeValidation;
			InitializeBlocks(node.data.container, BlockType.Condition);
		}
	}
}