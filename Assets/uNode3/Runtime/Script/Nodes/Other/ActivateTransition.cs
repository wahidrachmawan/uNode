using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

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

		public override void CheckError(ErrorAnalyzer analyzer) {
			base.CheckError(analyzer);

			var state = nodeObject.GetNodeInParent<StateNode>();
			if(state != null) {
				var transitions = state.GetTransitions();

				if(transitions.Any(item => item is Transition.CustomTransition transition && transition.name == transitionName) == false) {
					analyzer.RegisterError(this, $"No custom transition with name `{transitionName}`", () => {
						if(string.IsNullOrWhiteSpace(transitionName) == false) {
							state.transitions.container.AddChild(new NodeObject(new Transition.CustomTransition()) {
								name = transitionName,
								position = new Rect(0, 100, 0, 0),
							});
						}
					});
				}
			}
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
