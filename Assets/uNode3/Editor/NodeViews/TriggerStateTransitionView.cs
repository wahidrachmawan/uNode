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
					var transitions = stateNode.GetTransitions().ToList();
					var allTransitions = nodeObject.GetObjectInParent<EventGraphContainer>().GetNodesInChildren<Nodes.StateTransition>(true).ToList();
					foreach(var tr in transitions) {
						allTransitions.Remove(tr);
					}
					transitions.AddRange(allTransitions);
					foreach(var tr in allTransitions) {
						if(tr.IsExpose == false) transitions.Remove(tr);
					}
					foreach(var tr in transitions) {
						menu.AddItem(new GUIContent(GetTransitionTitle(tr)), false, () => {
							node.serializedTransition = new(tr);
							node.transition = tr;
							button.text = GetTransitionTitle(tr);
						});
					}
					if(menu.GetItemCount() > 0) {
						menu.ShowAsContext();
					}
				});
				titleContainer.Add(button);
			}
		}
		private string GetTransitionTitle(Nodes.StateTransition tr) {
			var names = new System.Text.StringBuilder().Append(tr.GetTitle());
			if(object.ReferenceEquals(tr.StateNode, (nodeObject.parent as NodeObject).node)) {
				names.Insert(0, $"this  »  ");
			}
			else {
				UGraphElement node = (tr.StateNode as Node).nodeObject;
				while(node.GetType() != typeof(EventGraphContainer)) {
					names.Insert(0, $"{node.name}  »  ");
					node = node.parent;
				}
			}
			return names.ToString();
		}

		protected override void OnReloadView() {
			base.OnReloadView();
			title = m_title;
			var node = targetNode as Nodes.TriggerStateTransition;
			var stateNode = (nodeObject.parent as NodeObject)?.node;
			if(node.transition != null && node.transition.IsExpose == false
			&& object.ReferenceEquals(node.transition.StateNode, stateNode) == false) {
				node.transition = null;
			}
			if(button != null) {
				button.text = node.transition != null ? GetTransitionTitle(node.transition) : "(None)";
			}
		}
	}
}