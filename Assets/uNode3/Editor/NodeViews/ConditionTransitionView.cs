using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Transition.ConditionTransition))]
	public class ConditionTransitionView : TransitionBlockView {
		protected override void InitializeView() {
			base.InitializeView();
			var tr = transition as Transition.ConditionTransition;
			InitializeBlocks(tr.data.container, BlockType.Condition);
		}
	}
}