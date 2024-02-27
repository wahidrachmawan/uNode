using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	public class NodeReroute : Node {
		public enum RerouteKind {
			Flow,
			Value,
		}
		[Hide]
		public RerouteKind kind;
		public FlowInput enter { get; set; }
		public FlowOutput exit { get; set; }
		public ValueInput input { get; set; }
		public ValueOutput output { get; set; }

		protected override void OnRegister() {
			switch(kind) {
				case RerouteKind.Flow:
					exit = PrimaryFlowOutput(nameof(exit));
					enter = PrimaryFlowInput(nameof(enter), (flow) => {
						flow.Next(exit);
					});
					enter.isCoroutine = exit.IsCoroutine;
					break;
				case RerouteKind.Value:
					input = ValueInput(nameof(input), ReturnType, MemberData.None);
					input.canSetValue = () => {
						var target = input.GetTargetPort();
						if(target != null) {
							return target.CanSetValue();
						}
						return false;
					};
					output = PrimaryValueOutput(nameof(output)).SetName("Out");
					break;
			}
		}

		public override void OnGeneratorInitialize() {
			CG.RegisterPort(enter, () => {
				return CG.FlowFinish(enter, exit);
			});
			CG.RegisterPort(output, () => {
				return CG.Value(input);
			});
		}

		public override Type ReturnType() {
			if(input == null)
				return typeof(object);
			return input.ValueType;
		}

		public override bool CanGetValue() {
			return input.CanGetValue();
		}

		public override bool CanSetValue() {
			return input.CanSetValue();
		}

		public override object GetValue(Flow flow) {
			return input.GetValue(flow);
		}

		public override void SetValue(Flow flow, object value) {
			input.SetValue(flow, value);
		}

		public override Type GetNodeIcon() {
			return IsFlowNode() ? typeof(TypeIcons.FlowIcon) : ReturnType();
		}

		public override string GetTitle() {
			return "Reroute";
		}

		public override string GetRichName() {
			switch(kind) {
				case RerouteKind.Flow:
					return uNodeUtility.GetNicelyDisplayName(exit, richName: true);
				case RerouteKind.Value:
					return uNodeUtility.GetNicelyDisplayName(input, richName: true);
			}
			return base.GetRichName();
		}
	}
}