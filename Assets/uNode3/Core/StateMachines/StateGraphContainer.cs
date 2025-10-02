using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode {
	[Serializable, EventGraph("StateMachine")]
	public class StateGraphContainer : NodeContainerWithEntry, IEventGraphCanvas, IIcon, IGeneratorPrePostInitializer {
		public enum UpdateType {
			Update,
			FixedUpdate,
			LateUpdate,
			Manual,
		}
		public UpdateType updateType = UpdateType.Update;

		public string Title => name;

		public const string Scope = "StateMachine";

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

		string IEventGraphCanvas.Scope => Scope;

		public Type GetIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			var fsm = new StateMachines.StateMachine();
			instance.eventData.postInitialize += val => {
				if(Entry is Nodes.StateEntryNode entry) {
					var startStateNode = entry.exit.GetTargetNode();
					if(startStateNode != null) {
						var startState = val.GetUserData(startStateNode) as StateMachines.BaseState;
						fsm.ActiveState = startState;
					}
					//else {
					//	throw new GraphException("The entry is not connected", entry);
					//}
				}
				else {
					var node = this.GetNodeInChildren<Nodes.ScriptState>();
					var startState = val.GetUserData(node) as StateMachines.BaseState;
					fsm.ActiveState = startState;
				}
			};
			instance.SetUserData(this, fsm);
			switch(updateType) {
				case UpdateType.Update:
					UEvent.Register(UEventID.Update, instance.target as Component, () => {
						fsm.Tick();
					});
					break;
				case UpdateType.FixedUpdate:
					UEvent.Register(UEventID.FixedUpdate, instance.target as Component, () => {
						fsm.Tick();
					});
					break;
				case UpdateType.LateUpdate:
					UEvent.Register(UEventID.LateUpdate, instance.target as Component, () => {
						fsm.Tick();
					});
					break;
			}
		}

		void IGeneratorPrePostInitializer.OnPostInitializer() { }

		void IGeneratorPrePostInitializer.OnPreInitializer() {
			var fsm = CG.RegisterPrivateVariable("m_FSM", typeof(StateMachines.StateMachine), null, this);
			var nodes = this.GetNodesInChildren<Nodes.AnyStateNode>();
			foreach(var node in nodes) {
				CG.RegisterDependency(node);
			}
			CG.RegisterDependency(Entry);

			CG.RegisterPostInitialization(() => {
				string code = null;
				code = CG.Set(fsm, CG.New(typeof(StateMachines.StateMachine)));
				CG.InsertCodeToFunction("Awake", code, int.MinValue);
				if(Entry is Nodes.StateEntryNode entry) {
					var start = entry.exit.GetTargetNode();
					if(start != null) {
						var state = CG.GetVariableNameByReference(start);
						CG.InsertCodeToFunction("Awake", CG.FlowInvoke(fsm, nameof(StateMachines.IStateMachine.ChangeState), state), int.MaxValue);
					}
				}
				if(updateType != UpdateType.Manual) {
					CG.InsertCodeToFunction(updateType.ToString(), CG.FlowInvoke(fsm, nameof(StateMachines.IStateMachine.Tick)));
				}
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

	public interface IStateTransitionNode {
		IStateNodeWithTransition StateNode { get; }
	}

	public interface IStateNodeWithTransition {
		UGraphElement TransitionContainer { get; }
		IEnumerable<Nodes.StateTransition> GetTransitions();
		bool CanTrigger(GraphInstance instance);
	}

	/// <summary>
	/// Used only for highlight node
	/// </summary>
	public interface INodeWithCustomCanvas {
		UGraphElement ParentCanvas { get; }
	}
}