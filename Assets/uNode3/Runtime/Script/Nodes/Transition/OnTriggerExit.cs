namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnTriggerExit", "OnTriggerExit")]
	public class OnTriggerExit : TransitionEvent {
		[Filter(typeof(UnityEngine.Collider), SetMember = true)]
		public MemberData storeCollider = new MemberData();

		public override void OnEnter(Flow flow) {
			UEvent.Register<UnityEngine.Collider>(UEventID.OnTriggerExit, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister<UnityEngine.Collider>(UEventID.OnTriggerExit, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
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
				var mData = CG.generatorData.GetMethodData("OnTriggerExit");
				if(mData == null) {
					mData = CG.generatorData.AddMethod(
						"OnTriggerExit",
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
