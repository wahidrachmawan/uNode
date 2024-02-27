using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Switch", icon = typeof(TypeIcons.BranchIcon), inputs = new[] { typeof(int), typeof(float), typeof(double), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint), typeof(string), typeof(System.Enum) })]
	public class NodeSwitch : FlowNode {
		public class Data {
			public string flowID = uNodeUtility.GenerateUID();
			public MemberData value;

			[NonSerialized]
			public FlowOutput flow;
		}
		[HideInInspector]
		public List<Data> datas = new List<Data>();
		[HideInInspector]
		public bool useVerticalLayout = true;

		public ValueInput target { get; set; }
		public FlowOutput defaultTarget { get; set; }

		protected override void OnRegister() {
			target = ValueInput(nameof(target), typeof(object));
			target.filter = new FilterAttribute(typeof(int), typeof(float), typeof(double), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint), typeof(string), typeof(System.Enum)) {
				InvalidTargetType = MemberData.TargetType.Null,
			};
			for(int i = 0; i < datas.Count; i++) {
				datas[i].flow = FlowOutput(datas[i].flowID).SetName(datas[i].value.GetNicelyDisplayName(richName:false));
			}
			defaultTarget = FlowOutput(nameof(defaultTarget)).SetName("Default");
			base.OnRegister();
			exit.SetName("Exit");
		}

		protected override void OnExecuted(Flow flow) {
			if(target == null || !target.isAssigned)
				return;
			object val = target.GetValue(flow);
			if(object.ReferenceEquals(val, null))
				return;
			for(int i = 0; i < datas.Count; i++) {
				if(!datas[i].value.isAssigned)
					continue;
				var dVal = datas[i].value.Get(flow);
				if(val.Equals(dVal)) {
					flow.Next(datas[i].flow);
					return;
				}
			}
			flow.Next(defaultTarget);
		}

		public override void OnGeneratorInitialize() {
			CG.RegisterPort(enter, () => {
				if(target.isAssigned) {
					string data = CG.Value(target);
					if(!string.IsNullOrEmpty(data)) {
						bool hasDefault = defaultTarget != null && defaultTarget.isAssigned;
						string[] cases = new string[datas.Count];
						string[] contents = new string[datas.Count];
						for(int i = 0; i < cases.Length; i++) {
							cases[i] = CG.Value(datas[i].value);
						}
						for(int i = 0; i < contents.Length; i++) {
							contents[i] = CG.Flow(datas[i].flow);
						}
						return CG.Flow(
							CG.Switch(data, cases, contents, hasDefault ? CG.Flow(defaultTarget) : null),
							CG.FlowFinish(enter, exit)
						);
					}
					throw new System.Exception("Can't Parse target");
				}
				throw new System.Exception("Target is unassigned");
			});
			if(CG.Nodes.HasStateFlowInput(this)) {
				CG.RegisterAsStateFlow(enter);
				for(int i = 0; i < datas.Count; i++) {
					CG.RegisterAsStateFlow(datas[i].flow.GetTargetFlow());
				}
				CG.RegisterAsStateFlow(defaultTarget.GetTargetFlow());
				CG.SetStateInitialization(enter, () => {
					if(target.isAssigned) {
						string data = CG.Value(target);
						if(!string.IsNullOrEmpty(data)) {
							bool hasDefault = defaultTarget != null && defaultTarget.isAssigned;
							string[] cases = new string[datas.Count];
							string[] contents = new string[datas.Count];
							for(int i = 0; i < cases.Length; i++) {
								cases[i] = CG.Value(datas[i].value);
							}
							for(int i = 0; i < contents.Length; i++) {
								contents[i] = CG.ReturnEvent(datas[i].flow);
							}
							return CG.Routine(
								CG.Routine(
									CG.Lambda(
										CG.Flow(
											CG.Switch(data, cases, contents, hasDefault ? CG.ReturnEvent(defaultTarget) : null),
											CG.Return(null)
										)
									)
								),
								CG.Routine(CG.GetEvent(exit))
							);
						}
						throw new System.Exception("Can't generate code for target");
					}
					throw new System.Exception("Target is unassigned");
				});
			}
		}

		protected override bool IsCoroutine() {
			for(int i = 0; i < datas.Count; i++) {
				if(datas[i].flow.IsCoroutine())
					return true;
			}
			return exit.IsCoroutine();
		}

		public override string GetRichName() {
			return $"{uNodeUtility.WrapTextWithKeywordColor("switch")}: {target.GetRichName()}";
		}
	}
}