using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class AnyStateNode : Node, IStateNodeWithTransition {
		[HideInInspector]
		public StateTranstionData transitions = new StateTranstionData();

		private static readonly string[] m_styles = new[] { "state-node", "state-any" };
		public override string[] Styles => m_styles;
		public UGraphElement TransitionContainer => transitions.container;

		public bool CanTrigger(GraphInstance instance) {
			var state = instance.GetUserData(nodeObject.parent) as StateMachines.IState;
			return state == null || state.IsActive;
		}

		public IEnumerable<StateTransition> GetTransitions() {
			return transitions.GetFlowNodes<StateTransition>();
		}

		protected override void OnRegister() {
			if(transitions == null) transitions = new();
			transitions.Register(this);
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var state = new StateMachines.AnyState();
			var fsm = instance.GetUserData(nodeObject.parent) as StateMachines.IStateMachine;
			state.FSM = fsm;
			instance.SetUserData(this, state);
		}

		public override string GetTitle() {
			return "Any";
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public bool AllowCoroutine() {
			return false;
		}
	}
}