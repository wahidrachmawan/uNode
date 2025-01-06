using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public class GraphDragger : MouseManipulator {
		bool m_rightClickPressed;
		bool m_hasMove;

		public bool isActive {
			get {
				return m_Active || m_rightClickPressed;
			}
		}

		private Vector2 m_Start;

		private bool m_Active;

		public Vector2 panSpeed {
			get;
			set;
		}

		public bool clampToParentEdges {
			get;
			set;
		}

		public GraphDragger() {
			m_Active = false;
			activators.Add(new ManipulatorActivationFilter {
				button = MouseButton.LeftMouse,
				modifiers = EventModifiers.Alt
			});
			activators.Add(new ManipulatorActivationFilter {
				button = MouseButton.MiddleMouse
			});
			if(uNodePreference.preferenceData.disableRightClickMove == false) {
				activators.Add(new ManipulatorActivationFilter {
					button = MouseButton.RightMouse
				});
			}
			panSpeed = new Vector2(1f, 1f);
			clampToParentEdges = false;
		}

		protected Rect CalculatePosition(float x, float y, float width, float height) {
			Rect result = new Rect(x, y, width, height);
			if(clampToParentEdges) {
				Rect rect = base.target.hierarchy.parent.layout;
				rect.x = 0;
				rect.y = 0;
				if(result.x < rect.xMin) {
					result.x = rect.xMin;
				} else if(result.xMax > rect.xMax) {
					result.x = rect.xMax - result.width;
				}
				if(result.y < rect.yMin) {
					result.y = rect.yMin;
				} else if(result.yMax > rect.yMax) {
					result.y = rect.yMax - result.height;
				}
				result.width = width;
				result.height = height;
			}
			return result;
		}

		protected override void RegisterCallbacksOnTarget() {
			GraphView graphView = target as GraphView;
			if(graphView == null) {
				throw new InvalidOperationException("Manipulator can only be added to a GraphView");
			}
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
		}

		public void OnMouseDown(MouseDownEvent e) {
			if(m_Active) {
				e.StopImmediatePropagation();
			} else if(CanStartManipulation(e)) {
				GraphView graphView = target as GraphView;
				if(graphView != null) {
					m_Start = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, e.localMousePosition);
					m_Active = true;
					m_rightClickPressed = e.button == 1;
					target.CaptureMouse();
					e.StopImmediatePropagation();
				}
			}
		}

		public void OnMouseMove(MouseMoveEvent e) {
			if(m_Active) {
				m_hasMove = true;
				GraphView graphView = target as GraphView;
				if(graphView != null) {
					Vector2 v = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, e.localMousePosition) - m_Start;
					Vector3 scale = graphView.contentViewContainer.transform.scale;
					graphView.viewTransform.position += Vector3.Scale(v, scale);
					graphView.UpdateViewTransform(graphView.viewTransform.position, graphView.viewTransform.scale);
					e.StopPropagation();
				}
			}
		}

		public void OnMouseUp(MouseUpEvent e) {
			if(m_Active && CanStopManipulation(e)) {
				GraphView graphView = target as GraphView;
				if(graphView != null) {
					Vector3 position = graphView.contentViewContainer.transform.position;
					Vector3 scale = graphView.contentViewContainer.transform.scale;
					graphView.UpdateViewTransform(position, scale);
					if(m_rightClickPressed && m_hasMove) {
						uNodeThreadUtility.Queue(() => {
							m_Active = false;
							target.ReleaseMouse();
							e.StopImmediatePropagation();
						});
						e.IgnoreEvent();
					}
					else {
						m_Active = false;
						target.ReleaseMouse();
						e.StopImmediatePropagation();
					}
				}
				m_rightClickPressed = false;
				m_hasMove = false;
			}
		}
	}
}