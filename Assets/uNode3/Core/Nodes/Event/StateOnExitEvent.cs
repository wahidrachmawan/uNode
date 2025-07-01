using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[EventMenu("", "On Exit", scope = NodeScope.State)]
	[StateEvent]
	[Description("On Exit is called once when the state becomes inactive.")]
	public class StateOnExitEvent : BaseGraphEvent {
		public override void OnRuntimeInitialize(GraphInstance instance) {
			var state = nodeObject.GetNodeInParent<IScriptState>();
			if(state != null) {
				state.OnExitState += flow => Trigger(flow);
			}
		}

		public override string GetTitle() => "On Exit";

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(nodeObject.parent is not NodeObject parentNode || parentNode.node is not IScriptState) {
				analizer.RegisterError(this, "On Exit event can only be placed inside State.");
			}
		}
	}
}