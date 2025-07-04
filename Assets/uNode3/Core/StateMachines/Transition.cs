using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxyGames.StateMachines {
	/// <summary>
	/// The transition
	/// </summary>
	public interface ITransition {
		/// <summary>
		/// The state that own this transition
		/// </summary>
		IState State { get; set; }
		/// <summary>
		/// The target state for transition the state to
		/// </summary>
		IState TargetState { get; set; }
		/// <summary>
		/// The State Machine
		/// </summary>
		IStateMachine StateMachine => State.FSM;

		/// <summary>
		/// Called when state is entered
		/// </summary>
		void OnEnter();
		/// <summary>
		/// Called after entering state and in tick from <see cref="State"/>, if true the transition will be exited
		/// </summary>
		/// <returns></returns>
		bool ShouldTransition();
		/// <summary>
		/// Callback when state is exited
		/// </summary>
		void OnExit();

		/// <summary>
		/// Do the transition
		/// </summary>
		void Transition() {
			if(State.IsActive) {
				//Do change state only when the original state is active
				StateMachine.ChangeState(TargetState);
			}
		}
	}

	public class Transition : BaseTransition {
		Func<bool> shouldTransition;
		Action onEnter, onExit;

		public Transition(Func<bool> shouldTransition = null, Action onEnter = null, Action onExit = null) {
			this.shouldTransition = shouldTransition;
			this.onEnter = onEnter;
			this.onExit = onExit;
		}

		public override void OnEnter() {
			onEnter?.Invoke();
		}

		public override void OnExit() {
			onExit?.Invoke();
		}

		public override bool ShouldTransition() => shouldTransition?.Invoke() == true;
	}

	public abstract class BaseTransition : ITransition {
		public IState State { get; set; }
		public IState TargetState { get; set; }

		public abstract void OnEnter();
		public abstract void OnExit();
		public abstract bool ShouldTransition();
	}
}