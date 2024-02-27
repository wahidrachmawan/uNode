using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow.Other", "Stop All", scope = NodeScope.StateGraph)]
	public class StopAllNode : BaseFlowNode {
		protected override void OnExecuted(Flow flow) {
			foreach(var element in nodeObject.parent) {
				if(element == nodeObject)
					continue;
				var node = element as NodeObject;
				if(node != null && node.primaryFlowInput != null) {
					flow.instance.StopState(node.primaryFlowInput);
				}
			}
		}

		protected override string GenerateFlowCode() {
			string data = null;
			foreach(var element in nodeObject.parent) {
				if(element == nodeObject)
					continue;
				var node = element as NodeObject;
				if(node != null && node.primaryFlowInput != null && CG.IsStateFlow(node.primaryFlowInput)) {
					data += CG.StopEvent(node.primaryFlowInput, false).AddLineInFirst();
				}
			}
			return data + CG.FlowFinish(enter).AddLineInFirst();
		}

		public override string GetTitle() {
			return "Stop All";
		}

		protected override bool IsSelfCoroutine() {
			return false;
		}
	}
}
