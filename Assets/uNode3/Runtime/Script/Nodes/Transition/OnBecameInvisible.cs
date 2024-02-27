namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnBecameInvisible", "OnBecameInvisible")]
	public class OnBecameInvisible : TransitionEvent {

		public override void OnEnter(Flow flow) {
			UEvent.Register(UEventID.OnBecameInvisible, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister(UEventID.OnBecameInvisible, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		void Execute(Flow flow) {
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				CG.InsertCodeToFunction(
					"OnBecameInvisible",
					typeof(void),
					CG.Condition("if", CG.CompareNodeState(enter, null), CG.FlowTransitionFinish(this)));
			}
			return null;
		}
	}
}
