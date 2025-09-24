﻿using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace MaxyGames.UNode.Editors {
	public class EdgeView : Edge {
		public Connection connection;
		public bool isProxy => connection != null ? connection.isProxy : false;
		public bool alwaysVisible;
		protected IMGUIContainer iMGUIContainer;

		public bool isHidding;
		public string edgeLabel {
			get {
				if(m_EdgeBubble != null) {
					return m_EdgeBubble.text;
				}
				return null;
			}
		}

		private EdgeBubble m_EdgeBubble;

		//private UGraphView graphView;

		public PortView Output => output as PortView;
		public PortView Input => input as PortView;

		public bool isFlow => Output?.isFlow == true || Input?.isFlow == true;

		public EdgeView() {
			ReloadView();
		}

		public EdgeView(EdgeData data) {
			this.connection = data.connection;
			this.input = data.input;
			this.output = data.output;
			var port = data.input;
			if(port == null) {
				port = data.output;
			}
			if(port != null) {
				if(port.isFlow) {
					AddToClassList("flow");
				}
				else {
					AddToClassList("value");
				}
			}
			if(input != null && output != null) {
				RegisterCallback<MouseDownEvent>((e) => {
					if(isValid) {
						if(e.button == 0 && e.clickCount == 2) {
							var owner = data.input?.owner?.owner ?? data.output?.owner?.owner;
							if(owner != null) {
								var graph = owner.graphEditor;
								var mPos = this.ChangeCoordinatesTo(owner.contentViewContainer, e.localMousePosition);
								Undo.SetCurrentGroupName("Create Reroute");
								NodeEditorUtility.AddNewNode<Nodes.NodeReroute>(graph.graphData, null, null, new Vector2(mPos.x, mPos.y), (node) => {
									if(port.isFlow) {
										node.kind = Nodes.NodeReroute.RerouteKind.Flow;
										node.Register();
										node.exit.ConnectTo(data.input.GetPortValue());
										data.output.GetPortValue().ConnectTo(node.enter);
									}
									else {
										node.kind = Nodes.NodeReroute.RerouteKind.Value;
										node.Register();
										if(this is ConversionEdgeView) {
											node.output.ConnectTo((this as ConversionEdgeView).node.input);
											node.input.ConnectTo(data.output.GetPortValue());
											//node.input.ConnectTo((this as ConversionEdgeView).node.output);
										}
										else {
											node.input.ConnectTo(data.output.GetPortValue());
											data.input.GetPortValue().ConnectTo(node.output);
										}
									}
								});
								graph.Refresh();
								e.StopImmediatePropagation();
							}
						}
					}
				});
			}

			//if(input != null) {
			//	graphView = input.owner?.owner;
			//} else if(output != null) {
			//	graphView = output.owner?.owner;
			//}
			ReloadView();
		}

		public void SetEdgeVisible(bool enable) {
			if(selected || input?.node?.selected == true || output?.node?.selected == true) {
				enable = true;
			}
			edgeControl.visible = enable;
			if(m_EdgeBubble != null) {
				m_EdgeBubble.visible = enable;
			}
		}

		public void ReloadView() {
			//if(input == null || output == null) return;
			if(isProxy) {
				SetEdgeVisible(false);
				edgeControl.SetEnabled(false);
			}
			#region Debug
			if(GraphDebug.debugMessage.HasMessage(connection)) {
				if(m_EdgeBubble == null && isFlow) {
					SetEdgeLabel("");
				}
				this.AddToClassList("edge-debug");
			}
			if(Application.isPlaying && GraphDebug.useDebug) {
				//if(graphView != null) {
				//	graphView.RegisterIMGUI(this, DebugGUI);
				//}
				//iMGUIContainer = graphView.IMGUIContainer;
				if(iMGUIContainer == null) {
					iMGUIContainer = new IMGUIContainer(OnGUI);
					iMGUIContainer.style.flexGrow = 1;
					iMGUIContainer.style.flexShrink = 0;
					iMGUIContainer.pickingMode = PickingMode.Ignore;
					iMGUIContainer.cullingEnabled = true;
					edgeControl.Add(iMGUIContainer);
				}
			}
			else if(iMGUIContainer != null) {
				iMGUIContainer.RemoveFromHierarchy();
				iMGUIContainer = null;
			}
			#endregion
		}

		public void UpdateEndPoints() {
			if(input != null) {
				edgeControl.to = this.WorldToLocal(input.GetGlobalCenter());
				edgeControl.from = this.WorldToLocal(output.GetGlobalCenter());
			}
		}

		public void SetEdgeLabel(string value) {
			if(string.IsNullOrEmpty(value)) {
				if(m_EdgeBubble != null) {
					m_EdgeBubble.RemoveFromHierarchy();
					m_EdgeBubble = null;
				}
			}
			if(m_EdgeBubble == null) {
				m_EdgeBubble = new EdgeBubble();
				Add(m_EdgeBubble);
			}
			m_EdgeBubble.text = value;
			m_EdgeBubble.AttachTo(edgeControl, SpriteAlignment.Center);
			m_EdgeBubble.visible = edgeControl.visible;
		}

		protected virtual void OnGUI() {
			DebugGUI(true);
		}

		protected void DebugGUI(bool showLabel) {
			if(!visible || isHidding)
				return;
			if(Application.isPlaying && GraphDebug.useDebug) {
				PortView port = input as PortView ?? output as PortView;
				if(port != null && edgeControl.controlPoints != null && edgeControl.controlPoints.Length == 4) {
					GraphDebug.DebugData debugData = port.owner.owner.graphEditor.GetDebugInfo();
					if(debugData != null) {
						bool needUpdate = false;

						if(port.isFlow) {
							PortView portView = output as PortView;
							var portData = portView?.GetPortValue<FlowOutput>();
							if(portData == null) return;

							var debug = debugData.GetDebugValue(portData);
							if(debug.isValid) {
								var time = debug.time;
								if(debug.frame == uNodeThreadUtility.playingFrame) {
									time = GraphDebug.debugTime;
								}
								var times = (GraphDebug.debugTime - time) * GraphDebug.transitionSpeed;
								if(times >= 0) {
									if(Mathf.Abs(edgeControl.controlPoints[0].x - edgeControl.controlPoints[3].x) <= 4) {
										Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
										Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
										DrawDebug(v1, v4, edgeControl.inputColor, edgeControl.outputColor, times, true);
									}
									else {
										Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
										Vector2 v2 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[1]);
										Vector2 v3 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[2]);
										Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
										DrawDebug(new Vector2[] { v1, v2, v3, v4 }, edgeControl.inputColor, edgeControl.outputColor, times, true);
									}
									if(times <= 1) {
										needUpdate = true;
									}
								}
							}
						}
						else {
							PortView portView = input as PortView;
							var portData = portView?.GetPortValue<ValueInput>();
							if(portData == null) return;

							var debug = debugData.GetDebugValue(portData);

							if(debug.isValid) {
								var time = debug.time;
								if(debug.frame == uNodeThreadUtility.playingFrame) {
									time = GraphDebug.debugTime;
								}
								var times = (GraphDebug.debugTime - time) * GraphDebug.transitionSpeed;
								if(Mathf.Abs(edgeControl.controlPoints[0].y - edgeControl.controlPoints[3].y) <= 4) {
									Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
									Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
									if(debug.isSet) {
										DrawDebug(v4, v1, edgeControl.outputColor, edgeControl.inputColor, times, true);
									}
									else {
										DrawDebug(v1, v4, edgeControl.inputColor, edgeControl.outputColor, times, true);
									}
									if(times <= .5f) {
										needUpdate = true;
									}
									if(showLabel) {//Debug label
										GUIContent debugContent;
										if(debug.value != null) {
											debugContent = new GUIContent(
												uNodeUtility.GetDisplayName(debug.value),
												uNodeEditorUtility.GetTypeIcon(debug.value.GetType())
											);
										}
										else {
											debugContent = new GUIContent("null");
										}
										Vector2 vec = (v1 + v4) / 2;
										Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(debugContent.text));
										size.x += 25;
										GUI.Box(
											new Rect(vec.x - (size.x / 2), vec.y - 10, size.x - 10, 20),
											debugContent,
											EditorStyles.helpBox);
									}
								}
								else {
									Vector2 v1 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[0]);
									Vector2 v2 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[1]);
									Vector2 v3 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[2]);
									Vector2 v4 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[3]);
									if(debug.isSet) {
										DrawDebug(new Vector2[] { v4, v3, v2, v1 }, edgeControl.outputColor, edgeControl.inputColor, times, true);
									}
									else {
										DrawDebug(new Vector2[] { v1, v2, v3, v4 }, edgeControl.inputColor, edgeControl.outputColor, times, true);
									}
									if(times <= .5f) {
										needUpdate = true;
									}
									if(showLabel) {//Debug label
										GUIContent debugContent;
										if(debug.value != null) {
											debugContent = new GUIContent(
												uNodeUtility.GetDisplayName(debug.value),
												uNodeEditorUtility.GetTypeIcon(debug.value.GetType())
											);
										}
										else {
											debugContent = new GUIContent("null");
										}
										Vector2 vec = (v2 + v3) / 2;
										Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(debugContent.text));
										size.x += 25;
										GUI.Box(
											new Rect(vec.x - (size.x / 2), vec.y - 10, size.x - 10, 20),
											debugContent,
											EditorStyles.helpBox);
									}
								}
							}
						}
						if(needUpdate) {
							//This to make sure to the UI is always repaint.
							iMGUIContainer.MarkDirtyRepaint();
						}
					}
				}
			}
		}

		private static void DrawDebug(Vector2[] vectors, Color inColor, Color outColor, float time, bool isFlow) {
			float timer = Mathf.Lerp(1, 0, time * 2f);//The debug timer speed.
			float distance = 0;
			for(int i = 0; i + 1 < vectors.Length; i++) {
				distance += Vector2.Distance(vectors[i], vectors[i + 1]);
			}
			float quantity = 100f / distance;

			float size = 15 * timer;
			float pointDist = 0;
			int currentSegment = 0;

			if(isFlow) {
				for(float i = -1; i < 1; i += quantity) {
					float t = i + GraphDebug.debugLinesTimer * (quantity);
					if(!(t < 0f || t > 1)) {
						if(currentSegment + 1 >= vectors.Length) break;
						float seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						while(Mathf.Lerp(0, distance, t) > pointDist + seqmentDistance && currentSegment + 2 < vectors.Length) {
							pointDist += seqmentDistance;
							currentSegment++;
							seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						}
						var vec = Vector2.Lerp(
							vectors[currentSegment],
							vectors[currentSegment + 1],
							(Mathf.Lerp(0, distance, t) - pointDist) / seqmentDistance);
						GUI.color = new Color(
							Mathf.Lerp(outColor.r, inColor.r, t),
							Mathf.Lerp(outColor.g, inColor.g, t),
							Mathf.Lerp(outColor.b, inColor.b, t), 1);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint);
					}
				}
			}
			else {
				for(float i = -1; i < 1; i += quantity) {
					float t = i + GraphDebug.debugLinesTimer * (quantity);
					if(!(t < 0f || t > 1)) {
						if(currentSegment + 1 >= vectors.Length) break;
						float seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						while(Mathf.Lerp(0, distance, t) > pointDist + seqmentDistance && currentSegment + 2 < vectors.Length) {
							pointDist += seqmentDistance;
							currentSegment++;
							seqmentDistance = Vector2.Distance(vectors[currentSegment], vectors[currentSegment + 1]);
						}
						var vec = Vector2.Lerp(
							vectors[currentSegment + 1],
							vectors[currentSegment],
							(Mathf.Lerp(0, distance, t) - pointDist) / seqmentDistance);
						GUI.color = new Color(
							Mathf.Lerp(inColor.r, outColor.r, t),
							Mathf.Lerp(inColor.g, outColor.g, t),
							Mathf.Lerp(inColor.b, outColor.b, t), 1);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint);
					}
				}
			}
			GUI.color = Color.white;
		}

		public static void DrawDebug(Vector2 start, Vector2 end, Color inColor, Color outColor, float time, bool isFlow) {
			float timer = Mathf.Lerp(1, 0, time);//The debug timer speed.
			float dist = Vector2.Distance(start, end);
			float size = 15 * timer;
			float quantity = 100f / dist;
			if(isFlow) {
				for(float i = -1; i < 1; i += quantity) {
					float t = i + GraphDebug.debugLinesTimer * (quantity);
					if(!(t < 0f || t > 1)) {
						var color = new Color(
							Mathf.Lerp(outColor.r, inColor.r, t),
							Mathf.Lerp(outColor.g, inColor.g, t),
							Mathf.Lerp(outColor.b, inColor.b, t), 1);
						Vector2 vec = Vector2.Lerp(start, end, t);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
					}
				}
			}
			else {
				for(float i = -1; i < 1; i += quantity) {
					float t = i + GraphDebug.debugLinesTimer * (quantity);
					if(!(t < 0f || t > 1)) {
						var color = new Color(
							Mathf.Lerp(inColor.r, outColor.r, t),
							Mathf.Lerp(inColor.g, outColor.g, t),
							Mathf.Lerp(inColor.b, outColor.b, t), 1);
						Vector2 vec = Vector2.Lerp(end, start, t);
						GUI.DrawTexture(new Rect(vec.x - size / 2, vec.y - size / 2, size, size), uNodeUtility.DebugPoint, ScaleMode.ScaleToFit, true, 0, color, 0, 0);
					}
				}
			}
		}

		/// <summary>
		/// Get the sender port.
		/// Value edge will return Input port.
		/// Flow edge will return Output port.
		/// </summary>
		/// <returns></returns>
		public PortView GetSenderPort() {
			if(isFlow) {
				if(input.direction == Direction.Input) {
					return output as PortView;
				}
				else {
					return input as PortView;
				}
			}
			else {
				if(input.direction == Direction.Input) {
					return input as PortView;
				}
				else {
					return output as PortView;
				}
			}
		}

		/// <summary>
		/// Get the receiver port.
		/// Value edge will return Output port.
		/// Flow edge will return Input port.
		/// </summary>
		/// <returns></returns>
		public PortView GetReceiverPort() {
			if(isFlow) {
				if(input.direction == Direction.Input) {
					return input as PortView;
				}
				else {
					return output as PortView;
				}
			}
			else {
				if(input.direction == Direction.Input) {
					return output as PortView;
				}
				else {
					return input as PortView;
				}
			}
		}

		/// <summary>
		/// Is the edge is valid ( not is ghost and visible )
		/// </summary>
		public bool isValid => (parent != null || isHidding) && !isGhostEdge && this.IsVisible();

		public virtual void Disconnect() {
			PortView port;
			if(Input?.isValue == true) {
				port = Input;
			}
			else {
				port = Output;
			}
			connection?.Disconnect();
			port?.ResetPortValue();
		}

		#region Overrides
		public override bool Overlaps(Rect rectangle) {
			if(isProxy) return false;
			return base.Overlaps(rectangle);
		}

		public override bool ContainsPoint(Vector2 localPoint) {
			if(isProxy) return false;
			return base.ContainsPoint(localPoint);
		}

		public override void OnPortChanged(bool isInput) {
			edgeControl.outputOrientation = (output?.orientation ?? input?.orientation ?? Orientation.Horizontal);
			if(input != null) {
				edgeControl.inputOrientation = input.orientation;
			}
			else if(output != null) {
				if(output is PortView portView && portView.isFlow && portView.orientation == Orientation.Horizontal && portView.owner.owner.graphLayout == GraphLayout.Vertical) {
					edgeControl.inputOrientation = Orientation.Vertical;
				}
				else {
					edgeControl.inputOrientation = output.orientation;
				}
			}
			else {
				edgeControl.inputOrientation = Orientation.Horizontal;
			}
			if(m_EdgeBubble != null && input != null && output != null) {
				m_EdgeBubble.AttachTo(edgeControl, SpriteAlignment.Center);
			}
			UpdateEdgeControl();
		}
		#endregion
	}

	public abstract class EdgeViewWithNode : EdgeView {
		public readonly NodeObject nodeObject;

		public abstract UPort inputPortForNode { get; }
		public abstract UPort outputPortForNode { get; }

		public EdgeViewWithNode(NodeObject nodeObject) : base() {
			this.nodeObject = nodeObject;
		}

		public EdgeViewWithNode(NodeObject nodeObject, EdgeData edgeData) : base(edgeData) {
			this.nodeObject = nodeObject;
		}

		protected void InitializeView() {
			if(iMGUIContainer == null) {
				iMGUIContainer = new IMGUIContainer(OnGUI);
				iMGUIContainer.style.flexGrow = 1;
				iMGUIContainer.style.flexShrink = 0;
				iMGUIContainer.pickingMode = PickingMode.Ignore;
				edgeControl.Add(iMGUIContainer);
			}
		}

		protected override void OnGUI() {
			DebugGUI(false);
			if(Event.current.type == EventType.Repaint) {
				var errors = GraphUtility.ErrorChecker.GetErrorMessages(nodeObject);
				if(errors.Any()) {
					//System.Text.StringBuilder sb = new System.Text.StringBuilder();
					//for(int i = 0; i < errors.Count; i++) {
					//	if(i != 0) {
					//		sb.AppendLine();
					//		sb.AppendLine();
					//	}
					//	sb.Append("-" + uNodeEditorUtility.RemoveHTMLTag(errors[i].message));
					//}
					Vector2 v2 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[1]);
					Vector2 v3 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[2]);
					Vector2 vec = (v2 + v3) / 2;
					GUI.DrawTexture(new Rect(vec.x - 8, vec.y - 8, 16, 16), uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.MissingIcon)));
					return;
				}
				{//Icon
					Vector2 v2 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[1]);
					Vector2 v3 = this.ChangeCoordinatesTo(iMGUIContainer, edgeControl.controlPoints[2]);
					Vector2 vec = (v2 + v3) / 2;
					GUI.DrawTexture(new Rect(vec.x - 9, vec.y - 9, 18, 17), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1, 1, 1, 0.5f), 1, 4);
					GUI.DrawTexture(new Rect(vec.x - 8, vec.y - 8, 16, 16), uNodeEditorUtility.GetTypeIcon(nodeObject.GetNodeIcon()));
					//GUI.DrawTexture(new Rect(vec.x - 17, vec.y - 10, 34, 18), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1, 1, 1, 0.3f), 1, 4);
					//GUI.DrawTexture(new Rect(vec.x - 16, vec.y - 8, 16, 16), uNodeEditorUtility.GetTypeIcon(node.GetNodeIcon()));
					//GUI.DrawTexture(new Rect(vec.x, vec.y - 8, 16, 16), uNodeEditorUtility.GetTypeIcon(node.ReturnType()));
				}
			}
		}

		public override void Disconnect() {
			base.Disconnect();
			nodeObject.Destroy();
		}
	}

	public class ConversionEdgeView : EdgeViewWithNode {
		public Nodes.NodeValueConverter node;

		public ConversionEdgeView(Nodes.NodeValueConverter node, EdgeData edgeData) : base(node, edgeData) {
			this.node = node;
			InitializeView();
		}

		public override UPort inputPortForNode => node.input;
		public override UPort outputPortForNode => node.output;
	}

	public class TransitionEdgeView : EdgeView {
		/// <summary>
		/// The width of the transition arrow.
		/// </summary>
		const float arrowWidth = 12;

		/// <summary>
		/// The vertical offset used when drawing a self-transition arrow.
		/// </summary>
		const float selfArrowOffset = 35;

		public bool showArrow = true;

		/// <summary>
		/// Initializes the transition edge by registering callbacks for geometry changes and visual content drawing.
		/// </summary>
		public TransitionEdgeView() : base() {
			edgeControl.RegisterCallback<GeometryChangedEvent>(OnEdgeGeometryChanged);
			generateVisualContent += DrawArrow;
		}
		public TransitionEdgeView(EdgeData data) : base(data) {
			edgeControl.RegisterCallback<GeometryChangedEvent>(OnEdgeGeometryChanged);
			generateVisualContent += DrawArrow;
		}

		/// <summary>
		/// Determines whether a given point is within the bounds of the transition edge
		/// </summary>
		public override bool ContainsPoint(Vector2 localPoint) {
			if(base.ContainsPoint(localPoint)) {
				return true;
			}

			Vector2 start = PointsAndTangents[PointsAndTangents.Length / 2 - 1];
			Vector2 end = PointsAndTangents[PointsAndTangents.Length / 2];
			Vector2 mid = (start + end) / 2;

			if(IsSelfTransition()) {
				mid = PointsAndTangents[0] + Vector2.up * selfArrowOffset;
			}

			return (localPoint - mid).sqrMagnitude <= (arrowWidth * arrowWidth);
		}


		/// <summary>
		/// Callback method for handling geometry changes in the edge's layout.
		/// </summary>
		/// <param name="evt">The event data containing information about the geometry change.</param>
		void OnEdgeGeometryChanged(GeometryChangedEvent evt) {
			if(Mathf.Abs(Mathf.RoundToInt(PointsAndTangents[0].x) - Mathf.RoundToInt(PointsAndTangents[3].x)) > 2) {
				PointsAndTangents[1] = PointsAndTangents[0];
				PointsAndTangents[2] = PointsAndTangents[3];
			}

			MarkDirtyRepaint();
		}

		/// <summary>
		/// Draws the arrow for the transition edge based on its points and tangents.
		/// </summary>
		/// <param name="context">The mesh generation context used to allocate vertices and set visual data.</param>
		void DrawArrow(MeshGenerationContext context) {
			if(showArrow == false) return;
			Vector2 start = PointsAndTangents[0];
			Vector2 end = PointsAndTangents[3];
			//if(start.x == end.x) {
			//	start.x += 2;
			//	end.x -= 2;
			//}
			//if(start.y == end.y) {
			//	start.y += 2;
			//	end.y -= 2;
			//}
			Vector2 mid = (start + end) / 2;
			Vector2 direction = end - start;

			if(IsSelfTransition()) {
				mid = PointsAndTangents[0] + Vector2.up * selfArrowOffset;
				direction = Vector2.down;
			}
			else {
				if(direction.x == 0) {
					direction.x += 4;
				}
				if(direction.y == 0) {
					direction.y += 4;
				}
			}

			float distanceFromMid = arrowWidth * Mathf.Sqrt(3) / 4;
			float angle = Vector2.SignedAngle(Vector2.right, direction);
			float perpendicularLength = GetPerpendicularLength(angle);

			Vector2 perpendicular = new Vector2(-direction.y, direction.x).normalized * perpendicularLength;

			if(IsSelfTransition()) {
				perpendicular = Vector2.right * perpendicularLength;
			}

			MeshWriteData mesh = context.Allocate(3, 3);
			Vertex[] vertices = new Vertex[3];

			vertices[0].position = mid + (direction.normalized * distanceFromMid);
			vertices[1].position = mid + (-direction.normalized * distanceFromMid) + (perpendicular.normalized * arrowWidth / 2);
			vertices[2].position = mid + (-direction.normalized * distanceFromMid) + (-perpendicular.normalized * arrowWidth / 2);

			for(int i = 0; i < vertices.Length; i++) {
				vertices[i].position += Vector3.forward * Vertex.nearZ;
				vertices[i].tint = GetColor();
			}

			mesh.SetAllVertices(vertices);
			mesh.SetAllIndices(new ushort[] { 0, 1, 2 });
		}

		/// <summary>
		/// Determines if the transition is a self-transition (where the input and output nodes are the same).
		/// </summary>
		/// <returns>True if the transition is a self-transition; otherwise, false.</returns>
		bool IsSelfTransition() {
			if(input != null && output != null) {
				return input.node == output.node;
			}

			return false;
		}

		/// <summary>
		/// Calculates the length of the perpendicular line for the arrow's direction based on the angle.
		/// </summary>
		/// <param name="angle">The angle of the arrow in degrees.</param>
		/// <returns>The perpendicular length used for arrow drawing.</returns>
		float GetPerpendicularLength(float angle) {
			if(angle < 60 && angle > 0) {
				return arrowWidth / (Mathf.Sin(Mathf.Deg2Rad * (angle + 120)) * 2);
			}
			else if(angle > -120 && angle < -60) {
				return arrowWidth / (Mathf.Sin(Mathf.Deg2Rad * (angle - 120)) * 2);
			}
			else if(angle > -60 && angle < 0) {
				return arrowWidth / (Mathf.Sin(Mathf.Deg2Rad * (angle + 60)) * 2);
			}

			return arrowWidth / (Mathf.Sin(Mathf.Deg2Rad * (angle - 60)) * 2);
		}

		/// <summary>
		/// Retrieves the color used for the transition edge based on its state.
		/// </summary>
		/// <returns>The color of the transition edge.</returns>
		Color GetColor() {
			Color color = defaultColor;

			if(output != null) {
				color = output.portColor;
			}

			if(selected) {
				color = selectedColor;
			}

			if(isGhostEdge) {
				color = ghostColor;
			}

			return color;
		}
	}
}