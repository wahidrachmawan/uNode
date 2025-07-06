using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.TriggerStateTransition))]
	public class TriggerStateTransitionView : BaseNodeView {
		Button button;
		string m_title;

		protected override void OnSetup() {
			base.OnSetup();
			var node = targetNode as Nodes.TriggerStateTransition;
			m_title = node.GetTitle();
			var stateNode = (nodeObject.parent as NodeObject)?.node as Nodes.ScriptState;
			if(stateNode != null) {
				m_title = "Trigger:";
				button = new Button(() => {
					GenericMenu menu = new GenericMenu();
					var transitions = stateNode.GetTransitions();
					foreach(var tr in transitions) {
						menu.AddItem(new GUIContent(tr.GetTitle()), false, () => {
							node.serializedTransition = new(tr);
							node.transition = tr;
							button.text = tr.GetTitle();
						});
					}
					if(menu.GetItemCount() > 0) {
						menu.ShowAsContext();
					}
				}) { text = node.transition != null ? node.transition.GetTitle() : "(None)" };
				titleContainer.Add(button);
			}
		}

		protected override void OnReloadView() {
			base.OnReloadView();
			title = m_title;
		}
	}
}