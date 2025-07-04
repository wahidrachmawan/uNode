using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MaxyGames.UNode.Nodes {
	public class TriggerStateTransition : Node {
		[NonSerialized]
		public FlowInput trigger;
		[NonSerialized]
		public StateTransition transition;

		protected override void OnRegister() {
			trigger = PrimaryFlowInput(nameof(trigger), OnTrigger);
		}

		private void OnTrigger(Flow flow) {
			if(transition == null)
				transition = nodeObject.GetNodeInParent<StateTransition>();
			transition.Finish(flow);
		}

		public override void CheckError(ErrorAnalyzer analyzer) {
			base.CheckError(analyzer);
			if(transition == null)
				transition = nodeObject.GetNodeInParent<StateTransition>();

			if(transition == null) {
				analyzer.RegisterError(this, "The node is not valid in current context");
			}
		}
	}
}