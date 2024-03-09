using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MaxyGames.UNode.Nodes;
using MaxyGames.UNode.Transition;

namespace MaxyGames.UNode.Editors.Drawer {
	public class ActivateTransitionDrawer : NodeDrawer<Nodes.ActivateTransition> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			UInspector.Draw(option.property[nameof(node.transitionName)]);

			if(GUILayout.Button("Change Transition", EditorStyles.miniButton)) {
				var state = node.nodeObject.GetNodeInParent<StateNode>();
				if(state != null) {
					GenericMenu menu = new GenericMenu();
					var transitions = state.GetTransitions();
					foreach(var tr in transitions) {
						if(tr is CustomTransition) {
							menu.AddItem(new GUIContent(tr.name), tr.name == node.transitionName, () => {
								node.transitionName = tr.name;
							});
						}
					}
					menu.ShowAsContext();
				}
			}

			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}
}