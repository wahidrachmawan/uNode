using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public abstract class GraphRuntimeData {
		public readonly GraphInstance instance;

		public IGraph graph => instance.graph;
		public object target => instance.target;
		public RuntimeGraphEventData eventData => instance.eventData;

		public GraphRuntimeData(GraphInstance instance) {
			this.instance = instance;
		}

		#region Local Data
		public abstract void SetLocalData(UGraphElement owner, object value);

		public abstract object GetLocalData(UGraphElement owner);

		public T GetLocalData<T>(UGraphElement owner) {
			return GetLocalData(owner).ConvertTo<T>();
		}

		public abstract void SetLocalData(UGraphElement owner, object key, object value);

		public abstract object GetLocalData(UGraphElement owner, object key);

		public T GetLocalData<T>(UGraphElement owner, object key) {
			return GetLocalData(owner, key).ConvertTo<T>();
		}
		#endregion

		#region Custom Data
		public void SetUserData(UGraphElement owner, object value) {
			instance.SetUserData(owner, value);
		}

		public object GetUserData(UGraphElement owner) => instance.GetUserData(owner);

		public void SetUserData(UGraphElement owner, object key, object value) {
			instance.SetUserData(owner, key, value);
		}

		public object GetUserData(UGraphElement owner, object key) => instance.GetUserData(owner, key);
		#endregion

		#region Utility
		/// <summary>
		/// Get the valid element ( used for support live editing )
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="element"></param>
		/// <returns></returns>
		public T GetValidElement<T>(T element) where T : UGraphElement {
			return instance.GetValidElement<T>(element);
		}

		/// <summary>
		/// Get the valid node ( used for support live editing )
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="element"></param>
		/// <returns></returns>
		public T GetValidNode<T>(T element) where T : Node {
			return instance.GetValidNode<T>(element);
		}

		/// <summary>
		/// Get the valid port ( used for support live editing )
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public ValueOutput GetValidPort(ValueOutput port) {
			return instance.GetValidPort(port);
		}

		/// <summary>
		/// Get the valid port ( used for support live editing )
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public ValueInput GetValidPort(ValueInput port) {
			return instance.GetValidPort(port);
		}

		public ref object GetElementDataByRef(UGraphElement owner) {
			return ref instance.GetElementDataByRef(owner);
		}

		public object GetElementData(UGraphElement owner) {
			return instance.GetElementData(owner);
		}

		public T GetElementData<T>(UGraphElement owner) {
			return instance.GetElementData<T>(owner);
		}

		public void SetElementData(UGraphElement owner, object value) {
			instance.SetElementData(owner, value);
		}

		public T GetOrCreateElementData<T>(UGraphElement owner) where T : new() {
			return instance.GetOrCreateElementData<T>(owner);
		}
		#endregion

		#region Port Datas
		public abstract RuntimeLocalValue GetOrCreateLocalDataValue(NodeObject owner);

		/// <summary>
		/// Set port cached value
		/// </summary>
		/// <param name="port"></param>
		/// <param name="value"></param>
		public void SetPortData(ValueOutput port, object value) {
			GetOrCreateLocalDataValue(port.node).SetOutputData(port.id, value);
		}

		/// <summary>
		/// Get port cached value
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public object GetPortData(ValueOutput port) {
			return GetOrCreateLocalDataValue(port.node).GetOutputData(port.id).value;
		}

		/// <summary>
		/// Get port cached value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <returns></returns>
		public T GetPortData<T>(ValueOutput port) {
			var data = GetOrCreateLocalDataValue(port.node).GetOutputData(port.id);
			if(object.ReferenceEquals(data.value, null))
				return default;
			return (T)data.value;
		}

		/// <summary>
		/// Get or create port cached value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="port"></param>
		/// <returns></returns>
		public T GetOrCreatePortData<T>(ValueOutput port) where T : new() {
			var data = GetOrCreateLocalDataValue(port.node).GetOutputData(port.id);
			if(object.ReferenceEquals(data.value, null)) {
				data.value = new T();
			}
			return (T)data.value;
		}

		/// <summary>
		/// Get port value by reference
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public ref object GetPortDataByRef(ValueOutput port) {
			var data = GetOrCreateLocalDataValue(port.node).GetOutputData(port.id);
			return ref data.value;
		}
		#endregion

		public static implicit operator GraphInstance(GraphRuntimeData data) {
			return data.instance;
		}
	}

	public abstract class Flow : GraphRuntimeData {
		/// <summary>
		/// Get/Set the current state
		/// </summary>
		[NonSerialized]
		public StateType state;
		/// <summary>
		/// Are this event is finished
		/// </summary>
		[NonSerialized]
		protected bool finished;
		/// <summary>
		/// Are this event has called
		/// </summary>
		[NonSerialized]
		protected bool hasCalled;
		public StateType currentState {
			get {
				if(!finished && hasCalled) {
					return StateType.Running;
				}
				return state;
			}
		}

		/// <summary>
		/// Are this event is finished.
		/// </summary>
		/// <returns></returns>
		public bool IsFinished() {
			return finished && state != StateType.Running;
		}

		/// <summary>
		/// Wait this event until finished.
		/// </summary>
		/// <returns></returns>
		public WaitUntil WaitUntilFinish() {
			return new WaitUntil(() => IsFinished());
		}

		public abstract GraphRunner graphRunner { get; }

		public Flow(GraphInstance instance) : base(instance) { }

		public virtual JumpStatement jumpStatement { get; set; }

		#region Local Data
		public override void SetLocalData(UGraphElement owner, object value) {
			graphRunner.SetLocalData(owner, value);
		}

		public override object GetLocalData(UGraphElement owner) => graphRunner.GetLocalData(owner);

		public override void SetLocalData(UGraphElement owner, object key, object value) {
			graphRunner.SetLocalData(owner, key, value);
		}

		public override object GetLocalData(UGraphElement owner, object key) => graphRunner.GetLocalData(owner, key);

		public override RuntimeLocalValue GetOrCreateLocalDataValue(NodeObject owner) => graphRunner.GetOrCreateLocalDataValue(owner);
		#endregion

		public abstract void Next(FlowPort port);


		public void TriggerCoroutine(FlowOutput output, out IEnumerator waitUntil) {
			TriggerCoroutine(output, out waitUntil, out _);
		}

		public void TriggerCoroutine(FlowOutput output, out IEnumerator waitUntil, out Func<JumpStatement> jumpStatement) {
#if UNITY_EDITOR
			if(output.node.IsValidElement() == false) {
				//For live editing
				var validPort = instance.GetValidPort(output);
				if(validPort != null) {
					//We found the valid port, redirect the invalid to valid value.
					TriggerCoroutine(validPort, out waitUntil, out jumpStatement);
					return;
				}
				else {
					//Log to console if the valid node is not found.
					Debug.Log(GraphException.GetMessage("Live editing: trying to execute invalid flow output." +
						"\nNode: " + output.node.GetTitle() + " - id:" + output.node.id +
						"\nPort: " + output.GetPrettyName(), output.node));
				}
			}
#endif
			OnTriggerCoroutine(output, out waitUntil, out jumpStatement);
		}


		public void Trigger(FlowOutput output) {
			Trigger(output, out _);
		}

		public void Trigger(FlowOutput output, out JumpStatement jump) {
#if UNITY_EDITOR
			if(output.node.IsValidElement() == false) {
				//For live editing
				var validPort = instance.GetValidPort(output);
				if(validPort != null) {
					//We found the valid port, redirect the invalid to valid value.
					Trigger(validPort, out jump);
					return;
				}
				else {
					//Log to console if the valid node is not found.
					Debug.Log(new GraphException("Live editing: trying to execute invalid flow output." +
						"\nNode: " + output.node.GetTitle() + " - id:" + output.node.id +
						"\nPort: " + output.GetPrettyName(), output.node));
				}
			}
#endif
			OnTrigger(output, out jump);
		}

		public void TriggerParallel(FlowOutput output) {
#if UNITY_EDITOR
			if(output.node.IsValidElement() == false) {
				//For live editing
				var validPort = instance.GetValidPort(output);
				if(validPort != null) {
					//We found the valid port, redirect the invalid to valid value.
					TriggerParallel(validPort);
					return;
				}
				else {
					//Log to console if the valid node is not found.
					Debug.Log(new GraphException("Live editing: trying to execute invalid flow output." +
						"\nNode: " + output.node.GetTitle() + " - id:" + output.node.id +
						"\nPort: " + output.GetPrettyName(), output.node));
				}
			}
#endif
			OnTriggerParallel(output);
		}

		protected abstract void OnTrigger(FlowOutput output, out JumpStatement jump);
		protected abstract void OnTriggerCoroutine(FlowOutput output, out IEnumerator waitUntil, out Func<JumpStatement> jumpStatement);
		protected abstract void OnTriggerParallel(FlowOutput output);

		public abstract void Stop();

		/// <summary>
		/// The next flow.
		/// </summary>
		/// <param name="nexts"></param>
		public void Next(params FlowOutput[] nexts) {
			for(int i = 0; i < nexts.Length; i++) {
				Next((FlowPort)nexts[i]);
			}
		}
	}

	public abstract class GraphRunner : GraphRuntimeData {
		public GraphRunner(GraphInstance instance) : base(instance) { }

		internal class NestedRunners : IEnumerator {
			public List<IEnumerator> enumerators = new List<IEnumerator>();

			public NestedRunners() { }

			public NestedRunners(IEnumerator enumerator) {
				enumerators.Add(enumerator);
			}

			public object Current { get; private set; }

			public bool MoveNext() {
				var target = enumerators[enumerators.Count - 1];
				bool flag = target.MoveNext();
				Current = target.Current;
				if(Current is bool) {
					return false;
				}
				if(flag) {
					if(Current is IEnumerator && !(Current is CustomYieldInstruction)) {
						enumerators.Add(Current as IEnumerator);
						return MoveNext();
					}
				}
				else {
					enumerators.RemoveAt(enumerators.Count - 1);
					if(enumerators.Count > 0) {
						return MoveNext();
					}
				}
				return enumerators.Count > 0;
			}

			public void Reset() {

			}
		}

		private Dictionary<RuntimeGraphID, object> localDatas = new Dictionary<RuntimeGraphID, object>();
		private Dictionary<(RuntimeGraphID, object), object> localDatas2 = new Dictionary<(RuntimeGraphID, object), object>();

		public override void SetLocalData(UGraphElement owner, object value) {
			localDatas[owner.runtimeID] = value;
		}

		public override object GetLocalData(UGraphElement owner) {
			if(!localDatas.TryGetValue(owner.runtimeID, out var data)) {
				localDatas[owner.runtimeID] = data;
			}
			return data;
		}

		public override void SetLocalData(UGraphElement owner, object key, object value) {
			RuntimeGraphID id;
			if(object.ReferenceEquals(owner, null) == false)
				id = owner.runtimeID;
			else
				id = default;
			localDatas2[(id, key)] = value;
		}

		public override object GetLocalData(UGraphElement owner, object key) {
			RuntimeGraphID id;
			if(object.ReferenceEquals(owner, null) == false)
				id = owner.runtimeID;
			else
				id = default;
			if(!localDatas2.TryGetValue((id, key), out var data)) {
				localDatas2[(id, key)] = data;
			}
			return data;
		}

		private Dictionary<RuntimeGraphID, RuntimeLocalValue> elementDatas = new Dictionary<RuntimeGraphID, RuntimeLocalValue>();

		public override RuntimeLocalValue GetOrCreateLocalDataValue(NodeObject owner) {
			var id = owner.runtimeID;
			if(!elementDatas.TryGetValue(id, out var data)) {
				data = new RuntimeLocalValue(this, owner);
				elementDatas[id] = data;
			}
			return data;
		}
	}

	public class StateFlow : Flow {
		public readonly FlowInput port;

		public StateGraphRunner runner => instance.stateRunner;
		public NodeObject node => port.node;

		public override GraphRunner graphRunner => runner;

		public StateFlow(GraphInstance instance, FlowInput port) : base(instance) {
			this.port = port;
		}

		[NonSerialized]
		private Coroutine coroutine;
		[NonSerialized]
		private Coroutine finishCoroutine;
		[NonSerialized]
		private List<FlowPort> nextFlows;

		public bool IsCoroutine => port != null ? port.IsCoroutine() : false;

		public void Run() {
			if(hasCalled && !IsFinished())
				return;
			jumpStatement = null;
			if(!hasCalled)
				hasCalled = true;
			finished = false;
			state = StateType.Running;
			if(nextFlows == null)
				nextFlows = new List<FlowPort>();
			if(finishCoroutine != null) {
				instance.StopCoroutine(finishCoroutine);
				finishCoroutine = null;
			}
#if UNITY_EDITOR
			if(GraphDebug.useDebug) {
				GraphDebug.FlowNode(target, port.node.graphContainer.GetGraphID(), node.id, port.isPrimaryPort ? null : port.id, state == StateType.Success ? true : state == StateType.Failure ? false : null);
			}
#endif
			try {
				if(IsCoroutine && port.actionCoroutine != null) {
					if(target is MonoBehaviour mb) {
						coroutine = mb.StartCoroutine(Iterator());
					}
					else {
						throw new GraphException("The target must inherit from MonoBehaviour or it's sub classes.", node);
					}
				}
				else {
					port.action?.Invoke(this);
					Finish();
				}
			}
			catch(Exception ex) {
				//Ensure the state stop on error.
				if(state == StateType.Running)
					state = StateType.Failure;
				finished = true;
				AfterFinish();
				throw new GraphException(ex, node);
			}
		}

		public override void Stop() {
#if UNITY_EDITOR
			if(node.IsValidElement() == false) {
				//For live editing
				var validPort = instance.GetValidPort(port);
				if(validPort != null) {
					//We found the valid port, redirect the invalid to valid value.
					DoStop(validPort);
					return;
				}
				else {
					//Log to console if the valid node is not found.
					Debug.Log(new GraphException("Live editing: trying to stop invalid flow output." +
						"\nNode: " + node.GetTitle() + " - id:" + node.id +
						"\nPort: " + port.GetPrettyName(), node));
				}
			}
#endif
			DoStop(port);
		}

		private void DoStop(FlowInput port) {
			//Check if this node is still running
			if(state == StateType.Running || hasCalled && !finished) {
				//Mark this node to finish
				finished = true;
				//Change state to failure.
				state = StateType.Failure;
				if(coroutine != null) {
					instance.StopCoroutine(coroutine);
					coroutine = null;
#if UNITY_EDITOR
					if(uNodeUtility.isPlaying && GraphDebug.useDebug) {
						GraphDebug.FlowNode(target, port.node.graphContainer.GetGraphID(), node.id, port.isPrimaryPort ? null : port.id, state == StateType.Success ? true : state == StateType.Failure ? false : null);
					}
#endif
				}
				nextFlows.Clear();
				if(finishCoroutine != null) {
					instance.StopCoroutine(finishCoroutine);
					finishCoroutine = null;
				}
				port.actionOnStopped?.Invoke(this);
			}
		}

		private void Finish() {
			if(nextFlows.Count > 0) {
				for(int i = 0; i < nextFlows.Count; i++) {
					if(nextFlows[i] is FlowOutput fOut) {
#if UNITY_EDITOR
						if(fOut.node.IsValidElement() == false) {
							//For live editing
							var validPort = instance.GetValidPort(fOut);
							if(validPort != null) {
								//We found the valid port, redirect the invalid to valid value.
								fOut = validPort;
							}
							else {
								//Log to console if the valid node is not found.
								Debug.Log(GraphException.GetMessage("Live editing: trying to execute invalid flow output." +
									"\nNode: " + fOut.node.GetTitle() + " - id:" + fOut.node.id +
									"\nPort: " + fOut.GetPrettyName(), fOut.node));
							}
						}
#endif
						this.TriggerCoroutine(fOut, out var wait, out var jump);
						if(wait != null) {
							finishCoroutine = instance.StartCoroutine(Finish(i, wait), node);
							return;
						}
						var js = jump();
						if(js != null) {
							jumpStatement = js;
							break;
						}
					}
					else if(nextFlows[i] is FlowInput fIn) {
#if UNITY_EDITOR
						if(fIn.node.IsValidElement() == false) {
							//For live editing
							var validPort = instance.GetValidPort(fIn);
							if(validPort != null) {
								//We found the valid port, redirect the invalid to valid value.
								fIn = validPort;
							}
							else {
								//Log to console if the valid node is not found.
								Debug.Log(new GraphException("Live editing: trying to execute invalid flow input." +
									"\nNode: " + fIn.node.GetTitle() + " - id:" + fIn.node.id +
									"\nPort: " + fIn.GetPrettyName(), fIn.node));
							}
						}
#endif
						var nextFlow = runner.GetStateData(fIn);
						nextFlow.Run();
						if(!nextFlow.IsFinished()) {
							var wait = nextFlow.WaitUntilFinish();
							if(wait != null) {
								finishCoroutine = instance.StartCoroutine(Finish(i, wait), node);
								return;
							}
							return;
						}
						var js = nextFlow.jumpStatement;
						if(js != null) {
							jumpStatement = js;
							break;
						}
					}
					else {
						if(nextFlows[i] == null) {
							throw new GraphException("Error on trying to execute null flow", node);
						}
						else {
							throw new GraphException("Invalid flow: " + nextFlows[i], node);
						}
					}
				}
			}
			AfterFinish();
		}

		private IEnumerator Finish(int startFlow, IEnumerator firstWait) {
			if(startFlow >= 0) {
				if(firstWait != null) {
					while(firstWait.MoveNext()) {
						var current = firstWait.Current;
						yield return current;
					}
				}
				var js = instance.GetStateData(nextFlows[startFlow])?.jumpStatement;
				if(js != null) {
					jumpStatement = js;
					finishCoroutine = null;
					AfterFinish();
					yield break;
				}
				startFlow++;
			}
			else {
				startFlow = 0;
			}
			for(int i = startFlow; i < nextFlows.Count; i++) {
				if(nextFlows[i] is FlowOutput fOut) {
#if UNITY_EDITOR
					if(fOut.node.IsValidElement() == false) {
						//For live editing
						var validPort = instance.GetValidPort(fOut);
						if(validPort != null) {
							//We found the valid port, redirect the invalid to valid value.
							fOut = validPort;
						}
						else {
							//Log to console if the valid node is not found.
							Debug.Log(new GraphException("Live editing: trying to execute invalid flow output." +
								"\nNode: " + fOut.node.GetTitle() + " - id:" + fOut.node.id +
								"\nPort: " + fOut.GetPrettyName(), fOut.node));
						}
					}
#endif
					this.TriggerCoroutine(fOut, out var wait, out var jump);
					if(wait != null) {
						yield return wait;
					}
					var js = jump();
					if(js != null) {
						jumpStatement = js;
						break;
					}
				}
				else if(nextFlows[i] is FlowInput fIn) {
#if UNITY_EDITOR
					if(fIn.node.IsValidElement() == false) {
						//For live editing
						var validPort = instance.GetValidPort(fIn);
						if(validPort != null) {
							//We found the valid port, redirect the invalid to valid value.
							fIn = validPort;
						}
						else {
							//Log to console if the valid node is not found.
							Debug.Log(new GraphException("Live editing: trying to execute invalid flow input." +
								"\nNode: " + fIn.node.GetTitle() + " - id:" + fIn.node.id +
								"\nPort: " + fIn.GetPrettyName(), fIn.node));
						}
					}
#endif
					var nextFlow = runner.GetStateData(fIn);
					nextFlow.Run();
					if(!nextFlow.IsFinished()) {
						var wait = nextFlow.WaitUntilFinish();
						if(wait != null) {
							yield return wait;
						}
					}
					var js = nextFlow.jumpStatement;
					if(js != null) {
						jumpStatement = js;
						break;
					}
				}
				else {
					if(nextFlows[i] == null) {
						throw new GraphException("Error on trying to execute null flow", node);
					}
					else {
						throw new GraphException("Invalid flow: " + nextFlows[i], node);
					}
				}
			}
			finishCoroutine = null;
			AfterFinish();
		}

		private void AfterFinish() {
			port.actionOnExit?.Invoke(this);
			coroutine = null;
			if(finishCoroutine != null) {
				instance.StopCoroutine(finishCoroutine);
				finishCoroutine = null;
			}
			finished = true;
			nextFlows.Clear();
			if(state == StateType.Running) {
				state = StateType.Success;
			}
#if UNITY_EDITOR
			if(uNodeUtility.isPlaying && GraphDebug.useDebug) {
				GraphDebug.FlowNode(target, port.node.graphContainer.GetGraphID(), node.id, port.isPrimaryPort ? null : port.id, state == StateType.Success ? true : state == StateType.Failure ? false : null);
			}
#endif
		}

		private IEnumerator Iterator() {
			var target = port.actionCoroutine(this);
			while(target.MoveNext()) {
				var current = target.Current;
				if(current is CustomYieldInstruction) {
					goto DEFAULT;
				}
				else if(current is IEnumerable) {
					var runner = new GraphRunner.NestedRunners((current as IEnumerable).GetEnumerator());
					while(runner.MoveNext()) {
						current = runner.Current;
						if(current is bool) {
							state = (bool)current ? StateType.Success : StateType.Failure;
							goto FINISH;
						}
						yield return current;
					}
					current = runner.Current;
					if(current is bool) {
						state = (bool)current ? StateType.Success : StateType.Failure;
						goto FINISH;
					}
					continue;
				}
				else if(current is IEnumerator) {
					var runner = new GraphRunner.NestedRunners(current as IEnumerator);
					while(runner.MoveNext()) {
						current = runner.Current;
						if(current is bool) {
							state = (bool)current ? StateType.Success : StateType.Failure;
							goto FINISH;
						}
						yield return current;
					}
					current = runner.Current;
					if(current is bool) {
						state = (bool)current ? StateType.Success : StateType.Failure;
						goto FINISH;
					}
					continue;
				}
				else {
					goto DEFAULT;
				}
			DEFAULT:
				if(current is bool) {
					state = (bool)current ? StateType.Success : StateType.Failure;
					Finish();
					yield break;
				}
				yield return current;
			}
		FINISH:
			target = Finish(-1, null);
			while(target.MoveNext()) {
				var current = target.Current;
				yield return current;
			}
		}

		public IEnumerator TriggerFlowCoroutine(params FlowOutput[] flows) {
			for(int i = 0; i < flows.Length; i++) {
				this.TriggerCoroutine(flows[i], out var wait, out var jump);
				if(wait != null) {
					yield return wait;
				}
				var js = jump();
				if(js != null) {
					jumpStatement = js;
					yield break;
				}
			}
			yield break;
		}

		public override void Next(FlowPort port) {
			nextFlows.Add(port);
		}

		protected override void OnTrigger(FlowOutput output, out JumpStatement jump) {
			var flow = instance.RunState(output);
			if(flow != null) {
				if(!flow.IsFinished()) {
					throw new GraphException("The target flow is a coroutine, therefore needed to triggerer with TriggerCoroutine or TriggerParallel.", flow.node);
				}
				jump = flow.jumpStatement;
			}
			else {
				jump = null;
			}
		}

		protected override void OnTriggerParallel(FlowOutput output) {
			instance.RunState(output);
		}

		protected override void OnTriggerCoroutine(FlowOutput output, out IEnumerator waitUntil, out Func<JumpStatement> jumpStatement) {
			var flow = instance.RunState(output);
			if(flow != null) {
				if(!flow.IsFinished()) {
					waitUntil = flow.WaitUntilFinish();
				}
				else {
					waitUntil = null;
				}
				jumpStatement = () => flow.jumpStatement;
			}
			else {
				waitUntil = null;
				jumpStatement = () => null;
			}
		}
	}

	public class StateGraphRunner : GraphRunner {
		public Flow defaultFlow;

		public StateGraphRunner(GraphInstance instance) : base(instance) {
			defaultFlow = new StateFlow(instance, null);
		}

		private Dictionary<FlowInput, StateFlow> datas = new Dictionary<FlowInput, StateFlow>();

		public void Run(FlowInput port) {
			GetStateData(port).Run();
		}

		public void Stop(FlowInput port) {
			GetStateData(port).Stop();
		}

		public StateFlow GetStateData(FlowInput port) {
			if(!datas.TryGetValue(port, out var result)) {
				result = new StateFlow(instance, port);
				datas[port] = result;
			}
			return result;
		}
	}

	public class CoroutineFlow : Flow {
		public readonly FlowInput port;
		public readonly CoroutineGraphRunner runner;

		public CoroutineFlow(FlowInput port, CoroutineGraphRunner runner) : base(runner.instance) {
			this.port = port;
			this.runner = runner;
		}

		public override GraphRunner graphRunner => runner;

		private Queue<FlowPort> nextFlows = new Queue<FlowPort>(4);

		public override void Next(FlowPort port) {
			nextFlows.Enqueue(port);
		}

		public IEnumerator GetIterator() {
			BeforeRun();
			var targetFlow = port;
			if(targetFlow.IsCoroutine() && targetFlow.actionCoroutine != null) {
				var target = targetFlow.actionCoroutine(this);
				while(target.MoveNext()) {
					var current = target.Current;
					if(current is CustomYieldInstruction) {
						yield return current;
					}
					else if(current is IEnumerable) {
						var runner = new GraphRunner.NestedRunners((current as IEnumerable).GetEnumerator());
						while(runner.MoveNext()) {
							if(jumpStatement != null) {
								goto FINISH;
							}
							current = runner.Current;
							yield return current;
						}
					}
					else if(current is IEnumerator) {
						var runner = new GraphRunner.NestedRunners(current as IEnumerator);
						while(runner.MoveNext()) {
							if(jumpStatement != null) {
								goto FINISH;
							}
							current = runner.Current;
							yield return current;
						}
					}
					else {
						yield return current;
					}
					if(jumpStatement != null) {
						goto FINISH;
					}
				}
FINISH:
				if(jumpStatement != null) {
					AfterRun();
					yield break;
				}
			}
			else {
				targetFlow.action?.Invoke(this);
			}
			while(nextFlows.TryDequeue(out var nextPort)) {
				FlowInput nextFlow = null;
				if(nextPort is FlowInput input) {
					nextFlow = input;
				}
				else if(nextPort is FlowOutput output) {
#if UNITY_EDITOR
					if(GraphDebug.useDebug && output != null) {
						var node = output.node;
						GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
					}
#endif
					nextFlow = output.GetTargetFlow();
				}
				if(nextFlow == null) {
					continue;
				}
				var flow = new CoroutineFlow(nextFlow, runner);
				var target = flow.GetIterator();
				while(target.MoveNext()) {
					var current = target.Current;
					if(current is CustomYieldInstruction) {
						yield return current;
					}
					else if(current is IEnumerable) {
						var runner = new GraphRunner.NestedRunners((current as IEnumerable).GetEnumerator());
						while(runner.MoveNext()) {
							if(flow.jumpStatement != null) {
								goto FINISH;
							}
							current = runner.Current;
							yield return current;
						}
						continue;
					}
					else if(current is IEnumerator) {
						var runner = new GraphRunner.NestedRunners(current as IEnumerator);
						while(runner.MoveNext()) {
							if(flow.jumpStatement != null) {
								goto FINISH;
							}
							current = runner.Current;
							yield return current;
						}
						continue;
					}
					else {
						yield return current;
					}
					if(flow.jumpStatement != null) {
						goto FINISH;
					}
				}
FINISH:
				if(flow.jumpStatement != null) {
					jumpStatement = flow.jumpStatement;
					break;
				}
			}
			AfterRun();
		}

		private void AfterRun() {
			port.actionOnExit?.Invoke(this);
			finished = true;
			nextFlows.Clear();
			if(state == StateType.Running) {
				state = StateType.Success;
			}
#if UNITY_EDITOR
			if(uNodeUtility.isPlaying && GraphDebug.useDebug) {
				var node = port.node;
				GraphDebug.FlowNode(target, node.graphContainer.GetGraphID(), node.id, port.isPrimaryPort ? null : port.id, state == StateType.Success ? true : state == StateType.Failure ? false : null);
			}
#endif
		}

		private void BeforeRun() {
			if(hasCalled && !IsFinished())
				return;
			jumpStatement = null;
			if(!hasCalled)
				hasCalled = true;
			finished = false;
			state = StateType.Running;
			nextFlows.Clear();
#if UNITY_EDITOR
			if(uNodeUtility.isPlaying && GraphDebug.useDebug) {
				var node = port.node;
				GraphDebug.FlowNode(target, port.node.graphContainer.GetGraphID(), node.id, port.isPrimaryPort ? null : port.id, state == StateType.Success ? true : state == StateType.Failure ? false : null);
			}
#endif
		}

		protected override void OnTrigger(FlowOutput output, out JumpStatement jump) {
			var targetFlow = output.GetTargetFlow();
			if(targetFlow != null) {
#if UNITY_EDITOR
				if(GraphDebug.useDebug && output != null) {
					var node = output.node;
					GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
				}
#endif
				var flow = new CoroutineFlow(targetFlow, runner);
				var iterator = flow.GetIterator();
				if(iterator.MoveNext()) {
					throw new GraphException("The target flow is a coroutine, therefore needed to triggerer with TriggerCoroutine or TriggerParallel.", targetFlow.node);
				}
				jump = flow.jumpStatement;
			}
			else {
				jump = null;
			}
		}

		protected override void OnTriggerCoroutine(FlowOutput output, out IEnumerator waitUntil, out Func<JumpStatement> jumpStatement) {
			var targetFlow = output.GetTargetFlow();
			if(targetFlow != null) {
#if UNITY_EDITOR
				if(GraphDebug.useDebug && output != null) {
					var node = output.node;
					GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
				}
#endif
				var flow = new CoroutineFlow(targetFlow, runner);
				waitUntil = flow.GetIterator();
				jumpStatement = () => flow.jumpStatement;
			}
			else {
				waitUntil = null;
				jumpStatement = () => null;
			}
		}

		protected override void OnTriggerParallel(FlowOutput output) {
			var targetFlow = output.GetTargetFlow();
			if(targetFlow != null) {
#if UNITY_EDITOR
				if(GraphDebug.useDebug && output != null) {
					var node = output.node;
					GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
				}
#endif
				var flow = new CoroutineFlow(targetFlow, runner);
				var iterator = flow.GetIterator();
				if(iterator.MoveNext()) {
					throw new GraphException("The target flow is a coroutine, therefore it is not supported to run with TriggerParallel.", targetFlow.node);
				}
			}
		}

		public override void Stop() {
			throw new NotSupportedException();
		}
	}

	public class CoroutineGraphRunner : GraphRunner {
		public readonly FlowInput port;

		public CoroutineGraphRunner(GraphInstance instance, FlowInput port) : base(instance) {
			this.port = port;
		}

		public CoroutineFlow NewCoroutine() => new CoroutineFlow(port, this);
	}

	public class RegularGraphRunner : GraphRunner {
		public readonly FlowInput port;

		public RegularGraphRunner(GraphInstance instance, FlowInput port) : base(instance) {
			this.port = port;
		}

		public RegularFlow New() => new RegularFlow(port, this);
	}

	public class RegularFlow : Flow {
		public readonly FlowInput port;
		public readonly RegularGraphRunner runner;

		public RegularFlow(FlowInput port, RegularGraphRunner runner) : base(runner.instance) {
			this.port = port;
			this.runner = runner;
		}

		public override GraphRunner graphRunner => runner;

		private Queue<FlowPort> nextFlows = new Queue<FlowPort>(4);

		public override void Next(FlowPort port) {
			nextFlows.Enqueue(port);
		}

		public void Run() {
			try {
				BeforeRun();
				var targetFlow = port;
				targetFlow.action?.Invoke(this);
				if(jumpStatement != null) {
					AfterRun();
					return;
				}
				while(nextFlows.TryDequeue(out var nextPort)) {
					FlowInput nextFlow = null;
					if(nextPort is FlowInput input) {
						nextFlow = input;
					}
					else if(nextPort is FlowOutput output) {
#if UNITY_EDITOR
						if(GraphDebug.useDebug && output != null) {
							var node = output.node;
							GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
						}
#endif
						nextFlow = output.GetTargetFlow();
					}
					if(nextFlow == null) {
						continue;
					}
					var flow = new RegularFlow(nextFlow, runner);
					flow.Run();
					if(flow.jumpStatement != null) {
						jumpStatement = flow.jumpStatement;
						break;
					}
				}
				AfterRun();
			}
			catch (Exception ex) {
				if(ex is not GraphException) {
					throw new GraphException(ex, this.port.node);
				}
				else {
					throw;
				}
			}
		}

		private void AfterRun() {
			port.actionOnExit?.Invoke(this);
			finished = true;
			nextFlows.Clear();
			if(state == StateType.Running) {
				state = StateType.Success;
			}
#if UNITY_EDITOR
			if(uNodeUtility.isPlaying && GraphDebug.useDebug) {
				var node = port.node;
				GraphDebug.FlowNode(target, node.graphContainer.GetGraphID(), node.id, port.isPrimaryPort ? null : port.id, state == StateType.Success ? true : state == StateType.Failure ? false : null);
			}
#endif
		}

		private void BeforeRun() {
			if(hasCalled && !IsFinished())
				return;
			jumpStatement = null;
			if(!hasCalled)
				hasCalled = true;
			finished = false;
			state = StateType.Running;
			nextFlows.Clear();
#if UNITY_EDITOR
			if(uNodeUtility.isPlaying && GraphDebug.useDebug) {
				var node = port.node;
				GraphDebug.FlowNode(target, port.node.graphContainer.GetGraphID(), node.id, port.isPrimaryPort ? null : port.id, state == StateType.Success ? true : state == StateType.Failure ? false : null);
			}
#endif
		}

		protected override void OnTrigger(FlowOutput output, out JumpStatement jump) {
			var targetFlow = output.GetTargetFlow();
			if(targetFlow != null) {
#if UNITY_EDITOR
				if(GraphDebug.useDebug && output != null) {
					var node = output.node;
					GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
				}
#endif
				var flow = new RegularFlow(targetFlow, runner);
				flow.Run();
				jump = flow.jumpStatement;
			}
			else {
				jump = null;
			}
		}

		protected override void OnTriggerCoroutine(FlowOutput output, out IEnumerator waitUntil, out Func<JumpStatement> jumpStatement) {
			throw new GraphException("Cannot run coroutine on non-coroutine flow at " + port.ToString(), port.node);

//			var targetFlow = output.GetTargetFlow();
//			if(targetFlow != null) {
//#if UNITY_EDITOR
//				if(GraphDebug.useDebug && output != null) {
//					var node = output.node;
//					GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
//				}
//#endif
//				var flow = new RegularFlow(targetFlow, runner);
//				waitUntil = null;
//				jumpStatement = () => flow.jumpStatement;
//			}
//			else {
//				waitUntil = null;
//				jumpStatement = () => null;
//			}
		}

		protected override void OnTriggerParallel(FlowOutput output) {
			var targetFlow = output.GetTargetFlow();
			if(targetFlow != null) {
#if UNITY_EDITOR
				if(GraphDebug.useDebug && output != null) {
					var node = output.node;
					GraphDebug.Flow(instance.target, node.graphContainer.GetGraphID(), node.id, output.id);
				}
#endif
				var flow = new RegularFlow(targetFlow, runner);
				flow.Run();
			}
		}

		public override void Stop() {
			throw new NotSupportedException();
		}
	}
}