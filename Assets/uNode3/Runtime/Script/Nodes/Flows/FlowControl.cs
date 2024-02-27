using UnityEngine;
using System.Collections.Generic;
using System;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "FlowControl", hasFlowOutput = true)]
	public class FlowControl : BaseFlowNode {
		[Range(1, 10)]
		public int flowCount = 2;

		[NonSerialized]
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
				flow.Next(flows[i]);
			}
		}

		protected override string GenerateFlowCode() {
			string data = null;
			for(int i = 0; i < flows.Length; i++) {
				if(!flows[i].isAssigned) continue;
				data += CG.Flow(flows[i]).AddLineInFirst();
			}
			return data;
		}

		protected override bool IsCoroutine() {
			return HasCoroutineInFlows(flows);
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}
	}
}
