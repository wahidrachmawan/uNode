using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Statement", "Foreach", inputs = new[] { typeof(IEnumerable) })]
	[Description("The foreach statement repeats a body of embedded statements for each element in an array or a generic type.")]
	public class ForeachLoop : FlowNode {
		public bool deconstructValue = true;

		public class DeconstructData {
			public string id = uNodeUtility.GenerateUID();
			public string name;

			[NonSerialized]
			public string originalName;

			public ValueOutput port { get; set; }
		}
		public List<DeconstructData> deconstructDatas;

		public FlowOutput body { get; private set; }
		public ValueInput collection { get; set; }
		public ValueOutput output { get; set; }

		private MethodInfo deconstructMethod;

		private static readonly FilterAttribute filter;

		static ForeachLoop() {
			filter = new FilterAttribute(typeof(IEnumerable)) {
				ValidateType = type => {
					if(type.IsCastableTo(typeof(IEnumerable))) {
						return true;
					}
					else {
						var member = type.GetMemberCached(nameof(IEnumerable.GetEnumerator));
						if(member is MethodInfo) {
							return true;
						}
					}
					return false;
				},
			};
		}

		protected override void OnRegister() {
			body = FlowOutput(nameof(body));
			collection = ValueInput<IEnumerable>(nameof(collection));
			collection.filter = filter;
			if(deconstructValue && collection.hasValidConnections && CanDeconstruct(collection.ValueType.ElementType())) {
				output = null;
				var type = collection.ValueType.ElementType();
				if(deconstructDatas == null)
					deconstructDatas = new List<DeconstructData>();
				//Get the deconstruct method
				deconstructMethod = type.GetMemberCached(nameof(KeyValuePair<int, int>.Deconstruct)) as MethodInfo;
				if(deconstructMethod != null) {
					//parameter outputs
					var parameters = deconstructMethod.GetParameters();
					//Resize the data
					uNodeUtility.ResizeList(deconstructDatas, parameters.Length);
					for(int i = 0; i < parameters.Length; i++) {
						//Cache the index for use in delegates
						var index = i;
						deconstructDatas[index].originalName = parameters[index].Name;
						//Create the port
						deconstructDatas[index].port = ValueOutput(deconstructDatas[index].id,
							parameters[index].ParameterType.ElementType() ?? parameters[index].ParameterType,
							PortAccessibility.ReadOnly);
						if(!string.IsNullOrEmpty(deconstructDatas[index].name)) {
							deconstructDatas[index].port.SetName(deconstructDatas[index].name);
						}
						else {
							deconstructDatas[index].port.SetName(deconstructDatas[index].originalName);
						}
						deconstructDatas[index].port.isVariable = true;
					}
				} else {
					if(type.IsGenericType && type.HasImplementInterface(typeof(System.Runtime.CompilerServices.ITuple))) {
						var arguments = type.GetGenericArguments();
						//Resize the data
						uNodeUtility.ResizeList(deconstructDatas, arguments.Length);
						for(int i = 0; i < arguments.Length; i++) {
							//Cache the index for use in delegates
							var index = i;
							//Create the port
							deconstructDatas[index].port = ValueOutput(deconstructDatas[index].id,
								arguments[index],
								PortAccessibility.ReadWrite
							);
							deconstructDatas[index].originalName = "Item" + (i + 1);
							if(!string.IsNullOrEmpty(deconstructDatas[index].name)) {
								deconstructDatas[index].port.SetName(deconstructDatas[index].name);
							}
							else {
								deconstructDatas[index].port.SetName(deconstructDatas[index].originalName);
							}
							deconstructDatas[index].port.isVariable = true;
						}
					}
				}
			} 
			else {
				output = ValueOutput(nameof(output), ReturnType);
			}
			base.OnRegister();
			exit.SetName("Exit");
		}

		private bool CanDeconstruct(Type type) {
			if(type == null) return false;
			if(type.GetMemberCached(nameof(KeyValuePair<int, int>.Deconstruct)) is MethodInfo) {
				return true;
			} 
			else if(type.IsGenericType && type.HasImplementInterface(typeof(System.Runtime.CompilerServices.ITuple))) {
				return type.IsGenericType;
			}
			return false;
		}

		public override Type ReturnType() {
			if(collection != null) {
				var type = collection.ValueType;
				return type.ElementType();
			}
			return base.ReturnType();
		}

		protected override bool IsCoroutine() {
			return HasCoroutineInFlows(body, exit);
		}

		private void UpdateLoopValue(Flow flow, object obj) {
			//If output null mean, the node uses deconstructors
			if(output == null) {
				if(deconstructMethod != null) {
					var parameters = new object[deconstructDatas.Count];
					deconstructMethod.InvokeOptimized(obj, parameters);
					//Update the deconstruct data values.
					for(int i = 0; i < parameters.Length; i++) {
						flow.SetPortData(deconstructDatas[i].port, parameters[i]);
					}
				}
				else if(obj is System.Runtime.CompilerServices.ITuple tuple) {
					var length = tuple.Length;
					//Update the deconstruct data values.
					for(int i = 0; i < length; i++) {
						flow.SetPortData(deconstructDatas[i].port, tuple[i]);
					}
				}
			}
			else {
				//Update the loop value
				flow.SetPortData(output, obj);
			}
		}

		protected override void OnExecuted(Flow flow) {
			IEnumerable lObj = collection.GetValue<IEnumerable>(flow);
			if(lObj != null) {
				foreach(object obj in lObj) {
					if(!body.isConnected)
						continue;
					UpdateLoopValue(flow, obj);
					flow.Trigger(body, out var js);
					if(js != null) {
						if(js.jumpType == JumpStatementType.Continue) {
							continue;
						} else {
							if(js.jumpType == JumpStatementType.Return) {
								flow.jumpStatement = js;
								return;
							}
							break;
						}
					}
				}
			} else {
				Debug.LogError("The collection is null or not IEnumerable");
			}
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			IEnumerable lObj = collection.GetValue<IEnumerable>(flow);
			if(lObj != null) {
				foreach(object obj in lObj) {
					if(!body.isConnected)
						continue;
					UpdateLoopValue(flow, obj);
					flow.TriggerCoroutine(body, out var wait, out var jump);
					if(wait != null) {
						yield return wait;
					}
					var js = jump();
					if(js != null) {
						if(js.jumpType == JumpStatementType.Continue) {
							continue;
						} else {
							if(js.jumpType == JumpStatementType.Return) {
								flow.jumpStatement = js;
								yield break;
							}
							break;
						}
					}
				}
			} else {
				Debug.LogError("The collection is null or not IEnumerable");
			}
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();

			if(output == null) {
				for(int i=0;i< deconstructDatas.Count;i++) {
					var data = deconstructDatas[i];
					if(data.port != null) {
						var vName = CG.RegisterVariable(data.port);
						CG.RegisterPort(data.port, () => vName);
					}
				}
			} else {
				var vName = CG.RegisterVariable(output, "loopValue");
				CG.RegisterPort(output, () => vName);
			}
		}

		protected override string GenerateFlowCode() {
			string targetCollection = CG.Value(collection);
			if(!string.IsNullOrEmpty(targetCollection)) {
				string contents = CG.Flow(body).AddLineInFirst();
				string additionalContents = null;
				var targetType = collection.ValueType;
				var elementType = targetType.ElementType();
				if(output == null) {
					string[] paramDatas = new string[deconstructDatas.Count];
					for(int i = 0; i < deconstructDatas.Count; i++) {
						var data = deconstructDatas[i];
						if(data.port != null && data.port.hasValidConnections) {
							string vName;
							if(!CG.CanDeclareLocal(data.port, body)) {
								vName = CG.GenerateName("tempVar", this);
								var vdata = CG.GetVariableData(data.port);
								vdata.SetToInstanceVariable();
								additionalContents = CG.Set(vdata.name, vName);
							}
							else {
								vName = CG.GetVariableData(data.port).name;
							}
							paramDatas[i] = vName;
						}
						else {
							//Discard if the port is not used.
							paramDatas[i] = "_";
						}
					}
					return CG.Flow(
							CG.Condition("foreach", "var (" + string.Join(", ", paramDatas) + ") in " + targetCollection, additionalContents + contents),
							CG.FlowFinish(enter, exit)
						);
				}
				else {
					string vName;
					if(CG.generatePureScript && ReflectionUtils.IsNativeType(targetType) == false) {
						//Auto convert to actual type if the target type is not native type.
						if(!CG.CanDeclareLocal(output, body)) {
							vName = CG.GenerateName("tempVar", this);
							additionalContents = CG.Set(CG.GetVariableName(output), CG.As(vName, elementType));
							//This for auto declare variable
							CG.DeclareVariable(output, "", body);
						}
						else {
							vName = CG.GenerateName("tempVar", this);
							additionalContents = "var " + CG.GetVariableName(output) + " = " + CG.As(vName, elementType) + ";";
						}
					}
					else {
						if(!CG.CanDeclareLocal(output, body)) {
							vName = CG.GenerateName("tempVar", this);
							additionalContents = CG.Set(CG.GetVariableName(output), vName);
							//This for auto declare variable
							CG.DeclareVariable(output, "", body);
						}
						else {
							vName = CG.GetVariableName(output);
						}
					}
					string loopType;
					if(elementType is RuntimeType) {
						loopType = "var ";
					}
					else {
						loopType = CG.Type(elementType) + " ";
					}
					return CG.Flow(
							CG.Condition("foreach", loopType + vName + " in " + targetCollection, additionalContents + contents),
							CG.FlowFinish(enter, exit)
						);
				}
			}
			return null;
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("foreach:") + collection.GetRichName();
		}
	}
}