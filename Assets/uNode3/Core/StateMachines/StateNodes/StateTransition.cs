using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class StateTransition : Node, ISuperNode, IScriptState, IGraphEventHandler {
		public IStateNodeWithTransition StateNode {
			get {
				return nodeObject.GetNodeInParent<IStateNodeWithTransition>();
			}
		}

		private static readonly string[] m_styles = new[] { "state-node", "state-transition" };
		public override string[] Styles => m_styles;

		public IEnumerable<NodeObject> NestedFlowNodes => nodeObject.GetObjectsInChildren<NodeObject>(obj => obj.node is BaseEventNode);

		[System.NonSerialized]
		public FlowInput enter;
		[System.NonSerialized]
		public FlowOutput exit;

		private event System.Action<Flow> m_onEnter;
		public event System.Action<Flow> OnEnterState {
			add {
				m_onEnter -= value;
				m_onEnter += value;
			}
			remove {
				m_onEnter -= value;
			}
		}
		private event System.Action<Flow> m_onExit;
		public event System.Action<Flow> OnExitState {
			add {
				m_onExit -= value;
				m_onExit += value;
			}
			remove {
				m_onExit -= value;
			}
		}

		protected override void OnRegister() {
			exit = FlowOutput(nameof(exit)).SetName("");
			enter = FlowInput(nameof(enter), (flow) => throw new System.InvalidOperationException()).SetName("");
		}

		public override string GetTitle() {
			var type = GetType();
			if(!string.IsNullOrEmpty(name)) {
				return name;
			}
			else {
				return type.PrettyName();
			}
		}

		/// <summary>
		/// Called once after state Enter, generally used for Setup.
		/// </summary>
		public virtual void OnEnter(Flow flow) {
			m_onEnter?.Invoke(flow);
		}

		///// <summary>
		///// Called every frame when state is running.
		///// </summary>
		//public virtual void OnUpdate(Flow flow) {
		//	m_onEnter?.Invoke(flow);
		//}

		/// <summary>
		/// Called once after state exit, generally used for reset.
		/// </summary>
		public virtual void OnExit(Flow flow) {
			m_onExit?.Invoke(flow);
		}

		/// <summary>
		/// Call to finish the transition.
		/// </summary>
		public void Finish(Flow flow) {
			var state = flow.GetUserData(StateNode as Node) as StateMachines.IState;
			if(state.IsActive) {
				state.FSM.ChangeState(flow.GetUserData(exit.GetTargetNode()) as StateMachines.IState);
#if UNITY_EDITOR
				if(GraphDebug.useDebug) {
					GraphDebug.Flow(flow.instance.target, nodeObject.graphContainer.GetGraphID(), id, nameof(exit));
				}
#endif
			}
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();
			if(exit.GetTargetNode() != null) {
				CG.RegisterNode(exit.GetTargetNode());
				foreach(var node in NestedFlowNodes) {
					CG.RegisterNode(node);
				}
				CG.RegisterNodeSetup(this, () => {
					var state = CG.GetVariableNameByReference(StateNode);
					string onEnter = null;
					string onExit = null;
					foreach(var evt in nodeObject.GetNodesInChildren<BaseGraphEvent>()) {
						if(evt != null) {
							if(evt is StateOnEnterEvent) {
								onEnter += evt.GenerateFlows().AddLineInFirst().Replace("yield ", "");
							}
							else if(evt is StateOnExitEvent) {
								onExit += evt.GenerateFlows().AddLineInFirst();
							}
							else {

							}
						}
					}
					if(onEnter != null) {
						onEnter = CG.Set(state.CGAccess(nameof(StateMachines.State.onEnter)), CG.Lambda(onEnter), SetType.Add);
					}
					if(onExit != null) {
						onExit = CG.Set(state.CGAccess(nameof(StateMachines.State.onExit)), CG.Lambda(onExit), SetType.Add);
					}
					if(onEnter != null || onExit != null) {
						CG.InsertCodeToFunction("Awake", CG.Flow(
							onEnter, onExit
						));
					}
				});
			}
		}

		/// <summary>
		/// Used to generating OnEnter code.
		/// </summary>
		/// <returns></returns>
		public virtual string GenerateOnEnterCode() {
			return null;
		}

		/// <summary>
		/// Used to generating OnUpdate code
		/// </summary>
		/// <returns></returns>
		public virtual string GenerateOnUpdateCode() {
			return null;
		}

		/// <summary>
		/// Used to generating OnExit code
		/// </summary>
		/// <returns></returns>
		public virtual string GenerateOnExitCode() {
			return null;
		}

		public override Type GetNodeIcon() {
			return null;
		}

		public override void CheckError(ErrorAnalyzer analyzer) {
			if(exit.isConnected == false) {
				analyzer.RegisterError(this, "No target");
			}
		}

		public bool AllowCoroutine() => false;

		public bool CanTrigger(GraphInstance instance) {
			return StateNode.CanTrigger(instance);
		}

		string IGraphEventHandler.GenerateTriggerCode(string contents) {
			var state = CG.GetVariableNameByReference(StateNode);
			if(state == null)
				return CG.WrapWithInformation(CG.WrapWithInformation(contents, this), StateNode);
			return CG.WrapWithInformation(CG.WrapWithInformation(CG.If(state.CGAccess(nameof(StateMachines.IState.IsActive)), contents), this), StateNode);
		}
	}
}