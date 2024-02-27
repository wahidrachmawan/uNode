using UnityEngine;
using System.Collections.Generic;
using System;

namespace MaxyGames.UNode.Nodes {
	public class ActivateTransition : BaseFlowNode {
		public string transitionName = "Custom";

		protected override void OnExecuted(Flow flow) {
			var stateNode = nodeObject.GetNodeInParent<StateNode>();
			foreach(var t in stateNode.GetTransitions()) {
				if(t != null && t.name.Equals(transitionName) && t is Transition.CustomTransition) {
					(t as Transition.CustomTransition).Execute(flow);
					break;
				}
			}
		}

		protected override string GenerateFlowCode() {
			return CG.FlowStaticInvoke(
				Transition.CustomTransition.KEY_Activate_Transition,
				CG.Value(transitionName + nodeObject.GetNodeInParent<StateNode>().id)
			);
		}

		public override string GetTitle() {
			if(string.IsNullOrEmpty(transitionName)) {
				return "Assign this";
			}
			return "Activate: " + transitionName;
		}

		public override Type GetNodeIcon() {
			return null;
		}
	}
}
