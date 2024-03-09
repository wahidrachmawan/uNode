namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnCollisionEnter2D", "OnCollisionEnter2D")]
	public class OnCollisionEnter2D : TransitionEvent {
		[Filter(typeof(UnityEngine.Collision2D), SetMember = true)]
		public MemberData storeCollision = new MemberData();

		public override void OnEnter(Flow flow) {
			UEvent.Register<UnityEngine.Collision2D>(UEventID.OnCollisionEnter2D, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister<UnityEngine.Collision2D>(UEventID.OnCollisionEnter2D, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}


		void Execute(Flow flow, UnityEngine.Collision2D collision) {
			if(storeCollision.isAssigned) {
				storeCollision.Set(flow, collision);
			}
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				var mData = CG.generatorData.GetMethodData("OnCollisionEnter2D");
				if(mData == null) {
					mData = CG.generatorData.AddMethod(
						"OnCollisionEnter2D",
						typeof(void),
						typeof(UnityEngine.Collision2D));
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
