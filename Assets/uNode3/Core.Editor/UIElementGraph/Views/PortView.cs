using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	public class PortView : Port, IEdgeConnectorListener {
		public new Type portType;
		/// <summary>
		/// The owner of the port
		/// </summary>
		public UNodeView owner => portData?.owner;

		public new string portName {
			get {
				return base.portName;
			}
			set {
				base.portName = value;
				m_ConnectorText.EnableInClassList("ui-hidden", string.IsNullOrEmpty(value));
			}
		}

		public PortData portData;

		protected ControlView controlView;
		protected Image portIcon;
		protected bool displayProxyTitle = true;

		//private static CustomStyleProperty<int> s_ProxyOffsetX = new CustomStyleProperty<int>("--proxy-offset-x");
		//private static CustomStyleProperty<int> s_ProxyOffsetY = new CustomStyleProperty<int>("--proxy-offset-y");

		List<EdgeView> edges = new List<EdgeView>();

		/// <summary>
		/// True if the port is flow port
		/// </summary>
		public bool isFlow => portData.isFlow;
		/// <summary>
		/// True if the port is value port
		/// </summary>
		public bool isValue => !portData.isFlow;

		#region Initialization
		public PortView(Orientation orientation, Direction direction, PortData portData) : base(orientation, direction, Capacity.Multi, typeof(object)) {
			this.portData = portData;
			portData.portView = this;
			this.AddStyleSheet("uNodeStyles/NativePortStyle");
			this.AddStyleSheet(UIElementUtility.Theme.portStyle);
			if(portData.isFlow) {
				AddToClassList("flow-port");
				if(orientation == Orientation.Vertical) {
					AddToClassList("flow-vertical");
				}
				else {
					AddToClassList("flow-horizontal");
				}
			}
			else {
				AddToClassList("value-port");
			}

			m_EdgeConnector = new EdgeConnector<EdgeView>(this);
			this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
			this.AddManipulator(m_EdgeConnector);
			DoUpdate();
			this.ExecuteAndScheduleAction(DoUpdate, 1000);
		}

		public virtual void Initialize(PortData portData) {
			if(portData.owner == null) {
				throw new Exception("The port owner must be assigned.");
			}
			this.portData = portData;
			ReloadView(true);
		}

		public virtual void ReloadView(bool refreshName = false) {
			if(portData != null) {
				portType = portData.portType;
				if(refreshName) {
					portName = ObjectNames.NicifyVariableName(portData.name);
					tooltip = portData.tooltip;
				}
			}
			if(isValue) {
				//portColor = new Color(0.09f, 0.7f, 0.4f);
				portColor = uNodePreference.GetColorForType(portType);
				if(portIcon == null) {
					portIcon = new Image();
					Insert(1, portIcon);
				}
				portIcon.image = uNodeEditorUtility.GetTypeIcon(portType);
				// portIcon.style.width = 16;
				// portIcon.style.height = 16;
				portIcon.pickingMode = PickingMode.Ignore;
			}
			else if(owner.flowLayout == Orientation.Horizontal) {
				if(portIcon == null) {
					portIcon = new Image();
					Insert(1, portIcon);
				}
				portIcon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon));
				portIcon.pickingMode = PickingMode.Ignore;
			}
			UpdatePortClass();
		}

		private void UpdatePortClass() {
			if(connected)
				AddToClassList("connected");
			else
				RemoveFromClassList("connected");

			switch(direction) {
				case Direction.Input: {
					EnableInClassList("input", true);
					EnableInClassList("output", false);
				}
				break;
				case Direction.Output:
					EnableInClassList("input", false);
					EnableInClassList("output", true);
					break;
			}
		}
		#endregion

		void BuildContextualMenu(ContextualMenuPopulateEvent evt) {

		}

		public void DisplayProxyTitle(bool value) {
			displayProxyTitle = value;
			proxyContainer?.RemoveFromHierarchy();
			proxyContainer = null;
			DoUpdate();
		}

		void DoUpdate() {
			if(portData != null && owner.IsVisible()) {
				bool isProxy = false;
				if(isFlow) {
					if(direction == Direction.Input) {
						var edges = GetEdges();
						if(edges != null && edges.Count > 0) {
							if(edges.Any(e => e != null && e.isProxy)) {
								isProxy = true;
							}
						}
					}
					else {
						if(connected) {
							var edges = GetEdges();
							if(edges != null && edges.Count > 0) {
								if(edges.Any(e => e != null && e.isProxy)) {
									isProxy = true;
								}
							}
						}
					}
				}
				else {
					if(direction == Direction.Input) {
						if(connected) {
							var edges = GetEdges();
							if(edges != null && edges.Count > 0) {
								if(edges.Any(e => e != null && e.isProxy)) {
									isProxy = true;
								}
							}
						}
					}
					else {
						var edges = GetEdges();
						if(edges != null && edges.Count > 0) {
							if(edges.Any(e => e != null && e.isProxy)) {
								isProxy = true;
							}
						}
					}
				}
				ToggleProxy(isProxy);
				if(isProxy) {
					var color = portColor;
					proxyContainer.style.backgroundColor = color;
					proxyCap.style.backgroundColor = color;
					proxyLine.style.backgroundColor = color;
					if(proxyTitleBox != null) {
						var edges = GetEdges();
						if(edges != null && edges.Count > 0) {
							var edge = edges.FirstOrDefault(e => e != null && e.isValid && e.isProxy);
							if(edge != null && proxyTitleLabel != null) {
								string label = null;
								if(!string.IsNullOrEmpty(edge.edgeLabel)) {
									if(isFlow) {
										if(direction == Direction.Input) {
											label = edge.edgeLabel;
										}
									}
									else {
										if(direction == Direction.Output) {
											label = edge.edgeLabel;
										}
									}
								}
								PortView port = edge.input != this ? edge.input as PortView : edge.output as PortView;
								if(port != null) {
									proxyTitleLabel.text = label ?? uNodeEditorUtility.RemoveHTMLTag(port.GetProxyName());
									if(isValue) {
										proxyTitleBox.style.SetBorderColor(port.portColor);
										proxyTitleIcon.image = port.portIcon?.image;
									}
								}
							}
						}
						MarkRepaintProxyTitle();
					}
				}
			}
		}

		private bool flagRepaintProxy;
		void MarkRepaintProxyTitle() {
			if(proxyTitleBox != null && !flagRepaintProxy) {
				flagRepaintProxy = true;
				if(orientation == Orientation.Horizontal && direction == Direction.Input) {
					proxyTitleBox.ScheduleOnce(() => {
						flagRepaintProxy = false;
						proxyTitleBox.style.left = -proxyTitleBox.layout.width;
					}, 0);
				}
			}
		}

		public override bool ContainsPoint(Vector2 localPoint) {
			if(isFlow && orientation == Orientation.Vertical && owner.ClassListContains("reroute-flow")) {
				Rect layout = m_ConnectorBox.layout;

				return layout.Contains(localPoint);
				//return new Rect(0.0f, 0.0f, layout.width, layout.height).Contains(localPoint);
			}
			else {
				Rect layout = m_ConnectorBox.layout;
				//Rect rect;
				//if(direction == Direction.Input) {
				//	rect = new Rect(0f - layout.xMin, 0f - layout.yMin, layout.width + layout.xMin, this.layout.height);
				//} else {
				//	rect = new Rect(-5, 0f - layout.yMin, this.layout.width - layout.xMin, this.layout.height);
				//}
				//rect.width += 5;
				//return rect.Contains(this.ChangeCoordinatesTo(m_ConnectorBox, localPoint));
				var marginWidth = m_ConnectorBox.resolvedStyle.marginLeft + m_ConnectorBox.resolvedStyle.marginRight;
				var marginHeight = layout.y;
				layout.x -= marginWidth;
				layout.width += marginWidth * 2;
				layout.y -= marginHeight;
				layout.height += marginHeight * 2;
				return layout.Contains(localPoint);
			}
		}

		protected override void OnCustomStyleResolved(ICustomStyle styles) {
			if(isValue) {
				portColor = uNodePreference.GetColorForType(portType);
			}
			base.OnCustomStyleResolved(styles);
		}

		#region Proxy
		private VisualElement proxyContainer;
		private VisualElement proxyCap;
		private VisualElement proxyLine;
		private VisualElement proxyTitleBox;
		private Label proxyTitleLabel;
		private Image proxyTitleIcon;
		private IMGUIContainer proxyDebug;

		public float GetProxyWidth() {
			if(displayProxyTitle) {
				if(proxyTitleBox != null) {
					return proxyTitleBox.layout.width + 20;
				}
			}
			return 0;
		}

		protected void ToggleProxy(bool enable) {
			if(enable) {
				if(proxyContainer == null) {
					VisualElement connector = this.Q("connector");
					proxyContainer = new VisualElement { name = "connector" };
					proxyContainer.pickingMode = PickingMode.Ignore;
					proxyContainer.EnableInClassList("proxy", true);
					{
						proxyCap = new VisualElement() { name = "cap" };
						proxyCap.Add(proxyLine = new VisualElement() { name = "proxy-line" });
						proxyContainer.Add(proxyCap);
					}
					bool displayTitle = displayProxyTitle;
					if(displayTitle) {
						if(isFlow) {
							if(direction != Direction.Output) {
								var edge = GetValidEdges().FirstOrDefault();
								if(string.IsNullOrEmpty(edge.edgeLabel)) {
									displayTitle = false;
								}
							}
						}
						else {
							displayTitle = direction == Direction.Input;
						}
					}
					if(displayTitle) {
						proxyTitleBox = new VisualElement() {
							name = "proxy-title",
						};
						proxyTitleBox.pickingMode = PickingMode.Ignore;
						proxyTitleLabel = new Label();
						proxyTitleLabel.pickingMode = PickingMode.Ignore;
						proxyTitleBox.Add(proxyTitleLabel);
						proxyContainer.Add(proxyTitleBox);
						if(isValue) {
							proxyTitleBox.AddToClassList("proxy-value");
							proxyTitleIcon = new Image();
							proxyTitleIcon.pickingMode = PickingMode.Ignore;
							proxyTitleBox.Add(proxyTitleIcon);
						}
						else {
							if(direction == Direction.Output) {
								proxyTitleBox.AddToClassList("proxy-flow");
							}
							else {
								proxyTitleBox.AddToClassList("proxy-flow");
							}
						}
						MarkRepaintProxyTitle();
					}
					if(Application.isPlaying && isValue && direction == Direction.Input) {
						if(proxyDebug != null) {
							proxyDebug.RemoveFromHierarchy();
						}
						proxyDebug = new IMGUIContainer(DebugGUI);
						proxyDebug.style.position = Position.Absolute;
						proxyDebug.style.overflow = Overflow.Visible;
						proxyDebug.pickingMode = PickingMode.Ignore;
						proxyContainer.Add(proxyDebug);
					}
					connector.Add(proxyContainer);
					MarkDirtyRepaint();
				}
			}
			else if(proxyContainer != null) {
				proxyContainer.RemoveFromHierarchy();
				proxyContainer = null;
			}
		}
		#endregion

		#region Debug
		void DebugGUI() {
			if(Application.isPlaying && GraphDebug.useDebug && proxyContainer != null) {
				GraphDebug.DebugData debugData = owner.owner.graph.GetDebugInfo();
				if(debugData != null) {
					if(isValue && direction == Direction.Input) {
						var port = GetPortValue<ValueInput>();
						var debug = debugData.GetDebugValue(port);
						if(debug.isValid) {
							GUIContent debugContent;
							if(debug.value != null) {
								debugContent= new GUIContent(uNodeUtility.GetDisplayName(debug.value), uNodeEditorUtility.GetTypeIcon(debug.value.GetType()));
							}
							else {
								debugContent = new GUIContent("null");
							}
							if(debugContent != null) {
								Vector2 pos;
								if(proxyTitleLabel != null) {
									pos = proxyTitleLabel.ChangeCoordinatesTo(proxyDebug, Vector2.zero);
								}
								else {
									pos = this.ChangeCoordinatesTo(proxyDebug, new Vector2(-proxyContainer.layout.width, 0));
								}
								Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(debugContent.text));
								size.x += 25;
								GUI.Box(
									new Rect(pos.x - size.x, pos.y, size.x - 5, 20),
									debugContent,
									EditorStyles.helpBox);
							}
						}
					}
				}
			}
		}
		#endregion

		#region Connect & Disconnect
		public override void Connect(Edge edge) {
			base.Connect(edge);
			edges.Add(edge as EdgeView);
			owner.OnPortConnected(this);
			UpdatePortClass();
		}

		public override void Disconnect(Edge edge) {
			base.Disconnect(edge);
			edges.Remove(edge as EdgeView);
			owner.OnPortDisconnected(this);
			UpdatePortClass();
		}

		public void ConnectPortTo(PortView other) {
			if(other == null)
				throw new ArgumentNullException(nameof(other));
			if(direction == Direction.Input) {
				owner.owner.Connect(this, other, true);
			}
			else {
				owner.owner.Connect(other, this, true);
			}
		}
		#endregion

		#region Drop Port

		private void OnDropOutsidePortFromFlowOutput(Vector2 position, PortView portView, PortView sidePort) {
			owner.owner.graph.ShowNodeMenu(position, null, (n) => {
				FlowInput flow = null;
				if(n.nodeObject.primaryFlowInput != null) {
					flow = n.nodeObject.primaryFlowInput;
				}
				else {
					flow = n.nodeObject.FlowInputs.FirstOrDefault();
				}
				if(flow != null) {
					Connection.CreateAndConnect(flow, portView.GetPortValue());
				}
				else {
					if(n is MultipurposeNode mNode && mNode.CanSetValue()) {
						var rType = mNode.ReturnType();
						if(rType.IsCastableTo(typeof(Delegate)) || rType.IsCastableTo(typeof(UnityEngine.Events.UnityEventBase))) {
							NodeEditorUtility.AddNewNode(owner.owner.graphData, null, null, position, (Nodes.EventHook nod) => {
								nod.EnsureRegistered();
								Connection.CreateAndConnect(nod.target, n.nodeObject.primaryValueOutput);
								Connection.CreateAndConnect(portView.GetPortValue(), nod.register);
							});
						}
						else {
							NodeEditorUtility.AddNewNode(owner.owner.graphData, null, null, position, (Nodes.NodeSetValue nod) => {
								nod.EnsureRegistered();
								Connection.CreateAndConnect(nod.target, n.nodeObject.primaryValueOutput);
								if(n.ReturnType() != typeof(void)) {
									nod.value.AssignToDefault(MemberData.Default(n.ReturnType()));
								}
								Connection.CreateAndConnect(portView.GetPortValue(), nod.nodeObject.primaryFlowInput);
							});
						}
					}
				}
				if(sidePort != null) {
					//Reset the original connection
					sidePort.ResetPortValue();
				}

			}, NodeFilter.FlowOutput);
		}

		private void OnDropOutsidePortFromFlowInput(Vector2 position, PortView portView, PortView sidePort) {
			owner.owner.graph.ShowNodeMenu(position, null, (n) => {
				n.EnsureRegistered();
				if(n is MultipurposeNode mNode && !mNode.IsFlowNode() && mNode.CanSetValue()) {
					var rType = mNode.ReturnType();
					if(rType.IsCastableTo(typeof(Delegate)) || rType.IsCastableTo(typeof(UnityEngine.Events.UnityEventBase))) {
						NodeEditorUtility.AddNewNode(owner.owner.graphData, null, null, position, (Nodes.EventHook nod) => {
							nod.EnsureRegistered();
							nod.target.ConnectTo(n.nodeObject.primaryValueOutput);
							n = nod;
						});
					}
					else {
						NodeEditorUtility.AddNewNode(owner.owner.graphData, null, null, position, (Nodes.NodeSetValue nod) => {
							nod.EnsureRegistered();
							nod.target.ConnectTo(n.nodeObject.primaryValueOutput);
							if(n.ReturnType() != typeof(void)) {
								nod.value.AssignToDefault(MemberData.CreateValueFromType(n.ReturnType()));
							}
							n = nod;
						});
					}
				}
				var flow = n.nodeObject.FlowOutputs.FirstOrDefault();
				if(flow != null) {
					flow.ConnectTo(portView.GetPortValue());
				}
				if(sidePort != null) {
					//Reset the original connection
					sidePort.ResetPortValue();
				}
			}, NodeFilter.FlowInput);
		}

		private void OnDropOutsidePortFromValueInput(Vector2 position, PortView portView, PortView sidePort) {
			var portData = this.portData as ValueInputData;
			FilterAttribute FA = portData.GetFilter();
			var types = FA.Types;
			FA = new FilterAttribute(FA) {
				MaxMethodParam = int.MaxValue,
				ValidateType = (type) => {
					for(int i = 0; i < types.Count; i++) {
						if(NodeEditorUtility.CanAutoConvertType(type, types[i])) {
							return true;
						}
					}
					return false;
				},
				// DisplayDefaultStaticType = false
			};
			List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
			var editorData = owner.owner.graphData;
			var portType = GetPortType();
			if(editorData.graph is IGraphWithVariables) {
				var IVS = editorData.graph as IGraphWithVariables;
				var type = portType;
				customItems.Add(ItemSelector.CustomItem.Create("Promote to variable", () => {
					uNodeEditorUtility.RegisterUndo(editorData.owner, "Promote to variable");
					NodeEditorUtility.AddNewVariable(editorData.graphData.variableContainer, "newVariable", type, v => {
						NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, position, (node) => {
							node.target = MemberData.CreateFromValue(v);
							node.Register();
							portView.GetPortValue().ConnectTo(node.output);
						});
					});
					uNodeGUIUtility.GUIChanged(editorData.graph, UIChangeType.Important);
				}, "@", icon: uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FieldIcon))));
			}
			if(editorData.selectedRoot is ILocalVariableSystem) {
				var IVS = editorData.selectedRoot as ILocalVariableSystem;
				var type = portType;
				customItems.Add(ItemSelector.CustomItem.Create("Promote to local variable", () => {
					uNodeEditorUtility.RegisterUndo(editorData.owner, "Promote to variable");
					NodeEditorUtility.AddNewVariable(editorData.selectedRoot.variableContainer, "newVariable", type, v => {
						NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, position, (node) => {
							node.target = MemberData.CreateFromValue(v);
							node.Register();
							portView.GetPortValue().ConnectTo(node.output);
						});
					});
					uNodeGUIUtility.GUIChanged(editorData.graph, UIChangeType.Important);
				}, "@", icon: uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FieldIcon))));
			}
			{//Create custom items for port commands
				foreach(var pType in types) {
					if(!pType.IsSubclassOf(typeof(Delegate)) && !pType.IsPrimitive) {
						var filter = new FilterAttribute() { MaxMethodParam = int.MaxValue };
						var ctors = pType.GetConstructors(
							System.Reflection.BindingFlags.Instance |
							System.Reflection.BindingFlags.Public |
							System.Reflection.BindingFlags.Static);
						for(int i = ctors.Length - 1; i >= 0; i--) {
							if(ReflectionUtils.IsValidParameters(ctors[i])) {
								var item = EditorReflectionUtility.GetReflectionItems(ctors[i], filter);
								if(item == null)
									continue;
								customItems.Add(ItemSelector.CustomItem.Create(item.displayName, item, category: "@"));
							}
						}
					}
				}
				var portCommands = NodeEditorUtility.FindPortCommands();
				if(portCommands != null && portCommands.Count > 0) {
					PortCommandData commandData = new PortCommandData() {
						portType = portType,
						portName = GetName(),
						port = portView.GetPortValue(),
						portKind = PortKind.ValueInput,
					};
					foreach(var command in portCommands) {
						if(command.onlyContextMenu)
							continue;
						command.graph = owner.owner.graph;
						command.mousePositionOnCanvas = position;
						command.filter = portData.GetFilter();
						if(command.IsValidPort(owner.targetNode, commandData)) {
							customItems.Add(ItemSelector.CustomItem.Create(command.name, () => {
								uNodeEditorUtility.RegisterUndo(editorData.owner, "Connect port");
								command.OnClick(owner.targetNode, commandData, position);
								owner.MarkRepaint();
							}, "@", icon: command.GetIcon()));
						}
					}
				}
			}
			owner.owner.graph.ShowNodeMenu(position, FA, (n) => {
				if(n.CanGetValue()) {
					if(n is MultipurposeNode mNode) {
						var type = mNode.ReturnType();
						Type rightType = type;
						for(int i = 0; i < types.Count; i++) {
							if(NodeEditorUtility.CanAutoConvertType(type, types[i])) {
								rightType = types[i];
								break;
							}
						}
						if(!type.IsCastableTo(rightType)) {
							NodeEditorUtility.AutoConvertPort(
								type,
								rightType,
								NodeEditorUtility.GetPort<ValueOutput>(n),
								NodeEditorUtility.GetPort<ValueInput>(portView.GetNodeObject()),
								(node) => {
									portView.GetPortValue().ConnectTo(NodeEditorUtility.GetPort<ValueOutput>(node));
								}, owner.graphData.currentCanvas, new FilterAttribute(rightType));
							return;
						}
					}
					portView.GetPortValue().ConnectTo(n.nodeObject.primaryValueOutput);
					if(sidePort != null) {
						//Reset the original connection
						sidePort.ResetPortValue();
					}
				}
			}, NodeFilter.ValueInput, additionalItems: customItems, expandedCategory: new[] { "@" });
		}

		private void OnDropOutsidePortFromValueOutput(Vector2 position, PortView portView, PortView sidePort) {
			Type type = portView.GetPortType();
			bool canSetValue = false;
			bool canGetValue = true;
			var port = portView.GetPortValue<ValueOutput>();
			if(port != null) {
				canSetValue = port.CanSetValue();
				canGetValue = port.CanGetValue();
			}
			bool onlySet = canSetValue && !canGetValue;
			FilterAttribute FA = new FilterAttribute {
				VoidType = true,
				MaxMethodParam = int.MaxValue,
				Public = true,
				Instance = true,
				Static = false,
				UnityReference = false,
				InvalidTargetType = MemberData.TargetType.Null | MemberData.TargetType.Values,
				// DisplayDefaultStaticType = false
			};
			List<ItemSelector.CustomItem> customItems = null;
			//TODO: fix this ( self target )
			//if(!onlySet && GetNode() is MultipurposeNode && type.IsCastableTo(typeof(uNodeRoot))) {
			//	MultipurposeNode multipurposeNode = GetNode() as MultipurposeNode;
			//	if(multipurposeNode.target != null && multipurposeNode.target.target != null && (multipurposeNode.target.target.targetType == MemberData.TargetType.SelfTarget || multipurposeNode.target.target.targetType == MemberData.TargetType.Values)) {
			//		var sTarget = multipurposeNode.target.target.startTarget;
			//		if(sTarget is uNodeRoot) {
			//			customItems = ItemSelector.MakeCustomItems(sTarget as uNodeRoot);
			//			customItems.AddRange(ItemSelector.MakeCustomItems(typeof(uNodeRoot), sTarget, FA, "Inherit Member"));
			//		}
			//	}
			//}
			if(customItems == null) {
				if(type is RuntimeType) {
					(type as RuntimeType).Update();
				}
				customItems = onlySet ? new List<ItemSelector.CustomItem>() : ItemSelector.MakeCustomItems(type, FA, "Data Members", "Data Members ( Inherited )");
				if(type.IsByRefLike == false) {
					var usingNamespaces = GetNodeObject().graphContainer.GetUsingNamespaces();
					customItems.AddRange(ItemSelector.MakeExtensionItems(type, usingNamespaces, FA, "Extensions"));
				}

				var customInputItems = NodeEditorUtility.FindCustomInputPortItems();
				if(customInputItems != null && customInputItems.Count > 0) {
					var source = portView.GetPortValue() as ValueOutput;
					foreach(var c in customInputItems) {
						c.graph = owner.owner.graph;
						c.mousePositionOnCanvas = position;
						if(c.IsValidPort(source,
							canSetValue && canGetValue ?
								PortAccessibility.ReadWrite :
								canGetValue ? PortAccessibility.ReadOnly : PortAccessibility.WriteOnly)) {
							var items = c.GetItems(source);
							if(items != null) {
								customItems.AddRange(items);
							}
						}
					}
				}
				var portCommands = NodeEditorUtility.FindPortCommands();
				if(portCommands != null && portCommands.Count > 0) {
					PortCommandData commandData = new PortCommandData() {
						portType = portType,
						portName = GetName(),
						port = GetPortValue(),
						portKind = PortKind.ValueOutput,
					};
					foreach(var command in portCommands) {
						if(command.onlyContextMenu)
							continue;
						command.graph = owner.owner.graph;
						command.mousePositionOnCanvas = position;
						command.filter = portData.GetFilter();
						if(command.IsValidPort(owner.targetNode, commandData)) {
							customItems.Add(ItemSelector.CustomItem.Create(command.name, () => {
								command.OnClick(owner.targetNode, commandData, position);
								owner.MarkRepaint();
							}, "@"));
						}
					}
				}
				foreach(var menuItem in NodeEditorUtility.FindNodeMenu()) {
					if(NodeEditorUtility.IsValidMenu(menuItem, type, NodeFilter.ValueOutput, owner.graphData) == false) {
						continue;
					}
					customItems.Add(ItemSelector.CustomItem.Create(
						menuItem,
						() => {
							NodeEditorUtility.AddNewNode<Node>(owner.graphData, menuItem.nodeName ?? menuItem.name.Split(' ')[0], menuItem.type, position, n => {
								NodeEditorUtility.AutoConnectPortToTarget(portView.GetPortValue(), n, owner.graphData.currentCanvas);
								owner.MarkRepaint();
							});
							portView.owner.owner.MarkRepaint();
						},
						icon: menuItem.icon != null ? uNodeEditorUtility.GetTypeIcon(menuItem.icon) : null));
				}
				customItems.AddRange(ItemSelector.MakeCustomItemsForMacros(owner.graphData.currentCanvas, position, NodeFilter.ValueOutput, type, n => {
					NodeEditorUtility.AutoConnectPortToTarget(portView.GetPortValue(), n, owner.graphData.currentCanvas);
					owner.owner.MarkRepaint();
				}));
			}
			if(customItems != null) {
				FA.Static = true;
				ItemSelector.SortCustomItems(customItems);
				ItemSelector w = ItemSelector.ShowWindow(portView.owner.nodeObject, FA, (MemberData mData) => {
					NodeGraph.CreateNodeProcessor(mData, owner.owner.graph.graphData, position, (n) => {
						bool needAutoConnect = true;
						if(n is MultipurposeNode multipurposeNode && multipurposeNode.instance != null) {//For auto connect to parameter ports
							if(type.IsCastableTo(mData.startType)) {
								var con = Connection.CreateAndConnect(multipurposeNode.member.instance, portView.GetPortValue());
								NodeEditorUtility.AutoRerouteAndProxy(con, owner.graphData.currentCanvas);
								needAutoConnect = false;
							}
							else {
								NodeEditorUtility.AutoConvertPort(type, mData.startType, portView.GetPortValue<ValueOutput>(), NodeEditorUtility.GetPort<ValueInput>(n), (val) => {
									multipurposeNode.instance.ConnectTo(val.nodeObject.primaryValueOutput);
									needAutoConnect = false;
								}, owner.graphData.currentCanvas, new FilterAttribute(mData.startType));
							}
						}
						if(needAutoConnect) {
							NodeEditorUtility.AutoConnectPortToTarget(portView.GetPortValue<ValueOutput>(), n, owner.graphData.currentCanvas);
						}
						portView.owner.owner.MarkRepaint();
						if(sidePort != null) {
							//Reset the original connection
							sidePort.ResetPortValue();
						}
					});
				}, customItems: customItems).ChangePosition(owner.owner.graph.GetMenuPosition());
				w.displayRecentItem = false;
				w.displayNoneOption = false;
				if(type == typeof(bool)) {
					w.defaultExpandedItems = new[] { "@", "Operator", "Data", "Flow" };
				}
				else if(type == typeof(int) || type == typeof(float) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(double) || type == typeof(long)) {
					w.defaultExpandedItems = new[] { "@", "Operator", "Flow" };
				}
				else {
					w.defaultExpandedItems = new[] { "@", "Operator", "Data Members", "Flow" };
				}
			}
		}

		public void OnDropOutsidePort(Edge edge, Vector2 position) {
			var input = edge.input as PortView;
			var output = edge.output as PortView;
			var screenRect = owner.owner.graph.window.GetMousePositionForMenu(position);
			Vector2 pos = owner.owner.graph.window.rootVisualElement.ChangeCoordinatesTo(
				owner.owner.graph.window.rootVisualElement.parent,
				screenRect - owner.owner.graph.window.position.position);
			position = owner.owner.contentViewContainer.WorldToLocal(pos);
			PortView sidePort = null;
			if(input != null && output != null) {
				var draggedPort = input.edgeConnector?.edgeDragHelper?.draggedPort ?? output.edgeConnector?.edgeDragHelper?.draggedPort;
				if(draggedPort == input) {
					sidePort = output;
					output = null;
				}
				else if(draggedPort == output) {
					sidePort = input;
					input = null;
				}
			}
			if(input != null) {//Process if source is input port.
				PortView portView = input as PortView;

#if false
				foreach(var node in owner.owner.nodeViews) {
					if(node != null && node != portView.owner) {
						if(node.layout.Contains(position)) {
							if((edge.input as PortView).isFlow) {//Flow
								foreach(var port in node.outputPorts) {
									if(port.isFlow) {//Find the first flow port and connect it.
										uNodeThreadUtility.Queue(() => {
											edge.input = portView;
											edge.output = port;
											owner.owner.Connect(edge as EdgeView, true);
											owner.owner.MarkRepaint();
										});
										return;
									}
								}
							}
							else {//Input Value
								FilterAttribute filter = portView.GetFilter();
								bool flag = true;
								if(filter.SetMember) {
									var tNode = portView.GetNodeObject();
									if(tNode == null || !tNode.CanSetValue()) {
										flag = false;
									}
								}
								if(flag) {
									foreach(var port in node.outputPorts) {
										if(port.isValue && portView.IsValidTarget(port)) {
											uNodeThreadUtility.Queue(() => {
												edge.input = portView;
												edge.output = port;
												OnDrop(owner.owner, edge);
											});
											return;
										}
									}
								}
							}
							break;
						}
					}
				}
#endif

				if(input.isValue) {//Input Value
					OnDropOutsidePortFromValueInput(position, portView, sidePort);
				}
				else {//Input Flow
					OnDropOutsidePortFromFlowInput(position, portView, sidePort);
				}
			}
			else if(output != null) {//Process if source is output port.
				PortView portView = output as PortView;

#if false
				foreach(var node in owner.owner.nodeViews) {
					if(node != null && node != portView.owner) {
						if(node.layout.Contains(position)) {
							if(output.isFlow) {//Flow
								foreach(var port in node.inputPorts) {
									if(port.isFlow) {
										uNodeThreadUtility.Queue(() => {
											edge.output = portView;
											edge.input = port;
											owner.owner.Connect(edge as EdgeView, true);
											owner.owner.MarkRepaint();
										});
										return;
									}
								}
							}
							else {//Output Value
								FilterAttribute filter = portView.GetFilter();
								bool flag = true;
								if(filter.SetMember) {
									//var tNode = portView.GetNode() as Node;
									//if(tNode == null || !tNode.CanSetValue()) {
									//	flag = false;
									//}
								}
								if(flag) {
									foreach(var port in node.inputPorts) {
										if(port.isValue && portView.IsValidTarget(port)) {
											uNodeThreadUtility.Queue(() => {
												edge.output = portView;
												edge.input = port;
												OnDrop(owner.owner, edge);
											});
											return;
										}
									}
								}
							}
							break;
						}
					}
				}
#endif

				if(output.isFlow) {//Output Flow
					OnDropOutsidePortFromFlowOutput(position, portView, sidePort);
				}
				else {//Output Value
					OnDropOutsidePortFromValueOutput(position, portView, sidePort);
				}
			}
		}

		public bool CanSetValue() {
			if(isValue) {
				if(direction == Direction.Input) {
					return GetPortValue<ValueInput>().CanSetValue();
				}
				else {
					return GetPortValue<ValueOutput>().CanSetValue();
				}
			}
			return false;
		}

		public bool CanConnect(PortView port) {
			if(port.direction == direction || port.owner == owner) {
				return false;
			}
			if(port.isValue) {
				PortView input;
				PortView output;
				FilterAttribute filter;
				if(port.direction == Direction.Input) {
					input = port;
					output = this;
					filter = port.GetFilter();
				}
				else {
					input = this;
					output = port;
					filter = this.GetFilter();
				}
				var outputPort = output.GetPortValue<ValueOutput>();
				var inputPort = input.GetPortValue<ValueInput>();
				if(filter != null) {
					if(filter.SetMember && !output.CanSetValue()) {
						return false;
					}
					var outType = output.GetPortType();
					if(!filter.IsValidType(outType)) {
						var types = filter.Types;
						if(types.Count > 1) {
							return false;
						}
						return NodeEditorUtility.CanAutoConvertType(outType, filter.GetActualType(), outputPort, inputPort, owner.graphData.currentCanvas);
					}
					return true;
				}
				return NodeEditorUtility.CanAutoConvertType(output.GetPortType(), input.GetPortType(), outputPort, inputPort, owner.graphData.currentCanvas);
			}
			return true;
		}

		private void AutoConvertPort(UGraphView graphView, Edge edge, Type rightType) {
			var leftPort = edge.output as PortView;
			var rightPort = edge.input as PortView;

			Type leftType = leftPort.portData.GetFilter().GetActualType();
			NodeEditorUtility.AutoConvertPort(leftType, rightType, leftPort.GetPortValue<ValueOutput>(), rightPort.GetPortValue<ValueInput>(), (node) => {
				UPort p = null;
				if(node.nodeObject.primaryValueOutput != null) {
					p = node.nodeObject.primaryValueOutput;
				}
				else {
					p = node.nodeObject.ValueOutputs.FirstOrDefault();
				}
				if(p != null) {
					p.ConnectTo(rightPort.GetPortValue());
				}
				leftPort.owner.MarkRepaint();
				rightPort.owner.MarkRepaint();
			}, owner.graphData.currentCanvas, rightPort.portData.GetFilter(), forceConvert: true);
			graphView.MarkRepaint();
		}

		private void OnDropInsideValueOutput(UGraphView graph, EdgeView edge) {
			if(IsValidTarget(edge.Output)) {
				uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Connect port");
				graph.Connect(edge, true);
			}
			else {
				int option;
				if(uNodePreference.preferenceData.autoConvertPort == uNodePreference.AutoConvertOption.Always) {
					option = 0;
				}
				else {
					option = EditorUtility.DisplayDialogComplex("Do you want to continue?",
						"The source port and destination port type is not match.",
						"Convert if possible", "Continue", "Cancel");
				}
				if(option == 0) {
					var filteredTypes = (edge.input as PortView).portData.GetFilter().GetFilteredTypes();
					if(filteredTypes.Count > 1) {
						var menu = new GenericMenu();
						for(int i = 0; i < filteredTypes.Count; i++) {
							var t = filteredTypes[i];
							menu.AddItem(new GUIContent(t.PrettyName(true)), false, () => {
								uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Connect port");
								AutoConvertPort(graph, edge, t);
							});
						}
						menu.ShowAsContext();
					}
					else {
						uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Connect port");
						AutoConvertPort(graph, edge, filteredTypes[0]);
					}
					return;
				}
				else if(option != 1) {
					return;
				}
			}
		}


		private void OnDropInsideValueInput(UGraphView graph, EdgeView edge) {
			if(IsValidTarget(edge.Input)) {
				uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Connect port");
				graph.Connect(edge, true);
			}
			else {
				int option;
				if(uNodePreference.preferenceData.autoConvertPort == uNodePreference.AutoConvertOption.Always) {
					option = 0;
				}
				else {
					option = EditorUtility.DisplayDialogComplex("Do you want to continue?",
					"The source port and destination port type is not match.",
					"Convert if possible", "Continue", "Cancel");
				}
				if(option == 0) {
					var filteredTypes = (edge.input as PortView).portData.GetFilter().GetFilteredTypes();
					if(filteredTypes.Count > 1) {
						var menu = new GenericMenu();
						for(int i = 0; i < filteredTypes.Count; i++) {
							var t = filteredTypes[i];
							menu.AddItem(new GUIContent(t.PrettyName(true)), false, () => {
								uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Connect port");
								AutoConvertPort(graph, edge, t);
							});
						}
						menu.ShowAsContext();
					}
					else {
						uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Connect port");
						AutoConvertPort(graph, edge, filteredTypes[0]);
					}
					return;
				}
				else if(option != 1) {
					return;
				}
			}
		}

		public void OnDrop(GraphView graphView, Edge edge) {
			var edgeView = edge as EdgeView;
			var graph = graphView as UGraphView;
			if(graph == null || edgeView == null || edgeView.input == null || edgeView.output == null)
				return;
			if(edgeView.Input.isValue) {
				if(edgeView.input == this) {
					OnDropInsideValueOutput(graph, edgeView);
				}
				else {
					OnDropInsideValueInput(graph, edgeView);
				}
			}
			else {
				uNodeEditorUtility.RegisterUndo(graph.graphData.owner, "Connect port");
				graph.Connect(edgeView, true);
			}
		}
#endregion

		#region Functions
		public void SetControl(VisualElement visualElement, bool autoLayout = false) {
			ControlView control = new ControlView();
			control.Add(visualElement);
			SetControl(control, autoLayout);
		}

		public void SetControl(ControlView control, bool autoLayout = false) {
			if(controlView != null) {
				controlView.RemoveFromHierarchy();
				controlView = null;
			}
			if(control != null) {
				control.EnableInClassList("Layout", autoLayout);
				control.EnableInClassList("output_port", true);
				this.Add(control);
				controlView = control;
			}
			//m_ConnectorText.EnableInClassList("Layout", autoLayout);
			portName = portName;
		}

		internal List<EdgeView> GetEdges() {
			return edges;
		}

		public IEnumerable<EdgeView> GetValidEdges() {
			foreach(var edge in edges) {
				if(!edge.isValid)
					continue;
				yield return edge;
			}
		}

		public HashSet<UNodeView> GetEdgeOwners() {
			HashSet<UNodeView> nodes = new HashSet<UNodeView>();
			foreach(var e in edges) {
				if(!e.isValid)
					continue;
				var sender = e.GetSenderPort()?.owner;
				if(sender != null) {
					nodes.Add(sender);
				}
				var receiver = e.GetReceiverPort()?.owner;
				if(receiver != null) {
					nodes.Add(receiver);
				}
			}
			return nodes;
		}

		public HashSet<UNodeView> GetConnectedNodes() {
			HashSet<UNodeView> nodes = new HashSet<UNodeView>();
			if(edges.Count > 0) {
				foreach(var e in edges) {
					if(!e.isValid)
						continue;
					if(direction == Direction.Input) {
						var targetPort = e.output as PortView;
						var targetView = targetPort.owner;
						if(targetView != null) {
							nodes.Add(targetView);
						}
					}
					else {
						var targetPort = e.input as PortView;
						var targetView = targetPort.owner;
						if(targetView != null) {
							nodes.Add(targetView);
						}
					}
				}
			}
			return nodes;
		}

		public HashSet<PortView> GetConnectedPorts() {
			HashSet<PortView> ports = new HashSet<PortView>();
			if(edges.Count > 0) {
				foreach(var e in edges) {
					if(!e.isValid)
						continue;
					if(direction == Direction.Input) {
						ports.Add(e.Output);
					}
					else {
						ports.Add(e.Input);
					}
				}
			}
			return ports;
		}

		public Type GetPortType() {
			if(portType.IsByRef)
				return portType.GetElementType();
			return portType;
		}

		public string GetPortID() {
			return portData.portID;
		}

		public UPort GetPortValue() {
			return portData.portValue;
		}

		public T GetPortValue<T>() where T : UPort {
			return portData.portValue as T;
		}

		public FilterAttribute GetFilter() {
			return portData.GetFilter();
		}

		public string GetName() {
			return portName;
		}

		public object GetDefaultValue() {
			return portData.defaultValue;
		}

		public string GetTooltip() {
			var str = portData.tooltip;
			if(string.IsNullOrEmpty(str)) {
				if(isFlow) {
					if(direction == Direction.Input) {
						if(this == owner.primaryInputFlow) {
							return "Input flow to execute this node";
						}
					}
					else {
						if(this == owner.primaryOutputFlow) {
							return "Flow to execute on finish";
						}
					}
				}
				else {
					if(direction == Direction.Input) {

					}
					else {
						if(this == owner.primaryOutputValue) {
							return "The result value";
						}
					}
				}
			}
			return str;
		}

		public string GetPrettyName() {
			var str = GetName();
			if(string.IsNullOrEmpty(str)) {
				if(isFlow) {
					if(direction == Direction.Input) {
						if(this == owner.primaryInputFlow) {
							return "Input";
						}
					}
					else {

					}
				}
				else {
					if(direction == Direction.Input) {

					}
					else {
						if(this == owner.primaryOutputValue) {
							return "Result";
						}
					}
				}
				if(portData.portID.StartsWith("$", StringComparison.Ordinal)) {
					return "Port";
				}
				return ObjectNames.NicifyVariableName(portData.portID);
			}
			return str;
		}

		private string GetProxyName() {
			var str = GetName();
			if(string.IsNullOrEmpty(str) || str == "Out" || str == "Output") {
				if(isFlow) {
					if(direction == Direction.Input) {
						//if(this == owner.primaryInputFlow) {
						//}
						return owner.nodeObject.GetTitle();
					}
					else {

					}
				}
				else {
					if(direction == Direction.Input) {

					}
					else {
						//if(this == owner.primaryOutputValue) {
						//}
						return owner.nodeObject.GetTitle();
					}
				}
				if(portData.portID.StartsWith("$", StringComparison.Ordinal)) {
					return "Port";
				}
				if(string.IsNullOrEmpty(str)) {
					return ObjectNames.NicifyVariableName(portData.portID);
				}
			}
			return str;
		}

		public void SetName(string str) {
			portName = ObjectNames.NicifyVariableName(str);
		}

		public NodeObject GetNodeObject() {
			return owner.nodeObject;
		}

		public Node GetNode() {
			return owner.targetNode;
		}

		public bool IsValidTarget(PortView portView) {
			if(portView != null) {
				if(portData.owner == portView.portData.owner || orientation != portView.orientation)
					return false;
				if(isValue) {
					var inputPort = portView.direction == Direction.Input ? portView : this;
					var outputPort = portView.direction == Direction.Output ? portView : this;

					var filter = inputPort.portData.GetFilter();
					var outputType = outputPort.portData.portType;
					if(filter.IsValidType(outputType) || outputType.IsCastableTo(inputPort.portType)) {
						return true;
					}
					else {
						var inputType = inputPort.portData.portType;
						if(inputType == outputType ||
							inputType == typeof(MemberData) ||
							//inputType.IsCastableTo(outputType) ||
							outputType.IsCastableTo(inputType)) {
							return true;
						}
						//else if(inputType is RuntimeType && (
						//  inputType.IsCastableTo(typeof(Component)) ||
						//  inputType.IsInterface)) {
						//	if(outputType == typeof(GameObject) || outputType.IsCastableTo(typeof(Component))) {
						//		return true;
						//	}
						//}
					}
					return false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// True if the port is proxy
		/// </summary>
		/// <returns></returns>
		public bool IsProxy() {
			if(edges.Count == 0)
				return false;
			return edges.All(e => e.isProxy);
		}

		/// <summary>
		/// Reset the port value
		/// </summary>
		public void ResetPortValue() {
			var portValue = GetPortValue();
			if(portValue is FlowInput || portValue is FlowOutput || portValue is ValueOutput) {
				portValue.ClearConnections();
			}
			else if(portValue is ValueInput) {
				var port = portValue as ValueInput;
				Type valType = port.ValueType ?? GetPortType();
				(portValue as ValueInput).AssignToDefault(MemberData.Default(valType));
			}
		}

		const int depthOffset = 8;
		private List<VisualElement> m_depths;
		/// <summary>
		/// Set the port depth
		/// </summary>
		/// <param name="depth"></param>
		public void SetPortDepth(int depth) {
			if(m_depths != null) {
				foreach(var v in m_depths) {
					v?.RemoveFromHierarchy();
				}
			}
			m_depths = new List<VisualElement>();
			for(int i = 0; i < depth; ++i) {
				VisualElement line = new VisualElement();
				line.name = "line";
				line.style.marginLeft = depthOffset + (i == 0 ? -2 : 0);
				line.style.marginRight = ((i == depth - 1) ? 2 : 0);
				m_depths.Add(line);
			}
			for(int i = m_depths.Count - 1; i >= 0; i--) {
				Insert(1, m_depths[i]);
			}
		}
		#endregion
	}
}