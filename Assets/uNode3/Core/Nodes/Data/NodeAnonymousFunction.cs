using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Anonymous Function", typeof(System.Delegate), icon = typeof(System.Delegate))]
	public class NodeAnonymousFunction : ValueNode {
		public FlowOutput body;
		[Filter(OnlyGetType = true, VoidType = true)]
		public SerializedType returnType = typeof(void);

		public class ParameterData {
			[HideInInspector]
			public string id = uNodeUtility.GenerateUID();
			public string name;
			public SerializedType type = typeof(object);
			public ValueOutput port;
		}
		[HideInInspector]
		public List<ParameterData> parameters = new List<ParameterData>();

		protected override void OnRegister() {
			base.OnRegister();
			body = FlowOutput(nameof(body));
			body.localFunction = true;
			for(int i = 0; i < parameters.Count; i++) {
				var index = i;
				parameters[i].port = ValueOutput(
						parameters[i].id, 
						() => parameters[index].type.type, 
						PortAccessibility.ReadWrite
					).SetName("p" + index.ToString());
				if(!string.IsNullOrEmpty(parameters[i].name)) {
					parameters[i].port.SetName(parameters[i].name);
				}
				parameters[i].port.isVariable = true;
			}
		}

		public override object GetValue(Flow flow) {
			System.Type type = returnType.type;
			if(type != null) {
				if(type == typeof(void)) {
					return CustomDelegate.CreateActionDelegate((obj) => {
						if(nodeObject == null)
							return;
						for(int i = 0; i < parameters.Count; i++) {
							flow.SetPortData(parameters[i].port, obj[i]);
						}
						flow.TriggerParallel(body);
					}, parameters.Select((item) => item.type.type).ToArray());
				} else {
					System.Type[] types = new System.Type[parameters.Count + 1];
					for(int x = 0; x < parameters.Count; x++) {
						types[x] = parameters[x].type.type;
					}
					types[types.Length - 1] = type;
					return CustomDelegate.CreateFuncDelegate((obj) => {
						if(nodeObject == null)
							return null;
						for(int i = 0; i < parameters.Count; i++) {
							flow.SetPortData(parameters[i].port, obj[i]);
						}
						flow.TriggerCoroutine(body, out var wait, out var jump);
						if(wait != null) {
							throw new System.Exception("Coroutine aren't supported by anonymous function in runtime.");
						}
						var js = jump();
						if(js == null || js.jumpType != JumpStatementType.Return) {
							throw new System.Exception("No return value");
						}
						return js.value;
					}, types);
				}
			}
			return null;
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			for(int i = 0; i < parameters.Count; i++) {
				var data = parameters[i];
				if(data.port != null) {
					var vname = CG.RegisterVariable(data.port);
					CG.RegisterPort(data.port, () => vname);
				}
			}
		}

		protected override string GenerateValueCode() {
			System.Type rType = returnType.type;
			if(rType != null && parameters.All(item => item.port != null && item.type.type != null)) {
				string contents = null;
				if(CG.debugScript == false && CG.CanSimplifyToLambda(body, rType, parameters.Select(m => m.port).ToArray())) {
					var targetFlow = body.GetTargetFlow();
					if(targetFlow != null) {
						var targetNode = targetFlow.node.node;
						if(rType == typeof(void)) {
							if(targetNode is MultipurposeNode mn) {
								string result = CG.Value(mn.member);
								if(result.EndsWith(")")) {
									int deep = 0;
									for(int i = result.Length - 1; i > 0; i--) {
										var c = result[i];
										if(c == '(') {
											if(deep == 0) {
												result = result.Remove(i);
												break;
											}
											else {
												deep--;
											}
										}
										else if(c == ')' && i != result.Length - 1) {
											deep++;
										}
									}
									return result;
								}
							}
						}
						else {
							if(targetNode is NodeReturn nr && nr.value != null && nr.value.isAssigned && nr.value.GetTargetNode().node is MultipurposeNode mn) {
								string result = CG.Value(mn.member);
								if(result.Contains(")")) {
									int deep = 0;
									for(int i = result.Length - 1; i > 0; i--) {
										var c = result[i];
										if(c == '(') {
											if(deep == 0) {
												result = result.Remove(i);
												break;
											}
											else {
												deep--;
											}
										}
										else if(c == ')' && i != result.Length - 1) {
											deep++;
										}
									}
								}
								return result;
							}
						}
					}
				}
				List<string> parameterNames = new List<string>();
				for(int i = 0; i < parameters.Count; i++) {
					string varName = null;
					var data = parameters[i];
					if(data.type.type != null) {
						if(!CG.CanDeclareLocal(data.port, body)) {//Auto generate instance variable for parameter.
							varName = CG.GenerateName("tempVar", this);
							var vdata = CG.GetVariableData(data.port);
							vdata.SetToInstanceVariable();
							contents = CG.Flow(
								CG.Set(vdata.name, varName),
								contents
							);
						}
						else {
							varName = CG.GetVariableData(data.port).name;
						}
						parameterNames.Add(varName);
					}
				}
				CG.BeginBlock(allowYield: false);//Ensure there's no yield statements
				contents += CG.Flow(body, false).AddLineInFirst();
				CG.EndBlock();//Ensure to restore to previous block
				return CG.Lambda(parameters.Select(p => p.type.type).ToArray(), parameterNames, contents);
			}
			throw new System.Exception("Return Type is missing or unassigned.");
		}

		public override System.Type ReturnType() {
			System.Type rType = null;
			if(returnType.isFilled) {
				rType = returnType.type;
			}
			if(rType != null && parameters.All(item => item.type.type != null)) {
				if(rType == typeof(void)) {
					return CustomDelegate.GetActionDelegateType(parameters.Select((item) => item.type.type).ToArray());
				} else {
					System.Type[] types = new System.Type[parameters.Count + 1];
					for(int x = 0; x < parameters.Count; x++) {
						types[x] = parameters[x].type.type;
					}
					types[types.Length - 1] = rType;
					return CustomDelegate.GetFuncDelegateType(types);
				}
			}
			return typeof(object);
		}

		public override string GetTitle() {
			return "AnonymousFunction";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			analizer.CheckValue(returnType, nameof(returnType), this);
			analizer.CheckValue(body, nameof(body), this);
		}
	}
}