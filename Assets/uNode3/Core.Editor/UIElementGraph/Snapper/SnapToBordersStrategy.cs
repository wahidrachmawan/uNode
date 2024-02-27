using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	class SnapToBordersStrategy : SnapStrategy {
		class SnapToBordersResult : SnapResult {
			public SnapReference SourceReference { get; set; }
			public SnapReference SnappableReference { get; set; }
			public Line IndicatorLine;
		}

		static float GetPos(Rect rect, SnapReference reference) {
			switch (reference) {
				case SnapReference.LeftEdge:
					return rect.x;
				case SnapReference.HorizontalCenter:
					return rect.center.x;
				case SnapReference.RightEdge:
					return rect.xMax;
				case SnapReference.TopEdge:
					return rect.y;
				case SnapReference.VerticalCenter:
					return rect.center.y;
				case SnapReference.BottomEdge:
					return rect.yMax;
				default:
					return 0;
			}
		}

		public override void UpdateSnap() {
			var preference = uNodePreference.GetPreference();
			Enabled = preference.enableSnapping && preference.graphSnapping;
		}

		LineView m_LineView;
		List<Rect> m_SnappableRects = new List<Rect>();

		public override void BeginSnap(GraphElement selectedElement) {
			//if (IsActive) {
			//	throw new InvalidOperationException("SnapStrategy.BeginSnap: Snap to borders already active. Call EndSnap() first.");
			//}
			IsActive = true;

			if (m_LineView == null) {
				m_LineView = new LineView();
			}

			m_GraphView = selectedElement.GetFirstAncestorOfType<UGraphView>();
			m_GraphView.Add(m_LineView);
			m_SnappableRects = GetNotSelectedElementRectsInView(selectedElement);
		}

		public override Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement, float scale, Vector2 mousePanningDelta = default) {
			if(!IsActive) {
				throw new InvalidOperationException("SnapStrategy.GetSnappedRect: Snap to borders not active. Call BeginSnap() first.");
			}

			if (IsPaused) {
				// Snapping was paused, we do not return a snapped rect and we clear the snap lines
				ClearSnapLines();
				return sourceRect;
			}
			//if(selectedElement is RegionNodeView)
			//	return sourceRect;

			Rect snappedRect = sourceRect;
			m_CurrentScale = scale;

			List<SnapToBordersResult> results = GetClosestSnapElements(sourceRect);

			m_LineView.lines.Clear();

			foreach (SnapToBordersResult result in results) {
				ApplySnapToBordersResult(ref snappingOffset, sourceRect, ref snappedRect, result);
				result.IndicatorLine = GetSnapLine(snappedRect, result.SourceReference, result.SnappableRect, result.SnappableReference);
				m_LineView.lines.Add(result.IndicatorLine);
			}
			m_LineView.MarkDirtyRepaint();

			m_SnappableRects = GetNotSelectedElementRectsInView(selectedElement);

			return snappedRect;
		}

		public override void EndSnap() {
			if (!IsActive) {
				throw new InvalidOperationException("SnapStrategy.EndSnap: Snap to borders already inactive. Call BeginSnap() first.");
			}
			IsActive = false;

			m_SnappableRects.Clear();
			m_LineView.lines.Clear();
			m_LineView.Clear();
			m_LineView.RemoveFromHierarchy();
		}

		List<Rect> GetNotSelectedElementRectsInView(GraphElement selectedElement) {
			List<Rect> notSelectedElementRects = new List<Rect>();
			var ignoredElements = m_GraphView.selection.OfType<GraphElement>().ToList();

			if(ignoredElements.Count == 1 && ignoredElements[0] is UNodeView && uNodePreference.preferenceData.carryNodes) {
				ignoredElements.AddRange(UIElementUtility.Nodes.FindNodeToCarry(ignoredElements[0] as UNodeView));
			}

			// Consider only the visible nodes.
			Rect rectToFit = m_GraphView.layout;

			foreach (var element in m_GraphView.graphElements.ToList()) {
				//if (selectedElement is RegionNodeView region && element.layout.Overlaps(region.layout)) {
				//    // If the selected element is a region, we do not consider the elements under it
				//    ignoredElements.Add(element);
				//} else 
				if (element is Edge) {
					// Don't consider edges
					ignoredElements.Add(element);
				} else if (!element.visible) {
					// Don't consider not visible elements
					ignoredElements.Add(element);
				} else if(element is not UNodeView) {
					// Don't consider not node elements
					ignoredElements.Add(element);
                } else if (!element.IsSelected(m_GraphView) && !(ignoredElements.Contains(element))) {
					var nodeView = element as UNodeView;
					if(nodeView.isBlock && ignoredElements.Contains(nodeView.ownerBlock as UNodeView)) {
						//Ignore invalid node
						ignoredElements.Add(element);
					}
					else if(ignoredElements.Contains(element) == false){
						var localSelRect = m_GraphView.ChangeCoordinatesTo(element, rectToFit);
						if(element.Overlaps(localSelRect)) {
							Rect geometryInContentViewContainerSpace = (element).parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, (element).GetPosition());
							notSelectedElementRects.Add(geometryInContentViewContainerSpace);
						}
					}
				}
				else {
					ignoredElements.Add(element);
				}
			}

			return notSelectedElementRects;
		}

		SnapToBordersResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, Rect snappableRect, SnapReference startReference, SnapReference endReference) {
			float sourcePos = GetPos(sourceRect, sourceRef);
			float offsetStart = sourcePos - GetPos(snappableRect, startReference);
			float offsetEnd = sourcePos - GetPos(snappableRect, endReference);
			float minOffset = offsetStart;
			SnapReference minSnappableReference = startReference;
			if (Math.Abs(minOffset) > Math.Abs(offsetEnd)) {
				minOffset = offsetEnd;
				minSnappableReference = endReference;
			}
			SnapToBordersResult minResult = new SnapToBordersResult {
				SourceReference = sourceRef,
				SnappableRect = snappableRect,
				SnappableReference = minSnappableReference,
				Offset = minOffset
			};

			return minResult.Distance <= SnapDistance * 1 / m_CurrentScale ? minResult : null;
		}

		SnapToBordersResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, SnapReference startReference, SnapReference endReference) {
			SnapToBordersResult minResult = null;
			float minDistance = float.MaxValue;
			foreach (Rect snappableRect in m_SnappableRects) {
				SnapToBordersResult result = GetClosestSnapElement(sourceRect, sourceRef, snappableRect, startReference, endReference);
				if (result != null && minDistance > result.Distance) {
					minDistance = result.Distance;
					minResult = result;
				}
			}
			return minResult;
		}

		List<SnapToBordersResult> GetClosestSnapElements(Rect sourceRect, Orientation orientation) {
			SnapReference startReference = orientation == Orientation.Horizontal ? SnapReference.LeftEdge : SnapReference.TopEdge;
			SnapReference centerReference = orientation == Orientation.Horizontal ? SnapReference.HorizontalCenter : SnapReference.VerticalCenter;
			SnapReference endReference = orientation == Orientation.Horizontal ? SnapReference.RightEdge : SnapReference.BottomEdge;
			List<SnapToBordersResult> results = new List<SnapToBordersResult>(3);
			SnapToBordersResult result = GetClosestSnapElement(sourceRect, startReference, startReference, endReference);
			if (result != null)
				results.Add(result);
			result = GetClosestSnapElement(sourceRect, centerReference, startReference, endReference);
			if (result != null)
				results.Add(result);
			result = GetClosestSnapElement(sourceRect, endReference, startReference, endReference);
			if (result != null)
				results.Add(result);
			// Look for the minimum
			if (results.Count > 0) {
				results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
				float minDistance = results[0].Distance;
				results.RemoveAll(r => Math.Abs(r.Distance - minDistance) > 0.01f);
			}
			return results;
		}

		List<SnapToBordersResult> GetClosestSnapElements(Rect sourceRect) {
			List<SnapToBordersResult> snapToBordersResults = GetClosestSnapElements(sourceRect, Orientation.Horizontal);
			return snapToBordersResults.Union(GetClosestSnapElements(sourceRect, Orientation.Vertical)).ToList();
		}

		static Line GetSnapLine(Rect r, SnapReference reference) {
			Vector2 start;
			Vector2 end;
			switch (reference) {
				case SnapReference.LeftEdge:
					start = r.position;
					end = new Vector2(r.x, r.yMax);
					break;
				case SnapReference.HorizontalCenter:
					start = r.center;
					end = start;
					break;
				case SnapReference.RightEdge:
					start = new Vector2(r.xMax, r.yMin);
					end = new Vector2(r.xMax, r.yMax);
					break;
				case SnapReference.TopEdge:
					start = r.position;
					end = new Vector2(r.xMax, r.yMin);
					break;
				case SnapReference.VerticalCenter:
					start = r.center;
					end = start;
					break;
				default: // case SnapReference.BottomEdge:
					start = new Vector2(r.x, r.yMax);
					end = new Vector2(r.xMax, r.yMax);
					break;
			}
			return new Line(start, end);
		}

		static Line GetSnapLine(Rect r1, SnapReference reference1, Rect r2, SnapReference reference2) {
			bool horizontal = reference1 <= SnapReference.RightEdge;
			Line line1 = GetSnapLine(r1, reference1);
			Line line2 = GetSnapLine(r2, reference2);
			Vector2 p11 = line1.Start;
			Vector2 p12 = line1.End;
			Vector2 p21 = line2.Start;
			Vector2 p22 = line2.End;
			Vector2 start;
			Vector2 end;

			if (horizontal) {
				float x = p21.x;
				float yMin = Math.Min(p22.y, Math.Min(p21.y, Math.Min(p11.y, p12.y)));
				float yMax = Math.Max(p22.y, Math.Max(p21.y, Math.Max(p11.y, p12.y)));
				start = new Vector2(x, yMin);
				end = new Vector2(x, yMax);
			} else {
				float y = p22.y;
				float xMin = Math.Min(p22.x, Math.Min(p21.x, Math.Min(p11.x, p12.x)));
				float xMax = Math.Max(p22.x, Math.Max(p21.x, Math.Max(p11.x, p12.x)));
				start = new Vector2(xMin, y);
				end = new Vector2(xMax, y);
			}
			return new Line(start, end);
		}

		static void ApplySnapToBordersResult(ref Vector2 snappingOffset, Rect sourceRect, ref Rect r1, SnapToBordersResult result) {
			if (result.SnappableReference <= SnapReference.RightEdge) {
				r1.x = sourceRect.x - result.Offset;
				snappingOffset.x = snappingOffset.x < float.MaxValue ? snappingOffset.x + result.Offset : result.Offset;
			} else {
				r1.y = sourceRect.y - result.Offset;
				snappingOffset.y = snappingOffset.y < float.MaxValue ? snappingOffset.y + result.Offset : result.Offset;
			}
		}

		void ClearSnapLines() {
			m_LineView.lines.Clear();
			m_LineView.MarkDirtyRepaint();
		}
	}
}