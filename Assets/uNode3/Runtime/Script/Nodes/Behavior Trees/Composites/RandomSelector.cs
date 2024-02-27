using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Behavior Tree.Composites", "Random Selector", IsCoroutine = true)]
	[Description("Execute node randomly, it will return success if any node return success " +
		"and if all of the node return failure it will return failure.")]
	public class RandomSelector : BaseCoroutineNode {
		[Range(1, 10)]
		public int flowCount = 2;
		[System.NonSerialized]
		public FlowOutput[] flows;

		protected override void OnRegister() {
			base.OnRegister();
			flows = new FlowOutput[flowCount];
			for(int i = 0; i < flowCount; i++) {
				flows[i] = FlowOutput("Flow-" + i).SetName("");
			}
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			List<int> eventIndex = new List<int>();
			for(int i = 0; i < flows.Length; ++i) {
				eventIndex.Add(i);
			}
			List<int> randomOrder = new List<int>();
			for(int i = flows.Length; i > 0; --i) {
				int index = Random.Range(0, i);
				randomOrder.Add(eventIndex[index]);
				eventIndex.RemoveAt(index);
			}
			for(int i = 0; i < flows.Length; i++) {
				var t = flows[randomOrder[i]];
				if(!t.isConnected)
					continue;
				flow.TriggerCoroutine(t, out var wait, out var jump);
				if(wait != null) {
					yield return wait;
				}
				var js = jump();
				if(js != null) {
					flow.jumpStatement = js;
					yield break;
				}
				if(t.GetCurrentState(flow) == StateType.Success) {
					//Success state
					yield return true;
				}
			}
			//Failure state
			yield return false;
		}

		public override void OnGeneratorInitialize() {
			//Register this node as state node, because this is coroutine node with state.
			CG.RegisterAsStateFlow(enter);
			CG.SetStateInitialization(enter, () => CG.GeneratePort(enter));
			for(int i = 0; i < flows.Length; i++) {
				if(flows[i] != null && flows[i].isAssigned) {
					CG.RegisterAsStateFlow(flows[i].GetTargetFlow());
				}
			}
			CG.RegisterPort(enter, () => {
				string data = null;
				for(int i = 0; i < flows.Length; i++) {
					if(flows[i] != null && flows[i].isAssigned) {
						if(!string.IsNullOrEmpty(data)) {
							data += ", ";
						}
						data += CG.GetEvent(flows[i]);
					}
				}
				return CG.New(typeof(Runtime.RandomSelector), data);
			});
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}
	}
}
