using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Jump", "Return", hasFlowInput = true)]
	public class NodeReturn : Node {
		public FlowInput enter { get; set; }
		public ValueInput value { get; set; }
		public bool returnAnyType = false;

		protected override void OnRegister() {
			if(GetNodeIcon() != typeof(void)) {
				value = ValueInput(nameof(value), GetNodeIcon);
			}
			enter = PrimaryFlowInput(nameof(enter), (flow) => {
				flow.jumpStatement = new JumpStatement(nodeObject, JumpStatementType.Return, GetNodeIcon() != typeof(void) ? value.GetValue(flow) : null);
			});
		}

		public override void OnGeneratorInitialize() {
			CG.RegisterPort(enter, () => {
				if(value != null && value.isAssigned) {
					return CG.Return(value.CGValue());
				}
				return "return;";
			});
		}

		public override string GetTitle() {
			return "Return";
		}

		public override string GetRichName() {
			if(value != null && value.isAssigned) {
				return uNodeUtility.WrapTextWithKeywordColor("return: ") + " " + value.GetRichName();
			}
			return uNodeUtility.WrapTextWithKeywordColor("return");
		}

		public override Type GetNodeIcon() {
			if(returnAnyType) {
				return typeof(object);
			} else {
				var result = GetActualReturnType(enter);
				if(result != null)
					return result;
				var func = nodeObject.GetObjectInParent<BaseFunction>();
				if(func != null) {
					return func.ReturnType() ?? typeof(void);
				}
			}
			return typeof(void);
		}

		private Type GetActualReturnType(FlowInput input, int deep = 0) {
			if(deep >= 200) {
				//For prevent overflow which make unity freeze.
				return null;
			}
			if(input != null && input.isConnected) {
				var con = input.ValidConnections.FirstOrDefault();
				if(con != null) {
					var node = con.Output.node?.node;
					if(node != null) {
						if(node == this) {
							var func = nodeObject.GetObjectInParent<BaseFunction>();
							if(func != null) {
								return func.ReturnType() ?? typeof(void);
							}
						}
						else {
							if(node is NodeLambda lambda) {
								var result = lambda.ReturnType();
								if(result == null)
									result = typeof(void);
								else {
									if(result.IsCastableTo(typeof(Delegate))) {
										var methodInfo = result.GetMethod("Invoke");
										return methodInfo.ReturnType;
									}
								}
								return result;
							}
							else if(node is NodeAnonymousFunction anon) {
								var result = anon.ReturnType();
								if(result == null)
									result = typeof(void);
								else {
									if(result.IsCastableTo(typeof(Delegate))) {
										var methodInfo = result.GetMethod("Invoke");
										return methodInfo.ReturnType;
									}
								}
								return result;
							}
							else {
								//Recursive upward until we found it.
								return GetActualReturnType(node.nodeObject.primaryFlowInput, deep + 1);
							}
						}
					}
				}
			}
			return null;
		}
	}
}