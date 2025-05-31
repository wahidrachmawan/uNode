using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode {
	public class MultipurposeNode : Node {
		public MultipurposeMember member = new MultipurposeMember();
		public bool useOutputParameters = true;

		public MemberData target {
			get => member.target;
			set {
				member.target = value;
				nodeObject.Register();
				if(instance != null && instance.isAssigned == false) {
					instance.AssignToDefault(value.instance);
					value.instance = null;
				}
			}
		}

		public FlowInput enter { get; protected set; }
		public FlowOutput exit { get; protected set; }
		public ValueOutput output { get; protected set; }

		public ValueInput instance => member.instance;

		public List<MultipurposeMember.MParamInfo> parameters => member.parameters;
		public List<MultipurposeMember.InitializerData> initializers => member.initializers;

		public override void OnGeneratorInitialize() {
			if(output != null && output.hasValidConnections) {
				member.OnGeneratorInitialize(output.ValidConnections.SelectMany(c => c.Input.node.FlowOutputs).ToArray());
			}
			else {
				member.OnGeneratorInitialize(exit);
			}
			CG.RegisterPort(enter, () => {
				return CG.Flow(CG.Value(member).AddSemicolon(), CG.FlowFinish(enter, exit));
			});
			if (output != null) {
				CG.RegisterPort(output, () => {
					return CG.Value(member, setVariable: CG.generationState.contextState == CG.ContextState.Set);
				});
			}
		}

		public override string GetTitle() {
			return uNodeUtility.GetNicelyDisplayName(target, richName: false);
		}

		public override string GetRichTitle() {
			return uNodeUtility.GetNicelyDisplayName(target);
		}

		public override string GetRichName() {
			return uNodeUtility.GetNicelyDisplayName(member, richName: true);
		}

		public override Type GetNodeIcon() {
			switch(target.targetType) {
				case MemberData.TargetType.Null:
					return typeof(TypeIcons.NullTypeIcon);
				case MemberData.TargetType.Method:
					if(target.IsExtension) {
						return typeof(TypeIcons.ExtensionMethodIcon);
					}
					return target.startType;
				case MemberData.TargetType.Field:
				case MemberData.TargetType.Property:
				case MemberData.TargetType.Event:
					return target.startType;
				case MemberData.TargetType.Values:
					return output?.type ?? typeof(object);
				case MemberData.TargetType.uNodeFunction:
					return output?.type ?? typeof(void);
			}
			if(target.isDeepTarget) {
				return target.startType;
			} else if(target.IsTargetingUNode) {
				return output?.type ?? typeof(object);
			}
			return typeof(void);
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
				preferOutputForParameters: useOutputParameters
			);
		}

		#region Runtime
		public override Type ReturnType() {
			if(output != null && target != null && target.isAssigned) {
				return target.type;
			}
			return typeof(object);
		}

		public override bool CanGetValue() {
			return member.CanGetValue();
		}

		public override bool CanSetValue() {
			return member.CanSetValue();
		}

		public override void SetValue(Flow flow, object value) {
			member.SetValue(flow, value);
			//This for boxed value
			if(target.isDeepTarget == false) {
				if(instance != null && instance.type != null && instance.type.IsValueType) {
					if(instance.CanSetValue()) {
						instance.SetValue(flow, target.startTarget);
					}
				}
			}
		}

		public override object GetValue(Flow flow) {
			return member.GetValue(flow);
		}
		#endregion

		public override void CheckError(ErrorAnalyzer analizer) {
			member.CheckErrors(this, analizer);

			if(enter != null && enter.isConnected && output != null && output.isConnected) {
				analizer.RegisterError(this, "Flow and Value is both connected, this causes double-execution");
			}

			if(useOutputParameters && member.target.HasRefOrOut) {
				if((enter == null || !enter.hasValidConnections) && (output == null || !output.hasValidConnections)) {
					foreach(var param in member.parameters) {
						if(param.output != null && param.output.hasValidConnections) {
							analizer.RegisterError(this, "Please connect the enter or output port in order to use output parameters");
							break;
						}
					}
				}
			}
		}
	}
}