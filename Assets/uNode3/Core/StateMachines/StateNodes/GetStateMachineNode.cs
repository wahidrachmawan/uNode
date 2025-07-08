using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class GetStateMachineNode : Node {
		public enum Kind {
			StateMachine,
			GetState,
			SetState,
		}
		public Kind kind;
		public UGraphElementRef reference, stateReference;

		[NonSerialized]
		private ValueOutput stateMachine, isActive, activeState;
		[NonSerialized]
		private FlowInput enter;
		[NonSerialized]
		private FlowOutput exit;

		protected override void OnRegister() {
			switch(kind) {
				case Kind.StateMachine: {
					stateMachine = ValueOutput(nameof(stateMachine), typeof(StateMachines.IStateMachine));
					stateMachine.AssignGetCallback(flow => {
						var fsm = flow.GetUserData(reference.reference) as StateMachines.IStateMachine;
						return fsm;
					});
					activeState = ValueOutput(nameof(stateMachine), typeof(StateMachines.IState));
					activeState.AssignGetCallback(flow => {
						var fsm = flow.GetUserData(reference.reference) as StateMachines.IStateMachine;
						return fsm.ActiveState;
					});
					break;
				}
				case Kind.GetState: {
					isActive = ValueOutput(nameof(isActive), typeof(bool));
					isActive.AssignGetCallback(flow => {
						var state = flow.GetUserData(stateReference.reference) as StateMachines.IState;
						return state.IsActive;
					});
					break;
				}
				case Kind.SetState: {
					exit = FlowOutput(nameof(exit));
					enter = FlowInput(nameof(enter), flow => {
						var fsm = flow.GetUserData(reference.reference) as StateMachines.IStateMachine;
						var state = flow.GetUserData(stateReference.reference) as StateMachines.IState;
						if(fsm == null) {
							throw new Exception("target State Machine is null");
						}
						if(state == null) {
							throw new Exception("target State is null");
						}
						fsm.ChangeState(state);
						flow.Next(exit);
					});
					break;
				}
			}
		}
		 
		public override string GetTitle() => kind switch {
			Kind.GetState => "Get State",
			Kind.SetState => "Set State",
			Kind.StateMachine => "Get State Machine",
			_ => base.GetTitle()
		};
	}
}