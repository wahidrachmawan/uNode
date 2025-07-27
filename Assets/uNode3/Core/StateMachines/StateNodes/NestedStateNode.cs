using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class NestedStateNode : Node, ISuperNodeWithEntry, INodeWithEventHandler, IStateNodeWithTransition, INodeWithConnection {
		public bool CanTriggerWhenActive = true;

		[HideInInspector]
		public StateTranstionData transitions = new StateTranstionData();

		[NonSerialized]
		public FlowInput enter;
		[NonSerialized]
		private Nodes.StateEntryNode entry;

		private static readonly string[] m_styles = new[] { "state-node", "state-nested" };
		public override string[] Styles => m_styles;
		public UGraphElement TransitionContainer => transitions.container;

		public bool CanTrigger(GraphInstance instance) {
			var state = instance.GetUserData(this) as StateMachines.IState;
			return state.IsActive;
		}

		public IEnumerable<NodeObject> NestedFlowNodes => new NodeObject[] { Entry };

		string ISuperNode.SupportedScope => StateGraphContainer.Scope;

		IEnumerable<NodeObject> INodeWithConnection.Connections => NestedFlowNodes.Concat(GetTransitions().Select(tr => tr.nodeObject));

		public BaseEntryNode Entry {
			get {
				if(entry == null) {
					entry = nodeObject.GetNodeInChildren<Nodes.StateEntryNode>();
					if(entry == null) {
						var n = new NodeObject(new Nodes.StateEntryNode());
						entry = n.node as StateEntryNode;
						nodeObject.AddChild(n);
						entry.EnsureRegistered();
					}
				}
				return entry;
			}
		}

		public IEnumerable<StateTransition> GetTransitions() {
			return transitions.GetFlowNodes<StateTransition>();
		}

		protected override void OnRegister() {
			transitions.Register(this);
			enter = PrimaryFlowInput(nameof(enter), flow => throw new Exception("State can be changed only from transition"));
			if(Entry != null) { }
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);

			var state = new StateMachines.NestedState();
			state.CanTriggerWhenActive = CanTriggerWhenActive;
			state.onEnter = () => {
#if UNITY_EDITOR
				if(GraphDebug.useDebug) {
					GraphDebug.FlowNode(instance.target, nodeObject.graphContainer.GetGraphID(), id, null);
				}
#endif
				var startStateNode = entry.exit.GetTargetNode();
				if(startStateNode == null) {
					throw new GraphException("The entry is not connected", entry);
				}
				var startState = instance.GetUserData(startStateNode) as StateMachines.BaseState;
				state.ChangeState(startState);

				foreach(var tr in GetTransitions()) {
					tr.OnEnter(instance.defaultFlow);
				}
			};
			state.onExit = () => {
#if UNITY_EDITOR
				if(GraphDebug.useDebug) {
					GraphDebug.FlowNode(instance.target, nodeObject.graphContainer.GetGraphID(), id, true);
				}
#endif
				state.ChangeState(null);
				foreach(var tr in GetTransitions()) {
					tr.OnExit(instance.defaultFlow);
				}
				////Stop all running coroutine flows
				//foreach(var element in nodeObject.GetObjectsInChildren(true)) {
				//	if(element is NodeObject node && node.node is not BaseEventNode) {
				//		foreach(var port in node.FlowInputs) {
				//			instance.StopState(port);
				//		}
				//	}
				//}
			};
			state.onUpdate = () => {
				if(state.CanTriggerWhenActive != CanTriggerWhenActive) {
					state.CanTriggerWhenActive = CanTriggerWhenActive;
				}
			};
			instance.SetUserData(this, state);
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			var fsm = CG.GetVariableNameByReference(nodeObject.parent);
			var state = CG.RegisterPrivateVariable("m_state_" + uNodeUtility.AutoCorrectName(name), typeof(StateMachines.NestedState), null, this);

			CG.RegisterNodeSetup(this, () => {
				var startStateNode = entry.exit.GetTargetNode();
				if(startStateNode == null) {
					throw new GraphException("The entry is not connected", entry);
				}
				string onEnter = CG.debugScript ? CG.Debug(enter, StateType.Running).AddLineInEnd() : null;
				string onExit = CG.debugScript ? CG.Debug(enter, StateType.Success).AddLineInEnd() : null;

				onEnter += CG.FlowInvoke(state, nameof(StateMachines.IStateMachine.ChangeState), CG.GetVariableNameByReference(startStateNode));
				onExit += CG.FlowInvoke(state, nameof(StateMachines.IStateMachine.ChangeState), CG.Null);

				if(onEnter != null) {
					onEnter = CG.SetValue(nameof(StateMachines.NestedState.onEnter), CG.Lambda(onEnter));
				}
				if(onExit != null) {
					onExit = CG.SetValue(nameof(StateMachines.NestedState.onExit), CG.Lambda(onExit));
				}
				CG.InsertCodeToFunction("Awake", CG.WrapWithInformation(CG.Flow(
					state.CGSet(CG.New(typeof(StateMachines.NestedState), null, new[] { onEnter, onExit })),
					state.CGAccess(nameof(StateMachines.IState.FSM)).CGSet(CG.GetVariableNameByReference(nodeObject.parent)),
					CanTriggerWhenActive == false ? state.CGAccess(nameof(StateMachines.IState.CanTriggerWhenActive)).CGSet(CG.Value(CanTriggerWhenActive)) : null
				), this));
			});
		}

		protected override string GenerateFlowCode() {
			var state = CG.GetVariableNameByReference(this);
			var fsm = CG.GetVariableNameByReference(nodeObject.parent);
			return CG.FlowInvoke(fsm, nameof(StateMachines.IStateMachine.ChangeState), state);
		}

		string INodeWithEventHandler.GenerateTriggerCode(string contents) {
			var state = CG.GetVariableNameByReference(this);
			return CG.If(state.CGAccess(nameof(StateMachines.IState.IsActive)), contents);
		}

		public override string GetTitle() {
			return name;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public bool AllowCoroutine() {
			return false;
		}
	}
}