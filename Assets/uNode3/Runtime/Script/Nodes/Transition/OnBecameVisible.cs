namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnBecameVisible", "OnBecameVisible")]
	public class OnBecameVisible : TransitionEvent {

		public override void OnEnter(Flow flow) {
			UEvent.Register(UEventID.OnBecameVisible, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister(UEventID.OnBecameVisible, flow.target as UnityEngine.Component, () => Execute(flow));
		}

		void Execute(Flow flow) {
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				CG.InsertCodeToFunction(
					"OnBecameVisible",
					typeof(void),
					CG.Condition("if", CG.CompareNodeState(enter, null), CG.FlowTransitionFinish(this)));
			}
			return null;
		}
	}
}
