namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnMouseOver", "OnMouseOver")]
	public class OnMouseOver : TransitionEvent {

		public override void OnEnter(Flow flow) {
			UEvent.Register(UEventID.OnMouseOver, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister(UEventID.OnMouseOver, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		void Execute(Flow flow) {
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				CG.InsertCodeToFunction(
					"OnMouseOver",
					typeof(void),
					CG.Condition("if", CG.CompareNodeState(enter, null), CG.FlowTransitionFinish(this)));
			}
			return null;
		}
	}
}
