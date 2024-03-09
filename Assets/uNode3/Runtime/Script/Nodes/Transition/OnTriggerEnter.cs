namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnTriggerEnter", "OnTriggerEnter")]
	public class OnTriggerEnter : TransitionEvent {
		[Filter(typeof(UnityEngine.Collider), SetMember = true)]
		public MemberData storeCollider = new MemberData();

		public override void OnEnter(Flow flow) {
			UEvent.Register<UnityEngine.Collider>(UEventID.OnTriggerEnter, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister<UnityEngine.Collider>(UEventID.OnTriggerEnter, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		void Execute(Flow flow, UnityEngine.Collider collider) {
			if(storeCollider.isAssigned) {
				storeCollider.Set(flow, collider);
			}
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				var mData = CG.generatorData.GetMethodData("OnTriggerEnter");
				if(mData == null) {
					mData = CG.generatorData.AddMethod(
						"OnTriggerEnter",
						typeof(void),
						typeof(UnityEngine.Collider));
				}
				string set = null;
				if(storeCollider.isAssigned) {
					set = CG.Set(CG.Value((object)storeCollider), mData.parameters[0].name).AddLineInEnd();
				}
				mData.AddCode(
					CG.Condition(
						"if",
						CG.CompareNodeState(enter, null),
						set + CG.FlowTransitionFinish(this)
					)
				);
			}
			return null;
		}
	}
}
