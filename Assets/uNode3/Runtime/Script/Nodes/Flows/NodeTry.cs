using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Try-Catch-Finally")]
	public class NodeTry : FlowNode {
		[System.NonSerialized]
		public FlowOutput Try;
		[System.NonSerialized]
		public FlowOutput Finally;

		public class Data {
			public string flowID = uNodeUtility.GenerateUID();
			public string valueID = uNodeUtility.GenerateUID();

			public SerializedType type = typeof(System.Exception);
			[System.NonSerialized]
			public FlowOutput flow;
			[System.NonSerialized]
			public ValueOutput value;
		}

		[HideInInspector]
		public List<Data> exceptions = new List<Data>();

		protected override bool AutoExit => false;

		protected override void OnRegister() {
			for(int i = 0; i < exceptions.Count; i++) {
				var data = exceptions[i];
				data.flow = FlowOutput(data.flowID).SetName(i.ToString());
				data.value = ValueOutput(data.valueID, () => data.type).SetName(i.ToString());
			}
			Try = FlowOutput(nameof(Try));
			Finally = FlowOutput(nameof(Finally));
			base.OnRegister();
			exit.SetName("Next");
		}

		protected override void OnExecuted(Flow flow) {
			if(Finally.isAssigned) {
				if(exceptions.Count > 0) {
					try {
						flow.Trigger(Try, out var js);
						flow.jumpStatement = js;
					}
					catch(System.Exception ex) {
						if(flow.jumpStatement != null)
							return;
						int index = 0;
						foreach(var data in exceptions) {
							if(data.type.isFilled) {
								if(data.type.type.IsAssignableFrom(ex.GetType())) {
									flow.SetPortData(data.value, ex);
									flow.Trigger(data.flow, out var js);
									if(js != null) {
										flow.jumpStatement = js;
										return;
									}
									break;
								}
							} else {
								flow.Trigger(data.flow, out var js);
								if(js != null) {
									flow.jumpStatement = js;
									return;
								}
								break;
							}
							index++;
						}
					}
					finally {
						if(flow.jumpStatement == null) {
							flow.Trigger(Finally);
						}
					}
				} else {
					try {
						flow.Trigger(Try, out var js);
						flow.jumpStatement = js;
					}
					finally {
						if(flow.jumpStatement == null) {
							flow.Trigger(Finally);
						}
					}
				}
			} else if(exceptions.Count > 0) {
				try {
					flow.Trigger(Try, out var js);
					flow.jumpStatement = js;
				}
				catch(System.Exception ex) {
					if(flow.jumpStatement != null)
						return;
					int index = 0;
					foreach(var data in exceptions) {
						if(data.type.isFilled) {
							if(data.type.type.IsAssignableFrom(ex.GetType())) {
								flow.SetPortData(data.value, ex);
								flow.Trigger(data.flow, out var js);
								if(js != null) {
									flow.jumpStatement = js;
									return;
								}
								break;
							}
						} else {
							flow.Trigger(data.flow, out var js);
							if(js != null) {
								flow.jumpStatement = js;
								return;
							}
							break;
						}
						index++;
					}
				}
			}
			if(flow.jumpStatement == null) {
				flow.Next(exit);
			}
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			for(int i = 0; i < exceptions.Count; i++) {
				var member = exceptions[i];
				if(member.type.type != null) {
					var vName = CG.RegisterVariable(member.value, "exception");
					CG.RegisterPort(member.value, () => vName);
				}
			}
		}

		protected override string GenerateFlowCode() {
			string T = null;
			string F = null;
			if(Try.isAssigned) {
				T = CG.Flow(Try);
			}
			if(Finally.isAssigned) {
				F = CG.Flow(Finally);
			}
			string data = "try " + CG.Block(T);
			for(int i = 0; i < exceptions.Count; i++) {
				var member = exceptions[i];
				System.Type type = member.type.type;
				if(type != null) {
					string varName;
					string contents = CG.Flow(exceptions[i].flow);
					if(!CG.CanDeclareLocal(exceptions[i].value, new[] { exceptions[i].flow })) {
						varName = CG.GenerateName("tempVar", this);
						var vdata = CG.GetVariableData(exceptions[i].value);
						vdata.SetToInstanceVariable();
						contents = CG.Flow(
							CG.Set(vdata.name, varName),
							contents
						);
					}
					else {
						varName = CG.GetVariableData(exceptions[i].value).name;
					}
					string declaration = CG.Type(type) + " " + varName;
					data += "\n" + CG.Condition("catch", declaration, contents);
				}
				else {
					data += "\ncatch " + CG.Block(CG.Flow(exceptions[i].flow));
					break;
				}
			}
			if(exceptions.Count == 0 || !string.IsNullOrEmpty(F)) {
				data += "\nfinally " + CG.Block(F);
			}
			return CG.Flow(data, CG.FlowFinish(enter, exit));
		}

		public override string GetTitle() {
			return "Try-Catch-Finally";
		}

		protected override bool IsCoroutine() {
			return Try.IsCoroutine() || Finally.IsCoroutine() || exit.IsCoroutine();
		}
	}
}