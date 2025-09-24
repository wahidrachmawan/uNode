using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	//public abstract class EdgeConnector : MouseManipulator {
	//	public abstract EdgeDragHelper edgeDragHelper { get; }
	//}

	public class EdgeConnector<TEdge> : EdgeConnector where TEdge : Edge, new() {
		readonly UEdgeDragHelper<TEdge> m_EdgeDragHelper;
		Edge m_EdgeCandidate;
		private bool m_Active;
		Vector2 m_MouseDownPosition;
		float m_clickedTime;

		internal const float k_ConnectionDistanceTreshold = 10f;

		public EdgeConnector(IEdgeConnectorListener listener) {
			m_EdgeDragHelper = new UEdgeDragHelper<TEdge>(listener);
			m_Active = false;
			activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
		}

		public override EdgeDragHelper edgeDragHelper => m_EdgeDragHelper;

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
			target.RegisterCallback<KeyDownEvent>(OnKeyDown);
			target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
			target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
		}

		protected virtual void OnMouseDown(MouseDownEvent e) {
			if(m_Active) {
				e.StopImmediatePropagation();
				return;
			}

			if(!CanStartManipulation(e)) {
				return;
			}

			var graphElement = target as Port;
			if(graphElement == null || !graphElement.ContainsPoint(e.localMousePosition)) {
				return;
			}

			m_MouseDownPosition = e.localMousePosition;
			m_clickedTime = Time.realtimeSinceStartup;

			m_EdgeCandidate = new TEdge();
			m_EdgeDragHelper.draggedPort = graphElement;
			m_EdgeDragHelper.edgeCandidate = m_EdgeCandidate;

			if(m_EdgeDragHelper.HandleMouseDown(e)) {
				m_Active = true;
				target.CaptureMouse();
				AutoHideGraphElement.RegisterNodeToIgnore(graphElement.node);

				e.StopPropagation();
			} else {
				m_EdgeDragHelper.Reset();
				m_EdgeCandidate = null;
			}
		}

		void OnMouseCaptureOut(MouseCaptureOutEvent e) {
			if(m_Active) {
				m_EdgeDragHelper.HandleMouseCaptureOut(e);
			}
			m_Active = false;
			if(m_EdgeCandidate != null)
				Abort();
		}

		protected virtual void OnMouseMove(MouseMoveEvent e) {
			if(!m_Active)
				return;

			m_EdgeDragHelper.HandleMouseMove(e);
			m_EdgeCandidate.candidatePosition = e.mousePosition;
			m_EdgeCandidate.UpdateEdgeControl();

			e.StopPropagation();
		}

		protected virtual void OnMouseUp(MouseUpEvent evt) {
			if(m_Active && evt.button == (int)MouseButton.RightMouse) {
				// Right click to cancel the edge connection.
				Abort();
				m_Active = false;
				uNodeThreadUtility.Queue(target.ReleaseMouse);
				evt.StopPropagation();
				return;
			}
			if(!m_Active || !CanStopManipulation(evt))
				return;

			if(CanPerformConnection(evt.localMousePosition)) {
				m_EdgeDragHelper.HandleMouseUp(evt);
			} else {
				Abort();
			}

			if(Time.realtimeSinceStartup - m_clickedTime < 0.2f) {
				//Debug.Log(Time.realtimeSinceStartup - m_clickedTime);
				if(target is PortView port) {
					uNodeThreadUtility.Queue(port.SendMakeConnectionEvent);
				}
			}

			m_Active = false;
			m_EdgeCandidate = null;
			target.ReleaseMouse();
			evt.StopPropagation();
		}

		private void OnKeyDown(KeyDownEvent e) {
			if(e.keyCode != KeyCode.Escape || !m_Active)
				return;

			Abort();

			m_Active = false;
			target.ReleaseMouse();
			e.StopPropagation();
		}

		void Abort() {
			var graphView = target?.GetFirstAncestorOfType<GraphView>();
			graphView?.RemoveElement(m_EdgeCandidate);

			m_EdgeCandidate.input = null;
			m_EdgeCandidate.output = null;
			m_EdgeCandidate = null;

			m_EdgeDragHelper.Reset();
			AutoHideGraphElement.ClearIgnoredNodes();
		}

		bool CanPerformConnection(Vector2 mousePosition) {
			return Vector2.Distance(m_MouseDownPosition, mousePosition) > k_ConnectionDistanceTreshold;
		}
	}
}