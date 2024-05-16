using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq.Expressions;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Delegate Function", typeof(System.Delegate), icon = typeof(System.Delegate))]
	public class NodeDelegateFunction : ValueNode {
		public MultipurposeMember member = new MultipurposeMember();

		public MemberData target => member.target;

		public ValueInput instance { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			if(target.isAssigned && target.isStatic == false) {
				if(target.targetType != MemberData.TargetType.uNodeFunction) {
					instance = ValueInput(nameof(instance), target.startType);
				}
			}
		}

		#region Codegen
		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
		}

		protected override string GenerateValueCode() {
			string result;
			if(instance != null) {
				result = CG.Value(instance).CGAccess(CG.Nameof(member.target));
			}
			else {
				result = CG.Nameof(member.target);
			}
			return CG.New(ReturnType(), result);
		}
		#endregion

		public override object GetValue(Flow flow) {
			if(target.isAssigned) {
				if(target.targetType == MemberData.TargetType.uNodeFunction) {
					var function = target.startItem.GetReferenceValue() as Function;
					if(function != null) {
						return function.GetDelegate(flow);
					}
				}
				else {
					var members = target.GetMembers();
					var lastMember = members[members.Length - 1] as MethodInfo;
					if(lastMember is IRuntimeMemberWithRef runtimeMemberWithRef) {
						var function = runtimeMemberWithRef.GetReferenceValue() as Function;
						if(function != null) {
							return function.GetDelegate(flow);
						}
					}
					if(instance != null) {
						return CustomDelegate.CreateDelegate(lastMember, instance.GetValue(flow));
					}
					else {
						return CustomDelegate.CreateDelegate(lastMember, null);
					}
				}
			}
			return null;
		}

		public override bool CanGetValue() {
			return true;
		}

		public override System.Type ReturnType() {
			if(target.isAssigned) {
				if(target.targetType == MemberData.TargetType.uNodeFunction) {
					var function = target.startItem.GetReferenceValue() as Function;
					if(function != null) {
						var type = function.ReturnType();
						var parameters = function.parameters.Select(p => p.Type);
						if(parameters.Any(p => p.IsByRef) == false) {
							if(type == typeof(void)) {
								return CustomDelegate.GetActionDelegateType(parameters.ToArray());
							}
							else {
								return CustomDelegate.GetFuncDelegateType(parameters.Append(type).ToArray());
							}
						}
					}
				}
				else {
					var members = target.GetMembers(false);
					if(members != null && members.Length > 0) {
						var method = members[members.Length - 1] as MethodInfo;
						if(method != null) {
							var type = method.ReturnType;
							var parameters = method.GetParameters().Select(p => p.ParameterType);
							if(parameters.Any(p => p.IsByRef) == false) {
								if(type == typeof(void)) {
									return CustomDelegate.GetActionDelegateType(parameters.ToArray());
								}
								else {
									return CustomDelegate.GetFuncDelegateType(parameters.Append(type).ToArray());
								}
							}
						}
					}
				}
			}
			return typeof(Delegate);
		}

		public override string GetTitle() {
			if(target.isAssigned) {
				return $"Delegate: {target.GetDisplayName()}";
			}
			return "Delegate Function";
		}

		public override string GetRichTitle() {
			if(target.isAssigned) {
				return $"Delegate: {target.GetNicelyDisplayName()}";
			}
			return "Delegate Function";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckValue(target, nameof(target), this);
			if(target.isAssigned) {
				if(target.isDeepTarget) {
					analizer.RegisterError(this, "Target is not support to use deep member targeting.");
				}
			}
		}
	}
}