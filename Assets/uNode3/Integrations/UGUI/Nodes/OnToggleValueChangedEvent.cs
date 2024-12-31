using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("GUI", "On Toggle Value Changed")]
	[StateEvent]
	public class OnToggleValueChangedEvent : BaseComponentEvent {
		public ValueInput target { get; set; }
		public ValueOutput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object));
			target.filter = new FilterAttribute(typeof(Toggle), typeof(GameObject));
			value = ValueOutput<bool>(nameof(value));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var val = target.GetValue(instance.defaultFlow);
			if(val != null) {
				if(val is GameObject) {
					UEvent.Register<bool>(UEventID.OnToggleValueChanged, val as GameObject, (val) => OnTriggered(instance, val));
				} else if(val is Component) {
					UEvent.Register<bool>(UEventID.OnToggleValueChanged, val as Component, (val) => OnTriggered(instance, val));
				}
			} else {
				if(instance.target is Component comp) {
					UEvent.Register<bool>(UEventID.OnToggleValueChanged, comp, (val) => OnTriggered(instance, val));
				} else {
					throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
				}
			}
		}

		void OnTriggered(GraphInstance instance, bool value) {
			instance.defaultFlow.SetPortData(this.value, value);
			Trigger(instance);
		}

		public override void OnGeneratorInitialize() {
			var variable = CG.RegisterVariable(value);
			CG.RegisterPort(value, () => variable);
		}

		public override void GenerateEventCode() {
			var mData = CG.GetOrRegisterFunction("Start", typeof(void));
			var contents = GenerateRunFlows();
			if(!string.IsNullOrEmpty(contents)) {
				string parameter;
				if(CG.CanDeclareLocal(value, outputs)) {
					parameter = CG.GetVariableData(value).name;
				}
				else {
					parameter = CG.GenerateName("parameter", this);
					var vdata = CG.GetVariableData(value);
					vdata.SetToInstanceVariable();
					contents = CG.Flow(
						CG.Set(vdata.name, parameter),
						contents
					);
				}
				if(target.isAssigned) {
					mData.AddCodeForEvent(
						CG.FlowInvoke(
							typeof(UEvent),
							nameof(UEvent.Register),
							CG.Value(UEventID.OnToggleValueChanged),
							CG.Value(target),
							CG.Lambda(new[] { typeof(bool) }, new[] { parameter }, contents)
						)
					);
				}
				else {
					mData.AddCodeForEvent(
						CG.GenericInvoke<Toggle>(CG.This, nameof(GameObject.GetComponent))
							.CGAccess(nameof(Toggle.onValueChanged))
							.CGFlowInvoke(
								nameof(Toggle.onValueChanged.AddListener),
								CG.Lambda(new[] { typeof(bool) }, new[] { parameter }, contents)
							)
					);
				}
			}
		}

		public override Type GetNodeIcon() {
			return typeof(Toggle);
		}
	}
}