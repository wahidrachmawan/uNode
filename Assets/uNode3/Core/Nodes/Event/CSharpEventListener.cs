using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace MaxyGames.UNode.Nodes {
	[EventMenu("", "C# Event", order = 1)]
	public class CSharpEventListener : BaseComponentEvent {
		[Filter(typeof(Delegate), typeof(UnityEventBase), ValidTargetType = MemberData.TargetType.Field | MemberData.TargetType.Event | MemberData.TargetType.Property, UnityReference =false)]
        public MemberData target = MemberData.None;

		public class Data {
			public ValueOutput port { get; set; }
		}
		[HideInInspector]
		public List<Data> datas = new List<Data>();
		public ValueInput instance { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			if(target.isAssigned) {
				if(!target.isStatic) {
					instance = ValueInput(nameof(instance), target.startType);
				}
				var targetType = target.type;
				if(targetType != null) {
					datas.Clear();
					if(targetType.IsCastableTo(typeof(Delegate))) {
						var method = targetType.GetMethod("Invoke");
						if(method.ReturnType == typeof(void)) {
							var types = method.GetParameters();
							for(int i = 0; i < types.Length; i++) {
								var data = new Data();
								var port = ValueOutput("value" + i, types[i].ParameterType).SetName(types[i].Name);
								data.port = port;
								datas.Add(data);
							}
						}
					}
					else if(targetType.IsCastableTo(typeof(UnityEventBase))) {
						var method = targetType.GetMethod("AddListener");
						var types = method.GetParameters()[0].ParameterType.GetGenericArguments();
						for(int i = 0; i < types.Length; i++) {
							var data = new Data();
							var port = ValueOutput("value" + i, types[i]);
							if(types.Length == 1) {
								port.SetName("value");
							}
							else {
								port.SetName("value" + i);
							}
							data.port = port;
							datas.Add(data);
						}
					}
				}
			}
		}

		private string evtCode;
		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			for(int i = 0; i < datas.Count; i++) {
				int index = i;
				var vName = CG.RegisterVariable(datas[index].port);
				CG.RegisterPort(datas[i].port, () => vName);
			}
		}

		public override void GenerateEventCode() {
			if(!target.isAssigned)
				return;
			var targetType = target.type;
			if(targetType == null)
				return;
			var mData = CG.generatorData.AddMethod(
				CG.GenerateNewName("M_GeneratedEvent"), 
				typeof(void), 
				datas.Select(item => new CG.MPData(CG.GetVariableName(item.port), item.port.type)).ToArray());
			mData.AddCodeForEvent(GenerateRunFlows());

			var enableEvent = CG.GetOrRegisterFunction(nameof(UEventID.OnEnable), typeof(void));
			var disableEvent = CG.GetOrRegisterFunction(nameof(UEventID.OnDisable), typeof(void));

			if(targetType.IsCastableTo(typeof(Delegate))) {
				enableEvent.AddCodeForEvent(
					CG.WrapWithInformation(CG.Value(target, instance: instance).CGSet(mData.name, SetType.Add), this)
				);
				disableEvent.AddCodeForEvent(
					CG.WrapWithInformation(CG.Value(target, instance: instance).CGSet(mData.name, SetType.Subtract), this)
				);
			}
			else if(targetType.IsCastableTo(typeof(UnityEventBase))) {
				enableEvent.AddCodeForEvent(
					CG.WrapWithInformation(CG.Value(target, instance: instance).CGFlowInvoke(nameof(UnityEvent.AddListener), mData.name), this)
				);
				disableEvent.AddCodeForEvent(
					CG.WrapWithInformation(CG.Value(target, instance: instance).CGFlowInvoke(nameof(UnityEvent.RemoveListener), mData.name), this)
				);
			}
			else {
				throw null;
			}
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var flow = instance.stateRunner;
			if(instance.target is Component comp) {
				var targetType = target.type;
				if(targetType != null) {
					if(targetType.IsCastableTo(typeof(Delegate))) {
						var method = targetType.GetMethod("Invoke");
						if(method.ReturnType == typeof(void)) {
							var evt = target.CreateRuntimeEvent();
							var types = method.GetParameters();
							var @delegate = CustomDelegate.CreateActionDelegate((values) => {
								if(values != null) {
									for(int i = 0; i < values.Length; i++) {
										flow.SetPortData(datas[i].port, values[i]);
									}
								}
								Trigger(flow);
							}, method.GetParameters().Select(i => i.ParameterType).ToArray());
							UEvent.Register(UEventID.OnEnable, comp, () => {
								object obj = this.instance.GetValue(instance.defaultFlow);
								target.startTarget = obj;
								evt.AddEventHandler(instance, @delegate);
							});
							UEvent.Register(UEventID.OnDisable, comp, () => {
								object obj = this.instance.GetValue(instance.defaultFlow);
								target.startTarget = obj;
								evt.RemoveEventHandler(instance, @delegate);
							});
						}
					}
					else if(targetType.IsCastableTo(typeof(UnityEventBase))) {
						var addListener = targetType.GetMethod(nameof(UnityEvent.AddListener));
						var removeListener = targetType.GetMethod(nameof(UnityEvent.RemoveListener));
						var delegateType = addListener.GetParameters()[0].ParameterType;
						var types = delegateType.GetGenericArguments();
						var @delegate = CustomDelegate.CreateActionDelegate((values) => {
							if(values != null) {
								for(int i = 0; i < values.Length; i++) {
									flow.SetPortData(datas[i].port, values[i]);
								}
							}
							Trigger(flow);
						}, types);
						@delegate = Delegate.CreateDelegate(delegateType, @delegate.Target, @delegate.Method);
						UEvent.Register(UEventID.OnEnable, comp, () => {
							object obj = this.instance.GetValue(instance.defaultFlow);
							target.startTarget = obj;
							addListener.InvokeOptimized(target.Get(instance.defaultFlow), @delegate);
						});
						UEvent.Register(UEventID.OnDisable, comp, () => {
							object obj = this.instance.GetValue(instance.defaultFlow);
							target.startTarget = obj;
							removeListener.InvokeOptimized(target.Get(instance.defaultFlow), @delegate);
						});
					}
					
				}
				else {
					throw null;
				}
			}
			else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override string GetTitle() {
			if(target != null && target.isAssigned) {
				return target.GetDisplayName();
			}
			return "C# Event";
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(target == null || target.isAssigned == false) {
				analizer.RegisterError(this, "Unassigned target event");
			}
		}
	}
}