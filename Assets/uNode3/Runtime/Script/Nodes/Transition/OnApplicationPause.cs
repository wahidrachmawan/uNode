namespace MaxyGames.UNode.Transition {
	[TransitionMenu("OnApplicationPause", "OnApplicationPause")]
	public class OnApplicationPause : TransitionEvent {
		[Filter(typeof(bool), SetMember = true)]
		public MemberData storeValue = new MemberData();

		public override void OnEnter(Flow flow) {
			UEvent.Register<bool>(UEventID.OnApplicationPause, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
		}

		public override void OnExit(Flow flow) {
			UEvent.Unregister<bool>(UEventID.OnApplicationPause, flow.target as UnityEngine.Component, (value) => Execute(flow, value));
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
				var mData = CG.generatorData.GetMethodData("OnApplicationPause");
				if(mData == null) {
					mData = CG.generatorData.AddMethod(
						"OnApplicationPause",
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
