using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	public static class UEvent {
		#region Properties
		private static readonly Dictionary<string, HashSet<IEventListener>> events = new Dictionary<string, HashSet<IEventListener>>();

		private static Dictionary<string, Type> _listenersMap;
		public static Dictionary<string, Type> Listeners {
			get {
				if(_listenersMap == null) {
					_listenersMap = new Dictionary<string, Type>();
					var atts = ReflectionUtils.GetAssemblyAttributes<RegisterEventListenerAttribute>();
					if(atts != null) {
						foreach(var a in atts) {
							_listenersMap[a.ListenerID] = a.ListenerType;
						}
					}
				}
				return _listenersMap;
			}
		}
		#endregion

#if UNITY_2019_3_OR_NEWER
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		static void InitAfterAssembly() {
			events.Clear();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Init() {
			events.Clear();
		}
#endif

		internal static T AddListener<T>(GameObject gameObject) where T : IEventListener {
			var listener = gameObject.GetComponent<T>();
			if(listener == null) {
				listener = (T)(object)gameObject.AddComponent(typeof(T));
				if(!events.TryGetValue(listener.eventID, out var handlers)) {
					handlers = new HashSet<IEventListener>();
					events.Add(listener.eventID, handlers);
				}
				handlers.Add(listener);
			}
			return listener;
		}

		internal static IEventListener AddListener(Type type, GameObject gameObject) {
			var listener = gameObject.GetComponent(type) as IEventListener;
			if(listener == null) {
				listener = gameObject.AddComponent(type) as IEventListener;
				if(!events.TryGetValue(listener.eventID, out var handlers)) {
					handlers = new HashSet<IEventListener>();
					events.Add(listener.eventID, handlers);
				}
				handlers.Add(listener);
				return listener;
			}
			return listener;
		}

		public static IEventListener AddListener(string id, GameObject gameObject) {
			if(!Listeners.TryGetValue(id, out var type)) {
				throw new Exception("There's no registered listener with id: " + id);
			}
			var listener = gameObject.GetComponent(type) as IEventListener;
			if(listener == null) {
				listener = gameObject.AddComponent(type) as IEventListener;
				if(!events.TryGetValue(listener.eventID, out var handlers)) {
					handlers = new HashSet<IEventListener>();
					events.Add(listener.eventID, handlers);
				}
				handlers.Add(listener);
			}
			return listener;
		}

		public static IEventListener<T> AddListener<T>(string id, GameObject gameObject) where T : Delegate {
			if(!Listeners.TryGetValue(id, out var type)) {
				throw new Exception("There's no registered listener with id: " + id);
			}
			var listener = gameObject.GetComponent(type) as IEventListener<T>;
			if(listener == null) {
				listener = gameObject.AddComponent(type) as IEventListener<T>;
				if(!events.TryGetValue(listener.eventID, out var handlers)) {
					handlers = new HashSet<IEventListener>();
					events.Add(listener.eventID, handlers);
				}
				handlers.Add(listener);
			}
			return listener;
		}

		public static IEventListener GetListener(string id, GameObject gameObject) {
			if(!Listeners.TryGetValue(id, out var type)) {
				throw new Exception("There's no registered listener with id: " + id);
			}
			return gameObject.GetComponent(type) as IEventListener;
		}

		public static IEventListener<T> GetListener<T>(string id, GameObject gameObject) where T : Delegate {
			if(!Listeners.TryGetValue(id, out var type)) {
				throw new Exception("There's no registered listener with id: " + id);
			}
			return gameObject.GetComponent(type) as IEventListener<T>;
		}

		public static void Register(string id, GameObject owner, Action handler) {
			Register(id, owner, handler as Delegate);
		}

		public static void Register<TArgs>(string id, GameObject owner, Action<TArgs> handler) {
			Register(id, owner, handler as Delegate);
		}

		public static void Register(string id, GameObject owner, Delegate handler) {
#if UNITY_EDITOR
			if(!Application.isPlaying) {
				//Skip if Unity is not in play mode.
				return;
			}
#endif
			if(!events.TryGetValue(id, out var handlers)) {
				handlers = new HashSet<IEventListener>();
				events.Add(id, handlers);
			}
			foreach(var h in handlers) {
				if(id != h.eventID || owner != h.eventOwner)
					continue;
				h.Register(owner, handler);
				return;
			}
			AddListener(id, owner).Register(owner, handler);
		}

		public static void Register(string id, Component owner, Action handler) {
			Register(id, owner, handler as Delegate);
		}

		public static void Register<TArgs>(string id, Component owner, Action<TArgs> handler) {
			Register(id, owner, handler as Delegate);
		}

		public static void Register(string id, Component owner, Delegate handler) {
#if UNITY_EDITOR
			if(!Application.isPlaying) {
				//Skip if Unity is not in play mode.
				return;
			}
#endif
			if(!events.TryGetValue(id, out var handlers)) {
				handlers = new HashSet<IEventListener>();
				events.Add(id, handlers);
			}
			var go = owner.gameObject;
			foreach(var h in handlers) {
				if(id != h.eventID || go != h.eventOwner)
					continue;
				h.Register(owner, handler);
				return;
			}
			AddListener(id, go).Register(owner, handler);
		}

		public static void RegisterEvent<T>(string id, GameObject owner, T handler) where T : Delegate {
#if UNITY_EDITOR
			if(!Application.isPlaying) {
				//Skip if Unity is not in play mode.
				return;
			}
#endif
			if(!events.TryGetValue(id, out var handlers)) {
				handlers = new HashSet<IEventListener>();
				events.Add(id, handlers);
			}
			foreach(var h in handlers) {
				if(id != h.eventID || owner != h.eventOwner)
					continue;
				(h as IEventListener<T>).Register(owner, handler);
				return;
			}
			AddListener<T>(id, owner).Register(owner, handler);
		}

		public static void RegisterEvent<T>(string id, Component owner, T handler) where T : Delegate {
#if UNITY_EDITOR
			if(!Application.isPlaying) {
				//Skip if Unity is not in play mode.
				return;
			}
#endif
			if(!events.TryGetValue(id, out var handlers)) {
				handlers = new HashSet<IEventListener>();
				events.Add(id, handlers);
			}
			var go = owner.gameObject;
			foreach(var h in handlers) {
				if(id != h.eventID || go != h.eventOwner)
					continue;
				(h as IEventListener<T>).Register(owner, handler);
				return;
			}
			AddListener<T>(id, go).Register(owner, handler);
		}

		public static void Unregister(string id, GameObject owner, Delegate handler) {
			if(events.TryGetValue(id, out var handlers)) {
				foreach(var h in handlers) {
					if(id != h.eventID || owner != h.eventOwner)
						continue;
					h.Unregister(handler);
				}
			}
		}

		public static void Unregister(string id, GameObject owner, Action handler) {
			Unregister(id, owner, handler as Delegate);
		}

		public static void Unregister<TArgs>(string id, GameObject owner, Action<TArgs> handler) {
			Unregister(id, owner, handler as Delegate);
		}

		public static void Unregister(string id, Component owner, Delegate handler) {
			Unregister(id, owner.gameObject, handler);
		}

		public static void Unregister(string id, Component owner, Action handler) {
			Unregister(id, owner, handler as Delegate);
		}

		public static void Unregister<TArgs>(string id, Component owner, Action<TArgs> handler) {
			Unregister(id, owner, handler as Delegate);
		}
	}

	public class URuntimeQueue {
		private static Action queuedActions;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Init() {
			queuedActions = null;
		}

		public static void Queue(Action action) {
			if(QueueBehavior.instance != null) {
				queuedActions += action;
			}
		}

		#region Queue Behavior
		class QueueBehavior : MonoBehaviour {
			static QueueBehavior _instance;
			public static QueueBehavior instance {
				get {
					if(_instance == null && Application.isPlaying) {
						var go = new GameObject("Queue Manager");
						go.hideFlags = HideFlags.DontSaveInEditor;
						GameObject.DontDestroyOnLoad(go);
						_instance = go.AddComponent<QueueBehavior>();
					}
					return _instance;
				}
			}

			private void Update() {
				var act = queuedActions;
				queuedActions = null;
				act?.Invoke();
			}
		}
		#endregion
	}
}