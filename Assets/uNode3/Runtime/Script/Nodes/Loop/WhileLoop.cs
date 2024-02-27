using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Statement", "While")]
	[Description("The while statement executes a body until a Condition evaluates to false.")]
	public class WhileLoop : FlowNode {
		public FlowOutput body { get; private set; }

		public ValueInput condition { get; set; }

		protected override void OnRegister() {
			body = FlowOutput(nameof(body));
			condition = ValueInput<bool>(nameof(condition), true);
			base.OnRegister();
			exit.SetName("Exit");
		}

		protected override bool IsCoroutine() {
			return HasCoroutineInFlows(body, exit);
		}

		protected override void OnExecuted(Flow flow) {
			while(condition.GetValue<bool>(flow)) {
				if(!body.isConnected)
					continue;
				flow.Trigger(body, out var js);
				if(js != null) {
					if(js.jumpType == JumpStatementType.Continue) {
						continue;
					} else {
						if(js.jumpType == JumpStatementType.Return) {
							flow.jumpStatement = js;
							return;
						}
						break;
					}
				}
			}
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			while(condition.GetValue<bool>(flow)) {
				if(!body.isConnected)
					continue;
				flow.TriggerCoroutine(body, out var wait, out var jump);
				if(wait != null) {
					yield return wait;
				}
				var js = jump();
				if(js != null) {
					if(js.jumpType == JumpStatementType.Continue) {
						continue;
					} else {
						if(js.jumpType == JumpStatementType.Return) {
							flow.jumpStatement = js;
							yield break;
						}
						break;
					}
				}
			}
		}

		protected override string GenerateFlowCode() {
			if(!body.isAssigned) {
				throw new System.Exception("body is unassigned");
			}
			string data = CG.Condition("while", CG.Value(condition), CG.Flow(body));
			return data + CG.FlowFinish(enter, exit);
		}
	}
}