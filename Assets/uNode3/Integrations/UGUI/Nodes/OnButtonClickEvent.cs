using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("GUI", "On Button Click")]
	[StateEvent]
	public class OnButtonClickEvent : BaseComponentEvent {
		public ValueInput target { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(object));
			target.filter = new FilterAttribute(typeof(Button), typeof(GameObject));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var val = target.GetValue(instance.defaultFlow);
			if(val != null) {
				if(val is GameObject) {
					UEvent.Register(UEventID.OnButtonClick, val as GameObject, () => Trigger(instance));
				} else if(val is Component) {
					UEvent.Register(UEventID.OnButtonClick, val as Component, () => Trigger(instance));
				}
			} else {
				if(instance.target is Component comp) {
					UEvent.Register(UEventID.OnButtonClick, comp, () => Trigger(instance));
				} else {
					throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
				}
			}
		}

		public override Type GetNodeIcon() {
			return typeof(Button);
		}

		public override void GenerateEventCode() {
			var mData = CG.GetOrRegisterFunction("Start", typeof(void));
			var contents = GenerateRunFlows();
			if(!string.IsNullOrEmpty(contents)) {
				if(target.isAssigned) {
					mData.AddCodeForEvent(
						CG.FlowInvoke(
							typeof(UEvent),
							nameof(UEvent.Register),
							CG.Value(UEventID.OnButtonClick),
							CG.Value(target),
							CG.Lambda(contents)
						)
					);
				}
				else {
					mData.AddCodeForEvent(
						CG.GenericInvoke<Button>(CG.This, nameof(GameObject.GetComponent)).CGAccess(nameof(Button.onClick)).CGFlowInvoke(nameof(Button.onClick.AddListener), CG.Lambda(contents))
					);
				}
			}
		}
	}
}