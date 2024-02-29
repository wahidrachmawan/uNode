using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace MaxyGames.UNode {
	public class uNodeAsyncOperation {
		/// <summary>
		/// True when the operation is completed naturally.
		/// </summary>
		public bool IsCompleted { get; private set; }
		/// <summary>
		/// True when the operation is Stopped manually.
		/// </summary>
		public bool IsStopped { get; private set; }
		/// <summary>
		/// True when the operation is completed or stopped.
		/// </summary>
		public bool IsFinished => IsCompleted || IsStopped;

		/// <summary>
		/// Callback when the operation is stopped.
		/// </summary>
		public Action stopCallback;
		/// <summary>
		/// Callback when the operation is completed or stopped.
		/// </summary>
		public Action finishCallback;
		/// <summary>
		/// Callback when the operation is completed.
		/// </summary>
		public Action completeCallback;

		/// <summary>
		/// Stop the operation
		/// </summary>
		public void Stop() {
			IsStopped = true;
			stopCallback?.Invoke();
		}

		/// <summary>
		/// Complete the operation
		/// </summary>
		internal void SetComplete() {
			IsCompleted = true;
			completeCallback?.Invoke();
		}
	}

	/// <summary>
	/// Privides threading utility.
	/// </summary>
	public static class uNodeThreadUtility {
		private class AsyncData {
			public uNodeAsyncOperation operation;
			public IEnumerator iterator;
		}

		private static object lockObject = new object();
		private static List<Action> actions = new List<Action>();
		private static List<AsyncData> asyncActions = new List<AsyncData>();
		private static Queue<Action> actionFrames = new Queue<Action>();
		private static List<KeyValuePair<long, Action>> actionAfterFrames = new List<KeyValuePair<long, Action>>();
		private static List<KeyValuePair<float, Action>> actionAfterDurations = new List<KeyValuePair<float, Action>>();
		private static List<KeyValuePair<float, Action>> actionWhileDurations = new List<KeyValuePair<float, Action>>();
		private static List<KeyValuePair<Func<bool>, Action>> actionWhile = new List<KeyValuePair<Func<bool>, Action>>();
		private static List<KeyValuePair<Func<bool>, Action>> actionAfterCondition = new List<KeyValuePair<Func<bool>, Action>>();
		private static ConcurrentDictionary<object, Action> actionExecutedOnce = new ConcurrentDictionary<object, Action>();
		private static int queueCount;

#if !UNITY_EDITOR
		class ThreadRunner : MonoBehaviour {
			public void Update() {
				uNodeThreadUtility.Update();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		internal static void Init() {
			GameObject go = new GameObject("ThreadRunner");
			//go.hideFlags = HideFlags.HideAndDontSave;
			go.AddComponent<ThreadRunner>();
			UnityEngine.Object.DontDestroyOnLoad(go);
		}
#endif

		/// <summary>
		/// The frame count from the start of application
		/// </summary>
		/// <value></value>
		public static long frame { get; private set; }
		/// <summary>
		/// The time since application startup
		/// </summary>
		/// <value></value>
		public static float time { get; private set; }
		/// <summary>
		/// The main thread delta time
		/// </summary>
		/// <value></value>
		public static float deltaTime { get; private set; }
		/// <summary>
		/// True when current thread is the main thread.
		/// </summary>
		public static bool IsInMainThread => uNodeUtility.IsInMainThread;

		/// <summary>
		/// Return true if there's a queue that need the Update to call.
		/// Note: queue with condition is not listed
		/// </summary>
		/// <returns></returns>
		public static bool IsNeedUpdate() {
			return actions.Count != 0 ||
				actionFrames.Count != 0 ||
				actionAfterFrames.Count != 0 ||
				actionAfterDurations.Count != 0 ||
				actionWhileDurations.Count != 0 ||
				actionExecutedOnce.Count != 0;
		}

		/// <summary>
		/// Don't call this, this function will be called automaticly by the uNodeEditorInitializer.
		/// </summary>
		public static void Update() {
			lock(lockObject) {
				deltaTime = UnityEngine.Time.realtimeSinceStartup - time;
#if UNITY_EDITOR
				if(deltaTime < 0) {
					deltaTime = 0.001f;
				}
#endif
				time = UnityEngine.Time.realtimeSinceStartup;
				GraphDebug.debugTimeAsDouble += deltaTime;
				for(int i = 0; i < actions.Count; i++) {
					try {
						if(actions[i] != null) {
							actions[i]();
						}
					}
					catch(System.Exception ex) {
						UnityEngine.Debug.LogException(ex);
					}
				}
				actions.Clear();
				if(actionFrames.Count > 0) {
					Action action = actionFrames.Dequeue();
					try {
						if(action != null) {
							action();
						}
					}
					catch(System.Exception ex) {
						UnityEngine.Debug.LogException(ex);
					}
					queueCount = actionFrames.Count;
				}
				else {
					queueCount = 0;
				}
				if(actionAfterFrames.Count > 0) {
					for(int i = 0; i < actionAfterFrames.Count; i++) {
						var pair = actionAfterFrames[i];
						if(pair.Key <= frame) {
							actionAfterFrames.RemoveAt(i);
							i--;
							try {
								pair.Value();
							}
							catch(Exception ex) {
								Debug.LogException(ex);
							}
						}
					}
				}
				if(actionAfterDurations.Count > 0) {
					for(int i = 0; i < actionAfterDurations.Count; i++) {
						var pair = actionAfterDurations[i];
						if(pair.Key <= time) {
							actionAfterDurations.RemoveAt(i);
							i--;
							try {
								pair.Value();
							}
							catch(Exception ex) {
								Debug.LogException(ex);
							}
						}
					}
				}
				if(actionWhileDurations.Count > 0) {
					for(int i = 0; i < actionWhileDurations.Count; i++) {
						var pair = actionWhileDurations[i];
						try {
							pair.Value();
						}
						catch(Exception ex) {
							Debug.LogException(ex);
						}
						if(pair.Key <= time) {
							actionWhileDurations.RemoveAt(i);
							i--;
						}
					}
				}
				List<Action> actionExecutedOnce = new List<Action>(uNodeThreadUtility.actionExecutedOnce.Count);
				foreach(var pair in uNodeThreadUtility.actionExecutedOnce) {
					actions.Add(pair.Value);
				}
				uNodeThreadUtility.actionExecutedOnce.Clear();
				for(int i = 0; i < actionExecutedOnce.Count; i++) {
					try {
						actionExecutedOnce[i]();
					}
					catch(System.Exception ex) {
						UnityEngine.Debug.LogException(ex);
					}
				}
				if(actionWhile.Count > 0) {
					for(int i = 0; i < actionWhile.Count; i++) {
						var pair = actionWhile[i];
						try {
							if(pair.Key()) {
								pair.Value();
							}
							else {
								actionWhile.RemoveAt(i);
								i--;
							}
						}
						catch(Exception ex) {
							Debug.LogException(ex);
							actionWhile.RemoveAt(i);
							i--;
						}
					}
				}
				if(actionAfterCondition.Count > 0) {
					for(int i = 0; i < actionAfterCondition.Count; i++) {
						var pair = actionAfterCondition[i];
						try {
							if(pair.Key()) {
								pair.Value();
								actionAfterCondition.RemoveAt(i);
								i--;
							}
						}
						catch(Exception ex) {
							Debug.LogException(ex);
							actionAfterCondition.RemoveAt(i);
							i--;
						}
					}
				}
				for(int i = 0; i < asyncActions.Count; i++) {
					try {
						if(asyncActions[i].operation.IsStopped) {
							asyncActions[i].operation.finishCallback?.Invoke();
							asyncActions.RemoveAt(i);
							i--;
						}
						else if(!asyncActions[i].iterator.MoveNext()) {
							asyncActions[i].operation.SetComplete();
							asyncActions[i].operation.finishCallback?.Invoke();
							asyncActions.RemoveAt(i);
							i--;
						}
					}
					catch(System.Exception ex) {
						UnityEngine.Debug.LogException(ex);
						asyncActions[i].operation.Stop();
						asyncActions[i].operation.finishCallback?.Invoke();
						asyncActions.RemoveAt(i);
						i--;
					}
				}
				frame++;
			}
		}

		internal static void ClearTask() {
			lock(lockObject) {
				actions.Clear();
				asyncActions.Clear();
				actionFrames.Clear();
				actionAfterFrames.Clear();
				actionAfterDurations.Clear();
				actionWhileDurations.Clear();
				actionWhile.Clear();
				actionAfterCondition.Clear();
				actionExecutedOnce.Clear();
				queueCount = 0;
			}
		}

		/// <summary>
		/// Execute a IEnumerator.MoveNext in each frame.
		/// Task will be called on the main thread.
		/// </summary>
		/// <param name="iterator"></param>
		/// <returns></returns>
		/// <remarks>Use <see cref="uNodeAsyncOperation.Stop"/>To stop the task</remarks>
		public static uNodeAsyncOperation Task(IEnumerator iterator) {
			var result = new uNodeAsyncOperation();
			asyncActions.Add(new AsyncData() {
				iterator = iterator,
				operation = result,
			});
			return result;
		}


		/// <summary>
		/// Execute a IEnumerator.MoveNext in each frame.
		/// Task will be called on the main thread.
		/// </summary>
		/// <param name="iterator"></param>
		/// <param name="onCompleted">Callback on completed</param>
		/// <param name="onStopped">Callback on stopped</param>
		/// <param name="onFinished">Callback on completed or stopped</param>
		/// <returns></returns>
		/// <remarks>Use <see cref="uNodeAsyncOperation.Stop"/>To stop the task</remarks>
		public static uNodeAsyncOperation Task(IEnumerator iterator, Action onCompleted = null, Action onStopped = null, Action onFinished = null) {
			var result = new uNodeAsyncOperation() {
				completeCallback = onCompleted,
				stopCallback = onStopped,
				finishCallback = onFinished,
			};
			asyncActions.Add(new AsyncData() {
				iterator = iterator,
				operation = result,
			});
			return result;
		}

		/// <summary>
		/// Queue an action to execute at once in update.
		/// </summary>
		/// <param name="action"></param>
		/// <remarks>
		/// Note: the action may or may not be executed in next frame,
		/// if queue action is being executed newly added queued will be executed afterward without waiting next frame.
		/// </remarks>
		public static void Queue(Action action) {
			lock(lockObject) {
				actions.Add(action);
			}
		}

		/// <summary>
		/// Execute once in the next frame, given by its UID.
		/// The <paramref name="action"/> will be updated when sceduled action contains same <paramref name="uniqueIdentifier"/>
		/// </summary>
		/// <param name="action"></param>
		public static void ExecuteOnce(Action action, object uniqueIdentifier) {
			lock(lockObject) {
				actionExecutedOnce[uniqueIdentifier] = action;
				//if(!actionExecutedOnce.ContainsKey(uniqueIdentifier)) {
				//	actionExecutedOnce[uniqueIdentifier] = action;
				//}
			}
		}

		/// <summary>
		/// Execute given action after how many frame.
		/// Action will be called on the main thread.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="action"></param>
		public static void ExecuteAfter(int frame, Action action) {
			lock(lockObject) {
				actionAfterFrames.Add(new KeyValuePair<long, Action>(uNodeThreadUtility.frame + frame, action));
			}
		}

		/// <summary>
		/// Execute an action after how many duration in seconds.
		/// Action will be called on the main thread.
		/// </summary>
		/// <param name="duration"></param>
		/// <param name="action"></param>
		public static void ExecuteAfterDuration(float duration, Action action) {
			lock(lockObject) {
				actionAfterDurations.Add(new KeyValuePair<float, Action>(time + duration, action));
			}
		}

		/// <summary>
		/// Execute an action every frame and stop after how many duration in seconds.
		/// Action will be called on the main thread.
		/// </summary>
		/// <param name="duration"></param>
		/// <param name="action"></param>
		public static void ExecuteWhileDuration(float duration, Action action) {
			lock(lockObject) {
				actionWhileDurations.Add(new KeyValuePair<float, Action>(time + duration, action));
			}
		}

		/// <summary>
		/// Execute an action every frame and stop when the condition is true.
		/// Action will be called on the main thread.
		/// </summary>
		/// <param name="duration"></param>
		/// <param name="action"></param>
		public static void ExecuteWhile(Func<bool> condition, Action action) {
			lock(lockObject) {
				actionWhile.Add(new KeyValuePair<Func<bool>, Action>(condition, action));
			}
		}

		/// <summary>
		/// Execute an action one time when the condition is true, condition will be called every frame.
		/// Action will be called on the main thread.
		/// </summary>
		/// <param name="duration"></param>
		/// <param name="action"></param>
		public static void ExecuteAfterCondition(Func<bool> condition, Action action) {
			lock(lockObject) {
				actionAfterCondition.Add(new KeyValuePair<Func<bool>, Action>(condition, action));
			}
		}

		/// <summary>
		/// Queue an action to execute at once in update and wait one frame.
		/// Note: Don't use it on main thread.
		/// </summary>
		/// <param name="action"></param>
		public static void QueueAndWait(Action action) {
			Queue(action);
			WaitOneFrame();
		}

		/// <summary>
		/// Queue an action to execute in update queue.
		/// </summary>
		/// <param name="action"></param>
		public static void QueueOnFrame(Action action) {
			lock(lockObject) {
				actionFrames.Enqueue(action);
				queueCount++;
			}
		}

		///// <summary>
		///// Clear the queue.
		///// </summary>
		//public static void ClearQueue() {
		//	lock(lockObject) {
		//		actionFrames.Clear();
		//		queueCount = 0;
		//	}
		//}

		/// <summary>
		/// Will run the action on main thread.
		/// This is safe to use in main thread or not in main thread.
		/// </summary>
		/// <param name="action"></param>
		public static void RunOnMainThread(Action action) {
			if(uNodeUtility.IsInMainThread) {
				action();
			}
			else {
				QueueAndWait(action);
			}
		}

		/// <summary>
		/// Wait till next update.
		/// Note: don't use it on main thread.
		/// </summary>
		public static void WaitOneFrame() {
			if(uNodeUtility.IsInMainThread)
				throw new Exception("Cannot wait from within main thread.");
			long currFrame = frame;
			while(currFrame == frame) {
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Wait till queue is empty.
		/// Note: don't use it on main thread.
		/// </summary>
		public static void WaitUntilEmpty() {
			if(uNodeUtility.IsInMainThread)
				throw new Exception("Cannot wait from within main thread.");
			while(queueCount != 0) {
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Wait until condition evaluate to true
		/// Note: don't use it on main thread.
		/// </summary>
		/// <param name="condition"></param>
		public static void WaitUntil(Func<bool> condition) {
			if(uNodeUtility.IsInMainThread)
				throw new Exception("Cannot wait from within main thread.");
			long currFrame = frame;
			bool flag = true;
			while(flag) {
				if(currFrame != frame) {
					Queue(() => {
						if(condition()) {
							flag = false;
						}
					});
					currFrame = frame;
				}
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Wait until condition evaluate to true
		/// Note: don't use it on main thread.
		/// </summary>
		/// <param name="condition"></param>
		/// <param name="onTrue"></param>
		public static void WaitUntil(Func<bool> condition, Action onTrue) {
			if(uNodeUtility.IsInMainThread)
				throw new Exception("Cannot wait from within main thread.");
			long currFrame = frame;
			bool flag = true;
			while(flag) {
				if(currFrame != frame) {
					Queue(() => {
						if(condition()) {
							flag = false;
							onTrue();
						}
					});
					currFrame = frame;
				}
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Wait until how number frame
		/// Note: Don't use it on main thread.
		/// </summary>
		/// <param name="frameCount"></param>
		public static void WaitFrame(int frameCount) {
			if(uNodeUtility.IsInMainThread)
				throw new Exception("Cannot wait from within main thread.");
			long currFrame = frame;
			while(currFrame + frameCount >= frame) {
				Thread.Sleep(1);
			}
		}

		/// <summary>
		/// Create a new thread.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public static Thread CreateThread(Action action) {
			Thread thread = new Thread(new ThreadStart(action));
			thread.IsBackground = false;
			return thread;
		}
	}
}