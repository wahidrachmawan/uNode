using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow.Other", "ParallelControl", scope = NodeScope.StateGraph)]
	public class ParallelControl : BaseFlowNode {
		[Range(0, 10)]
		public int flowCount = 1;
		[System.NonSerialized]
		public FlowOutput[] flows;

		protected override void OnRegister() {
			base.OnRegister();
			flows = new FlowOutput[flowCount];
			for(int i = 0; i < flowCount; i++) {
				flows[i] = FlowOutput("Flow-" + i).SetName("");
			}
		}

		protected override void OnExecuted(Flow flow) {
			for(int i = 0; i < flows.Length; i++) {
				flow.TriggerParallel(flows[i]);
			}
		}

		public override void OnGeneratorInitialize() {
			CG.RegisterAsStateFlow(enter);
			CG.RegisterPort(enter, () => {
				string data = null;
				for(int i = 0; i < flows.Length; i++) {
					if(!flows[i].isAssigned)
						continue;
					data += CG.Flow(flows[i], false).AddLineInFirst();
				}
				return data;
			});
		}
	}
}
