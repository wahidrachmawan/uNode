using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using MaxyGames.UNode;

namespace MaxyGames.Runtime {
	public interface IEventIterator : IEnumerator { }

	public interface IEventStopable {
		void Stop();
	}

	public static class Routine {
		public static IEventIterator New(IEnumerable iterator) {
			return new RoutineIterable(iterator);
		}

		public static IEventIterator New(System.Action action) {
			return new RoutineAction(action);
		}

		public static IEventIterator New(System.Func<object> func) {
			return new RoutineFunc<object>(func);
		}

		public static IEventIterator New<T>(System.Func<T> func) {
			return new RoutineFunc<T>(func);
		}

		public static IEventIterator New(params System.Func<object>[] func) {
			return new RoutineFuncs<object>(func);
		}

		public static IEventIterator New<T>(params System.Func<T>[] func) {
			return new RoutineFuncs<T>(func);
		}

		public static IEventIterator New(EventCoroutine events) {
			return new RoutineEvent(events);
		}

		public static IEventIterator New(params EventCoroutine[] events) {
			return new RoutineEvents(events);
		}

		public static IEventIterator New(params IEventIterator[] flows) {
			return new RoutineFlow(flows);
		}

		public static IEventIterator Yield(IEnumerable iterator) {
			return new RoutineIterable(iterator);
		}

		public static IEventIterator Yield(System.Func<object> func) {
			return new RoutineYieldable<object>(func);
		}

		public static IEventIterator Yield<T>(System.Func<T> func) {
			return new RoutineYieldable<T>(func);
		}

		/// <summary>
		/// Wait for the given amount of seconds using scaled time
		/// </summary>
		/// <param name="waitTime"></param>
		/// <returns></returns>
		public static IEventIterator Wait(Func<float> waitTime) {
			return new WaitIterator(waitTime);
		}

		/// <summary>
		/// Wait for the given amount of seconds using unscaled time
		/// </summary>
		/// <param name="waitTime"></param>
		/// <returns></returns>
		public static IEventIterator WaitRealtime(Func<float> waitTime) {
			return new WaitRealtimeIterator(waitTime);
		}

		/// <summary>
		/// Wait for the given amount of seconds using scaled time
		/// </summary>
		/// <param name="waitTime"></param>
		/// <returns></returns>
		public static IEventIterator Wait(float waitTime) {
			return new WaitIterator(() => waitTime);
		}

		/// <summary>
		/// Wait for the given amount of seconds using unscaled time
		/// </summary>
		/// <param name="waitTime"></param>
		/// <returns></returns>
		public static IEventIterator WaitRealtime(float waitTime) {
			return new WaitRealtimeIterator(() => waitTime);
		}

		/// <summary>
		/// Wait until predicate evaluate to true.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public static IEventIterator WaitUntil(Func<bool> predicate) {
			return new WaitUntil(predicate);
		}

		/// <summary>
		/// Wait until predicate evaluate to false.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public static IEventIterator WaitWhile(Func<bool> predicate) {
			return new WaitWhile(predicate);
		}

		public static IEventIterator Event(IEnumerable iterator) {
			return new RoutineIterable(iterator);
		}

		public static IEventIterator Event(Func<EventCoroutine> evt) {
			return new RoutineEventFunc(evt);
		}

		public static IEventIterator Event(EventCoroutine events) {
			return new RoutineEvent(events);
		}

		public static IEventIterator Event(params EventCoroutine[] events) {
			return new RoutineEvents(events);
		}
	}

	#region Common Iteration
	class WaitIterator : IEventIterator {
		private readonly Func<float> waitTime;
		private bool run;
		private float time;

		public WaitIterator(Func<float> waitTime) {
			this.waitTime = waitTime;
		}

		public object Current { get; }

		public bool MoveNext() {
			if(!run) {
				run = true;
				time = Time.time + waitTime();
			}
			return Time.time - time < 0;
		}

		public void Reset() {
			run = false;
		}
	}

	class WaitRealtimeIterator : IEventIterator {
		private readonly Func<float> waitTime;
		private bool run;
		private float time;

		public WaitRealtimeIterator(Func<float> waitTime) {
			this.waitTime = waitTime;
		}

		public object Current { get; }

		public bool MoveNext() {
			if(!run) {
				run = true;
				time = Time.unscaledTime + waitTime();
			}
			return Time.unscaledTime - time < 0;
		}

		public void Reset() {
			run = false;
		}
	}

	class WaitUntil : IEventIterator {
		private readonly Func<bool> predicate;
		public WaitUntil(Func<bool> predicate) {
			this.predicate = predicate;
		}

		public object Current { get; }

		public bool MoveNext() {
			return !predicate();
		}

		public void Reset() { }
	}

	class WaitWhile : IEventIterator {
		private readonly Func<bool> predicate;
		public WaitWhile(Func<bool> predicate) {
			this.predicate = predicate;
		}

		public object Current { get; }

		public bool MoveNext() {
			return predicate();
		}

		public void Reset() { }
	}

	class RoutineAction : IEventIterator {
		private readonly System.Action action;

		public RoutineAction(System.Action action) {
			this.action = action;
		}

		public object Current => null;

		public bool MoveNext() {
			action?.Invoke();
			return false;
		}

		public void Reset() { }
	}

	class RoutineYieldable<T> : IEventIterator {
		private readonly System.Func<T> func;
		private bool flag;

		public RoutineYieldable(System.Func<T> func) {
			this.func = func;
		}

		public object Current { get; private set; }

		public bool MoveNext() {
			if(flag) {
				Current = null;
				//if(frame == Time.frameCount) {
				//	//Make wait for the next frame.
				//	return true;
				//}
				return false;
			} else {
				flag = true;
				Current = func.Invoke();
				return true;
			}
		}

		public void Reset() {
			Current = null;
			flag = false;
		}
	}

	class RoutineFunc<T> : IEventIterator {
		private readonly System.Func<T> func;

		public RoutineFunc(System.Func<T> func) {
			this.func = func;
		}

		public object Current { get; private set; }

		public bool MoveNext() {
			Current = func.Invoke();
			return false;
		}

		public void Reset() {
			Current = null;
		}
	}

	class RoutineFuncs<T> : IEventIterator {
		private readonly System.Func<T>[] func;
		private int index;

		public RoutineFuncs(params System.Func<T>[] func) {
			this.func = func;
		}

		public object Current { get; private set; }

		public bool MoveNext() {
			Current = func[index].Invoke();
			index++;
			return index < func.Length;
		}

		public void Reset() {
			Current = null;
			index = 0;
		}
	}

	class RoutineEventFunc : IEventIterator {
		private readonly Func<EventCoroutine> evt;
		private int state;
		private EventCoroutine e;

		public RoutineEventFunc(Func<EventCoroutine> evt) {
			this.evt = evt;
		}

		public object Current { get; private set; }

		public bool MoveNext() {
			if(state == 0) {
				//Begin of event
				e = evt();
				if(e != null) {
					state++;
					e.Run();
				} else {
					return false;
				}
			}
			if(state == 1) {
				if(e.IsRunning) {
					//Wait until finished
					return true;
				} else {
					Current = e.state;
					//Finish
					state = 2;
				}
			}
			return false;
		}

		public void Reset() {
			state = 0;
			Current = null;
		}
	}

	class RoutineEvent : IEventIterator {
		private readonly EventCoroutine evt;
		private int state;

		public RoutineEvent(EventCoroutine evt) {
			this.evt = evt;
		}

		public object Current { get; private set; }

		public bool MoveNext() {
			if(state == 0) {
				//Begin of event
				state++;
				evt.Run();
			}
			if(state == 1) {
				if(evt.IsRunning) {
					//Wait until finished
					return true;
				} else {
					Current = evt.state;
					//Finish event
					state = 2;
				}
			}
			return false;
		}

		public void Reset() {
			state = 0;
			Current = null;
		}
	}

	class RoutineEvents : IEventIterator {
		private readonly EventCoroutine[] events;
		private int index = -1;

		public RoutineEvents(params EventCoroutine[] events) {
			this.events = events;
		}

		public object Current => null;

		public bool MoveNext() {
			if(index == -1) {
				if(events.Length == 0)
					return false;
				//Begin of event
				index = 0;
				events[index].Run();
			}
			if(index < events.Length) {
				if(events[index].IsRunning) {
					//Wait until finished
					return true;
				} else {
					//Next event
					index++;
					if(index < events.Length) {
						events[index].Run();
					}
					return MoveNext();
				}
			}
			return false;
		}

		public void Reset() {
			index = -1;
		}
	}

	class RoutineFlow : IEventIterator, IEventStopable {
		private readonly EventCoroutine.Processor[] processors;
		private int index;
		private int state;
		private bool isStopped;

		public RoutineFlow(params IEventIterator[] flows) {
			this.processors = new EventCoroutine.Processor[flows.Length];
			for(int i = 0; i < flows.Length; i++) {
				processors[i] = new EventCoroutine.Processor(flows[i]);
			}
		}

		public object Current { get; private set; }

		public bool MoveNext() {
			return Execute(index);
		}

		private bool Execute(int index) {
			if(isStopped) return false;
			if(processors[index].Process(ref state)) {
				this.index = index;
				//Keep executing
				return true;
			}
			else {
				index++;
				if(index < processors.Length) {
					//Continue the next flow
					return Execute(index);
				}
				else {
					//Finish
					return false;
				}
			}
		}

		public void Stop() {
			isStopped = true;
			for(int i=0;i< processors.Length;i++) {
				processors[i].Stop();
			}
		}

		public void Reset() {
			Current = null;
			index = 0;
			isStopped = false;
			for(int i = 0; i < processors.Length; i++) {
				processors[i].Reset();
			}
		}
	}

	class RoutineIterable : IEventIterator {
		public readonly IEnumerable target;
		private IEnumerator iterator;

		public RoutineIterable(IEnumerable target) {
			this.target = target;
		}

		public object Current {
			get {
				if(iterator == null)
					iterator = target.GetEnumerator();
				return iterator.Current;
			}
		}

		public bool MoveNext() {
			if(iterator == null)
				iterator = target.GetEnumerator();
			return iterator.MoveNext();
		}

		public void Reset() {
			iterator = target.GetEnumerator();
		}
	}
	#endregion

	#region BT
	public class Selector : IEventIterator {
		public readonly EventCoroutine[] events;
		private int index = -1;
		private bool? state;

		public Selector(params EventCoroutine[] events) {
			this.events = events;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(index == -1) {
				//Begin of event
				index = 0;
				events[index].Run();
			}
			if(index < events.Length) {
				if(events[index].IsRunning) {
					//Wait until finished
					return true;
				} else {
					if(events[index].IsSuccess) {
						state = true;
						return false;
					} else {
						//Next event
						index++;
						if(index < events.Length) {
							events[index].Run();
						}
						return MoveNext();
					}
				}
			}
			//End of event
			state = false;
			return false;
		}

		public void Reset() {
			index = -1;
			state = null;
		}
	}

	public class RandomSelector : IEventIterator {
		public readonly EventCoroutine[] events;
		private int index = -1;
		private bool? state;
		private List<int> randomOrder = new List<int>();

		public RandomSelector(params EventCoroutine[] events) {
			this.events = events;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(index == -1) {
				//Begin of event
				index = 0;
				randomOrder.Clear();
				List<int> indexs = new List<int>();
				for(int i = 0; i < events.Length; ++i) {
					indexs.Add(i);
				}
				for(int i = events.Length; i > 0; --i) {
					int index = UnityEngine.Random.Range(0, i);
					randomOrder.Add(indexs[index]);
					indexs.RemoveAt(index);
				}
				events[randomOrder[index]].Run();
			}
			if(index < events.Length) {
				if(events[randomOrder[index]].IsRunning) {
					//Wait until finished
					return true;
				} else {
					if(events[randomOrder[index]].IsSuccess) {
						state = true;
						return false;
					} else {
						//Next event
						index++;
						if(index < events.Length) {
							events[randomOrder[index]].Run();
						}
						return MoveNext();
					}
				}
			}
			//End of event
			state = false;
			return false;
		}

		public void Reset() {
			index = -1;
			state = null;
		}
	}

	public class Sequence : IEventIterator {
		public readonly EventCoroutine[] events;
		private int index = -1;
		private bool? state;

		public Sequence(params EventCoroutine[] events) {
			this.events = events;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(index == -1) {
				//Begin of event
				index = 0;
				events[index].Run();
			}
			if(index < events.Length) {
				if(events[index].IsRunning) {
					//Wait until finished
					return true;
				} else {
					if(events[index].IsFailure) {
						state = false;
						return false;
					} else {
						//Next event
						index++;
						if(index < events.Length) {
							events[index].Run();
						}
						return MoveNext();
					}
				}
			}
			//End of event
			state = true;
			return false;
		}

		public void Reset() {
			index = -1;
			state = null;
		}
	}

	public class RandomSequence : IEventIterator {
		public readonly EventCoroutine[] events;
		private int index = -1;
		private bool? state;
		private List<int> randomOrder = new List<int>();

		public RandomSequence(params EventCoroutine[] events) {
			this.events = events;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(index == -1) {
				//Begin of event
				index = 0;
				randomOrder.Clear();
				List<int> indexs = new List<int>();
				for(int i = 0; i < events.Length; ++i) {
					indexs.Add(i);
				}
				for(int i = events.Length; i > 0; --i) {
					int index = UnityEngine.Random.Range(0, i);
					randomOrder.Add(indexs[index]);
					indexs.RemoveAt(index);
				}
				events[randomOrder[index]].Run();
			}
			if(index < events.Length) {
				if(events[randomOrder[index]].IsRunning) {
					//Wait until finished
					return true;
				} else {
					if(events[randomOrder[index]].IsFailure) {
						state = false;
						return false;
					} else {
						//Next event
						index++;
						if(index < events.Length) {
							events[randomOrder[index]].Run();
						}
						return MoveNext();
					}
				}
			}
			//End of event
			state = true;
			return false;
		}

		public void Reset() {
			index = -1;
			state = null;
		}
	}

	public class Conditional : IEventIterator {
		private Func<bool> condition;
		private EventCoroutine onTrue;
		private EventCoroutine onFalse;
		private EventCoroutine onFinished;
		private bool? state;
		private bool finish;
		private EventCoroutine eval;

		public Conditional(Func<bool> condition, EventCoroutine onTrue = null, EventCoroutine onFalse = null, EventCoroutine onFinished = null) {
			this.condition = condition;
			this.onTrue = onTrue;
			this.onFalse = onFalse;
			this.onFinished = onFinished;
		}


		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(finish) {
				if(onFinished.IsRunning) {
					//Wait until finished
					return true;
				}
				state = eval == onTrue;
				return false;
			}
			if(eval == null) {
				if(condition()) {
					if(onTrue == null) {
						state = true;
						return false;
					}
					eval = onTrue.Run();
				} else {
					if(onFalse == null) {
						state = false;
						return false;
					}
					eval = onFalse.Run();
				}
			}
			if(eval.IsRunning) {
				//Wait until finished
				return true;
			}
			if(onFinished == null) {
				state = eval == onTrue;
				return false;
			} else {
				finish = true;
				onFinished.Run();
				return MoveNext();
			}
		}

		public void Reset() {
			eval = null;
			state = null;
			finish = false;
		}
	}

	public class Failer : IEventIterator {
		private EventCoroutine target;
		private bool? state;
		private EventCoroutine eval;

		public Failer(EventCoroutine target) {
			this.target = target;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(eval == null) {
				eval = target.Run();
			}
			if(eval.IsRunning) {
				//Wait until finished
				return true;
			}
			state = false;
			return false;
		}

		public void Reset() {
			eval = null;
			state = null;
		}
	}

	public class Inverter : IEventIterator {
		private EventCoroutine target;
		private bool? state;
		private EventCoroutine eval;

		public Inverter(EventCoroutine target) {
			this.target = target;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(eval == null) {
				eval = target.Run();
			}
			if(eval.IsRunning) {
				//Wait until finished
				return true;
			}
			state = !eval.IsSuccess;
			return false;
		}

		public void Reset() {
			eval = null;
			state = null;
		}
	}

	public class Succeeder : IEventIterator {
		private EventCoroutine target;
		private bool? state;
		private EventCoroutine eval;

		public Succeeder(EventCoroutine target) {
			this.target = target;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(eval == null) {
				eval = target.Run();
			}
			if(eval.IsRunning) {
				//Wait until finished
				return true;
			}
			state = true;
			return false;
		}

		public void Reset() {
			eval = null;
			state = null;
		}
	}

	public class Repeater : IEventIterator {
		private EventCoroutine target;
		private int repeatCount;
		private bool stopOnFailure;
		private bool? state;
		private EventCoroutine eval;
		private int currentRepeat;

		public Repeater(EventCoroutine target, int repeatCount = -1, bool stopOnFailure = false) {
			this.target = target;
			this.repeatCount = repeatCount;
			this.stopOnFailure = stopOnFailure;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(eval == null) {
				if(repeatCount == 0) {
					//Skip
					state = true;
					return false;
				}
				eval = target.Run();
			}
			if(eval.IsRunning) {
				//Wait until finished
				return true;
			}
			if(stopOnFailure && eval.IsFailure) {
				state = true;
				return false;
			}
			if(repeatCount >= 0) {
				currentRepeat++;
				if(currentRepeat >= repeatCount) {
					state = true;
					return false;
				}
			}
			eval = null;
			//Wait for other to repeat.
			return true;
		}

		public void Reset() {
			eval = null;
			state = null;
			currentRepeat = 0;
		}
	}

	public class UntilFailure : IEventIterator {
		private EventCoroutine target;
		private bool? state;
		private EventCoroutine eval;

		public UntilFailure(EventCoroutine target) {
			this.target = target;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(eval == null) {
				eval = target.Run();
			}
			if(eval.IsRunning) {
				//Wait until finished
				return true;
			}
			if(eval.IsFailure) {
				state = true;
				return false;
			}
			//Repeat again
			eval = null;
			return true;
		}

		public void Reset() {
			eval = null;
			state = null;
		}
	}

	public class UntilSuccess : IEventIterator {
		private EventCoroutine target;
		private bool? state;
		private EventCoroutine eval;

		public UntilSuccess(EventCoroutine target) {
			this.target = target;
		}

		public object Current {
			get {
				if(state.HasValue) {
					return state.Value;
				}
				return null;
			}
		}

		public bool MoveNext() {
			if(eval == null) {
				eval = target.Run();
			}
			if(eval.IsRunning) {
				//Wait until finished
				return true;
			}
			if(eval.IsSuccess) {
				state = true;
				return false;
			}
			//Repeat again
			eval = null;
			return true;
		}

		public void Reset() {
			eval = null;
			state = null;
		}
	}
	#endregion
}