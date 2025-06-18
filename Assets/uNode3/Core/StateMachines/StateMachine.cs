using System;
using System.Collections.Generic;

namespace MaxyGames.StateMachines {
	public class StateMachine : IStateMachine {
		private IState m_activeState;
		public IState ActiveState {
			get => m_activeState;
			set {
				if(value != null) {
					value.FSM = this;
				}
				m_activeState = value;
			}
		}
		public AnyState AnyState { get; set; } = new();

		public void ChangeState(IState state) {
			ActiveState?.Exit();
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
				AnyState.FSM = this;
				if(AnyState != null) {
					foreach(var tr in AnyState.transitions) {
						tr.OnEnter();
						if(tr.ShouldTransition()) {
							tr.Transition();
							return;
						}
					}
				}
			}
			if(AnyState != null) {
				foreach(var tr in AnyState.transitions) {
					if(tr.ShouldTransition()) {
						tr.Transition();
						return;
					}
				}
			}
			ActiveState?.Tick();
		}
	}

	/// <summary>
	/// The nested state that can have state inside of it
	/// </summary>
	public class NestedState : BaseState, IStateMachine {
		private IState m_activeState;
		public IState ActiveState {
			get => m_activeState;
			set {
				if(value != null) {
					value.FSM = this;
				}
				m_activeState = value;
			}
		}
		public AnyState AnyState { get; set; } = new();

		public void ChangeState(IState state) {
			ActiveState?.Exit();
			ActiveState = state;
			//Tick();
		}

		[NonSerialized]
		bool m_hasInitialize;

		protected override void OnTick() {
			if(m_hasInitialize == false) {
				m_hasInitialize = true;
				AnyState.FSM = this;
				if(AnyState != null) {
					foreach(var tr in AnyState.transitions) {
						tr.OnEnter();
						if(tr.ShouldTransition()) {
							tr.Transition();
							return;
						}
					}
				}
			}
			if(AnyState != null) {
				foreach(var tr in AnyState.transitions) {
					if(tr.ShouldTransition()) {
						tr.Transition();
						return;
					}
				}
			}
			ActiveState?.Tick();
		}
	}

	public abstract class BaseTransition : ITransition {
		public IState State { get; set; }
		public IState TargetState { get; set; }

		public abstract void OnEnter();
		public abstract void OnExit();
		public abstract bool ShouldTransition();
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
			StateMachine.ChangeState(TargetState);
		}
	}

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

		public virtual bool IsActive => FSM.ActiveState == this;

		public IStateMachine FSM { get; set; }

		/// <summary>
		/// Called when state is entered
		/// </summary>
		protected virtual void OnEnter() { }

		/// <summary>
		/// Called when state is exited
		/// </summary>
		protected virtual void OnExit() { }

		/// <summary>
		/// Called every state is updated
		/// </summary>
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

		public void AddTransition(ITransition transition, IState target) {
			if(target == this)
				throw new Exception("The target state must not same with the self state");
			transition.State = this;
			transition.TargetState = target;
			transitions.Add(transition);
		}

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
		/// Any state is always active
		/// </summary>
		public override bool IsActive => true;
	}
}

namespace MaxyGames.UNode {
	[EventGraph("StateMachine")]
	public class StateGraphContainer : NodeContainerWithEntry, IEventGraph {
		public string Title => name;

		public override BaseEntryNode Entry {
			get {
				if(this == null) return null;
				if(entryObject == null || entryObject.node is not Nodes.StateEntryNode) {
					entryObject = this.GetNodeInChildren<Nodes.StateEntryNode>();
					if(entryObject == null) {
						AddChild(entryObject = new NodeObject(new Nodes.StateEntryNode()));
						entryObject.EnsureRegistered();
					}
				}
				return entryObject.node as BaseEntryNode;
			}
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class EventGraphAttribute : Attribute {
		public string name;

		public EventGraphAttribute(string name) {
			this.name = name;
		}
	}
}

namespace MaxyGames.UNode.Nodes {
	public class StateEntryNode : BaseEntryNode {
		public FlowInput enter;

		public FlowOutput exit;

		[NonSerialized]
		public NodeContainerWithEntry container;

		protected override void OnRegister() {
			enter = new FlowInput(this, nameof(enter), flow => flow.Next(exit));
			exit = PrimaryFlowOutput(nameof(exit));
			container = nodeObject.GetObjectInParent<NodeContainerWithEntry>();
			if(container != null && container.Entry == this) {
				container.RegisterEntry(this);
			}
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(container != null && container.Entry != this) {
				analizer.RegisterError(this, "Multiple entry node is not supported.");
			}
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.FlowIcon);
		}
	}
}