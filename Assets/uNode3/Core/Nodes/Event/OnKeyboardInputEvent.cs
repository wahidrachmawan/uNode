using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Input", "On Keyboard Input")]
	[StateEvent]
	public class OnKeyboardInputEvent : BaseComponentEvent {
		public enum ActionState {
			Down,
			Up,
			Hold,
		}
		public ActionState action;
		public ValueInput key { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			key = ValueInput(nameof(key), KeyCode.Space);
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
					if(Input.GetKeyDown(key.GetValue<KeyCode>(instance.defaultFlow))) {
						Trigger(instance);
					}
					break;
				case ActionState.Up:
					if(Input.GetKeyUp(key.GetValue<KeyCode>(instance.defaultFlow))) {
						Trigger(instance);
					}
					break;
				case ActionState.Hold:
					if(Input.GetKey(key.GetValue<KeyCode>(instance.defaultFlow))) {
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
						code = nameof(Input.GetKeyDown);
						break;
					case ActionState.Up:
						code = nameof(Input.GetKeyUp);
						break;
					case ActionState.Hold:
						code = nameof(Input.GetKey);
						break;
					default:
						throw null;
				}
				mData.AddCodeForEvent(CG.If(CG.Invoke(typeof(Input), code, CG.Value(key)), contents));
			}
		}
	}
}