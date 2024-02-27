using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors.UI {
	internal interface IListDragAndDropArgs {
		object target { get; }
		int insertAtIndex { get; }
		IDragAndDropData dragAndDropData { get; }
		DragAndDropPosition dragAndDropPosition { get; }
	}

	internal struct ListDragAndDropArgs : IListDragAndDropArgs {
		public object target { get; set; }
		public int insertAtIndex { get; set; }
		public DragAndDropPosition dragAndDropPosition { get; set; }
		public IDragAndDropData dragAndDropData { get; set; }
	}

	internal enum DragAndDropPosition {
		OverItem,
		BetweenItems,
		OutsideItems
	}

	internal interface IDragAndDrop {
		void StartDrag(StartDragArgs args);
		void AcceptDrag();
		void SetVisualMode(DragVisualMode visualMode);
		IDragAndDropData data { get; }
	}

	internal interface IDragAndDropData {
		object GetGenericData(string key);
		object userData { get; }
		IEnumerable<Object> unityObjectReferences { get; }
	}

	internal interface IDragAndDropController<in TArgs> {
		bool CanStartDrag(IEnumerable<ITreeViewItemElement> itemIndices);
		StartDragArgs SetupDragAndDrop(IEnumerable<ITreeViewItemElement> itemIndices, bool skipText = false);
		DragVisualMode HandleDragAndDrop(TArgs args);
		void OnDrop(TArgs args);
	}

	internal enum DragVisualMode {
		None,
		Copy,
		Move,
		Rejected
	}

	internal interface ICollectionDragAndDropController : IDragAndDropController<IListDragAndDropArgs>, IReorderable { }

	internal interface IReorderable {
		bool enableReordering { get; set; }
	}

	internal class StartDragArgs {
		public string title { get; }
		public object userData { get; }

		private readonly Hashtable m_GenericData = new Hashtable();

		internal Hashtable genericData => m_GenericData;
		internal IEnumerable<Object> unityObjectReferences { get; private set; } = null;

		internal StartDragArgs() {
			title = string.Empty;
		}

		public StartDragArgs(string title, object userData) {
			this.title = title;
			this.userData = userData;
		}

		public void SetGenericData(string key, object data) {
			m_GenericData[key] = data;
		}

		public void SetUnityObjectReferences(IEnumerable<Object> references) {
			unityObjectReferences = references;
		}
	}

	internal abstract class DragEventsProcessor {
		internal enum DragState {
			None,
			CanStartDrag,
			Dragging
		}

		bool m_IsRegistered;
		internal DragState m_DragState;
		private Vector3 m_Start;
		internal readonly VisualElement m_Target;

		// Used in tests
		internal bool isRegistered => m_IsRegistered;

		private const int k_DistanceToActivation = 5;

		internal DragEventsProcessor(VisualElement target) {
			m_Target = target;
			m_Target.RegisterCallback<AttachToPanelEvent>(RegisterCallbacksFromTarget);
			m_Target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);

			RegisterCallbacksFromTarget();
		}

		private void RegisterCallbacksFromTarget(AttachToPanelEvent evt) {
			RegisterCallbacksFromTarget();
		}

		private void RegisterCallbacksFromTarget() {
			if(m_IsRegistered)
				return;

			m_IsRegistered = true;

			m_Target.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
			m_Target.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
			m_Target.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
			m_Target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
			m_Target.RegisterCallback<PointerCancelEvent>(OnPointerCancelEvent);
			m_Target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCapturedOut);

			m_Target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
			m_Target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
			m_Target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
		}

		private void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt) {
			UnregisterCallbacksFromTarget();
		}

		/// <summary>
		/// Unregisters all pointer and drag callbacks.
		/// </summary>
		/// <param name="unregisterPanelEvents">Whether or not we should also unregister panel attach/detach events. Use this when you are about to replace the dragger instance.</param>
		internal void UnregisterCallbacksFromTarget(bool unregisterPanelEvents = false) {
			m_IsRegistered = false;

			m_Target.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
			m_Target.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
			m_Target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
			m_Target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
			m_Target.UnregisterCallback<PointerCancelEvent>(OnPointerCancelEvent);
			m_Target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCapturedOut);
			m_Target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
			m_Target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
			m_Target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);

			if(unregisterPanelEvents) {
				m_Target.UnregisterCallback<AttachToPanelEvent>(RegisterCallbacksFromTarget);
				m_Target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
			}
		}

		protected abstract bool CanStartDrag(Vector3 pointerPosition);

		// Internal for tests.
		protected internal abstract StartDragArgs StartDrag(Vector3 pointerPosition);
		protected internal abstract DragVisualMode UpdateDrag(Vector3 pointerPosition);
		protected internal abstract void OnDrop(Vector3 pointerPosition);

		protected abstract void ClearDragAndDropUI();

		private void OnPointerDownEvent(PointerDownEvent evt) {
			if(evt.button != (int)MouseButton.LeftMouse) {
				m_DragState = DragState.None;
				return;
			}

			if(CanStartDrag(evt.position)) {
				m_DragState = DragState.CanStartDrag;
				m_Start = evt.position;
			}
		}

		internal void OnPointerUpEvent(PointerUpEvent evt) {
			m_DragState = DragState.None;
		}

		private void OnPointerLeaveEvent(PointerLeaveEvent evt) {
			if(evt.target == m_Target)
				ClearDragAndDropUI();
		}

		void OnPointerCancelEvent(PointerCancelEvent evt) {

		}

		private void OnPointerCapturedOut(PointerCaptureOutEvent evt) {
			m_DragState = DragState.None;
		}

		private void OnDragExitedEvent(DragExitedEvent evt) {
			ClearDragAndDropUI();
		}

		private void OnDragPerformEvent(DragPerformEvent evt) {
			m_DragState = DragState.None;
			OnDrop(evt.mousePosition);

			ClearDragAndDropUI();
			DragAndDropUtility.dragAndDrop.AcceptDrag();
		}

		private void OnDragUpdate(DragUpdatedEvent evt) {
			var visualMode = UpdateDrag(evt.mousePosition);
			DragAndDropUtility.dragAndDrop.SetVisualMode(visualMode);
		}


		private void OnPointerMoveEvent(PointerMoveEvent evt) {
			if(m_DragState != DragState.CanStartDrag)
				return;

			if(Mathf.Abs(m_Start.x - evt.position.x) > k_DistanceToActivation ||
				Mathf.Abs(m_Start.y - evt.position.y) > k_DistanceToActivation) {
				var startDragArgs = StartDrag(m_Start);

				if(startDragArgs == null) {
					m_DragState = DragState.None;
					return;
				}
				// Drag can only be started by mouse events or else it will throw an error, so we leave early.
				if(Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseDrag)
					return;

				DragAndDropUtility.dragAndDrop.StartDrag(startDragArgs);

				m_DragState = DragState.Dragging;
			}
		}
	}

	internal static class DragAndDropUtility {
		private static Func<IDragAndDrop> s_MakeClientFunc;
		private static IDragAndDrop s_DragAndDrop;

		public static IDragAndDrop dragAndDrop {
			get {
				if(s_DragAndDrop == null) {
					s_DragAndDrop = new EditorDragAndDrop();
				}

				return s_DragAndDrop;
			}
		}

		internal static void RegisterMakeClientFunc(Func<IDragAndDrop> makeClient) {
			if(s_MakeClientFunc != null)
				throw new UnityException($"The MakeClientFunc has already been registered. Registration denied.");

			s_MakeClientFunc = makeClient;
		}
	}

	internal class EditorDragAndDrop : IDragAndDrop, IDragAndDropData {
		private const string k_UserDataKey = "user_data";

		public object userData => DragAndDrop.GetGenericData(k_UserDataKey);
		public IEnumerable<Object> unityObjectReferences => DragAndDrop.objectReferences;

		public object GetGenericData(string key) {
			return DragAndDrop.GetGenericData(key);
		}

		public void StartDrag(StartDragArgs args) {
			DragAndDrop.PrepareStartDrag();

			if(args.unityObjectReferences != null)
				DragAndDrop.objectReferences = args.unityObjectReferences.ToArray();

			DragAndDrop.SetGenericData(k_UserDataKey, args.userData);
			foreach(DictionaryEntry entry in args.genericData)
				DragAndDrop.SetGenericData((string)entry.Key, entry.Value);

			DragAndDrop.StartDrag(args.title);
		}

		public void AcceptDrag() {
			DragAndDrop.AcceptDrag();
		}

		public void SetVisualMode(DragVisualMode visualMode) {
			switch(visualMode) {
				case DragVisualMode.Copy:
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					break;
				case DragVisualMode.None:
					DragAndDrop.visualMode = DragAndDropVisualMode.None;
					break;
				case DragVisualMode.Move:
					DragAndDrop.visualMode = DragAndDropVisualMode.Move;
					break;
				case DragVisualMode.Rejected:
					DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
					break;
				default:
					throw new ArgumentException($"Visual mode {visualMode} is not supported", nameof(visualMode), null);
			}
		}

		public IDragAndDropData data {
			get { return this; }
		}
	}
}