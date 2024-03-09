namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnCollisionEnter", "OnCollisionEnter")]
	public class OnCollisionEnter : TransitionEvent {
		[Filter(typeof(UnityEngine.Collision), SetMember = true)]
		public MemberData storeCollision = new MemberData();

		public override void OnEnter(Flow flow) {
			UEvent.Register<UnityEngine.Collision>(UEventID.OnTriggerStay2D, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister<UnityEngine.Collision>(UEventID.OnTriggerStay2D, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		void Execute(Flow flow, UnityEngine.Collision collision) {
			if(storeCollision.isAssigned) {
				storeCollision.Set(flow, collision);
			}
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				var mData = CG.generatorData.GetMethodData("OnCollisionEnter");
				if(mData == null) {
					mData = CG.generatorData.AddMethod(
						"OnCollisionEnter",
						typeof(void),
						typeof(UnityEngine.Collision));
				}
				string set = null;
				if(storeCollision.isAssigned) {
					set = CG.Set(CG.Value((object)storeCollision), mData.parameters[0].name).AddLineInEnd();
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
