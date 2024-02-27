using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Validation", order = -1)]
	public class NodeValidation : FlowNode, IStackedNode {
		[HideInInspector]
		public BlockData data = new BlockData();

		public FlowOutput onTrue { get; set; }
		public FlowOutput onFalse { get; set; }
		public IEnumerable<NodeObject> stackedNodes => data.GetFlowNodes();

		protected override void OnRegister() {
			data.Register(this);
			onTrue = FlowOutput(nameof(onTrue));
			onFalse = FlowOutput(nameof(onFalse));
			base.OnRegister();
			exit.SetName("Exit");
		}

		protected override bool IsCoroutine() {
			return HasCoroutineInFlows(onTrue, onFalse, exit);
		}

		protected override void OnExecuted(Flow flow) {
			if(data.Validate(flow)) {
				flow.Next(onTrue);
			} else {
				flow.Next(onFalse);
			}
		}

		protected override string GenerateFlowCode() {
			if(!stackedNodes.Any()) {
				return null;
			}
			if(CG.IsStateFlow(enter)) {
				CG.SetStateInitialization(enter,
					CG.New(
						typeof(Runtime.Conditional),
						CG.SimplifiedLambda(data.GenerateConditionCode()),
						CG.GetEvent(onTrue).AddFirst("onTrue: "),
						CG.GetEvent(onFalse).AddFirst("onFalse: "),
						CG.GetEvent(exit).AddFirst("onFinished: ")
					));
				return null;
			}
			else if(CG.debugScript) {
				return CG.If(data.GenerateConditionCode(),
					CG.FlowFinish(enter, true, true, false, onTrue, exit),
					CG.FlowFinish(enter, false, true, false, onFalse, exit)
				);
			}
			if(onTrue.isAssigned) {
				if(onFalse.isAssigned) {
					//True and False is assigned
					return CG.Flow(
						CG.If(data.GenerateConditionCode(), CG.Flow(onTrue), CG.Flow(onFalse)),
						CG.FlowFinish(enter, exit)
					);
				}
				else {
					//True only
					return CG.Flow(
						CG.If(data.GenerateConditionCode(), CG.Flow(onTrue)),
						CG.FlowFinish(enter, exit)
					);
				}
			}
			else if(onFalse.isAssigned) {
				//False only
				return CG.Flow(
					CG.If(data.GenerateConditionCode().CGNot(), CG.Flow(onFalse)),
					CG.FlowFinish(enter, exit)
				);
			}
			else {
				//No true and False
				return CG.Flow(
					CG.If(data.GenerateConditionCode(), null),
					CG.FlowFinish(enter, exit)
				);
			}
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.ValidationIcon);
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			data.CheckErrors(analizer, true);
		}
	}
}
