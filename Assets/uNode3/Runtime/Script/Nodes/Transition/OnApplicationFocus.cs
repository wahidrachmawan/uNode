namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnApplicationFocus", "OnApplicationFocus")]
	public class OnApplicationFocus : TransitionEvent {
		[Filter(typeof(bool), SetMember =true)]
		public MemberData storeValue = new MemberData();

		public override void OnEnter(Flow flow) {
			UEvent.Register<bool>(UEventID.OnApplicationFocus, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister<bool>(UEventID.OnApplicationFocus, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		void Execute(Flow flow, bool val) {
			if(storeValue.isAssigned) {
				storeValue.Set(flow, val);
			}
			Finish(flow);
		}

		public override string GenerateOnEnterCode() {
			if(!CG.HasInitialized(this)) {
				CG.SetInitialized(this);
				var mData = CG.generatorData.GetMethodData("OnApplicationFocus");
				if(mData == null) {
					mData = CG.generatorData.AddMethod(
						"OnApplicationFocus",
						typeof(void),
						typeof(bool));
				}
				string set = null;
				if(storeValue.isAssigned) {
					set = CG.Set(CG.Value((object)storeValue), mData.parameters[0].name).AddLineInEnd();
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
