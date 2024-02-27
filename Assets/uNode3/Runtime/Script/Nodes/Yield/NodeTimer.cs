using UnityEngine;
using System;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Yield", "Timer", IsCoroutine = true, scope = NodeScope.StateGraph, icon = typeof(TypeIcons.ClockIcon))]
	public class NodeTimer : Node {
		[NonSerialized]
		public FlowInput start;
		[NonSerialized]
		public FlowInput pause;
		[NonSerialized]
		public FlowInput resume;
		[NonSerialized]
		public FlowInput reset;

		[NonSerialized]
		public ValueInput waitTime;
		[NonSerialized]
		public FlowOutput onStart;
		[NonSerialized]
		public FlowOutput onUpdate;
		[NonSerialized]
		public FlowOutput onFinished;

		[NonSerialized]
		public ValueOutput elapsed;
		[NonSerialized]
		public ValueOutput elapsedPercent;
		[NonSerialized]
		public ValueOutput remaining;
		[NonSerialized]
		public ValueOutput remainingPercent;

		class RuntimeData {
			public bool timerOn;
			public float elapsed;
			public float duration;
			public bool paused;

			public Action updateAction;
		}

		protected override void OnRegister() {
			onStart = FlowOutput(nameof(onStart));
			onUpdate = FlowOutput(nameof(onUpdate));
			onFinished = FlowOutput(nameof(onFinished));

			waitTime = ValueInput<float>(nameof(waitTime), 1);

			start = FlowInput(nameof(start), (flow) => {
				var data = flow.GetOrCreateElementData<RuntimeData>(this);
				if(!data.timerOn) {
					data.timerOn = true;
					data.paused = false;
					data.elapsed = 0;
					data.duration = waitTime.GetValue<float>(flow);
					if(onStart.isAssigned) {
						flow.Next(onStart);
					}
					data.updateAction = () => DoUpdate(flow, data);
					UEvent.Register(UEventID.Update, flow.target as Component, data.updateAction);
				}
			});
			pause = FlowInput(nameof(pause), (flow) => {
				var data = flow.GetOrCreateElementData<RuntimeData>(this);
				data.paused = true;
			});
			resume = FlowInput(nameof(resume), (flow) => {
				var data = flow.GetOrCreateElementData<RuntimeData>(this);
				data.paused = false;
			});
			reset = FlowInput(nameof(reset), Reset);

			elapsed = ValueOutput(nameof(elapsed), typeof(float));
			elapsed.AssignGetCallback(instance => {
				var data = instance.GetOrCreateElementData<RuntimeData>(this);
				return data.elapsed;
			});
			elapsedPercent = ValueOutput(nameof(elapsedPercent), typeof(float));
			elapsedPercent.AssignGetCallback(instance => {
				var data = instance.GetOrCreateElementData<RuntimeData>(this);
				return Mathf.Clamp01(data.elapsed / data.duration);
			});
			remaining = ValueOutput(nameof(remaining), typeof(float));
			remaining.AssignGetCallback(instance => {
				var data = instance.GetOrCreateElementData<RuntimeData>(this);
				return Mathf.Max(0, data.duration - data.elapsed);
			});
			remainingPercent = ValueOutput(nameof(remainingPercent), typeof(float));
			remainingPercent.AssignGetCallback(instance => {
				var data = instance.GetOrCreateElementData<RuntimeData>(this);
				return Mathf.Clamp01((data.duration - data.elapsed) / data.duration);
			});
		}

		void Reset(Flow flow) {
			var data = flow.GetOrCreateElementData<RuntimeData>(this);
			data.timerOn = false;
			data.paused = false;
			data.elapsed = 0;
			UEvent.Unregister(UEventID.Update, flow.target as Component, data.updateAction);
		}

		void DoUpdate(Flow flow, RuntimeData data) {
			if(data.timerOn && !data.paused) {
				data.elapsed += Time.deltaTime;
				if(data.elapsed >= data.duration) {
					Reset(flow);
					flow.TriggerParallel(onFinished);
				} else if(onUpdate.isAssigned) {
					flow.TriggerParallel(onUpdate);
				}
			}
		}

		public override void OnGeneratorInitialize() {
			var isActive = CG.RegisterPrivateVariable("timerOn", typeof(bool), false);
			var elapsed = CG.RegisterPrivateVariable("elapsed", typeof(float), 0);
			var paused = CG.RegisterPrivateVariable("paused", typeof(bool), false);
			var duration = CG.RegisterPrivateVariable("duration", typeof(float), 0);

			CG.RegisterPort(start, () => {
				return CG.If(
					isActive.CGNot(),
					CG.Flow(
						isActive.CGSet(true.CGValue()),
						elapsed.CGSet(0.CGValue()),
						paused.CGSet(false.CGValue()),
						duration.CGSet(waitTime.CGValue()),
						onStart.CGFlow(false)
					)
				);
			});
			CG.RegisterPort(pause, () => {
				return CG.Set(paused, true.CGValue());
			});
			CG.RegisterPort(resume, () => {
				return CG.Set(paused, false.CGValue());
			});
			CG.RegisterPort(reset, () => {
				return CG.Flow(
					CG.Set(isActive, false.CGValue()),
					CG.Set(paused, false.CGValue()),
					CG.Set(elapsed, 0.CGValue()),
					CG.Set(duration, 0.CGValue())
				);
			});
			CG.RegisterPort(this.elapsed, () => {
				return elapsed;
			});
			CG.RegisterPort(elapsedPercent, () => {
				return CG.Invoke(typeof(Mathf), nameof(Mathf.Clamp01), CG.Divide(elapsed, duration));
			});
			CG.RegisterPort(remaining, () => {
				return CG.Invoke(typeof(Mathf), nameof(Mathf.Max), CG.Value(0), CG.Subtract(duration, elapsed));
			});
			CG.RegisterPort(remainingPercent, () => {
				return CG.Invoke(typeof(Mathf), nameof(Mathf.Clamp01),
					CG.Divide(
						CG.Wrap(CG.Subtract(duration, elapsed)),
						duration));
			});
			CG.RegisterNodeSetup(this, () => {
				var updateContents =
					CG.If(
						CG.And(
							isActive,
							paused.CGNot()),
						CG.Flow(
							elapsed.CGSet(typeof(Time).CGAccess(nameof(Time.deltaTime)), SetType.Add),
							CG.If(
								elapsed.CGCompare(duration, ComparisonType.GreaterThanOrEqual),
								CG.Flow(
									elapsed.CGSet(0.CGValue()),
									isActive.CGSet(false.CGValue()),
									onFinished.CGFlow(false)
								), onUpdate.CGFlow(false)
							)
						)
					);
				if(CG.includeGraphInformation) {
					//Wrap the update contents with information of this node.
					updateContents = CG.WrapWithInformation(updateContents, this);
				}
				CG.InsertCodeToFunction("Update", typeof(void), updateContents);
			});
		}
	}
}
