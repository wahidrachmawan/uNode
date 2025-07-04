using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class StateTransition : Node, ISuperNode, IGraphEventHandler {
		public IStateNodeWithTransition node {
			get {
				return nodeObject.GetNodeInParent<IStateNodeWithTransition>();
			}
		}

		private static readonly string[] m_styles = new[] { "state-node", "state-transition" };
		public override string[] Styles => m_styles;

		public IEnumerable<NodeObject> nestedFlowNodes => nodeObject.GetObjectsInChildren<NodeObject>(obj => obj.node is BaseEventNode);

		[System.NonSerialized]
		public FlowInput enter;
		[System.NonSerialized]
		public FlowOutput exit;

		protected override void OnRegister() {
			exit = FlowOutput(nameof(exit)).SetName("");
			enter = FlowInput(nameof(enter), (flow) => throw new System.InvalidOperationException()).SetName("");
		}

		public override string GetTitle() {
			var type = GetType();
			//if(type.IsDefined(typeof(TransitionMenu), true)) {
			//	return type.GetCustomAttribute<TransitionMenu>().name;
			//}
			//else 
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

		}

		/// <summary>
		/// Called every frame when state is running.
		/// </summary>
		public virtual void OnUpdate(Flow flow) {

		}

		/// <summary>
		/// Called once after state exit, generally used for reset.
		/// </summary>
		public virtual void OnExit(Flow flow) {

		}

		/// <summary>
		/// Call to finish the transition.
		/// </summary>
		public void Finish(Flow flow) {
			var state = flow.GetUserData(node as Node) as StateMachines.IState;
			if(state.IsActive) {
				state.FSM.ChangeState(flow.GetUserData(exit.GetTargetNode()) as StateMachines.IState);
#if UNITY_EDITOR
				if(GraphDebug.useDebug) {
					GraphDebug.Flow(flow.instance.target, nodeObject.graphContainer.GetGraphID(), id, nameof(exit));
				}
#endif
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
			return node.CanTrigger(instance);
		}
	}
}