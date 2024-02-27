using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Statement", "Do While", inputs = new[] { typeof(bool) })]
	[Description("The do while statement executes first 1x body and will loop calling a body until a Condition evaluates to false.")]
	public class DoWhileLoop : FlowNode {
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
			do {
				if(!body.isConnected)
					continue;
				flow.Trigger(body, out var js);
				if(js != null) {
					if(js.jumpType == JumpStatementType.Continue) {
						continue;
					}
					else {
						if(js.jumpType == JumpStatementType.Return) {
							flow.jumpStatement = js;
							return;
						}
						break;
					}
				}
			} while(condition.GetValue<bool>(flow));
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			do {
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
					}
					else {
						if(js.jumpType == JumpStatementType.Return) {
							flow.jumpStatement = js;
							yield break;
						}
						break;
					}
				}
			} while(condition.GetValue<bool>(flow));
		}

		protected override string GenerateFlowCode() {
			string data = CG.Value(condition);
			if(!string.IsNullOrEmpty(data)) {
				data = CG.Condition("do", data, CG.Flow(body));
			}
			return data + CG.FlowFinish(enter, exit).AddLineInFirst();
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("do while: ") + condition.GetRichName();
		}
	}
}