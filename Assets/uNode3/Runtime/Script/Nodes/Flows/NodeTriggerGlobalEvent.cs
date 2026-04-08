using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Trigger Global Event", hasFlowInput =true)]
	[Description("Used for trigger global event")]
	public class NodeTriggerGlobalEvent : FlowNode {
		[AllowAssetReference]
		public UGlobalEvent target;

		public class Data {
			[NonSerialized]
			public ValueInput port;
		}
		[HideInInspector]
		public List<Data> inputs = new List<Data>();

		private const bool cacheEvent = true;
		private const string CachedEventFieldKey = nameof(NodeTriggerGlobalEvent) + ".CachedEventField";

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
			// If the graph is MonoBehaviour, we can cache the event to avoid call GetGlobalEvent every time.
			if(cacheEvent && nodeObject.graphContainer.GetGraphType().IsCastableTo(typeof(MonoBehaviour))) {
				var eventField = CG.GetUserObject<string>(target, CachedEventFieldKey);
				if(eventField == null) {
					var assetID = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(target));
					eventField = CG.RegisterPrivateVariable("m_Event_" + target.EventName, target.GetType(), reference: target);
					CG.InsertCodeToFunction("Awake", 
						CG.Set(
							eventField, 
							CG.Invoke(
								typeof(uNodeUtility), 
								nameof(uNodeUtility.GetGlobalEvent),
								CG.Value(assetID))
							.CGConvert(target.GetType())), 
						int.MinValue);
					CG.RegisterUserObject(eventField, target, CachedEventFieldKey);
				}
				if(target is IGlobalEvent globalEvent) {
					var count = globalEvent.ParameterCount;
					Type[] parameterTypes = new Type[count];
					for(int i = 0; i < count; i++) {
						parameterTypes[i] = globalEvent.GetParameterType(i);
					}
					var method = target.GetType().GetMethod(nameof(globalEvent.Trigger), MemberData.flags, null, parameterTypes, null);
					if(method != null) {
						return CG.Flow(
							eventField.CGFlowInvoke(
								nameof(IGlobalEvent.Trigger),
								inputs.Select(p => CG.GeneratePort(p.port)).ToArray()
							),
							CG.FlowFinish(enter, exit));
					}
					else {
						method = target.GetType().GetMethod(nameof(globalEvent.Trigger), MemberData.flags);
						if(method != null) {
							var parameters = method.GetParameters();
							if(parameters.Length == 1 && parameters[0].ParameterType == typeof(object[])) {
								return CG.Flow(
									eventField.CGFlowInvoke(
										nameof(IGlobalEvent.Trigger),
										CG.MakeArray(typeof(object), inputs.Select(p => CG.GeneratePort(p.port)).ToArray())
									),
									CG.FlowFinish(enter, exit));
							}
							//else {
							//	throw new Exception("Unsupported event type: " + target.GetType());
							//}
						}
					}
				}
				return CG.Flow(
					eventField.CGFlowInvoke(
						nameof(UGlobalEvent.TriggerWeak),
						CG.MakeArray(typeof(object), inputs.Select(p => CG.GeneratePort(p.port)).ToArray())
					),
					CG.FlowFinish(enter, exit));
			}
			else {
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
			}
			
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
