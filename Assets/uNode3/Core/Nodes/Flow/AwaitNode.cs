using System;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class AwaitNode : FlowNode {
		public ValueInput value { get; set; }
		public ValueOutput output { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			value = ValueInput(nameof(value), typeof(object), MemberData.Null);
			if(ReturnType() != typeof(void)) {
				output = PrimaryValueOutput(nameof(output)).SetName("Out");
			}
		}

		protected override void OnExecuted(Flow flow) {
			throw new NotImplementedException("Await node is not supported in reflection mode");
		}

		protected override System.Type ReturnType() {
			if(value.isAssigned) {
				try {
					return uNodeUtility.GetAsyncReturnType(value.ValueType) ?? typeof(object);
				}
				catch { }
			}
			return typeof(object);
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			CG.RegisterPort(output, () => {
				return "(await " + CG.Value(value) + ")";
			});
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(
				("await " + CG.Value(value)).AddSemicolon(),
				CG.FlowFinish(enter, exit)
			);
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.ValueIcon);
		}

		public override string GetTitle() => "Await";

		public override string GetRichName() {
			return $"Await: {value.GetRichName()}";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckPort(value);
			if(value.isAssigned) {
				if(value.UseDefaultValue) {
					if(value.DefaultValue.IsTargetingNull) {
						analizer.RegisterError(this, $"The value must be assigned.");
						return;
					}
				}
				var type = value.ValueType;
				if(type != null && type != typeof(void)) {
					var awaiterMethod = type.GetMemberCached(nameof(System.Threading.Tasks.Task.GetAwaiter));
					if(awaiterMethod != null && awaiterMethod is System.Reflection.MethodInfo methodInfo) {
						var returnType = methodInfo.ReturnType;
						if(returnType.HasImplementInterface(typeof(System.Runtime.CompilerServices.INotifyCompletion))) {
							var resultMethod = returnType.GetMemberCached("GetResult") as System.Reflection.MethodInfo;
							if(resultMethod == null) {
								analizer.RegisterError(this, $"Invalid await type `{returnType.PrettyName()}` the type doesn't implement `GetResult` method.");
							}
						}
						else {
							analizer.RegisterError(this, $"Invalid await type `{returnType.PrettyName()}` the type doesn't implement `{typeof(System.Runtime.CompilerServices.INotifyCompletion)}` interface.");
						}
					}
					else {
						analizer.RegisterError(this, $"Invalid target type `{type.PrettyName()}` the type doesn't implement `GetAwaiter` method.");
					}
				}
			}
		}
	}
}