using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Data", "Lambda", typeof(System.Delegate), icon = typeof(System.Delegate))]
	public class NodeLambda : ValueNode {
		public bool autoDelegateType = true;
		[Filter(typeof(System.Delegate), OnlyGetType = true, DisplayAbstractType = false)]
		public SerializedType delegateType = typeof(System.Action);

		[System.NonSerialized]
		public FlowOutput body;
		[System.NonSerialized]
		public ValueInput input;

		private System.Reflection.MethodInfo methodInfo;
		private System.Type[] types;

		public class ParameterData {
			public ValueOutput port;
		}
		[HideInInspector]
		public List<ParameterData> parameters = new List<ParameterData>();

		protected override void OnRegister() {
			base.OnRegister();
			if(autoDelegateType) {
				var firstConnection = output.ValidConnections.FirstOrDefault();
				if(firstConnection != null) {
					var other = (firstConnection.Input as ValueInput);
					Type actualType = null;
					if(other.filter != null) {
						actualType = other.filter.GetActualType();
					}
					if(actualType == null) {
						actualType = other.type;
					}
					if(actualType != null && actualType.IsSubclassOf(typeof(Delegate))) {
						if(delegateType != actualType) {
							delegateType = actualType;
						}
					}
				}
			}
			if(!delegateType.isFilled)
				return;
			var type = delegateType.type;
			methodInfo = type.GetMethod("Invoke");
			if(methodInfo.ReturnType == typeof(void)) {
				body = FlowOutput(nameof(body));
				body.localFunction = true;
				var types = methodInfo.GetParameters();
				while(parameters.Count < types.Length) {
					parameters.Add(new ParameterData());
				}
				for(int x = 0; x < types.Length; x++) {
					int index = x;
					parameters[index].port = ValueOutput(
						"parameter" + index,
						types[x].ParameterType, 
						PortAccessibility.ReadWrite
					).SetName(types[x].Name);
					parameters[index].port.isVariable = true;
				}
				this.types = types.Select(p => p.ParameterType).ToArray();
			} else {
				input = ValueInput(nameof(input), methodInfo.ReturnType);
				var types = methodInfo.GetParameters();
				while(parameters.Count < types.Length) {
					parameters.Add(new ParameterData());
				}
				for(int x = 0; x < types.Length; x++) {
					int index = x;
					parameters[index].port = ValueOutput(
						"parameter" + index,
						types[x].ParameterType,
						PortAccessibility.ReadWrite).SetName(types[x].Name);
					parameters[index].port.isVariable = true;
				}
				this.types = types.Select(p => p.ParameterType).Append(methodInfo.ReturnType).ToArray();
			}
			// m_Delegate = ReflectionUtils.ConvertDelegate(m_Delegate, e.EventHandlerType);
		}

		#region Codegen
		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			for(int i = 0; i < parameters.Count; i++) {
				var data = parameters[i];
				if(data.port != null) {
					var vName = CG.RegisterVariable(data.port);
					CG.RegisterPort(data.port, () => vName);
				}
			}
		}

		protected override string GenerateValueCode() {
			if(!delegateType.isAssigned) throw new System.Exception("Delegate Type is not assigned");
			var type = delegateType.type;
			var methodInfo = type.GetMethod("Invoke");
			bool flag;
			if(CG.debugScript) {
				//Disable simplify the delegate
				flag = false;
			} else if(methodInfo.ReturnType == typeof(void)) {
				flag = body.isAssigned && CG.CanSimplifyToLambda(body, methodInfo.ReturnType, parameters.Select(p => p.port).ToArray());
			} else {
				flag = input.isAssigned && CG.CanSimplifyToLambda(input, methodInfo.ReturnType, parameters.Select(p => p.port).ToArray());
			}
			if(flag) {
				var bodyNode = methodInfo.ReturnType == typeof(void) ? body.GetTargetNode() : input.GetTargetNode();
				if(bodyNode.node is MultipurposeNode mn) {
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
			string contents = null;
			List<string> parameterNames = new List<string>();
			for(int i = 0; i < parameters.Count; i++) {
				string varName = null;
				var data = parameters[i];
				if(data != null) {
					if(input != null) {
						if(!CG.CanDeclareLocal(data.port, new[] { input })) {//Auto generate instance variable for parameter.
							varName = CG.GenerateName("tempVar" + i, this);
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
					}
					else {
						if(!CG.CanDeclareLocal(data.port, body)) {//Auto generate instance variable for parameter.
							varName = CG.GenerateName("tempVar" + i, this);
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
					}
					parameterNames.Add(varName);
				}
			}
			if(methodInfo.ReturnType == typeof(void)) {
				CG.BeginBlock(allowYield: false); //Ensure that there is no yield statement
				contents += CG.Flow(body, false).AddLineInFirst();
				CG.EndBlock();
				return CG.Lambda(methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(), parameterNames, contents);
			}
			else {
				contents += CG.Return(input.CGValue()).AddLineInFirst();
				CG.EndBlock();
				return CG.Lambda(methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(), parameterNames, contents);
			}
		}
		#endregion

		public override object GetValue(Flow flow) {
			System.Delegate m_Delegate = null;
			if(methodInfo.ReturnType == typeof(void)) {
				m_Delegate = CustomDelegate.CreateActionDelegate((obj) => {
					if(nodeObject == null)
						return;
					if(obj != null) {
						for(int i = 0; i < obj.Length; i++) {
							flow.SetPortData(parameters[i].port, obj[i]);
						}
					}
					flow.Trigger(body);
				}, types);
			}
			else {
				m_Delegate = CustomDelegate.CreateFuncDelegate((obj) => {
					if(nodeObject == null)
						return null;
					if(obj != null) {
						for(int i = 0; i < obj.Length; i++) {
							flow.SetPortData(parameters[i].port, obj[i]);
						}
					}
					return input.GetValue(flow, methodInfo.ReturnType);
				}, types.ToArray());
			}
			return m_Delegate;
		}

		public override bool CanGetValue() {
			return true;
		}

		public override System.Type ReturnType() {
			if(autoDelegateType) {
				var firstConnection = output.ValidConnections.FirstOrDefault();
				if(firstConnection != null) {
					var other = (firstConnection.Input as ValueInput);
					Type actualType = null;
					if(other.filter != null) {
						actualType = other.filter.GetActualType();
					}
					if(actualType == null) {
						actualType = other.type;
					}
					if(actualType != null && actualType.IsSubclassOf(typeof(Delegate))) {
						if(delegateType != actualType) {
							delegateType = actualType;
							Register();
						}
						return actualType;
					}
				}
				else {
					return typeof(Delegate);
				}
			}
			if(!delegateType.isFilled)
				return typeof(Delegate);
			var type = delegateType.type;
			methodInfo = type.GetMethod("Invoke");
			if(methodInfo != null) {
				if(methodInfo.ReturnType == typeof(void)) {
					return CustomDelegate.GetActionDelegateType(methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
				} else {
					var types = methodInfo.GetParameters().Select(i => i.ParameterType).ToList();
					types.Add(methodInfo.ReturnType);
					return CustomDelegate.GetFuncDelegateType(types.ToArray());
				}
			}
			return typeof(Delegate);
		}

		public override string GetTitle() {
			return "Lambda";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			analizer.CheckPort(input);
			analizer.CheckValue(delegateType, nameof(delegateType), this);
		}
	}
}