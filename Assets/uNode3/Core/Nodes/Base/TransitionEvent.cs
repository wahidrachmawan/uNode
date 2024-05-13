using UnityEngine;
using MaxyGames.UNode.Nodes;
using System.Reflection;
using System;

namespace MaxyGames.UNode {
	/// <summary>
	/// Base class for all transition event.
	/// </summary>
	public abstract class TransitionEvent : Node {
		public StateNode node {
			get {
				return nodeObject.GetNodeInParent<StateNode>();
			}
		}

		[System.NonSerialized]
		public FlowInput enter;
		[System.NonSerialized]
		public FlowOutput exit;

		protected override void OnRegister() {
			exit = FlowOutput(nameof(exit)).SetName("");
			enter = FlowInput(nameof(enter), (flow) => throw new System.InvalidOperationException()).SetName("");
		}

		protected StateNode GetStateNode() {
			return node;
		}

		public override string GetTitle() {
			var type = GetType();
			if(type.IsDefined(typeof(TransitionMenu), true)) {
				return type.GetCustomAttribute<TransitionMenu>().name;
			}
			else if(!string.IsNullOrEmpty(name)) {
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
		protected void Finish(Flow flow) {
			if(flow.state == StateType.Running) {
				flow.instance.GetStateData(GetStateNode().enter).Stop();
				flow.Stop();
				flow.TriggerParallel(exit);
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
	}
}
