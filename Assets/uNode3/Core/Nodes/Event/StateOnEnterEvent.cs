using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("", "On Enter", category ="State")]
	[StateEvent]
	[Description("On Enter is called once when the state becomes active.")]
	public class StateOnEnterEvent : BaseGraphEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			if(nodeObject.parent is NodeObject parentNode && parentNode.node is StateNode stateNode) {
				stateNode.onEnter += (flow) => {
					Trigger(flow);
				};
			}
		}

		public override string GetTitle() => "On Enter";

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(nodeObject.parent is not NodeObject parentNode || parentNode.node is not StateNode) {
				analizer.RegisterError(this, "On Enter event can only be placed inside State node.");
			}
		}
	}
}