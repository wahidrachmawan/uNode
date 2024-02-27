using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	struct Line {
		public Vector2 Start;
		public Vector2 End;
		public Line(Vector2 start, Vector2 end) {
			Start = start;
			End = end;
		}
	}

	class LineView : ImmediateModeElement {
		public LineView() {
			this.pickingMode = PickingMode.Ignore;
			this.StretchToParentSize();
		}

		public List<Line> lines { get; } = new List<Line>();

		protected override void ImmediateRepaint() {
            var gView = GetFirstAncestorOfType<GraphView>();
			if (gView == null) {
				return;
			}
			var container = gView.contentViewContainer;
			foreach (var line in lines) {
				var start = container.ChangeCoordinatesTo(this, line.Start);
				var end = container.ChangeCoordinatesTo(this, line.End);
				var x = Math.Min(start.x, end.x);
				var y = Math.Min(start.y, end.y);
				var width = Math.Max(1, Math.Abs(start.x - end.x));
				var height = Math.Max(1, Math.Abs(start.y - end.y));
				var rect = new Rect(x, y, width, height);

				DrawRectangleOutline(rect, ColorPalette.snappingLineColor);
			}
		}
        
        private void DrawRectangleOutline(Rect rect, Color color) {
			Color color2 = Handles.color;
			Handles.color = color;
			Handles.DrawPolyLine(new Vector3(rect.x, rect.y, 0f), new Vector3(rect.x + rect.width, rect.y, 0f), new Vector3(rect.x + rect.width, rect.y + rect.height, 0f), new Vector3(rect.x, rect.y + rect.height, 0f), new Vector3(rect.x, rect.y, 0f));
			Handles.color = color2;
		}
	}

	abstract class SnapStrategy {
		internal class SnapResult {
			public Rect SnappableRect { get; set; }
			public float Offset { get; set; }
			public float Distance => Math.Abs(Offset);
		}

		protected enum SnapReference {
			LeftEdge,
			HorizontalCenter,
			RightEdge,
			TopEdge,
			VerticalCenter,
			BottomEdge
		}

		public bool Enabled { get; set; }

		protected float m_CurrentScale = 1.0f;
		protected UGraphView m_GraphView;

		public float SnapDistance { get; protected set; }
		public bool IsPaused { get; protected set; }
		public bool IsActive { get; protected set; }

		const float k_DefaultSnapDistance = 8.0f;

		protected SnapStrategy() {
			SnapDistance = k_DefaultSnapDistance;
		}

		public abstract void BeginSnap(UnityEditor.Experimental.GraphView.GraphElement selectedElement);

		public abstract Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, UnityEditor.Experimental.GraphView.GraphElement selectedElement, float scale, Vector2 mousePanningDelta = default);

		public abstract void EndSnap();

		public abstract void UpdateSnap();

		public void PauseSnap(bool isPaused) {
			if (!IsActive) {
				throw new InvalidOperationException("SnapStrategy.PauseSnap: Already inactive. Call BeginSnap() first.");
			}

			IsPaused = isPaused;
		}
	}
}