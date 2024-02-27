using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Toggle", hasFlowInput = true, hasFlowOutput = true)]
	[Description("When In is called, calls On or Off depending on the current toggle state. Whenever Toggle input is called the state changes.")]
	public class FlowToggle : Node {
		[Tooltip("Input flow to execute the node.")]
		public FlowInput input { get; set; }

		[Tooltip("Turn toggle state to on.")]
		public FlowInput turnOn { get; set; }

		[Tooltip("Turn toggle state to off.")]
		public FlowInput turnOff { get; set; }

		[Tooltip("Invert the toggle state.")]
		public FlowInput toggle { get; set; }

		[Tooltip("Flow to execute when the toggle is on.")]
		public FlowOutput onOpen { get; set; }

		[Tooltip("Flow to execute when the toggle is off.")]
		public FlowOutput onClosed { get; set; }

		[Tooltip("Flow to execute when the toggle is turned on.")]
		public FlowOutput onTurnedOn { get; set; }

		[Tooltip("Flow to execute when the toggle is turned off.")]
		public FlowOutput onTurnedOff { get; set; }
		public ValueOutput isOn { get; set; }

		class RuntimeData {
			public bool open;
		}

		protected override void OnRegister() {
			input = FlowInput(nameof(input), (flow) => {
				var data = flow.GetOrCreateElementData<RuntimeData>(this);
				if(data.open) {
					flow.Next(onOpen);
				} else {
					flow.Next(onClosed);
				}
			}).SetName("In");
			turnOn = FlowInput(nameof(turnOn), (flow) => {
				var data = flow.GetOrCreateElementData<RuntimeData>(this);
				if(!data.open) {
					data.open = true;
					flow.Next(onTurnedOn);
				}
			}).SetName("On");
			turnOff = FlowInput(nameof(turnOff), (flow) => {
				var data = flow.GetOrCreateElementData<RuntimeData>(this);
				if(data.open) {
					data.open = false;
					flow.Next(onTurnedOff);
				}
			}).SetName("Off");
			toggle = FlowInput(nameof(toggle), (flow) => {
				var data = flow.GetOrCreateElementData<RuntimeData>(this);
				data.open = !data.open;
				if(data.open) {
					flow.Next(onTurnedOn);
				} else {
					flow.Next(onTurnedOff);
				}
			});
			isOn = ValueOutput<bool>(nameof(isOn));
			onOpen = FlowOutput(nameof(onOpen)).SetName("On");
			onClosed = FlowOutput(nameof(onClosed)).SetName("Off");
			onTurnedOn = FlowOutput(nameof(onTurnedOn)).SetName("Turned On");
			onTurnedOff = FlowOutput(nameof(onTurnedOff)).SetName("Turned Off");

			input.isCoroutine = IsCoroutine;
			turnOn.isCoroutine = onTurnedOn.IsCoroutine;
			input.isCoroutine = IsCoroutine;
			turnOn.isCoroutine = onTurnedOff.IsCoroutine;

			isOn.AssignGetCallback(instance => instance.GetOrCreateElementData<RuntimeData>(this).open);
		}

		public override void OnGeneratorInitialize() {
			string varName = CG.RegisterPrivateVariable(nameof(RuntimeData.open), typeof(bool));
			CG.RegisterPort(input, () => {
				return CG.If(varName,
					CG.FlowFinish(input, onOpen),
					CG.FlowFinish(input, onClosed));
			});
			CG.RegisterPort(turnOn, () => {
				return CG.If(varName.CGNot(),
					CG.Flow(
						CG.Set(varName, true.CGValue()),
						CG.FlowFinish(turnOn, onTurnedOn))
				);
			});
			CG.RegisterPort(turnOff, () => {
				return CG.If(varName,
					CG.Flow(
						CG.Set(varName, false.CGValue()),
						CG.FlowFinish(turnOff, onTurnedOff))
				);
			});
			CG.RegisterPort(toggle, () => {
				return CG.Flow(
					CG.Set(varName, "!" + varName),
					CG.If(
						varName,
						CG.FlowFinish(toggle, onTurnedOn),
						CG.FlowFinish(toggle, onTurnedOff))
				);
			});
		}

		private bool IsCoroutine() => onOpen.IsCoroutine() || onClosed.IsCoroutine();
	}
}
