using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Action", order = -1)]
	public class NodeAction : FlowNode, IStackedNode {
		[HideInInspector]
		public BlockData data = new BlockData();

		public IEnumerable<NodeObject> stackedNodes => data.GetFlowNodes();

		protected override void OnRegister() {
			data.Register(this);
			base.OnRegister();
		}

		public override string GetTitle() {
			return name;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.ActionIcon);
		}

		protected override bool IsSelfCoroutine() {
			foreach(var block in data.GetFlowNodes()) {
				if(block.primaryFlowInput != null && block.primaryFlowInput.IsSelfCoroutine()) {
					return true;
				}
			}
			return false;
		}

		protected override void OnExecuted(Flow flow) {
			data.NextFlows(flow);
		}

		protected override string GenerateFlowCode() {
			string contents = null;
			foreach(var node in stackedNodes) {
				if(node.primaryFlowInput != null) {
					contents += CG.GeneratePort(node.primaryFlowInput).AddLineInFirst();
				}
			}
			return CG.Flow(data.GenerateFlowCode(), CG.FlowFinish(enter, exit));
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			data.CheckErrors(analizer, false);
		}
	}

	
}

namespace MaxyGames.UNode {
	public abstract class BaseNodeContainerData<T> where T : UGraphElement, new() {
		[SerializeField]
		private T _container;

		public T container => _container;

		public IEnumerable<NodeObject> GetNodes() {
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var n = container.GetChild(i) as NodeObject;
				if(n == null)
					continue;
				yield return n;
			}
		}

		public IEnumerable<NodeObject> GetFlowNodes() {
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var n = container.GetChild(i) as NodeObject;
				if(n == null)
					continue;
				if(IsValidFlowNode(n)) {
					yield return n;
				}
			}
		}

		public IEnumerable<N> GetFlowNodes<N>() where N : Node {
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var n = container.GetChild(i) as NodeObject;
				if(n == null)
					continue;
				if(n.node is N && IsValidFlowNode(n)) {
					yield return n.node as N;
				}
			}
		}

		protected abstract bool IsValidFlowNode(NodeObject node);

		public virtual void Register(Node node) {
			InitializeContainer(node);
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var n = container.GetChild(i) as NodeObject;
				if(n == null)
					continue;
				n.EnsureRegistered();
				if(n.primaryFlowOutput != null && n.primaryFlowOutput.isConnected) {
					//Clear the connection for primary flow output so it will not get called.
					n.primaryFlowOutput.ClearConnections();
				}
				if(n.primaryFlowInput != null && n.primaryFlowInput.isConnected) {
					//Clear the connection for primary flow input so it will not get called by other nodes.
					n.primaryFlowInput.ClearConnections();
				}
			}
		}

		protected void InitializeContainer(Node node) {
			if(_container == null) {
				_container = new T();
				node.nodeObject.AddChild(container);
			}
		}
	}

	[Serializable]
	public class BlockData : BaseNodeContainerData<BlockContainer> {
		protected override bool IsValidFlowNode(NodeObject node) {
			return node.primaryFlowInput != null;
		}

		public void NextFlows(Flow flow) {
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var n = container.GetChild(i) as NodeObject;
				if(n == null)
					continue;
				if(n.primaryFlowInput != null) {
					flow.Next(n.primaryFlowInput);
				}
			}
		}

		public void CheckErrors(ErrorAnalyzer analizer, bool isConditional) {
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var n = container.GetChild(i) as NodeObject;
				if(n == null)
					continue;
				if(isConditional) {
					if(n.primaryValueOutput == null) {
						analizer.RegisterError(n, "This node is not valid in current context because it does not have primary value output");
					}
					else if(n.primaryValueOutput.CanGetValue() == false) {
						analizer.RegisterError(n, "This node is not valid in current context because the value cannot be get / retrieved");
					}
					else if(n.primaryValueOutput.type.IsCastableTo(typeof(bool)) == false) {
						analizer.RegisterError(n, "This node is not valid in current context because it the return type is not bool");
					}
				}
				else {
					if(n.primaryFlowInput == null) {
						analizer.RegisterError(n, "This node is not valid in current context because it does not have primary flow input");
					}
				}
			}
		}

		public bool Validate(Flow flow) {
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var n = container.GetChild(i) as NodeObject;
				if(n == null)
					continue;
				if(n.CanGetValue() && n.ReturnType().IsCastableTo(typeof(bool))) {
					if(!n.primaryValueOutput.GetValue(flow).ConvertTo<bool>()) {
						return false;
					}
				}
			}
			return true;
		}

		public string GenerateFlowCode() {
			string contents = null;
			foreach(var node in GetFlowNodes()) {
				if(node.primaryFlowInput != null) {
					contents += CG.GeneratePort(node.primaryFlowInput).AddLineInFirst();
				}
			}
			return contents;
		}

		public string GenerateConditionCode() {
			string contents = null;
			var count = container.childCount;
			for(int i = 0; i < count; i++) {
				var node = container.GetChild(i) as NodeObject;
				if(node == null)
					continue;
				if(node.primaryValueOutput != null) {
					if(string.IsNullOrEmpty(contents)) {
						contents = CG.GeneratePort(node.primaryValueOutput);
					}
					else {
						contents = CG.And(contents, CG.GeneratePort(node.primaryValueOutput));
					}
				}
			}
			return contents;
		}
	}

	public class BlockContainer : UGraphElement {

	}
}