using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[EventMenu("", "On State Exit", scope = NodeScope.State)]
	[StateEvent]
	[Description("On Exit is called once when the state becomes inactive.")]
	public class StateOnExitEvent : BaseGraphEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			var state = nodeObject.GetNodeInParent<INodeWithEnterExitEvent>();
			if(state != null) {
				state.OnExitCallback += flow => Trigger(flow);
			}
		}

		public override string GetTitle() => "On State Exit";

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(nodeObject.parent is not NodeObject parentNode || parentNode.node is not INodeWithEnterExitEvent) {
				analizer.RegisterError(this, "On Exit event can only be placed inside State.");
			}
		}
	}
}