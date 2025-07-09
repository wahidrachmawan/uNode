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
		private ValueOutput stateMachine, isActive, state, activeState;
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
					activeState = ValueOutput(nameof(activeState), typeof(StateMachines.IState));
					activeState.AssignGetCallback(flow => {
						var fsm = flow.GetUserData(reference.reference) as StateMachines.IStateMachine;
						return fsm.ActiveState;
					});
					break;
				}
				case Kind.GetState: {
					state = ValueOutput(nameof(state), typeof(StateMachines.IState));
					state.AssignGetCallback(flow => {
						var state = flow.GetUserData(stateReference.reference) as StateMachines.IState;
						return state;
					});
					isActive = ValueOutput(nameof(isActive), typeof(bool));
					isActive.AssignGetCallback(flow => {
						var state = flow.GetUserData(stateReference.reference) as StateMachines.IState;
						return state.IsActive;
					});
					break;
				}
				case Kind.SetState: {
					exit = FlowOutput(nameof(exit)).SetName("");
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
					}).SetName("");
					break;
				}
			}
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public override string GetTitle() {
			switch(kind) {
				case Kind.StateMachine: {
					var fsm = reference?.reference as StateGraphContainer;
					if(fsm != null) {
						return $"Get: {fsm.name}";
					}
					return "Get State Machine";
				}
				case Kind.GetState: {
					var node = stateReference?.reference as NodeObject;
					if(node != null) {
						return $"Get State: {node.GetTitle()}";
					}
					return "Get State";
				}
				case Kind.SetState: {
					var fsm = reference?.reference as StateGraphContainer;
					var node = stateReference?.reference as NodeObject;
					if(fsm != null && node != null) {
						return $"Set '{fsm.name}' State to: {node.GetTitle()}";
					}
					return "Set State";
				}
			}
			return base.GetTitle();
		}

		public override void OnGeneratorInitialize() {
			switch(kind) {
				case Kind.StateMachine: {
					var fsm = reference?.reference as StateGraphContainer;
					if(fsm == null) {
						throw new Exception("Reference state machine is null");
					}
					CG.RegisterPort(stateMachine, () => {
						return CG.GetVariableNameByReference(fsm);
					});
					CG.RegisterPort(activeState, () => {
						return CG.GetVariableNameByReference(fsm).CGAccess(nameof(StateMachines.IStateMachine.ActiveState));
					});
					break;
				}
				case Kind.GetState: {
					var node = stateReference?.reference as NodeObject;
					if(node == null) {
						throw new Exception("Reference state is null");
					}
					CG.RegisterPort(state, () => {
						return CG.GetVariableNameByReference(node);
					});
					CG.RegisterPort(isActive, () => {
						return CG.GetVariableNameByReference(node).CGAccess(nameof(StateMachines.IState.IsActive));
					});
					break;
				}
				case Kind.SetState: {
					var fsm = reference?.reference as StateGraphContainer;
					var node = stateReference?.reference as NodeObject;
					if(fsm == null) {
						throw new Exception("Reference state machine is null");
					}
					if(node == null) {
						throw new Exception("Reference state is null");
					}
					CG.RegisterPort(enter, () => {
						return CG.Flow(
							CG.FlowInvoke(CG.GetVariableNameByReference(fsm), nameof(StateMachines.IStateMachine.ChangeState), CG.GetVariableNameByReference(node)),
							CG.FlowFinish(enter, exit)
						);
					});
					break;
				}
			}
		}
	}
}