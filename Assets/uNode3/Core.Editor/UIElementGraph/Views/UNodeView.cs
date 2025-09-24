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
	internal interface IHighlightableReference {
		bool Highlighted { get; set; }
		bool ShouldHighlightReference(object reference) => OriginalReference != null && OriginalReference == reference;
		object OriginalReference { get; }
	}

	public abstract class UNodeView : NodeView, IHighlightableReference {
		public List<PortView> inputPorts = new List<PortView>();
		public List<PortView> outputPorts = new List<PortView>();

		public List<ControlView> inputControls = new List<ControlView>();
		public List<ControlView> outputControls = new List<ControlView>();

		#region Uss Class Name
		public const string ussClassBorderFlowNode = "flowNodeBorder";
		public const string ussClassBorderOnlyInput = "onlyInput";
		public const string ussClassBorderOnlyOutput = "onlyOutput";
		public const string ussClassBorderHasInputValue = "hasInputValue";
		public const string ussClassBorderHasOutputValue = "hasOutputValue";
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
		/// <summary>
		/// The node object that this view is representing.
		/// </summary>
		public NodeObject nodeObject { protected set; get; }
		public Node targetNode => nodeObject.node;
		/// <summary>
		/// The graph view that owns this node view.
		/// </summary>
		public UGraphView owner { protected set; get; }
		/// <summary>
		/// Get the graph editor reference.
		/// </summary>
		public UIElementGraph graphEditor {
			get {
				return owner.graphEditor;
			}
		}
		/// <summary>
		/// Get the graph data reference.
		/// </summary>
		public GraphEditorData graphData => owner.graphData;
		/// <summary>
		/// Should the node view automatically reload when the node data changed?
		/// </summary>
		public virtual bool autoReload => false;
		/// <summary>
		/// Should the node view fully reload when the node data changed?
		/// </summary>
		public virtual bool fullReload => nodeObject == null;

		public VisualElement flowInputContainer { protected set; get; }
		public VisualElement flowOutputContainer { protected set; get; }
		public VisualElement controlsContainer { protected set; get; }

		/// <summary>
		/// The rect position before the node is hidden.
		/// </summary>
		public Rect hidingRect { get; set; }
		/// <summary>
		/// Is the node hidden?
		/// </summary>
		public bool isHidden => parent == null;
		/// <summary>
		/// Is the node a block node?
		/// </summary>
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

		/// <summary>
		/// The reference object that can be used to highlight the node view.
		/// </summary>
		public virtual object OriginalReference => null;

		/// <summary>
		/// Highlight the node view.
		/// </summary>
		public bool Highlighted {
			get => ClassListContains("highlighted-reference");
			set => EnableInClassList("highlighted-reference", value);
		}

		/// <summary>
		/// Override this to highlight the node when the reference is matched.
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		public virtual bool ShouldHighlightReference(object reference) => OriginalReference != null && OriginalReference == reference;

		#region Hidden Views
		class HiddenView : UNodeView {
			public override void ReloadView() {

			}

			public override void Initialize(UGraphView owner, NodeObject node) {
				this.AddToClassList("hidden-node");
				nodeObject = node;
				this.owner = owner;
				this.HideElement();
				this.SetDisplay(false);
			}
		}

		public static UNodeView GetHiddenView(UGraphView owner, NodeObject node) {
			var result = new HiddenView();
			result.Initialize(owner, node);
			return result;
		}
		#endregion

		#region VisualElement
		private IconBadge errorBadge;
		protected VisualElement debugContainer;
		protected VisualElement border;
		protected Image titleIcon;
		protected Label titleLabel;
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
				Add(flowInputContainer);
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

			titleLabel = titleContainer.Q<Label>("title-label");
			titleIcon = new Image() { name = "title-icon" };
			titleContainer.Add(titleIcon);
			titleIcon.SendToBack();

			OnSetup();

			RegisterCallback<MouseDownEvent>(evt => {
				var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
				if(evt.button == 0 && evt.shiftKey && !evt.altKey) {
					CustomInspector.Inspect(mPos, new GraphEditorData(graphEditor.graphData, nodeObject));
				}
			});
			RegisterCallback<MouseOverEvent>((e) => {
				SetProxyEdgeVisible(true);
			});
			RegisterCallback<MouseLeaveEvent>((e) => {
				if(selected) return;
				SetProxyEdgeVisible(false);
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

		public override void OnSelected() {
			base.OnSelected();
			SetProxyEdgeVisible(true);
		}

		public override void OnUnselected() {
			base.OnUnselected();
			SetProxyEdgeVisible(false);
		}

		public void SetProxyEdgeVisible(bool visible) {
			for(int i = 0; i < inputPorts.Count; i++) {
				var edges = inputPorts[i].GetEdges();
				foreach(var edge in edges) {
					if(edge == null)
						continue;
					if(edge.isProxy) {
						edge.SetEdgeVisible(visible);
					}
				}
			}
			for(int i = 0; i < outputPorts.Count; i++) {
				var edges = outputPorts[i].GetEdges();
				foreach(var edge in edges) {
					if(edge == null)
						continue;
					if(edge.isProxy) {
						edge.SetEdgeVisible(visible);
					}
				}
			}
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
			{//Add styles
				var styles = nodeObject.Styles;
				if(styles != null) {
					for(int i = 0; i < styles.Length; i++) {
						AddToClassList(styles[i]);
					}
				}
			}
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
		/// Increment update the UI, this also called after ReloadView
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
			//MarkDirtyRepaint();
		}
		#endregion

		#region Functions
		/// <summary>
		/// This will be called by the <see cref="Initialize(UGraphView, NodeObject)"/> or when the UI is incrementally reloaded.
		/// </summary>
		public virtual void ReloadView() {
			base.expanded = true;
			nodeObject.Register();
			RemovePorts();
			RemoveControls();
		}

		public virtual void RegisterUndo(string name = "") {
			uNodeEditorUtility.RegisterUndo(nodeObject.GetUnityObject(), name);
		}

		public virtual void OnZoomUpdated(float zoom) {
			if(zoom > 0.2f) {
				if(titleIcon != null) {
					titleIcon.style.visibility = StyleKeyword.Null;
				}
				if(titleLabel != null) {
					titleLabel.style.visibility = StyleKeyword.Null;
				}
				if(controlsContainer != null) {
					controlsContainer.style.visibility = StyleKeyword.Null;
				}
			}
			else {
				if(titleIcon != null) {
					titleIcon.style.visibility = Visibility.Hidden;
				}
				if(titleLabel != null) {
					titleLabel.style.visibility = Visibility.Hidden;
				}
				if(controlsContainer != null) {
					controlsContainer.style.visibility = Visibility.Hidden;
				}
			}
			if(zoom > 0.2f) {
				foreach(var p in inputPorts) {
					p.visible = true;
				}
				foreach(var p in outputPorts) {
					p.visible = true;
				}
			}
			else {
				foreach(var p in inputPorts) {
					p.visible = false;
				}
				foreach(var p in outputPorts) {
					p.visible = false;
				}
			}
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
		/// Called when node will be removed/deleted
		/// </summary>
		public virtual void OnNodeRemoved() { }

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
					owner.graphEditor.graphData.currentCanvas = targetNode.nodeObject;
					owner.graphEditor.Refresh();
					owner.graphEditor.UpdatePosition();
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

		public virtual UGraphElement GetSelectableObject() => nodeObject;

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

		/// <summary>
		/// Do update the node
		/// </summary>
		protected virtual void DoUpdate() {
			if(!this.IsVisible())
				return;
			#region Errors
			var errors = GraphUtility.ErrorChecker.GetErrorMessages(nodeObject, InfoType.Error);
			if(errors != null && errors.Any()) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				foreach(var error in errors) {
					if(sb.Length > 0) {
						sb.AppendLine();
						sb.AppendLine();
					}
					sb.Append("-" + uNodeEditorUtility.RemoveHTMLTag(error.message));
				}
				UpdateError(sb.ToString());
			}
			else {
				UpdateError(string.Empty);
			}
			#endregion
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
			owner.MarkUIChange();
			base.SetPosition(position);
		}

		public virtual IEnumerable<UNodeView> GetCarryNodes() => null;

		//public override Rect GetPosition() {
		//	if(resolvedStyle.position == Position.Absolute) {
		//		return new Rect(resolvedStyle.right, resolvedStyle.top, layout.width, layout.height);
		//	}
		//	return base.GetPosition();
		//}
		#endregion
		}
}