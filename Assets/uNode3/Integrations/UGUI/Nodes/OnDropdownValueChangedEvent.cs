using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("GUI", "On Dropdown Value Changed")]
	[StateEvent]
	public class OnDropdownValueChangedEvent : BaseComponentEvent {
		public ValueInput target { get; set; }
		public ValueOutput index { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object));
			target.filter = new FilterAttribute(typeof(Dropdown), typeof(GameObject));
			index = ValueOutput<int>(nameof(index));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var val = target.GetValue(instance.defaultFlow);
			if(val != null) {
				if(val is GameObject) {
					UEvent.Register<int>(UEventID.OnDropdownValueChanged, val as GameObject, (val) => OnTriggered(instance, val));
				} else if(val is Component) {
					UEvent.Register<int>(UEventID.OnDropdownValueChanged, val as Component, (val) => OnTriggered(instance, val));
				}
			} else {
				if(instance.target is Component comp) {
					UEvent.Register<int>(UEventID.OnDropdownValueChanged, comp, (val) => OnTriggered(instance, val));
				} else {
					throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
				}
			}
		}

		void OnTriggered(GraphInstance instance, int value) {
			instance.defaultFlow.SetPortData(index, value);
			Trigger(instance);
		}

		public override Type GetNodeIcon() {
			return typeof(Dropdown);
		}

		public override void OnGeneratorInitialize() {
			var variable = CG.RegisterVariable(index);
			CG.RegisterPort(index, () => variable);
		}

		public override void GenerateEventCode() {
			var mData = CG.GetOrRegisterFunction("Start", typeof(void));
			var contents = GenerateRunFlows();
			if(!string.IsNullOrEmpty(contents)) {
				string parameter;
				if(CG.CanDeclareLocal(index, outputs)) {
					parameter = CG.GetVariableData(index).name;
				}
				else {
					parameter = CG.GenerateName("parameter", this);
					var vdata = CG.GetVariableData(index);
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
							CG.Value(UEventID.OnDropdownValueChanged),
							CG.Value(target),
							CG.Lambda(new[] { typeof(int) }, new[] { parameter }, contents)
						)
					);
				}
				else {
					mData.AddCodeForEvent(
						CG.GenericInvoke<Dropdown>(CG.This, nameof(GameObject.GetComponent))
							.CGAccess(nameof(Dropdown.onValueChanged))
							.CGFlowInvoke(
								nameof(Dropdown.onValueChanged.AddListener),
								CG.Lambda(new[] { typeof(int) }, new[] { parameter }, contents)
							)
					);
				}
			}
		}
	}
}