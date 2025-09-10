using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public interface INodeWithEnterExitEvent {
		public event System.Action<Flow> OnEnterCallback;
		public event System.Action<Flow> OnExitCallback;
	}

	public interface INodeWithUpdateEvent {
		public event System.Action<Flow> OnUpdateCallback;
	}

	public class ScriptState : Node, ISuperNode, INodeWithEventHandler, INodeWithEnterExitEvent, INodeWithUpdateEvent, IStateNodeWithTransition, INodeWithConnection {
		public bool CanTriggerWhenActive = true;

		[HideInInspector]
		public StateTranstionData transitions = new StateTranstionData();


		private static readonly string[] m_styles = new[] { "state-node", "state-script" };
		public override string[] Styles => m_styles;
		public UGraphElement TransitionContainer => transitions.container;

		public bool CanTrigger(GraphInstance instance) {
			var state = instance.GetUserData(this) as StateMachines.IState;
			return state.IsActive;
		}

		public IEnumerable<NodeObject> NestedFlowNodes => nodeObject.GetObjectsInChildren<NodeObject>(obj => obj.node is BaseEventNode);

		string ISuperNode.SupportedScope => NodeScope.State + "|" + NodeScope.FlowGraph + "|" + NodeScope.Coroutine;

		IEnumerable<NodeObject> INodeWithConnection.Connections => NestedFlowNodes.Concat(GetTransitions().Select(tr => tr.nodeObject));

		public IEnumerable<StateTransition> GetTransitions() {
			// Ensure the container is the last sibling so the transition events are executed after the state events
			transitions.container.SetSiblingIndex(transitions.container.parent.childCount);
			return transitions.GetFlowNodes<StateTransition>();
		}

		private event System.Action<Flow> m_onEnter;
		public event System.Action<Flow> OnEnterCallback {
			add {
				m_onEnter -= value;
				m_onEnter += value;
			}
			remove {
				m_onEnter -= value;
			}
		}
		private event System.Action<Flow> m_onExit;
		public event System.Action<Flow> OnExitCallback {
			add {
				m_onExit -= value;
				m_onExit += value;
			}
			remove {
				m_onExit -= value;
			}
		}
		private event System.Action<Flow> m_onUpdate;
		public event System.Action<Flow> OnUpdateCallback {
			add {
				m_onUpdate -= value;
				m_onUpdate += value;
			}
			remove {
				m_onUpdate -= value;
			}
		}

		[NonSerialized]
		public FlowInput enter;

		protected override void OnRegister() {
			if(transitions == null) transitions = new();
			transitions.Register(this);
			enter = PrimaryFlowInput(nameof(enter), flow => throw new Exception("State can be changed only from transition"));
			//enter.Next(new RuntimeFlow(OnExit));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var state = new StateMachines.State();
			state.CanTriggerWhenActive = CanTriggerWhenActive;
			state.onEnter = () => {
#if UNITY_EDITOR
				if(GraphDebug.useDebug) {
					GraphDebug.FlowNode(instance.target, nodeObject.graphContainer.GetGraphID(), id, null);
				}
#endif
				m_onEnter?.Invoke(instance.defaultFlow);
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
				m_onExit?.Invoke(instance.defaultFlow);
				foreach(var tr in GetTransitions()) {
					tr.OnExit(instance.defaultFlow);
				}
				//Stop all running coroutine flows
				foreach(var element in nodeObject.GetObjectsInChildren(true)) {
					if(element is NodeObject node && node.node is not BaseEventNode) {
						foreach(var port in node.FlowInputs) {
							instance.StopState(port);
						}
					}
				}
			};
			state.onUpdate = () => {
				m_onUpdate?.Invoke(instance.defaultFlow);
				if(state.CanTriggerWhenActive != CanTriggerWhenActive) {
					state.CanTriggerWhenActive = CanTriggerWhenActive;
				}
			};
			instance.SetUserData(this, state);
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			var state = CG.RegisterPrivateVariable("m_state_" + uNodeUtility.AutoCorrectName(name), typeof(StateMachines.State), null, this);

			CG.RegisterNodeSetup(this, () => {
				string onEnter = CG.debugScript ? CG.Debug(enter, StateType.Running) : null;
				string onExit = CG.debugScript ? CG.Debug(enter, StateType.Success) : null;
				string onUpdate = null;
				foreach(var evt in nodeObject.GetNodesInChildren<BaseGraphEvent>()) {
					if(evt != null) {
						if(evt is StateOnEnterEvent) {
							onEnter += evt.GenerateFlows().AddLineInFirst().Replace("yield ", "");
						}
						else if(evt is StateOnExitEvent) {
							onExit += evt.GenerateFlows().AddLineInFirst();
						}
						else if(evt is StateOnUpdateEvent) {
							onUpdate += evt.GenerateFlows().AddLineInFirst();
						}
						else {

						}
					}
				}
				nodeObject.ForeachInChildrens(element => {
					if(element is NodeObject nodeObject) {
						foreach(var flow in nodeObject.FlowInputs) {
							if(/*flow.IsSelfCoroutine() &&*/ CG.IsStateFlow(flow)) {
								onExit += CG.StopEvent(flow).AddLineInFirst();
							}
						}
					}
				});
				if(onEnter != null) {
					onEnter = CG.SetValue(nameof(StateMachines.State.onEnter), CG.Lambda(onEnter));
				}
				if(onExit != null) {
					onExit = CG.SetValue(nameof(StateMachines.State.onExit), CG.Lambda(onExit));
				}
				if(onUpdate != null) {
					onUpdate = CG.SetValue(nameof(StateMachines.State.onUpdate), CG.Lambda(onUpdate));
				}
				CG.InsertCodeToFunction("Awake", CG.WrapWithInformation(CG.Flow(
					state.CGSet(CG.New(typeof(StateMachines.State), null, new[] { onEnter, onUpdate, onExit })),
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

		public void OnExit(Flow flow) {
			foreach(var element in nodeObject.GetObjectsInChildren(true)) {
				if(element is NodeObject node && node.node is not BaseEventNode) {
					foreach(var port in node.FlowInputs) {
						flow.instance.StopState(port);
					}
				}
			}
			foreach(BaseEventNode node in NestedFlowNodes) {
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