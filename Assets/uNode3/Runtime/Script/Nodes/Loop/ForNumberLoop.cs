using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Statement", "For")]
	[Description("The for number statement can run a node repeatedly until a condition evaluates to false.")]
	public class ForNumberLoop : FlowNode {
		public FlowOutput body { get; private set; }

		[Filter(typeof(int), typeof(float), typeof(decimal), typeof(long), typeof(byte), typeof(sbyte),
			typeof(short), typeof(double), typeof(uint), typeof(ulong), typeof(ushort), OnlyGetType = true, UnityReference = false)]
		public SerializedType indexType = typeof(int);
		public ComparisonType compareType = ComparisonType.LessThan;
		public SetType iteratorSetType = SetType.Add;

		public ValueInput start { get; private set; }
		public ValueInput count { get; private set; }
		public ValueInput step { get; private set; }
		public ValueOutput index { get; private set; }

		protected override void OnRegister() {
			body = FlowOutput(nameof(body));
			start = ValueInput(nameof(start), indexType.type, 0);
			count = ValueInput(nameof(count), indexType.type, 10);
			step = ValueInput(nameof(step), indexType.type, 1);
			index = ValueOutput(nameof(index), indexType.type, PortAccessibility.ReadWrite);
			index.isVariable = true;
			base.OnRegister();
			exit.SetName("Exit");
		}

		protected override bool IsCoroutine() {
			return HasCoroutineInFlows(body, exit);
		}

		protected override void OnExecuted(Flow flow) {
			ref object indexValue = ref flow.GetPortDataByRef(index);
			for(indexValue = start.GetValue(flow);
				uNodeHelper.OperatorComparison(indexValue, count.GetValue(flow), compareType);
				uNodeHelper.SetObject(ref indexValue, step.GetValue(flow), iteratorSetType)) {
				if(!body.isConnected)
					continue;
				flow.Trigger(body, out var js);
				if(js != null) {
					if(js.jumpType == JumpStatementType.Continue) {
						continue;
					}
					else if(js.jumpType == JumpStatementType.Break) {
						break;
					}
					else if(js.jumpType == JumpStatementType.Return) {
						flow.jumpStatement = js;
						break;
					}
				}
			}
		}

		protected override IEnumerator OnExecutedCoroutine(Flow flow) {
			for(var indexValue = start.GetValue(flow); 
				uNodeHelper.OperatorComparison(indexValue, count.GetValue(flow), compareType);
				indexValue = uNodeHelper.SetObject(flow.GetPortData(index), step.GetValue(flow), iteratorSetType)) 
			{
				flow.SetPortData(index, indexValue);
				if(!body.isConnected)
					continue;
				flow.TriggerCoroutine(body, out var wait, out var jump);
				if(wait != null) {
					yield return wait;
				}
				var js = jump();
				if(js != null) {
					if(js.jumpType == JumpStatementType.Continue) {
						continue;
					}
					else if(js.jumpType == JumpStatementType.Break) {
						break;
					}
					else if(js.jumpType == JumpStatementType.Return) {
						flow.jumpStatement = js;
						break;
					}
				}
			}
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			var vName = CG.RegisterVariable(index);
			CG.RegisterPort(index, () => vName);
		}

		protected override string GenerateFlowCode() {
			if(!start.isAssigned || !count.isAssigned || !step.isAssigned) return null;
			string vName = CG.GetVariableName(index);
			string data = CG.GetCompareCode(vName, count, compareType);
			string iterator = CG.SetValue(vName, CG.Value(step), iteratorSetType);
			if(!string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(iterator)) {
				var content = CG.FlowFinish(enter, exit);
				data = CG.For(CG.DeclareVariable(index, indexType, CG.Value(start), body).RemoveSemicolon(), data, iterator,
					CG.Flow(body)) +
					content.AddFirst("\n");
				return data;
			}
			return null;
		}

		public override string GetRichName() {
			if(!start.isAssigned || !count.isAssigned || !indexType.isAssigned || !step.isAssigned) {
				return base.GetRichName();
			}
			return $"{uNodeUtility.WrapTextWithKeywordColor("for")}({indexType.GetRichName()} i={start.GetRichName()}; {CG.Compare("i", count.GetRichName(), compareType)}; {CG.SetValue("i", step.GetRichName(), iteratorSetType)})";
		}
	}
}