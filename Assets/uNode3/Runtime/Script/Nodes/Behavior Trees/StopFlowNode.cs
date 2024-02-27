using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow.Other", "Stop Flow", scope = NodeScope.StateGraph)]
	public class StopFlowNode : FlowNode {
		[Range(0, 10)]
		public int flowCount = 1;
		[System.NonSerialized]
		public FlowOutput[] flows;

		protected override void OnRegister() {
			flows = new FlowOutput[flowCount];
			for(int i = 0; i < flowCount; i++) {
				flows[i] = FlowOutput("Flow-" + i).SetName("");
			}
			base.OnRegister();
			exit.SetName("Exit");
		}

		protected override void OnExecuted(Flow flow) {
			for(int i = 0; i < flows.Length; i++) {
				flow.instance.StopState(flows[i]);
			}
		}

		public override void OnGeneratorInitialize() {
			CG.RegisterAsStateFlow(flows);
			CG.RegisterPort(enter, () => {
				string data = null;
				for(int i = 0; i < flows.Length; i++) {
					if(!flows[i].isAssigned)
						continue;
					data += CG.StopEvent(flows[i].GetTargetFlow(), false).AddLineInFirst();
				}
				return data + CG.FlowFinish(enter, flows).AddLineInFirst();
			});
		}

		public override string GetTitle() {
			return "Stop Flow";
		}

		protected override bool IsCoroutine() {
			//return HasCoroutineInFlow(nextNode);
			return false;
		}
	}
}
