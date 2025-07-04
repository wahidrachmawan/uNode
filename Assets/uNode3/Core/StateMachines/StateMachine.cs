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
		public AnyState AnyState { get; set; } = new();

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
			if(m_transitionState != null) {
				m_activeState = m_transitionState;
				m_transitionState = null;
				m_activeState.Enter();
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
}

namespace MaxyGames.UNode {
	[EventGraph("StateMachine")]
	public class StateGraphContainer : NodeContainerWithEntry, IEventGraph, IIcon, IGeneratorPrePostInitializer {
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

		public Type GetIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			var fsm = new StateMachines.StateMachine();
			instance.eventData.postInitialize += val => {
				if(Entry is Nodes.StateEntryNode entry) {
					var startStateNode = entry.exit.GetTargetNode();
					if(startStateNode == null) {
						throw new GraphException("The entry is not connected", entry);
					}
					var startState = val.GetUserData(startStateNode) as StateMachines.BaseState;
					fsm.ActiveState = startState;
				}
				else {
					var node = this.GetNodeInChildren<Nodes.ScriptState>();
					var startState = val.GetUserData(node) as StateMachines.BaseState;
					fsm.ActiveState = startState;
				}
			};
			instance.SetUserData(this, fsm);
			UEvent.Register(UEventID.Update, instance.target as Component, () => {
				fsm.Tick();
			});
		}

		void IGeneratorPrePostInitializer.OnPostInitializer() {
		}

		void IGeneratorPrePostInitializer.OnPreInitializer() {

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
	public interface IStateNodeWithTransition {
		UGraphElement TransitionContainer { get; }
		IEnumerable<StateTransition> GetTransitions();
		bool CanTrigger(GraphInstance instance);
	}

	[Serializable]
	public class StateTranstionData : BaseNodeContainerData<TransitionContainer> {
		protected override bool IsValidFlowNode(NodeObject node) {
			return node.node is StateTransition;
		}
	}
}