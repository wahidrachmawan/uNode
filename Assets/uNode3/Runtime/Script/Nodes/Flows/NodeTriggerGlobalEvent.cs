using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Trigger Global Event")]
	public class NodeTriggerGlobalEvent : FlowNode {
		public UGlobalEvent target;

		public class Data {
			[NonSerialized]
			public ValueInput port;
		}
		[HideInInspector]
		public List<Data> inputs = new List<Data>();

		public override string GetTitle() {
			if(target != null) {
				return "Trigger: " + target.EventName;
			}
			return base.GetTitle();
		}

		protected override void OnRegister() {
			base.OnRegister();
			if(target != null && target is IGlobalEvent globalEvent) {
				inputs.Clear();
				int count = globalEvent.ParameterCount;
				for(int i = 0; i < count; i++) {
					var data = new Data();
					var port = ValueInput("port" + i, globalEvent.GetParameterType(i)).SetName(globalEvent.GetParameterName(i));
					data.port = port;
					inputs.Add(data);
				}
			}
		}

		protected override void OnExecuted(Flow flow) {
			var parameters = new object[inputs.Count];
			for(int i = 0; i < parameters.Length; i++) {
				parameters[i] = inputs[i].port.GetValue(flow);
			}
			target.TriggerWeak(parameters);
		}

		protected override string GenerateFlowCode() {
#if UNITY_EDITOR
			var assetID = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(target));
			return CG.Flow(
				CG.Invoke(
					typeof(uNodeUtility),
					nameof(uNodeUtility.GetGlobalEvent),
					CG.Value(assetID)).
					CGFlowInvoke(
						nameof(IGlobalEvent.Trigger),
						CG.MakeArray(typeof(object), inputs.Select(p => CG.GeneratePort(p.port)).ToArray())
					),
				CG.FlowFinish(enter, exit));
			
#else
			throw null;
#endif
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(target == null) {
				analizer.RegisterError(this, "Unassigned target event");
			}
		}
	}
}