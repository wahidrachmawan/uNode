using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

		public virtual bool IsActive => FSM?.ActiveState == this;

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

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var fsm = new StateMachines.StateMachine();
			instance.SetUserData(this, fsm);
			UEvent.Register(UEventID.Update, instance.target as Component, () => {
				fsm.Tick();
			});
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

	public class ScriptState : Node, ISuperNode, IGraphEventHandler {
		[HideInInspector]
		public TransitionData transitions = new TransitionData();

		public bool CanTrigger(GraphInstance instance) {
			var state = instance.GetUserData(this) as StateMachines.IState;
			return state.IsActive;
		}

		public IEnumerable<NodeObject> nestedFlowNodes => nodeObject.GetObjectsInChildren<NodeObject>(obj => obj.node is BaseEventNode);

		string ISuperNode.SupportedScope => NodeScope.State + "|" + NodeScope.FlowGraph;

		public IEnumerable<TransitionEvent> GetTransitions() {
			return transitions.GetFlowNodes<TransitionEvent>();
		}

		private event System.Action<Flow> m_onEnter;
		public event System.Action<Flow> onEnter {
			add {
				m_onEnter -= value;
				m_onEnter += value;
			}
			remove {
				m_onEnter -= value;
			}
		}
		private event System.Action<Flow> m_onExit;
		public event System.Action<Flow> onExit {
			add {
				m_onExit -= value;
				m_onExit += value;
			}
			remove {
				m_onExit -= value;
			}
		}

		protected override void OnRegister() {
			transitions.Register(this);
			//enter.Next(new RuntimeFlow(OnExit));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var state = new StateMachines.State(
				onEnter: () => {
					m_onEnter?.Invoke(instance.defaultFlow);
				},
				onExit: () => {
					m_onExit?.Invoke(instance.defaultFlow);
				});
			instance.SetUserData(this, state);
		}

		public void OnExit(Flow flow) {
			foreach(var element in nodeObject.GetObjectsInChildren(true)) {
				if(element is NodeObject node && node.node is not BaseEventNode) {
					foreach(var port in node.FlowInputs) {
						flow.instance.StopState(port);
					}
				}
			}
			foreach(BaseEventNode node in nestedFlowNodes) {
				node.Stop(flow.instance);
			}
			if(m_onExit != null) {
				m_onExit(flow);
			}
			foreach(var tr in GetTransitions()) {
				tr.OnExit(flow);
			}
		}

		public override string GetTitle() {
			return name;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public bool AllowCoroutine() {
			return true;
		}
	}
}