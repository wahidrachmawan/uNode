using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Game Event", "On Application Focus")]
	[StateEvent]
	public class OnApplicationFocusEvent : BaseComponentEvent {
		public ValueOutput value { get; set; }

		protected override void OnRegister() {
			base.OnRegister();
			value = ValueOutput<bool>(nameof(value));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			if(instance.target is Component comp) {
				UEvent.Register(UEventID.OnApplicationFocus, comp, (bool val) => {
					instance.defaultFlow.SetPortData(value, val);
					Trigger(instance);
				});
			} else {
				throw new Exception("Invalid target: " + instance.target + "\nThe target type must inherit from `UnityEngine.Component`");
			}
		}

		public override void OnGeneratorInitialize() {
			var variable = CG.RegisterVariable(value);
			CG.RegisterPort(value, () => variable);
		}

		public override void GenerateEventCode() {
			DoGenerateCode(UEventID.OnApplicationFocus, new[] { value });
		}
	}
}