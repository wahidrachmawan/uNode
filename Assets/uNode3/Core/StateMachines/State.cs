using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.StateMachines {
	/// <summary>
	/// The state
	/// </summary>
	public interface IState {
		/// <summary>
		/// The state machine that has this state
		/// </summary>
		IStateMachine FSM { get; set; }
		/// <summary>
		/// True if the state is active
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// Callback when state is entered
		/// </summary>
		void Enter();
		/// <summary>
		/// Callback when state is exited
		/// </summary>
		void Exit();
		/// <summary>
		/// Callback when state is updated
		/// </summary>
		void Tick();

		///// <summary>
		///// If false, state will exit immediately. If true, exiting state will take effect after <see cref="CanExit"/> is true.
		///// </summary>
		//bool NeedExitTime { get; }
		///// <summary>
		///// Called every state is exited when <see cref="NeedExitTime"/> is true
		///// </summary>
		///// <returns></returns>
		//bool CanExit();
	}

	/// <summary>
	/// The base class for all state
	/// </summary>
	public abstract class BaseState : IState {
		/// <summary>
		/// The name of state
		/// </summary>
		public string name;
		/// <summary>
		/// The id of state
		/// </summary>
		public int id;

		[NonSerialized]
		public List<ITransition> transitions = new();

		public virtual bool IsActive => FSM != null && FSM.ActiveState == this && FSM.IsActive;

		public IStateMachine FSM { get; set; }

		/// <summary>
		/// Executes custom logic when entering a state
		/// </summary>
		/// <remarks>This method is intended to be overridden in derived classes to provide behavior specific to
		/// entering a state or context. It is called automatically during the transition process and should not be invoked
		/// directly.</remarks>
		protected virtual void OnEnter() { }

		/// <summary>
		/// Performs cleanup or finalization tasks when the state is exiting.
		/// </summary>
		/// <remarks>This method is called during the state exit. Override this method in a
		/// derived class to implement custom logic that should be executed when the state exits.</remarks>
		protected virtual void OnExit() { }

		/// <summary>
		/// Invoked periodically to perform operations or updates during a timed interval.
		/// </summary>
		/// <remarks>This method is intended to be overridden in derived classes to implement custom behavior  that
		/// should occur on each tick of a timer or similar periodic mechanism. The base implementation  does
		/// nothing.</remarks>
		protected virtual void OnTick() { }

		public void Enter() {
			for(int i = 0; i < transitions.Count; i++) {
				transitions[i].OnEnter();
				if(transitions[i].ShouldTransition()) {
					transitions[i].Transition();
					return;
				}
			}
			OnEnter();
		}

		public void Exit() {
			for(int i = 0; i < transitions.Count; i++) {
				transitions[i].OnExit();
			}
			OnExit();
		}

		public void Tick() {
			for(int i = 0; i < transitions.Count; i++) {
				if(transitions[i].ShouldTransition()) {
					transitions[i].Transition();
					return;
				}
			}
			OnTick();
		}

		/// <summary>
		/// Adds a transition to the current state, specifying the target state.
		/// </summary>
		/// <remarks>This method associates the provided transition with the current state and sets its target state.
		/// The transition is added to the internal collection of transitions for the current state.</remarks>
		/// <param name="transition">The transition to add. Must not be null.</param>
		/// <param name="target">The target state for the transition. Must not be the same as the current state.</param>
		/// <exception cref="Exception">Thrown if <paramref name="target"/> is the same as the current state.</exception>
		public void AddTransition(ITransition transition, IState target) {
			if(target == this)
				throw new Exception("The target state must not same with the self state");
			transition.State = this;
			transition.TargetState = target;
			transitions.Add(transition);
		}

		/// <summary>
		/// Clears all transitions from the current collection.
		/// </summary>
		/// <remarks>This method removes all transitions, leaving the collection empty.</remarks>
		public void ClearTransition() {
			transitions.Clear();
		}
	}

	/// <summary>
	/// The regular state that can be customized their callbacks.
	/// </summary>
	public class State : BaseState {
		public Action onEnter, onUpdate, onExit;

		public State(Action onEnter = null, Action onTick = null, Action onExit = null) {
			this.onEnter = onEnter;
			this.onUpdate = onTick;
			this.onExit = onExit;
		}

		protected override void OnEnter() {
			onEnter?.Invoke();
		}

		protected override void OnTick() {
			onUpdate?.Invoke();
		}

		protected override void OnExit() {
			onExit?.Invoke();
		}
	}

	/// <summary>
	/// The state that always active
	/// </summary>
	public class AnyState : BaseState {
		/// <summary>
		/// Any state is always active when FSM is active
		/// </summary>
		public override bool IsActive => FSM != null && FSM.IsActive;
	}

	/// <summary>
	/// The nested state that can have state inside of it
	/// </summary>
	public class NestedState : BaseState, IStateMachine {
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

		public void ChangeState(IState state) {
			ActiveState = state;
			//Tick();
		}

		[NonSerialized]
		bool m_hasInitialize;

		protected override void OnTick() {
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