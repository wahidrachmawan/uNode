using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode {
	public class NodeBaseCaller : MultipurposeNode {

		public override void OnGeneratorInitialize() {
			target.instance = null;
			member.OnGeneratorInitialize(exit);
			CG.RegisterPort(enter, () => {
				return CG.Flow("base".CGAccess(CG.Value(member)).AddSemicolon(), CG.FlowFinish(enter, exit));
			});
			if (output != null) {
				CG.RegisterPort(output, () => {
					return "base".CGAccess(CG.Value(member, setVariable: CG.generationState.contextState == CG.ContextState.Set));
				});
			}
		}

		public override string GetTitle() {
			var strs = target.DisplayName().Split('.');
			strs[0] = "base";
			return string.Join('.', strs);
		}

		public override string GetRichTitle() {
			var kind = uNodeUtility.preferredDisplay;
			if(kind == DisplayKind.Partial)
				kind = DisplayKind.Default;
			var strs = uNodeUtility.GetNicelyDisplayName(target, kind).Split('.');
			strs[0] = uNodeUtility.WrapTextWithKeywordColor("base");
			return string.Join('.', strs);
		}

		protected override void OnRegister() {
			if(target == null || !target.isAssigned)
				return;
			member.Register(this,
				createOutPort: () => {
					output = PrimaryValueOutput("out");
				},
				createFlowPort: () => {
					enter = PrimaryFlowInput(nameof(enter), (flow) => {
						nodeObject.GetPrimaryValue(flow);
						flow.Next(exit);
					});
					enter.isCoroutine = () => exit.IsCoroutine();
					exit = PrimaryFlowOutput(nameof(exit));
				},
				preferOutputForParameters: useOutputParameters,
				createInstancePort: false
			);
		}

		#region Runtime
		public override void SetValue(Flow flow, object value) {
			var members = target.GetMembers();
			if(members.Length == 1) {
				if(members[0] is PropertyInfo property) {
					if(property is IRuntimeMemberWithRef memberWithRef) {
						var prop = memberWithRef.GetReference().ReferenceValue as Property;
						if(prop != null) {
							prop.DoSet(flow, value);
							return;
						}
						else {
							throw null;
						}
					}
					else {
						target.startTarget = flow.target;
						member.SetValue(flow, value);
						return;
					}
				}
				else if(members[0] is FieldInfo) {
					target.startTarget = flow.target;
					//Dirrect set the variable since variable cannot be virtual/override
					member.SetValue(flow, value);
					return;
				}
			}
			throw new InvalidOperationException("Invalid target.");
		}

		public override object GetValue(Flow flow) {
			var members = target.GetMembers();
			if(members.Length == 1) {
				if(members[0] is MethodInfo method) {
					if(method is IRuntimeMemberWithRef memberWithRef) {
						var func = memberWithRef.GetReferenceValue() as Function;
						if(func != null) {
							return func.DoInvoke(flow, member.GetParameterValues(flow));
						}
						else {
							throw null;
						}
					}
					else {
						target.startTarget = flow.target;
						return member.GetValue(flow);
					}
				}
				else if(members[0] is PropertyInfo property) {
					if(property is IRuntimeMemberWithRef memberWithRef) {
						var prop = memberWithRef.GetReference().ReferenceValue as Property;
						if(prop != null) {
							return prop.DoGet(flow);
						}
						else {
							throw null;
						}
					}
					else {
						target.startTarget = flow.target;
						return member.GetValue(flow);
					}
				}
				else if(members[0] is FieldInfo) {
					//Dirrect get the variable since variable cannot be virtual/override
					target.startTarget = flow.target;
					return member.GetValue(flow);
				}
			}
			throw new InvalidOperationException("Invalid target.");
		}
		#endregion

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(target == null) return;
			if(target.targetType.HasFlags(
				MemberData.TargetType.Field |
				MemberData.TargetType.Property | 
				MemberData.TargetType.Method) == false) {
				analizer.RegisterError(this, "Unassigned or invalid target.");
			}
			if(target.isDeepTarget) {
				analizer.RegisterError(this, "Base Caller doesn't support deep targeting.");
			}
		}
	}
}