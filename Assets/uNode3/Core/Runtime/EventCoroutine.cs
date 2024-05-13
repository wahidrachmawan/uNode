using System.Collections;
using UnityEngine;
using MaxyGames.UNode;
using System.Collections.Generic;

namespace MaxyGames.Runtime {
	/// <summary>
	/// EventCoroutine is a class to Start coroutine with return data and some function that useful for making event based coroutine.
	/// </summary>
	public class EventCoroutine : CustomYieldInstruction {
		#region Classes
		internal class Processor {
			public IEventIterator target;
			public bool isStopped;

			private object current;
			private bool isNotEnd;
			private bool canMoveNext = true;

			public Processor(IEventIterator target) {
				this.target = target;
			}

			public void Reset() {
				target.Reset();
				current = null;
				canMoveNext = true;
				isStopped = false;
			}

			public void Stop() {
				isStopped = true;
				if(target is IEventStopable stopable) {
					stopable.Stop();
				}
			}

			public bool Process(ref int state) {
				if(isStopped)
					return false;
				if(canMoveNext) {
					isNotEnd = target.MoveNext();
					this.current = target.Current;
				}
				var current = this.current;
				if(current == null) {
					return isNotEnd;
				} else if(current is bool) {
					bool r = (bool)current;
					state = r ? 1 : 2;
					return false;
				} else if(current is string) {
					string r = current as string;
					if(r == "Success") {
						state = 1;
						//Mark process finish
						return false;
					} else if(r == "Failure") {
						state = 2;
						//Mark process finish
						return false;
					}
				} else if(current is CustomYieldInstruction) {
					if((current as CustomYieldInstruction).keepWaiting) {
						canMoveNext = false;
						//Wait for next process.
						return true;
					} else {
						//Immediately process to next
						this.current = null;
						canMoveNext = isNotEnd;
						return Process(ref state);
					}
				} else if(current is WaitSecond) {
					if(((WaitSecond)current).IsTimeExceed()) {
						this.current = null;
						canMoveNext = isNotEnd;
						return Process(ref state);
					} else {
						//Wait for next process.
						return true;
					}
				} else if(current is WaitForSeconds) {
					this.current = new WaitSecond((float)current.GetType().GetFieldCached("m_Seconds").GetValueOptimized(current), false);
					canMoveNext = false;
					return true;
				} else if(current is WaitForEndOfFrame || current is WaitForFixedUpdate) {
					//Wait for next process.
					canMoveNext = isNotEnd;
					return true;
				}
				return isNotEnd;
			}

			struct WaitSecond {
				public float time;
				public bool unscaled;

				public WaitSecond(float time, bool unscaled) {
					if(unscaled) {
						this.time = Time.unscaledTime + time;
					} else {
						this.time = Time.time + time;
					}
					this.unscaled = unscaled;
				}

				public bool IsTimeExceed() {
					if(unscaled) {
						return Time.unscaledTime - time >= 0;
					} else {
						return Time.time - time >= 0;
					}
				}
			}
		}
		#endregion

		private int rawState;

		/// <summary>
		/// The owner of coroutine to start running coroutine
		/// </summary>
		public MonoBehaviour owner;
		private Processor target;
		private Processor onStop;
		private bool hasRun, hasStop;
		private bool run;

		/// <summary>
		/// Indicate state of Coroutine, 
		/// "Success" indicate state is success, 
		/// "Failure" indicate state is failure, 
		/// otherwise indicate state is running or never running
		/// </summary>
		public string state {
			get {
				switch(rawState) {
					case 1:
						return "Success";
					case 2:
						return "Failure";
					default:
						return null;
				}
			}
		}

		/// <summary>
		/// Indicate coroutine is finished running when its has running before
		/// </summary>
		public bool IsFinished {
			get {
				return hasRun && rawState != 0;
			}
		}

		/// <summary>
		/// Indicate coroutine is finished running or never running
		/// </summary>
		public bool IsFinishedOrNeverRun {
			get {
				return rawState != 0;
			}
		}

		/// <summary>
		/// True if the state is "Success"
		/// </summary>
		public bool IsSuccess => rawState == 1;
		/// <summary>
		/// Try if the state is "Failure"
		/// </summary>
		public bool IsFailure => rawState == 2;
		/// <summary>
		/// True if the state is "Running"
		/// </summary>
		public bool IsRunning => rawState == 0 && run;

		public override bool keepWaiting => !IsFinishedOrNeverRun;

		/// <summary>
		/// Create a new event.
		/// </summary>
		/// <returns></returns>
		public static EventCoroutine New() {
			return new EventCoroutine();
		}

		#region Create
		/// <summary>
		/// Create a new event
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static EventCoroutine Create(IEventIterator target) {
			return new EventCoroutine().Setup(target);
		}

		/// <summary>
		/// Create a new event
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public static EventCoroutine Create(MonoBehaviour owner, IEventIterator target) {
			return new EventCoroutine().Setup(owner, target);
		}

		/// <summary>
		/// Create a new event
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static EventCoroutine Create(MonoBehaviour owner, params IEventIterator[] targets) {
			return new EventCoroutine().Setup(owner, Routine.New(targets));
		}

		/// <summary>
		/// Create a new event
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static EventCoroutine Create(MonoBehaviour owner, params EventCoroutine[] targets) {
			return new EventCoroutine().Setup(owner, Routine.New(targets));
		}

		/// <summary>
		/// Create a new event
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public static EventCoroutine Create(MonoBehaviour owner, System.Action target) {
			return new EventCoroutine().Setup(owner, Routine.New(target));
		}

		/// <summary>
		/// Create a new event
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="owner"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public static EventCoroutine Create<T>(MonoBehaviour owner, System.Func<T> target) {
			return new EventCoroutine().Setup(owner, Routine.New(target));
		}
		#endregion

		#region Initializers
		/// <summary>
		/// Initialize Event Coroutine without owner.
		/// </summary>
		/// <param name="target"></param>
		public EventCoroutine Setup(IEventIterator target) {
			this.target = new Processor(target);
			this.owner = RuntimeSMHelper.Instance;
			return this;
		}

		/// <summary>
		/// Initialize Event Coroutine.
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		public EventCoroutine Setup(MonoBehaviour owner, IEventIterator target) {
			this.target = new Processor(target);
			this.owner = owner ?? RuntimeSMHelper.Instance;
			return this;
		}

		/// <summary>
		/// Initialize Event Coroutine
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		/// <param name="onStop"></param>
		public EventCoroutine Setup(MonoBehaviour owner, IEventIterator target, IEventIterator onStop) {
			this.target = new Processor(target);
			this.owner = owner ?? RuntimeSMHelper.Instance;
			this.onStop = new Processor(onStop);
			return this;
		}

		/// <summary>
		/// Initialize Event Coroutine
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		/// <param name="onStop"></param>
		public EventCoroutine Setup(MonoBehaviour owner, IEventIterator target, System.Action onStop) {
			this.target = new Processor(target);
			this.owner = owner ?? RuntimeSMHelper.Instance;
			this.onStop = new Processor(Routine.New(onStop));
			return this;
		}


		/// <summary>
		/// Initialize Event Coroutine without owner.
		/// </summary>
		/// <param name="target"></param>
		public EventCoroutine Setup(IEnumerable target) {
			this.target = new Processor(Routine.New(target));
			this.owner = RuntimeSMHelper.Instance;
			return this;
		}

		/// <summary>
		/// Initialize Event Coroutine.
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		public EventCoroutine Setup(MonoBehaviour owner, IEnumerable target) {
			this.target = new Processor(Routine.New(target));
			this.owner = owner ?? RuntimeSMHelper.Instance;
			return this;
		}

		/// <summary>
		/// Initialize Event Coroutine
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		/// <param name="onStop"></param>
		public EventCoroutine Setup(MonoBehaviour owner, IEnumerable target, System.Action onStop) {
			this.target = new Processor(Routine.New(target));
			this.owner = owner ?? RuntimeSMHelper.Instance;
			this.onStop = new Processor(Routine.New(onStop));
			return this;
		}

		/// <summary>
		/// Initialize Event Coroutine
		/// </summary>
		/// <param name="owner">The coroutine owner</param>
		/// <param name="target">The coroutine function target</param>
		/// <param name="onStop"></param>
		public EventCoroutine Setup(MonoBehaviour owner, IEnumerable target, IEnumerable onStop) {
			this.target = new Processor(Routine.New(target));
			this.owner = owner ?? RuntimeSMHelper.Instance;
			this.onStop = new Processor(Routine.New(onStop));
			return this;
		}

		public EventCoroutine OnStop(IEventIterator target) {
			this.onStop = new Processor(target);
			return this;
		}

		public EventCoroutine OnStop(System.Action target) {
			this.onStop = new Processor(Routine.New(target));
			return this;
		}

		public EventCoroutine OnStop(IEnumerable target) {
			this.onStop = new Processor(Routine.New(target));
			return this;
		}

		public EventCoroutine OnStop(EventCoroutine target) {
			this.onStop = new Processor(Routine.New(target));
			return this;
		}

		public EventCoroutine OnStop<T>(System.Func<T> target) {
			this.onStop = new Processor(Routine.New(target));
			return this;
		}
		#endregion

		/// <summary>
		/// Run the coroutine if not running
		/// </summary>
		/// <returns></returns>
		public EventCoroutine Run() {
			if(!run) {
				if(hasRun) {
					target.Reset();
				}
				rawState = 0;
				run = true;
				hasRun = true;
#if UNITY_EDITOR
				if(debug) {
					DebugEvent();
				}
#endif
				Update();
				if(rawState == 0) {
					//We use the queue because the Update has called on current frame, therefore we need to call Update in next frame.
					URuntimeQueue.Queue(() => {
						UEvent.Unregister(UEventID.Update, owner, Update);
						UEvent.Register(UEventID.Update, owner, Update);
					});
				}
			}
			return this;
		}


		public string id;
		private int processID;

		void Update() {
			if(run) {
				var process = ++processID;
				if(!target.Process(ref rawState)) {
					if(process == processID) {
						bool s = rawState != 2;
						rawState = 0;
						Stop(s);
					}
				}
			}
		}

		void UpdateStop() {
			int state = 0;
			if(!onStop.Process(ref state)) {
				run = false;
				hasStop = true;
				UEvent.Unregister(UEventID.Update, owner, UpdateStop);
			}
		}

		/// <summary>
		/// Stop Running Coroutine.
		/// </summary>
		public void Stop(bool state = false) {
			if(rawState == 0) {
				rawState = state ? 1 : 2;
				run = false;
				if(hasRun) {
					//Ensure to stop only if the event has been run.
					target.Stop();
				}
#if UNITY_EDITOR
				if(debug) {
					DebugEvent();
				}
#endif
				if(onStop != null) {
					if(hasStop)
						onStop.Reset();
					UpdateStop();
					if(rawState == 0)
						UEvent.Register(UEventID.Update, owner, UpdateStop);
				}
			}
		}

		#region Debug
#if UNITY_EDITOR
		private bool debug;
		private int debugNodeID;
		private int debugObjectUID;
		private string debugPortID;
#endif

		/// <summary>
		/// Call this to implement debugging in editor.
		/// </summary>
		/// <param name="nodeUID"></param>
		public void Debug(int objectUID, int nodeUID, string portID) {
#if UNITY_EDITOR
			debug = true;
			debugObjectUID = objectUID;
			debugNodeID = nodeUID;
			debugPortID = portID;
#endif
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private void DebugEvent() {
#if UNITY_EDITOR
			if(!GraphDebug.useDebug || !Application.isPlaying)
				return;
			var objectUID = debugObjectUID;
			var portID = debugPortID;
			var nodeUID = debugNodeID;

			StateType Func() {
				if(string.IsNullOrEmpty(this.state) || !this.IsFinished) {
					return StateType.Running;
				}
				else {
					return this.IsSuccess ? StateType.Success : StateType.Failure;
				}
			}

			if(!GraphDebug.debugData.TryGetValue(objectUID, out var debugMap)) {
				debugMap = new();
				GraphDebug.debugData[objectUID] = debugMap;
			}
			object obj = this.owner;
			if(!debugMap.TryGetValue(obj, out var data)) {
				data = new GraphDebug.DebugData();
				debugMap.AddOrUpdate(obj, data);
			}
			if(portID == null) {
				if(!data.nodeDebug.TryGetValue(nodeUID, out var nodeDebug)) {
					nodeDebug = new GraphDebug.DebugFlow();
					data.nodeDebug[nodeUID] = nodeDebug;
				}
				nodeDebug.calledTime = GraphDebug.debugTime;
				nodeDebug.customCondition = Func;
				if(GraphDebug.Breakpoint.HasBreakpoint(objectUID, nodeUID)) {
					nodeDebug.breakpointTimes = GraphDebug.debugTime;
					UnityEngine.Debug.Break();
				}
			}
			else {
				if(!data.flowDebug.TryGetValue(nodeUID, out var flowData)) {
					flowData = new Dictionary<string, GraphDebug.DebugFlow>();
					data.flowDebug[nodeUID] = flowData;
				}
				if(!flowData.TryGetValue(portID, out var nodeDebug)) {
					nodeDebug = new GraphDebug.DebugFlow();
					flowData[portID] = nodeDebug;
				}
				nodeDebug.calledTime = GraphDebug.debugTime;
				nodeDebug.customCondition = Func;
				if(GraphDebug.Breakpoint.HasBreakpoint(objectUID, nodeUID)) {
					nodeDebug.breakpointTimes = GraphDebug.debugTime;
					UnityEngine.Debug.Break();
				}
			}
#endif
		}
		#endregion
	}
}