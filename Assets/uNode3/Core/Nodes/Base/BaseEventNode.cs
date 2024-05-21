using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode {
	/// <summary>
	/// Base class for all event node.
	/// </summary>
	public abstract class BaseEventNode : BaseEntryNode, IEventGenerator {
		public int outputCount = 1;
		public FlowOutput[] outputs { get; set; }

		public override string GetTitle() {
			if(GetType().IsDefined(typeof(EventMenuAttribute), true)) {
				return (GetType().GetCustomAttributes(typeof(EventMenuAttribute), true)[0] as EventMenuAttribute).name;
			}
			return base.GetTitle();
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}

		/// <summary>
		/// Trigger the event so that the event will execute the flows.
		/// </summary>
		/// <param name="instance"></param>
		public void Trigger(GraphInstance instance) {
			{//For live editing
				if(IsValid == false) {
					var validNode = instance.GetValidNode(this);
					if(validNode != null) {
						//We found valid element therefore call Trigger on valid element instead.
						validNode.OnTrigger(instance);
						return;
					}
					//If we reach here it is mean we don't found an valid element.
					return;
				}
			}
			OnTrigger(instance);
		}

		/// <summary>
		/// Trigger the event so that the event will execute the flows.
		/// Note: Use Trigger for callback for support live editing.
		/// </summary>
		protected virtual void OnTrigger(GraphInstance instance) {
			//if(uNodeUtility.isInEditor && GraphDebug.useDebug) {
			//	GraphDebug.FlowNode(owner, owner.GetInstanceID(), this.GetInstanceID(), true);
			//}
			var flows = outputs;
			int nodeCount = flows.Length;
			if(nodeCount > 0) {
				for(int x = 0; x < nodeCount; x++) {
					if(flows[x] != null && flows[x].isConnected) {
						instance.RunState(flows[x]);
					}
				}
			}
		}

		public void Stop(GraphInstance instance) {
			{//For live editing
				if(IsValid == false) {
					var validNode = instance.GetValidNode(this);
					if(validNode != null) {
						//We found valid element therefore call Stop on valid element instead.
						validNode.OnStop(instance);
						return;
					}
					//If we reach here it is mean we don't found an valid element.
					return;
				}
			}
			OnStop(instance);
		}

		/// <summary>
		/// Stop all running outputs.
		/// </summary>
		protected virtual void OnStop(GraphInstance instance) {
			var flows = outputs;
			int nodeCount = flows.Length;
			if(nodeCount > 0) {
				for(int x = 0; x < nodeCount; x++) {
					if(flows[x] != null && flows[x].isConnected) {
						instance.StopState(flows[x]);
					}
				}
			}
		}

		protected override void OnRegister() {
			if(outputCount < 1)
				outputCount = 1;
			outputs = new FlowOutput[outputCount];
			for(int i = 0; i < outputCount; i++) {
				outputs[i] = FlowOutput("flow:" + i).SetName("");
			}
		}

		public virtual string GenerateFlows() {
			string contents = "";
			var flows = outputs;
			if(flows != null && outputs.Length > 0) {
				foreach(var flow in flows) {
					if(flow == null || flow.GetTargetFlow() == null)
						continue;
					try {
						contents += CG.Flow(flow, false);
					}
					catch(Exception ex) {
						Debug.LogException(new GraphException(ex, this), nodeObject.graphContainer as UnityEngine.Object);
					}
				}
			}
			return contents;
		}

		public virtual string GenerateStopFlows() {
			string contents = "";
			var flows = outputs;
			if(flows != null && outputs.Length > 0) {
				foreach(var flow in flows) {
					if(flow == null || flow.GetTargetFlow() == null)
						continue;
					try {
						contents += CG.StopEvent(flow.GetTargetFlow());
					}
					catch(Exception ex) {
						Debug.LogException(new GraphException(ex, this), nodeObject.graphContainer as UnityEngine.Object);
					}
				}
			}
			return contents;
		}

		protected string GenerateRunFlows() {
			if(IsHandledByState()) {
				return CG.WrapWithInformation(CG.If(CG.CompareEventState(nodeObject.GetNodeInParent<Nodes.StateNode>().enter, null), CG.RunEvent(this)), this);
			}
			else {
				return CG.WrapWithInformation(GenerateFlows(), this);
			}
		}

		public virtual void GenerateEventCode() { }

		public bool IsHandledByState() {
			var parent = nodeObject?.parent as NodeObject;
			if(parent != null && parent.node is Nodes.StateNode) {
				return true;
			}
			return false;
		}
	}

	public abstract class BaseComponentEvent : BaseGraphEvent {
		protected IGraphEventHandler eventHandler;

		/// <summary>
		/// Required.
		/// </summary>
		/// <param name="instance"></param>
		public override void OnRuntimeInitialize(GraphInstance instance) {
			eventHandler = nodeObject.GetObjectOrNodeInParent<IGraphEventHandler>();
		}

		protected override void OnTrigger(GraphInstance instance) {
			if(eventHandler == null || eventHandler.CanTrigger(instance)) {
				//Trigger the event when handler is null or when handler CanTrigger is true.
				base.OnTrigger(instance);
			}
		}
	}

	/// <summary>
	/// This is the base class for all graph event.
	/// </summary>
	public abstract class BaseGraphEvent : BaseEventNode {
		protected CG.MData GetMethodData(string name, params Type[] parameterTypes) {
			if(parameterTypes == null) {
				parameterTypes = Array.Empty<Type>();
			}
			var mData = CG.generatorData.GetMethodData(name, parameterTypes);
			if(mData == null) {
				var func = CG.graph.GetFunction(name);
				Type funcType = typeof(void);
				if(func != null) {
					funcType = func.ReturnType();
				}
				mData = CG.generatorData.AddMethod(name, funcType, parameterTypes);
			}
			return mData;
		}

		protected CG.MData DoGenerateCode(string name, ValueOutput[] outputs) {
			var parameterTypes = new Type[outputs.Length];
			for(int i=0;i<outputs.Length;i++) {
				parameterTypes[i] = outputs[i].type;
			}
			var mdata = GetMethodData(name, parameterTypes);
			CG.RegisterUserObject(mdata, this);
			var contents = GenerateRunFlows();
			if(!string.IsNullOrEmpty(contents)) {
				if(outputs.Length > 0) {
					mdata.AddCodeForEvent(
						CG.Flow(
							CG.Flow(outputs.Select((p, index) => CG.DeclareVariable(p, mdata.parameters[index].name, this.outputs))),
							contents
						));
				}
				else {
					mdata.AddCodeForEvent(contents);
				}
			}
			return mdata;
		}

		protected CG.MData DoGenerateCode(string name, Type[] parameterTypes = null) {
			var mdata = GetMethodData(name, parameterTypes);
			CG.RegisterUserObject(mdata, this);
			mdata.AddCodeForEvent(GenerateRunFlows());
			return mdata;
		}
	}

	public abstract class BaseGraphEventWithCustomTarget : BaseGraphEvent {

	}
}