using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Input", "On Button Input")]
	[StateEvent]
	public class OnButtonInputEvent : BaseComponentEvent {
		public enum ActionState {
			Down,
			Up,
			Hold,
		}
		public ActionState action;
		public ValueInput buttonName { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			buttonName = ValueInput<string>(nameof(buttonName), string.Empty);
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.Update, comp, () => OnUpdate(instance));
			} else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		void OnUpdate(GraphInstance instance) {
			switch(action) {
				case ActionState.Down:
					if(Input.GetButtonDown(buttonName.GetValue<string>(instance.defaultFlow))) {
						Trigger(instance);
					}
					break;
				case ActionState.Up:
					if(Input.GetButtonUp(buttonName.GetValue<string>(instance.defaultFlow))) {
						Trigger(instance);
					}
					break;
				case ActionState.Hold:
					if(Input.GetButton(buttonName.GetValue<string>(instance.defaultFlow))) {
						Trigger(instance);
					}
					break;
			}
		}

		public override Type GetNodeIcon() {
			return typeof(Input);
		}

		public override void GenerateEventCode() {
			var mData = CG.GetOrRegisterFunction(UEventID.Update, typeof(void));
			var contents = GenerateRunFlows();
			if(!string.IsNullOrEmpty(contents)) {
				string code;
				switch(action) {
					case ActionState.Down:
						code = nameof(Input.GetButtonDown);
						break;
					case ActionState.Up:
						code = nameof(Input.GetButtonUp);
						break;
					case ActionState.Hold:
						code = nameof(Input.GetButton);
						break;
					default:
						throw null;
				}
				mData.AddCodeForEvent(CG.If(CG.Invoke(typeof(Input), code, CG.Value(buttonName)), contents));
			}
		}
	}
}