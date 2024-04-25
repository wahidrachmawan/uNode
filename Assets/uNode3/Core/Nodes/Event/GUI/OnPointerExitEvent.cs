using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace MaxyGames.UNode.Nodes {
	[EventMenu("GUI", "On Pointer Exit")]
	[StateEvent]
	public class OnPointerExitEvent : BaseComponentEvent {
		public ValueInput target { get; set; }
		public ValueOutput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object));
			target.filter = new FilterAttribute(typeof(Component), typeof(GameObject));
			value = ValueOutput<PointerEventData>(nameof(value));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var val = target.GetValue(instance.defaultFlow);
			if(val != null) {
				if(val is GameObject) {
					UEvent.Register<PointerEventData>(UEventID.OnPointerExit, val as GameObject, (value) => OnTriggered(instance, value));
				}
				else if(val is Component) {
					UEvent.Register<PointerEventData>(UEventID.OnPointerExit, val as Component, (value) => OnTriggered(instance, value));
				}
			}
			else {
				if(instance.target is Component comp) {
					UEvent.Register<PointerEventData>(UEventID.OnPointerExit, comp, (value) => OnTriggered(instance, value));
				}
				else {
					throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
				}
			}
		}

		void OnTriggered(GraphInstance instance, PointerEventData value) {
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
							CG.Value(UEventID.OnPointerExit),
							CG.Value(target),
							CG.Lambda(new[] { typeof(PointerEventData) }, new[] { parameter }, contents)
						)
					);
				}
				else {
					mData.AddCodeForEvent(
						CG.FlowInvoke(
							typeof(UEvent),
							nameof(UEvent.Register),
							CG.Value(UEventID.OnPointerExit),
							CG.This,
							CG.Lambda(new[] { typeof(PointerEventData) }, new[] { parameter }, contents)
						)
					);
				}
			}
		}

		public override Type GetNodeIcon() {
			return typeof(PointerEventData);
		}
	}
}