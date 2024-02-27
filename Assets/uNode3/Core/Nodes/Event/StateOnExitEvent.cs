using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[EventMenu("", "On Exit", category = "State")]
	[StateEvent]
    public class StateOnExitEvent : BaseGraphEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			var stateNode = nodeObject.GetNodeInParent<StateNode>();
			if(stateNode != null) {
				stateNode.onExit += flow => Trigger(flow);
			}
		}

		public override string GetTitle() => "On Exit";

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(nodeObject.parent is not NodeObject parentNode || parentNode.node is not StateNode) {
				analizer.RegisterError(this, "On Exit event can only be placed inside State node.");
			}
		}
	}
}