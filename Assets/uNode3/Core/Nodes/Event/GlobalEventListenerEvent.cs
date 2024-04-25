using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[EventMenu("", "Global Event", order = 1)]
	public class GlobalEventListenerEvent : BaseComponentEvent {
        public UGlobalEvent target;

		public class Data {
			public ValueOutput port { get; set; }
		}
		[HideInInspector]
		public List<Data> datas = new List<Data>();

		protected override void OnRegister() {
			base.OnRegister();
			if(target != null && target is IGlobalEvent globalEvent) {
				datas.Clear();
				int count = globalEvent.ParameterCount;
				for(int i = 0; i < count; i++) {
					var data = new Data();
					var port = ValueOutput(
						"port" + i,
						globalEvent.GetParameterType(i),
						PortAccessibility.ReadOnly).SetName(globalEvent.GetParameterName(i));
					data.port = port;
					datas.Add(data);
				}
			}
		}

		private string evtCode;
		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			CG.RegisterUserObject(target.GenerateMethodCode(out var names, out evtCode), this);
			for(int i = 0; i < names.Length; i++) {
				int index = i;
				CG.RegisterPort(datas[i].port, () => names[index]);
			}
		}

		public override void GenerateEventCode() {
#if UNITY_EDITOR
			var mData = CG.GetUserObject<CG.MData>(this);
			mData.AddCodeForEvent(GenerateRunFlows());
			var assetID = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(target));
			var enableEvent = CG.GetOrRegisterFunction(nameof(UEventID.OnEnable), typeof(void));
			var disableEvent = CG.GetOrRegisterFunction(nameof(UEventID.OnDisable), typeof(void));
			enableEvent.AddCodeForEvent(
				CG.Invoke(
					typeof(uNodeUtility), 
					nameof(uNodeUtility.GetGlobalEvent),
					CG.Value(assetID)).
				CGFlowInvoke(
					nameof(IGlobalEvent.AddListener), 
					evtCode)
			);
			disableEvent.AddCodeForEvent(
				CG.Invoke(
					typeof(uNodeUtility),
					nameof(uNodeUtility.GetGlobalEvent),
					CG.Value(assetID)).
				CGFlowInvoke(
					nameof(IGlobalEvent.RemoveListener),
					evtCode)
			);
#endif
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				Delegate @delegate = null;
				if(target is not UGlobalEventCustom) {
					@delegate = CustomDelegate.CreateActionDelegate((Action<object[]>)(values => {
						if(values != null) {
							for(int i = 0; i < values.Length; i++) {
								instance.defaultFlow.SetPortData((ValueOutput)datas[(int)i].port, (object)values[(int)i]);
							}
						}
						base.Trigger(instance);
					}), datas.Select(data => data.port.type).ToArray());
				}
				else {
					@delegate = new Action<object[]>(values => {
						if(values != null) {
							for(int i = 0; i < values.Length; i++) {
								instance.defaultFlow.SetPortData(datas[i].port, values[i]);
							}
						}
						Trigger(instance);
					});
				}
				(target as IGlobalEvent).AddListener(@delegate);
				UEvent.Register(UEventID.OnEnable, comp, () => {
					(target as IGlobalEvent).RemoveListener(@delegate);
					(target as IGlobalEvent).AddListener(@delegate);
				});
				UEvent.Register(UEventID.OnDisable, comp, () => {
					(target as IGlobalEvent).RemoveListener(@delegate);
				});
			}
			else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override string GetTitle() {
			if(target != null) {
				return target.EventName;
			}
			return "Global Event";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(target == null) {
				analizer.RegisterError(this, "Unassigned target event");
			}
		}
	}
}