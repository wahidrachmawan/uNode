using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace MaxyGames.UNode.Editors {
	public abstract class UNodeView : NodeView {
		public List<PortView> inputPorts = new List<PortView>();
		public List<PortView> outputPorts = new List<PortView>();

		public List<ControlView> inputControls = new List<ControlView>();
		public List<ControlView> outputControls = new List<ControlView>();

		#region Uss Class Name
		public const string ussClassBorderFlowNode = "flowNodeBorder";
		public const string ussClassBorderOnlyInput = "onlyInput";
		public const string ussClassBorderOnlyOutput = "onlyOutput";
		public const string ussClassEntryNode = "entry-node";

		public const string ussClassAutoHideControl = "autohide";

		public const string ussClassHidePortIcon = "hide-image";


		public const string ussClassCompact = "compact";
		public const string ussClassCompactValue = "compact-value";
		public const string ussClassCompactValueMinimalize = "compact-value-minimalize";
		public const string ussClassCompactTitle = "compact-title";
		public const string ussClassCompactNode = "compact-node";
		public const string ussClassCompactNodeMinimalize = "compact-node-minimalize";
		public const string ussClassCompactOutput = "compact-output";
		public const string ussClassCompactInput = "compact-input";
		public const string ussClassCompactControl = "compact-control";
		#endregion

		public PortView primaryInputFlow { get; protected set; }
		public PortView primaryOutputFlow { get; protected set; }
		public PortView primaryOutputValue { get; protected set; }

		public INodeBlock ownerBlock { get; set; }
		public NodeObject nodeObject { protected set; get; }
		public Node targetNode => nodeObject.node;
		public UGraphView owner { protected set; get; }
		public GraphEditorData graphData => owner.graphData;
		public virtual bool autoReload => false;
		public virtual bool fullReload => nodeObject == null;

		public bool isNativeGraph => owner.isNativeGraph;

		public VisualElement portInputContainer { protected set; get; }
		public VisualElement flowInputContainer { protected set; get; }
		public VisualElement flowOutputContainer { protected set; get; }
		public VisualElement controlsContainer { protected set; get; }

		//For Hiding
		public VisualElement parentElement { get; set; }
		public Rect hidingRect { get; set; }
		public bool isHidden => parent == null;
		public bool isBlock => ownerBlock != null;
		public Orientation flowLayout => owner.graphLayout == GraphLayout.Vertical && !isBlock ? Orientation.Vertical : Orientation.Horizontal;

		public override bool expanded {
			get {
				return base.expanded;
			}
			set {
				nodeObject.nodeExpanded = value;
				RefreshControl(value);
				base.expanded = value;
			}
		}

		public UIElementGraph graph {
			get {
				return owner.graph;
			}
		}

		#region VisualElement
		private IconBadge errorBadge;
		protected VisualElement debugContainer;
		protected VisualElement border;
		protected Image titleIcon;
		#endregion

		#region Initialization
		protected void Initialize(UGraphView owner) {
			this.owner = owner;
			this.AddToClassList("node-view");
			if(nodeObject.node is BaseEntryNode) {
				this.AddToClassList(ussClassEntryNode);
			}

			if(!ShowExpandButton()) {//Hides colapse button
				m_CollapseButton.style.position = Position.Absolute;
				m_CollapseButton.style.width = 0;
				m_CollapseButton.style.height = 0;
				m_CollapseButton.visible = false;
			}
			base.expanded = true;

			border = this.Q("node-border");
			{//Flow inputs
				flowInputContainer = new VisualElement();
				flowInputContainer.name = "flow-inputs";
				flowInputContainer.AddToClassList("flow-container");
				flowInputContainer.AddToClassList("input");
				flowInputContainer.pickingMode = PickingMode.Ignore;
				border.Insert(0, flowInputContainer);
			}
			{//Flow outputs
				flowOutputContainer = new VisualElement();
				flowOutputContainer.name = "flow-outputs";
				flowOutputContainer.AddToClassList("flow-container");
				flowOutputContainer.AddToClassList("output");
				flowOutputContainer.pickingMode = PickingMode.Ignore;
				Add(flowOutputContainer);
			}
			controlsContainer = new VisualElement { name = "controls" };
			mainContainer.Add(controlsContainer);

			titleIcon = new Image() { name = "title-icon" };
			titleContainer.Add(titleIcon);
			titleIcon.SendToBack();

			OnSetup();

			RegisterCallback<MouseDownEvent>(evt => {
				var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graph.window);
				if(evt.button == 0 && evt.shiftKey && !evt.altKey) {
					ActionPopupWindow.ShowWindow(Vector2.zero, () => {
						CustomInspector.ShowInspector(new GraphEditorData(graph.graphData, nodeObject));
					}, 300, 300).ChangePosition(mPos);
				}
			});
			RegisterCallback<MouseOverEvent>((e) => {
				for(int i = 0; i < inputPorts.Count; i++) {
					var edges = inputPorts[i].GetEdges();
					foreach(var edge in edges) {
						if(edge == null)
							continue;
						if(edge.isProxy) {
							edge.SetEdgeVisible(true);
						}
					}
				}
				for(int i = 0; i < outputPorts.Count; i++) {
					var edges = outputPorts[i].GetEdges();
					foreach(var edge in edges) {
						if(edge == null)
							continue;
						if(edge.isProxy) {
							edge.SetEdgeVisible(true);
						}
					}
				}
			});
			RegisterCallback<MouseLeaveEvent>((e) => {
				for(int i = 0; i < inputPorts.Count; i++) {
					var edges = inputPorts[i].GetEdges();
					foreach(var edge in edges) {
						if(edge == null)
							continue;
						if(edge.isProxy) {
							edge.SetEdgeVisible(false);
						}
					}
				}
				for(int i = 0; i < outputPorts.Count; i++) {
					var edges = outputPorts[i].GetEdges();
					foreach(var edge in edges) {
						if(edge == null)
							continue;
						if(edge.isProxy) {
							edge.SetEdgeVisible(false);
						}
					}
				}
			});
			long trickedFrame = 0;
			RegisterCallback<KeyDownEvent>(e => {
				trickedFrame = uNodeThreadUtility.frame;
			}, TrickleDown.TrickleDown);
			RegisterCallback<GeometryChangedEvent>(evt => {
				if(owner.isLoading || this.isHidden || !this.IsVisible() || trickedFrame == 0 || trickedFrame + 10 < uNodeThreadUtility.frame)
					return;//For fix node auto move.
				if(evt.oldRect != Rect.zero && evt.oldRect.width != evt.newRect.width) {
					Teleport(new Rect(evt.newRect.x + (evt.oldRect.width - evt.newRect.width), evt.newRect.y, evt.newRect.width, evt.newRect.height));
				}
			});
		}

		/// <summary>
		/// Initialize once on created.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="node"></param>
		public virtual void Initialize(UGraphView owner, NodeObject node) {
			nodeObject = node;
			Initialize(owner);
			var bind = UBind.FromGraphElement(node);
			if(bind != null) {
				bind.childValueChanged += (bind) => {
					MarkRepaint();
				};
			}
			ReloadView();
		}

		/// <summary>
		/// Called once on node created.
		/// </summary>
		protected virtual void OnSetup() {

			if(nodeObject.node is BaseEntryNode) {
				this.AddToClassList(ussClassEntryNode);
			}
		}

		/// <summary>
		/// This will be called after default initialization of node and edges
		/// </summary>
		public virtual void InitializeEdge() {

		}

		protected void RefreshControl(bool isVisible) {
			portInputContainer.SetOpacity(isVisible);
			foreach(var c in inputControls) {
				// c.visible = isVisible;
				c.SetOpacity(isVisible);
				if(isVisible) {
					c.RemoveFromClassList("hidden");
				} else {
					c.AddToClassList("hidden");
				}
			}
			foreach(var c in outputControls) {
				// c.visible = isVisible;
				c.SetOpacity(isVisible);
				if(isVisible) {
					c.RemoveFromClassList("hidden");
				} else {
					c.AddToClassList("hidden");
				}
			}
		}
		#endregion

		#region Add Ports
		public PortView AddInputValuePort(ValueInputData port) {
			return AddPort(port, Direction.Input, Orientation.Horizontal);
		}

		public PortView AddOutputValuePort(ValueOutputData port) {
			return AddPort(port, Direction.Output, Orientation.Horizontal);
		}

		public PortView AddInputFlowPort(FlowInputData port) {
			return AddPort(port, Direction.Input, flowLayout);
		}

		public PortView AddOutputFlowPort(FlowOutputData port) {
			return AddPort(port, Direction.Output, flowLayout);
		}

		public PortView AddOutputFlowPort(FlowOutputData port, Orientation orientation) {
			return AddPort(port, Direction.Output, orientation);
		}

		public PortView AddPrimaryInputFlow() {
			var port = nodeObject.primaryFlowInput;
			if(port != null && !isBlock) {
				return primaryInputFlow = AddInputFlowPort(new FlowInputData(port));
			}
			return null;
		}

		public PortView AddPrimaryOutputFlow() {
			var port = nodeObject.primaryFlowOutput;
			if(port != null && !isBlock) {
				return primaryOutputFlow = AddOutputFlowPort(new FlowOutputData(port));
			}
			return null;
		}

		public PortView AddPrimaryOutputValue() {
			var port = nodeObject.primaryValueOutput;
			if(port != null && (!isBlock || ownerBlock.blockType != BlockType.Condition)) {
				return primaryOutputValue = AddOutputValuePort(new ValueOutputData(port));
			}
			return null;
		}

		protected PortView AddPort(PortData portData, Direction direction, Orientation orientation) {
			portData.owner = this;
			PortView p = new PortView(orientation, direction, portData);
			if(p.direction == Direction.Input) {
				inputPorts.Add(p);
				if(portData.isFlow) {
					if(orientation == Orientation.Vertical) {
						flowInputContainer.Add(p);
					}
					else {
						inputContainer.Add(p);
					}
				}
				else if(portData is ValueInputData valueInput) {
					if(/*isBlock ||*/ UIElementUtility.Theme.preferredDisplayValue == DisplayValueKind.Inside) {
						p.EnableInClassList("port-control", true);
						var control = new ControlView(valueInput.InstantiateControl(true), true);
						control.AddToClassList(ussClassAutoHideControl);
						p.Add(control);
					}
					else {
						var portInputView = new PortInputView(valueInput);
						portInputContainer.Add(portInputView);
					}
					if(portData.portType.IsByRef) {
						p.EnableInClassList("port-byref", true);
					}
					inputContainer.Add(p);
				}
			}
			else {
				outputPorts.Add(p);
				if(portData.isFlow) {
					if(orientation == Orientation.Vertical) {
						flowOutputContainer.Add(p);
					}
					else {
						outputContainer.Add(p);
					}
				}
				else {
					outputContainer.Add(p);
				}
			}
			p.Initialize(portData);
			return p;
		}
		#endregion

		#region Add Controls
		public void AddControl(Direction direction, ControlView control) {
			switch(direction) {
				case Direction.Input: {
					inputControls.Add(control);
					inputContainer.Add(control);
					break;
				}
				case Direction.Output: {
					outputControls.Add(control);
					outputContainer.Add(control);
					break;
				}
			}
		}

		public void AddControl(Direction direction, ValueControl visualElement) {
			var control = new ControlView(visualElement);
			AddControl(direction, control);
		}

		public void AddControl(Direction direction, VisualElement visualElement) {
			var control = new ControlView();
			control.Add(visualElement);
			switch(direction) {
				case Direction.Input: {
					inputControls.Add(control);
					inputContainer.Add(control);
					break;
				}
				case Direction.Output: {
					outputControls.Add(control);
					outputContainer.Add(control);
					break;
				}
			}
		}
		#endregion

		#region Remove Control & Ports
		protected void RemoveControls() {
			for(int i = 0; i < inputControls.Count; i++) {
				if(inputControls[i].parent != null) {
					inputControls[i].RemoveFromHierarchy();
				}
			}
			for(int i = 0; i < outputControls.Count; i++) {
				if(outputControls[i].parent != null) {
					outputControls[i].RemoveFromHierarchy();
				}
			}
			inputControls.Clear();
			outputControls.Clear();
		}

		protected void RemovePorts() {
			for(int i = 0; i < inputPorts.Count; i++) {
				RemovePort(inputPorts[i]);
				i--;
			}
			for(int i = 0; i < outputPorts.Count; i++) {
				RemovePort(outputPorts[i]);
				i--;
			}
		}

		public void RemovePort(PortView p) {
			inputPorts.Remove(p);
			outputPorts.Remove(p);
			if(p.direction == Direction.Input) {
				//if(p.orientation == Orientation.Vertical) {
				//	flowInputContainer.Remove(p);
				//} else {
				//	inputContainer.Remove(p);
				//}
			} else {
				//if(p.orientation == Orientation.Vertical) {
				//	flowOutputContainer.Remove(p);
				//} else {
				//	outputContainer.Remove(p);
				//}
			}
			if(p.parent != null) {
				p.parent.Remove(p);
			}
		}
		#endregion

		#region Repaint
		/// <summary>
		/// Repaint the node view in next frame of editor loop.
		/// </summary>
		public void MarkRepaint() {
			MarkDirtyRepaint();
			owner.MarkRepaint(this);
		}

		/// <summary>
		/// Increment update the UI
		/// </summary>
		public virtual void UpdateUI() {
			RefreshPortTypes();
		}
		
		public void RefreshPortTypes() {
			for (int i = 0; i < inputPorts.Count;i++) {
				inputPorts[i]?.ReloadView();
			}
			for (int i = 0; i < outputPorts.Count;i++) {
				outputPorts[i]?.ReloadView();
			}
			MarkDirtyRepaint();
		}
		#endregion

		#region Functions
		public virtual void ReloadView() {
			base.expanded = true;
			nodeObject.Register();
			RemovePorts();
			RemoveControls();
			if(portInputContainer != null) {//Remove port container to make sure data is up to date.
				Remove(portInputContainer);
			}

			portInputContainer = new VisualElement {
				name = "portInputContainer",
				pickingMode = PickingMode.Ignore,
			};
			Add(portInputContainer);
			portInputContainer.SendToBack();
		}

		public virtual void RegisterUndo(string name = "") {
			uNodeEditorUtility.RegisterUndo(nodeObject.GetUnityObject(), name);
		}

		/// <summary>
		/// Called on port has been connected
		/// </summary>
		/// <param name="port"></param>
		public virtual void OnPortConnected(PortView port) {
			if(HasControl()) {
				m_CollapseButton.SetEnabled(false);
				m_CollapseButton.SetEnabled(true);
			}
		}

		/// <summary>
		/// Called on port has been disconnected
		/// </summary>
		/// <param name="port"></param>
		public virtual void OnPortDisconnected(PortView port) {

		}

		/// <summary>
		/// Called on any value changed.
		/// </summary>
		public virtual void OnValueChanged() {

		}

		public virtual string GetTitle() {
			return uNodeEditorUtility.RemoveHTMLTag(title);
		}

		public virtual bool ShowExpandButton() {
			return false;
		}

		public bool HasControl() {
			return inputControls.Count > 0 || outputControls.Count > 0;
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			if(nodeObject.node is ISuperNode) {
				evt.menu.AppendAction("Open...", (e) => {
					owner.graph.graphData.currentCanvas = targetNode.nodeObject;
					owner.graph.Refresh();
					owner.graph.UpdatePosition();
				}, DropdownMenuAction.AlwaysEnabled);
			}
		}

		public PortView GetInputPortByID(string id) {
			for (int i = 0; i < inputPorts.Count;i++) {
				if(inputPorts[i]?.GetPortID() == id) {
					return inputPorts[i];
				}
			}
			return null;
		}

		public void UpdateError(string message) {
			if(!string.IsNullOrEmpty(message)) {//Has error
				if(errorBadge == null) {
					errorBadge = IconBadge.CreateError(message);
					Add(errorBadge);
					errorBadge.AttachTo(titleContainer, SpriteAlignment.LeftCenter);
				} else {
					errorBadge.badgeText = message;
				}
			} else {
				if(errorBadge != null && errorBadge.parent != null) {
					errorBadge.Detach();
					errorBadge.RemoveFromHierarchy();
					errorBadge = null;
				}
			}
		}

		public virtual void Teleport(Rect position) {
			if(float.IsNaN(layout.width)) {
				//position.width = 0;
			}
			else {
				position.width = layout.width;
			}
			if(float.IsNaN(layout.height)) {
				//position.height = 0;
			}
			else {
				position.height = layout.height;
			}

			DoTeleport(position);
			nodeObject.position = position;
		}

		public override Rect GetPosition() {
			if(isHidden) {
				return hidingRect;
			}
			return base.GetPosition();
		}

		public override void SetPosition(Rect newPos) {
			DoTeleport(newPos);
		}

		private void DoTeleport(Rect position) {
			if(ownerBlock != null) {
				style.position = Position.Relative;
				return;
			}
			//style.position = Position.Absolute;
			//style.left = new StyleLength(StyleKeyword.Auto);
			//style.right = position.x;
			//style.top = position.y;
			if(isHidden) {
				hidingRect = new Rect(position.position, hidingRect.size);
			}
			base.SetPosition(position);
		}

		//public override Rect GetPosition() {
		//	if(resolvedStyle.position == Position.Absolute) {
		//		return new Rect(resolvedStyle.right, resolvedStyle.top, layout.width, layout.height);
		//	}
		//	return base.GetPosition();
		//}
		#endregion
	}
}