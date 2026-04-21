using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Statement", "Foreach Indexed", inputs = new[] { typeof(IList) })]
	[Description("The foreach indexed repeats a body of embedded statements for each element in an array or a generic type. It's loop using For statements")]
	public class ForeachIndexed : FlowNode {
		public bool deconstructValue = true;
		[Tooltip("If true, this ensure to Get value of collection one time in each call.")]
		public bool cacheInput = true;
		public string itemName;
		public string indexName;

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
		public ValueOutput index { get; set; }

		private MethodInfo deconstructMethod;
		private PropertyInfo countMethod;
		private MethodInfo indexerMethod;

		private static readonly FilterAttribute filter;

		static ForeachIndexed() {
			filter = new FilterAttribute(typeof(IList)) {
				ValidateType = type => {
					if(type.IsCastableTo(typeof(IList))) {
						return true;
					}
					else {
						var property = type.GetPropertyCached(nameof(IList.Count)) ?? type.GetPropertyCached(nameof(Array.Length));
						if(property != null && property.CanRead && property.PropertyType == typeof(int) && type.GetMemberCached("get_Item") != null) {
							return true;
						}
					}
					return false;
				},
			};
		}

		protected override void OnRegister() {
			body = FlowOutput(nameof(body));
			collection = ValueInput<IList>(nameof(collection));
			collection.filter = filter;
			if(collection.isAssigned) {
				var type = collection.ValueType;
				countMethod = type.GetPropertyCached(nameof(IList.Count)) ?? type.GetPropertyCached(nameof(Array.Length));
				indexerMethod = type.GetMemberCached("get_Item") as MethodInfo;
			}
			if(deconstructValue && CanDeconstruct()) {
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
				output = ValueOutput(nameof(output), ReturnType).SetName(string.IsNullOrEmpty(itemName) ? "Item" : itemName);
				output.isVariable = true;
			}
			index = ValueOutput(nameof(index), typeof(int), PortAccessibility.ReadOnly);
			if(string.IsNullOrEmpty(indexName) == false) {
				index.SetName(indexName);
			}
			base.OnRegister();
			exit.SetName("Exit");
		}

		public bool CanDeconstruct() {
			return collection != null && collection.hasValidConnections && CanDeconstruct(collection.ValueType.ElementType());
		}

		private static bool CanDeconstruct(Type type) {
			if(type == null) return false;
			if(type.GetMemberCached(nameof(KeyValuePair<int, int>.Deconstruct)) is MethodInfo) {
				return true;
			} 
			else if(type.IsGenericType && type.HasImplementInterface(typeof(System.Runtime.CompilerServices.ITuple))) {
				return type.IsGenericType;
			}
			return false;
		}

		protected override Type ReturnType() {
			if(collection != null) {
				var type = collection.ValueType;
				return type.ElementType() ?? typeof(object);
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
			var val = collection.GetValue(flow);
			if(val is IList lObj) {
				for(int idx = 0; idx < lObj.Count; idx++) {
					var obj = lObj[idx];
					if(index.isConnected) {
						flow.SetPortData(index, idx);
					}
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
				if(val != null) {
					if(countMethod != null && indexerMethod != null) {
						for(int idx = 0; idx < (int)countMethod.GetValueOptimized(val); idx++) {
							var obj = indexerMethod.InvokeOptimized(val, idx);
							if(index.isConnected) {
								flow.SetPortData(index, idx);
							}
							if(!body.isConnected)
								continue;
							UpdateLoopValue(flow, obj);
							flow.Trigger(body, out var js);
							if(js != null) {
								if(js.jumpType == JumpStatementType.Continue) {
									continue;
								}
								else {
									if(js.jumpType == JumpStatementType.Return) {
										flow.jumpStatement = js;
										return;
									}
									break;
								}
							}
						}
						return;
					}
				}
				Debug.LogError("The collection is null or not IList");
			}
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			var val = collection.GetValue(flow);
			if(val is IList lObj) {
				for(int idx = 0; idx < lObj.Count; idx++) {
					var obj = lObj[idx];
					if(index.isConnected) {
						flow.SetPortData(index, idx);
					}
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
				if(val != null) {
					if(countMethod != null && indexerMethod != null) {
						for(int idx = 0; idx < (int)countMethod.GetValueOptimized(val); idx++) {
							var obj = indexerMethod.InvokeOptimized(val, idx);
							if(index.isConnected) {
								flow.SetPortData(index, idx);
							}
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
								}
								else {
									if(js.jumpType == JumpStatementType.Return) {
										flow.jumpStatement = js;
										yield break;
									}
									break;
								}
							}
						}
						yield break;
					}
				}
				Debug.LogError("The collection is null or not IList");
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
				var vName = CG.RegisterVariable(output, string.IsNullOrEmpty(itemName) ? "loopValue" : itemName);
				CG.RegisterPort(output, () => vName);
			}
			var iName = CG.RegisterVariable(index, string.IsNullOrEmpty(indexName) ? "index" : indexName);
			CG.RegisterPort(index, () => iName);
		}

		protected override string GenerateFlowCode() {
			string targetCollection = CG.Value(collection);
			if(!string.IsNullOrEmpty(targetCollection)) {
				var originalCollection = targetCollection;
				if(cacheInput) {
					targetCollection = CG.GenerateNewName("collection");
				}
				string contents = CG.Flow(body).AddLineInFirst();
				string additionalContents = null;
				if(output == null) {
					string[] paramDatas = new string[deconstructDatas.Count];
					bool hasParam = false;
					for(int i = 0; i < deconstructDatas.Count; i++) {
						var data = deconstructDatas[i];
						if(data.port != null && data.port.hasValidConnections) {
							hasParam = true;
							string vName;

							if(CG.generatePureScript && ReflectionUtils.IsNativeType(data.port.type) == false) {
								//Auto convert to actual type if the target type is not native type.
								if(!CG.CanDeclareLocal(data.port, body)) {
									vName = CG.GenerateName("tempVar", (this, data.port));
									var vdata = CG.GetVariableData(data.port);
									vdata.SetToInstanceVariable();
									additionalContents = CG.Flow(additionalContents, CG.Set(vdata.name, CG.As(vName, data.port.type)));
									//This for auto declare variable
									CG.DeclareVariable(data.port, "", body);
								}
								else {
									vName = CG.GenerateName("tempVar", (this, data.port));
									additionalContents = CG.Flow(additionalContents, "var " + CG.Set(CG.GetVariableName(data.port), CG.As(vName, data.port.type)));
								}
							}
							else {
								if(!CG.CanDeclareLocal(data.port, body)) {
									vName = CG.GenerateName("tempVar", (this, data.port));
									var vdata = CG.GetVariableData(data.port);
									vdata.SetToInstanceVariable();
									additionalContents = CG.Flow(additionalContents, CG.Set(vdata.name, vName));
								}
								else {
									vName = CG.GetVariableData(data.port).name;
								}
							}
							paramDatas[i] = vName;
						}
						else {
							//Discard if the port is not used.
							paramDatas[i] = "_";
						}
					}
					string iName = CG.GetVariableName(index);
					string compare = CG.GetCompareCode(iName, targetCollection.CGAccess(countMethod.Name), ComparisonType.LessThan);
					string iterator = CG.IncrementValue(iName);
					if(hasParam) {
						additionalContents = CG.Flow(CG.Set("var (" + string.Join(", ", paramDatas) + ")", targetCollection.CGAccessElement(iName)), additionalContents);
					}
					return CG.Flow(
						cacheInput ? CG.DeclareVariable(targetCollection, originalCollection) : string.Empty,
						CG.For(CG.DeclareVariable(index, 0.CGValue(), body).RemoveSemicolon(), compare, iterator, CG.Flow(additionalContents, contents)),
						CG.FlowFinish(enter, exit)
					);
				}
				else {
					var targetType = collection.ValueType;
					var elementType = targetType.ElementType();
					string iName = CG.GetVariableName(index);
					string compare = CG.GetCompareCode(iName, targetCollection.CGAccess(countMethod.Name), ComparisonType.LessThan);
					string iterator = CG.IncrementValue(iName);
					if(output.hasValidConnections) {
						if(CG.generatePureScript && ReflectionUtils.IsNativeType(targetType) == false) {
							additionalContents = CG.DeclareVariable(output, targetCollection.CGAccessElement(iName).CGConvert(elementType), body);
						}
						else {
							additionalContents = CG.DeclareVariable(output, targetCollection.CGAccessElement(iName), body);
						}
					}
					return CG.Flow(
						cacheInput ? CG.DeclareVariable(targetCollection, originalCollection) : string.Empty,
						CG.For(CG.DeclareVariable(index, 0.CGValue(), body).RemoveSemicolon(), compare, iterator, CG.Flow(additionalContents, contents)),
						CG.FlowFinish(enter, exit)
					);
				}
			}
			return null;
		}

		public override string GetRichName() {
			if(deconstructValue && deconstructMethod != null) {
				return uNodeUtility.WrapTextWithKeywordColor("foreach: var ") + string.Join(", ", deconstructDatas.Select(d => d.port.GetRichName())).Wrap() + uNodeUtility.WrapTextWithKeywordColor(" in ") + collection.GetRichName();
			}
			return uNodeUtility.WrapTextWithKeywordColor("foreach:") + collection.GetRichName();
		}
	}
}