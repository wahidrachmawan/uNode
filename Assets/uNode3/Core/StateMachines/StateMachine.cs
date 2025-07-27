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
		/// Gets a value indicating whether the FSM is active.
		/// </summary>
		bool IsActive => true;
		/// <summary>
		/// Advances the state of the object by performing a single update cycle.
		/// </summary>
		/// <remarks>This method is typically called at regular intervals to update the object's state. Ensure that
		/// the object is in a valid state before calling this method.</remarks>
		void Tick();
		/// <summary>
		/// Change the active state
		/// </summary>
		/// <param name="state"></param>
		void ChangeState(IState state);
		/// <summary>
		/// Registers a state to be used as an "Any State" in the state machine.
		/// </summary>
		/// <remarks>An "Any State" represents a state that can transition to other states regardless of the current
		/// state. This method allows the specified state to be treated as an "Any State" within the state machine. Ensure
		/// that the provided state is properly configured for transitions.</remarks>
		/// <param name="state">The state to register as an "Any State". Cannot be null.</param>
		void RegisterAnyState(IState state);
	}

	public class StateMachine : IStateMachine {
		[NonSerialized]
		private IState m_activeState;
		[NonSerialized]
		private IState m_transitionState;
		public IState ActiveState {
			get => m_activeState;
			set {
				if(value != null) {
					value.FSM = this;
				}
				if(value == m_activeState && value.CanTriggerWhenActive == false) {
					//Return when it is cannot be triggered again while is active
					return;
				}
				if(m_transitionState == null) {
					m_activeState?.Exit();
					m_activeState = null;
					m_transitionState = value;
				}
			}
		}
		/// <summary>
		/// Represents a collection of states that can be entered from any other state.
		/// </summary>
		public readonly List<BaseState> AnyStates = new();

		[NonSerialized]
		private bool m_isPaused;
		public bool IsActive => m_isPaused == false;

		/// <summary>
		/// Pauses the current operation or process.
		/// </summary>
		/// <remarks>This method sets the internal state to indicate that the operation is paused.  While paused, the
		/// operation will not proceed until resumed.</remarks>
		public void Pause() => m_isPaused = true;
		/// <summary>
		/// Resumes the operation by clearing the paused state.
		/// </summary>
		/// <remarks>This method sets the internal state to indicate that the operation is no longer paused. Call this
		/// method to continue processing after a pause.</remarks>
		public void Resume() => m_isPaused = false;

		public void ChangeState(IState state) {
			ActiveState = state;
			//Tick();
		}

		[NonSerialized]
		bool m_hasInitialize;

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
			m_activeState?.Tick();
		}

		/// <summary>
		/// Registers a state as an "Any State" in the finite state machine.
		/// </summary>
		/// <remarks>"Any States" are special states that can transition from any other state in the finite state
		/// machine. This method associates the provided state with the finite state machine and adds it to the collection of
		/// "Any States".</remarks>
		/// <param name="state">The state to register as an "Any State". Must be of type <see cref="BaseState"/>.</param>
		/// <exception cref="NullReferenceException"></exception>
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