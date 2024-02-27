using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Yield", "Yield Return", IsCoroutine=true)]
	public class NodeYieldReturn : CoroutineNode {
		[System.NonSerialized]
		public ValueInput value;

		protected override void OnRegister() {
			base.OnRegister();
			value = ValueInput(nameof(value), () => {
				var func = nodeObject.GetObjectInParent<Function>();
				if(func != null) {
					var type = func.ReturnType();
					if(type != null) {
						if(type.HasImplementInterface(typeof(IEnumerable<>)) || type.HasImplementInterface(typeof(IEnumerator<>))) {
							return type.GetGenericArguments()[0];
						}
					}
				}
				return typeof(object);
			}, MemberData.Null);
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			yield return value.GetValue(flow);
		}

		public override void OnGeneratorInitialize() {
			CG.RegisterPort(enter, () => {
				if(!value.isAssigned) throw new System.Exception("Unassigned value");
				return CG.Flow(
					CG.YieldReturn(CG.Value(value)),
					CG.FlowFinish(enter, exit)
				);
			});
			if(CG.IsStateFlow(enter)) {
				CG.SetStateInitialization(enter, () => {
					return CG.Routine(
						CG.RoutineYield(CG.SimplifiedLambda(CG.Value(value))),
						exit.isAssigned ? CG.Routine(CG.GetEvent(exit)) : null
					);
				});
				CG.RegisterAsStateFlow(exit.GetTargetFlow());
			}
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("yield return ") + value.GetRichName();
		}
	}
}
