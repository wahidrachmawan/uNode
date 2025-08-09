using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "If", icon = typeof(TypeIcons.BranchIcon), inputs = new[] { typeof(bool) })]
	[Description("The If node will Identifies which flow to run based on the value of a Boolean expression")]
	public class NodeIf : FlowNode {
		public ValueInput condition { get; set; }
		public FlowOutput onTrue { get; set; }
		public FlowOutput onFalse { get; set; }

		protected override void OnRegister() {
			condition = ValueInput(nameof(condition), typeof(bool));
			condition.SetTooltip("The condition to evaluate.");
			onTrue = FlowOutput(nameof(onTrue));
			onTrue.SetTooltip("Flow to execute when the condition is true");
			onFalse = FlowOutput(nameof(onFalse));
			onFalse.SetTooltip("Flow to execute when the condition is false");
			base.OnRegister();
			exit.SetName("Exit");
		}

		protected override bool IsCoroutine() {
			return HasCoroutineInFlows(onTrue, onFalse, exit);
		}

		protected override void OnExecuted(Flow flow) {
			if(condition.GetValue<bool>(flow)) {
				flow.Next(onTrue);
				flow.state = StateType.Success;
			}
			else {
				flow.Next(onFalse);
				flow.state = StateType.Failure;
			}
		}

		public override void OnGeneratorInitialize() {
			CG.RegisterPort(enter, () => {
				if(!condition.isAssigned)
					throw new Exception("Condition is unassigned.");
				string data = CG.Value(condition);
				if(!string.IsNullOrEmpty(data)) {
					if(CG.IsStateFlow(enter)) {
						CG.SetStateInitialization(enter,
							CG.New(
								typeof(Runtime.Conditional),
								CG.SimplifiedLambda(data),
								CG.GetEvent(onTrue).AddFirst("onTrue: "),
								CG.GetEvent(onFalse).AddFirst("onFalse: "),
								CG.GetEvent(exit).AddFirst("onFinished: ")
							));
						return null;
					}
					else if(CG.debugScript) {
						return CG.If(data,
							CG.FlowFinish(enter, true, onTrue, exit),
							CG.FlowFinish(enter, false, onFalse, exit));
					}
					if(onTrue.isAssigned) {
						if(onFalse.isAssigned) {
							data = CG.If(data, CG.Flow(onTrue));
							string failure = CG.Flow(onFalse);
							if(!string.IsNullOrEmpty(failure)) {
								var flag = onFalse.GetTargetNode().node is NodeIf other && !other.exit.isAssigned;
								if(flag && CG.IsRegularFlow(enter)) {
									data += "\n else " + failure.RemoveLineAndTabOnFirst();
								}
								else {
									data += "\n else {" + failure.AddLineInFirst().AddTabAfterNewLine(1) + "\n}";
								}
							}
						}
						else {
							data = CG.If(data, CG.Flow(onTrue));
						}
						data += CG.FlowFinish(enter, exit).AddLineInFirst();
						return data;
					}
					else if(onFalse.isAssigned) {
						return
							CG.If(
								data.AddFirst("!(").Add(")"),
								CG.Flow(onFalse)) +
							CG.FlowFinish(enter, false, exit).AddLineInFirst();
					}
					return
						CG.If(data, "") +
						CG.FlowFinish(enter, exit).AddLineInFirst();
				}
				else {
					throw new Exception("Condition generates empty code.");
				}
			});
		}

		public override string GetRichName() {
			if(condition.isAssigned) {
				return $"If: {condition.GetRichName()}";
			}
			return base.GetRichName();
		}
	}
}