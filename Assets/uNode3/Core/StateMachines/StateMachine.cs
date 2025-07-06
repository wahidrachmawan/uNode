using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.StateMachines {
	/// <summary>
	/// The state machine
	/// </summary>
	public interface IStateMachine {
		/// <summary>
		/// The active state
		/// </summary>
		IState ActiveState { get; }
		/// <summary>
		/// Update the state machines
		/// </summary>
		void Tick();
		/// <summary>
		/// Change the active state
		/// </summary>
		/// <param name="state"></param>
		void ChangeState(IState state);
		/// <summary>
		/// Register any state to the fsm
		/// </summary>
		/// <param name="state"></param>
		void RegisterAnyState(IState state);
	}

	public class StateMachine : IStateMachine {
		private IState m_activeState;
		private IState m_transitionState;
		public IState ActiveState {
			get => m_activeState;
			set {
				if(value != null) {
					value.FSM = this;
				}
				if(m_transitionState == null) {
					m_activeState?.Exit();
					m_activeState = null;
					m_transitionState = value;
				}
			}
		}
		public readonly List<BaseState> AnyStates = new();

		public void ChangeState(IState state) {
			ActiveState = state;
			//Tick();
		}

		[NonSerialized]
		bool m_hasInitialize;

		/// <summary>
		/// Update the state machines
		/// </summary>
		public void Tick() {
			if(m_hasInitialize == false) {
				m_hasInitialize = true;
				foreach(var any in AnyStates) {
					if(any != null) {
						foreach(var tr in any.transitions) {
							tr.OnEnter();
							if(tr.ShouldTransition()) {
								tr.Transition();
								return;
							}
						}
					}
				}
			}
			if(m_transitionState != null) {
				m_activeState = m_transitionState;
				m_transitionState = null;
				m_activeState.Enter();
			}
			foreach(var any in AnyStates) {
				if(any != null) {
					foreach(var tr in any.transitions) {
						if(tr.ShouldTransition()) {
							tr.Transition();
							return;
						}
					}
				}
			}
			ActiveState?.Tick();
		}

		public void RegisterAnyState(IState state) {
			if(state is not BaseState any) throw null;
			any.FSM = this;
			AnyStates.Add(any);
		}
	}
}

namespace MaxyGames.UNode.Nodes {

	[Serializable]
	public class StateTranstionData : BaseNodeContainerData<TransitionContainer> {
		protected override bool IsValidFlowNode(NodeObject node) {
			return node.node is StateTransition;
		}
	}
}