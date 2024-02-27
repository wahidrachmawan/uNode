using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	class Snapper {
		GraphView m_GraphView;
		List<SnapStrategy> m_SnappingStrategies = new List<SnapStrategy>();

		internal bool IsActive => m_SnappingStrategies.Any(s => s.Enabled);

		internal Snapper() {
			InitSnappingStrategies();
		}

		void InitSnappingStrategies() {
			m_SnappingStrategies.Add(new SnapToBordersStrategy());
			m_SnappingStrategies.Add(new SnapToPortStrategy());
			m_SnappingStrategies.Add(new SnapToGridStrategy());
			m_SnappingStrategies.Add(new SnapToSpacingStrategy());
		}

		internal void BeginSnap(GraphElement selectedElement) {
            foreach(var snapStrategy in m_SnappingStrategies) {
				snapStrategy.UpdateSnap();
			}
			foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled)) {
				snapStrategy.BeginSnap(selectedElement);
			}
		}

		internal Rect GetSnappedRect(Rect sourceRect, GraphElement selectedElement, float scale, Vector2 mousePanningDelta = default) {
			Rect snappedRect = sourceRect;

			foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled)) {
				AdjustSnappedRect(ref snappedRect, sourceRect, selectedElement, scale, snapStrategy, mousePanningDelta);
			}

			return snappedRect;
		}

		internal void EndSnap() {
			foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled)) {
				snapStrategy.EndSnap();
			}
		}

		internal void PauseSnap(bool isPaused) {
			foreach (var snapStrategy in m_SnappingStrategies.Where(snapStrategy => snapStrategy.Enabled)) {
				snapStrategy.PauseSnap(isPaused);
			}
		}

		static void AdjustSnappedRect(ref Rect snappedRect, Rect sourceRect, GraphElement selectedElement, float scale, SnapStrategy snapStrategy, Vector2 mousePanningDelta = default) {
			// Retrieve the snapping strategy's suggested snapped rect and its snapping offset
			Vector2 snappingOffset = new Vector2(float.MaxValue, float.MaxValue);
			Rect suggestedSnappedRect = snapStrategy.GetSnappedRect(ref snappingOffset, sourceRect, selectedElement, scale, mousePanningDelta);

			// Set snapped rect coordinates using the suggested rect's relevant coordinates
			SetSnappedRect(ref snappedRect, suggestedSnappedRect, snappingOffset);
		}

		static void SetSnappedRect(ref Rect snappedRect, Rect suggestedSnappedRect, Vector2 snappingOffset) {
			// If the snapping offset is smaller than float.MaxValue, the coordinate value needs to be considered
			if (snappingOffset.y < float.MaxValue) {
				snappedRect.y = suggestedSnappedRect.y;
			}
			if (snappingOffset.x < float.MaxValue) {
				snappedRect.x = suggestedSnappedRect.x;
			}
		}
	}
}
