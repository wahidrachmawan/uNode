namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnMouseUpAsButton", "OnMouseUpAsButton")]
	public class OnMouseUpAsButton : TransitionEvent {

		public override void OnEnter(Flow flow) {
			UEvent.Register(UEventID.OnMouseUpAsButton, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister(UEventID.OnMouseUpAsButton, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		void Execute(Flow flow) {
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				CG.InsertCodeToFunction(
					"OnMouseUpAsButton",
					typeof(void),
					CG.Condition("if", CG.CompareNodeState(enter, null), CG.FlowTransitionFinish(this)));
			}
			return null;
		}
	}
}
