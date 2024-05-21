using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Reflection;

namespace MaxyGames.UNode {
	[GraphElement]
	public abstract class Node : IErrorCheck {
		public string name => _nodeObject?.name;
		public int id => _nodeObject?.id ?? 0;
		public string comment => _nodeObject?.comment;
		/// <summary>
		/// The node position in graph.
		/// </summary>
		public Rect position { get => nodeObject.position; set => nodeObject.position = value; }

		private NodeObject _nodeObject;
		/// <summary>
		/// The node object that is bound to this.
		/// </summary>
		public NodeObject nodeObject {
			get {
				if(_nodeObject == null) {
					if(object.ReferenceEquals(_nodeObject, null)) {
						throw new System.Exception("The data is not initialized.");
					}
				}
				return _nodeObject;
			}
			set {
				_nodeObject = value;
			}
		}

		protected GraphException exceptionInvalidFlow => new GraphException("Live editing: trying to execute flow on invalid node." + "Node: " + GetTitle() + " - id:" + id, this);
		protected GraphException exceptionInvalidValue => new GraphException("Live editing: trying to get / set on invalid node." + "Node: " + GetTitle() + " - id:" + id, this);

		/// <summary>
		/// True if the node is valid and not destroyed or destroyed with safe mode.
		/// </summary>
		public bool IsValid => _nodeObject.IsValidElement();

		/// <summary>
		/// Register the node if it not registered.
		/// </summary>
		public void EnsureRegistered() => nodeObject.EnsureRegistered();

		/// <summary>
		/// Re-register the node.
		/// </summary>
		public void Register() => nodeObject.Register();

		/// <summary>
		/// The node registration.
		/// Note: called on Editor and Runtime and may called multiple times in Editor.
		/// </summary>
		protected abstract void OnRegister();

		public virtual void OnRuntimeInitialize(GraphInstance instance) { }

		#region Editors
		public virtual string GetTitle() {
			var type = GetType();
			if(type.IsDefined(typeof(NodeMenu), true)) {
				return type.GetCustomAttribute<NodeMenu>().name;
			} else if(!string.IsNullOrEmpty(name)) {
				return name;
			} else {
				return type.PrettyName();
			}
		}

		public virtual string GetRichTitle() {
			return GetTitle();
		}

		public virtual string GetRichName() {
			return GetRichTitle();
		}

		public virtual System.Type GetNodeIcon() {
			if(GetType().IsDefined(typeof(NodeMenu), true)) {
				var icon = GetType().GetCustomAttribute<NodeMenu>().icon;
				if(icon != null)
					return icon;
			}
			if(IsFlowNode()) {
				return typeof(TypeIcons.FlowIcon);
			} else {
				return typeof(TypeIcons.ExtensionIcon);
			}
		}

		/// <summary>
		/// (editor only) called on inspector is being show.
		/// </summary>
		public virtual void OnInspectorInitialize() {
			var members = ReflectionUtils.GetFieldAndPropertiesCached(this.GetType());
			for(int i = 0; i < members.Length; i++) {
				if(members[i].IsDefined(typeof(TooltipAttribute), true)) {
					if(members[i] is FieldInfo field) {
						if(field.FieldType.IsCastableTo(typeof(UPort))) {
							var val = field.GetValueOptimized(this) as UPort;
							val.SetTooltip(field.GetCustomAttribute<TooltipAttribute>().tooltip);
						}
					} else if(members[i] is PropertyInfo property) {
						if(property.PropertyType.IsCastableTo(typeof(UPort))) {
							var val = property.GetValueOptimized(this) as UPort;
							val.SetTooltip(property.GetCustomAttribute<TooltipAttribute>().tooltip);
						}
					}
				}
			}
		}
		#endregion

		/// <summary>
		/// The get action for the primary value output.
		/// Note: if not overriding, will get the port output data.
		/// </summary>
		/// <returns></returns>
		public virtual object GetValue(Flow flow) {
			var port = nodeObject.primaryValueOutput;
			if(port != null) {
				//Get port value with default Get Callback
				return port.DefaultGet(flow);
			}
			return null;
		}

		/// <summary>
		/// The set action for the primary value output.
		/// Note: if not overriding, will set the port output data.
		/// </summary>
		/// <param name="value"></param>
		public virtual void SetValue(Flow flow, object value) {
			var port = nodeObject.primaryValueOutput;
			if(port != null) {
				//Set port with default Set Callback
				port.DefaultSet(flow, value);
			}
		}

		/// <summary>
		/// The type port of the primary value output.
		/// </summary>
		/// <returns></returns>
		public virtual Type ReturnType() => typeof(object);

		public virtual bool CanGetValue() => nodeObject.primaryValueOutput != null;
		public virtual bool CanSetValue() => false;

		/// <summary>
		/// Initialization for code generation.
		/// Note: overriding without calling it's base will disable GenerateFlowCode and GenerateValueCode methods.
		/// </summary>
		public virtual void OnGeneratorInitialize() { 
			if(nodeObject.primaryFlowInput != null) {
				CG.RegisterPort(nodeObject.primaryFlowInput, GenerateFlowCode);
			}
			if(nodeObject.primaryValueOutput != null) {
				CG.RegisterPort(nodeObject.primaryValueOutput, GenerateValueCode);
			}
		}

		/// <summary>
		/// Generate flow code for primary input flow.
		/// Note: you need to manually register other flow inputs in OnGeneratorInitialize.
		/// </summary>
		/// <returns></returns>
		protected virtual string GenerateFlowCode() {
			throw new NotImplementedException("Not implement on: " + GetType());
		}

		/// <summary>
		/// Generate value code for primary output value.
		/// Note: you need to manually register other value outputs in OnGeneratorInitialize.
		/// </summary>
		/// <returns></returns>
		protected virtual string GenerateValueCode() {
			throw new NotImplementedException("Not implement on: " + GetType());
		}

		#region Functions
		protected ValueOutput PrimaryValueOutput(string id) {
			if(nodeObject.primaryValueOutput != null)
				throw new Exception("The primaty value output has been registered but you're trying to register multiple times.");
			var result = ValueOutput(id, ReturnType, PortAccessibility.ReadWrite);
			result.canGetValue = CanGetValue;
			result.canSetValue = CanSetValue;
			result.AssignGetCallback(nodeObject.GetPrimaryValue);
			result.AssignSetCallback(nodeObject.SetPrimaryValue);
			nodeObject.primaryValueOutput = result;
			return result;
		}

		protected ValueOutput PrimaryValueOutput(string id, Func<Type> type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
			if(nodeObject.primaryValueOutput != null)
				throw Exception_MultiplePrimaryValueOutput;
			var result = ValueOutput(id, type, accessibility);
			nodeObject.primaryValueOutput = result;
			return result;
		}

		protected ValueOutput PrimaryValueOutput(string id, Type type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
			if(nodeObject.primaryValueOutput != null)
				throw Exception_MultiplePrimaryValueOutput;
			var result = ValueOutput(id, type, accessibility);
			nodeObject.primaryValueOutput = result;
			return result;
		}

		protected ValueOutput ValueOutput(string id, Func<Type> type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
			if(nodeObject.ValueOutputs.Any(p => p.id == id)) {
				throw new Exception($"Duplicate port for '{id}' in {GetType()}");
			}
			var port = new ValueOutput(this, id, type, accessibility);
			return nodeObject.RegisterPort(port);
		}

		protected ValueOutput ValueOutput(string id, Type type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
			if(nodeObject.ValueOutputs.Any(p => p.id == id)) {
				throw new Exception($"Duplicate port for '{id}' in {GetType()}");
			}
			var port = new ValueOutput(this, id, type, accessibility);
			return nodeObject.RegisterPort(port);
		}

		protected ValueOutput ValueOutput<T>(string id, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
			return ValueOutput(id, typeof(T), accessibility);
		}

		protected ValueInput ValueInput(string id, Type type) {
			return ValueInput(id, type, null);
		}

		protected ValueInput ValueInput(string id, Type type, object @default) {
			return ValueInput(id, type, MemberData.CreateFromValue(@default, type));
		}

		protected ValueInput ValueInput(string id, Type type, MemberData @default) {
			if(nodeObject.ValueInputs.Any(p => p.id == id)) {
				throw new ArgumentException($"Duplicate port for '{id}' in {GetType()}");
			}
			var port = nodeObject.RegisterPort(new ValueInput(this, id, type), out var isNew);
			if(isNew) {
				if(@default != null) {
					port.defaultValue = @default;
				}
				else {
					port.defaultValue = MemberData.None;
				}
			}
			return port;
		}

		protected ValueInput ValueInput(string id, Func<Type> type, MemberData @default = null) {
			if(nodeObject.ValueInputs.Any(p => p.id == id)) {
				throw new ArgumentException($"Duplicate port for '{id}' in {GetType()}");
			}
			var port = nodeObject.RegisterPort(new ValueInput(this, id, type), out var isNew);
			if(isNew) {
				if(@default != null) {
					port.defaultValue = @default;
				}
				else {
					port.defaultValue = MemberData.None;
				}
			}
			return port;
		}

		protected ValueInput ValueInput(string id, Type type, out bool isNew) {
			if(nodeObject.ValueInputs.Any(p => p.id == id)) {
				throw new ArgumentException($"Duplicate port for '{id}' in {GetType()}");
			}
			return nodeObject.RegisterPort(new ValueInput(this, id, type), out isNew);
		}

		protected ValueInput ValueInput(string id, Func<Type> type, out bool isNew) {
			if(nodeObject.ValueInputs.Any(p => p.id == id)) {
				throw new ArgumentException($"Duplicate port for '{id}' in {GetType()}");
			}
			return nodeObject.RegisterPort(new ValueInput(this, id, type), out isNew);
		}

		protected ValueInput ValueInput<T>(string id) {
			return ValueInput(id, typeof(T));
		}

		protected ValueInput ValueInput<T>(string id, T @default) {
			var port = ValueInput(id, typeof(T), MemberData.CreateFromValue(@default));
			return port;
		}

		protected FlowInput PrimaryFlowInput(string id, Action<Flow> action) {
			if(nodeObject.primaryFlowInput != null)
				throw Exception_MultiplePrimaryFlowInput;
			var result = FlowInput(id, action).SetName("");
			nodeObject.primaryFlowInput = result;
			return result;
		}

		protected FlowInput PrimaryFlowInput(string id, Func<Flow, IEnumerator> action) {
			if(nodeObject.primaryFlowInput != null)
				throw Exception_MultiplePrimaryFlowInput;
			var result = FlowInput(id, action).SetName("");
			nodeObject.primaryFlowInput = result;
			return result;
		}

		protected FlowInput FlowInput(string id, Action<Flow> action) {
			if(nodeObject.ValueInputs.Any(p => p.id == id)) {
				throw new ArgumentException($"Duplicate port for '{id}' in {GetType()}");
			}
			var port = new FlowInput(this, id, action);
			return nodeObject.RegisterPort(port);
		}

		protected FlowInput FlowInput(string id, Func<Flow, IEnumerator> action) {
			if(nodeObject.ValueInputs.Any(p => p.id == id)) {
				throw new ArgumentException($"Duplicate port for '{id}' in {GetType()}");
			}
			var port = new FlowInput(this, id, action);
			return nodeObject.RegisterPort(port);
		}

		internal static Exception Exception_MultiplePrimaryFlowOutput => new Exception("The primaty flow output has been registered but you're trying to register multiple times.");
		internal static Exception Exception_MultiplePrimaryFlowInput => new Exception("The primaty flow input has been registered but you're trying to register multiple times.");
		internal static Exception Exception_MultiplePrimaryValueOutput => new Exception("The primaty value output has been registered but you're trying to register multiple times.");

		protected FlowOutput PrimaryFlowOutput(string id) {
			if(nodeObject.primaryFlowOutput != null)
				throw Exception_MultiplePrimaryFlowOutput;
			var result = FlowOutput(id).SetName("");
			result.isNextFlow = true;
			nodeObject.primaryFlowOutput = result;
			return result;
		}

		protected FlowOutput FlowOutput(string id) {
			if(nodeObject.ValueInputs.Any(p => p.id == id)) {
				throw new ArgumentException($"Duplicate port for '{id}' in {GetType()}");
			}
			var port = new FlowOutput(this, id);
			return nodeObject.RegisterPort(port);
		}

		protected bool HasCoroutineInFlows(params FlowOutput[] flows) {
			if(flows == null)
				return false;
			for(int i = 0; i < flows.Length; i++) {
				if(flows[i].IsCoroutine())
					return true;
			}
			return false;
		}
		#endregion

		#region Utility
		public bool IsFlowNode() => nodeObject.primaryFlowInput != null;
		public bool IsValueNode() => nodeObject.primaryValueOutput != null;
		public virtual void CheckError(ErrorAnalyzer analyzer) {
			foreach(var port in nodeObject.ValueInputs) {
				analyzer.CheckPort(port);
			}
		}

		public static class Utilities {
			internal static void DoRegister(Node node) {
				node.OnRegister();
			}

			public static ValueOutput PrimaryValueOutput(Node node, string id) {
				return node.PrimaryValueOutput(id);
			}

			public static ValueOutput PrimaryValueOutput(Node node, string id, Func<Type> type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
				return node.PrimaryValueOutput(id, type, accessibility);
			}

			public static ValueOutput PrimaryValueOutput(Node node, string id, Type type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
				return node.PrimaryValueOutput(id, type, accessibility);
			}

			public static ValueOutput ValueOutput(Node node, string id, Func<Type> type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
				return node.ValueOutput(id, type, accessibility);
			}

			public static ValueOutput ValueOutput(Node node, string id, Type type, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
				return node.ValueOutput(id, type, accessibility);
			}

			public static ValueOutput ValueOutput<T>(Node node, string id, PortAccessibility accessibility = PortAccessibility.ReadOnly) {
				return node.ValueOutput<T>(id, accessibility);
			}

			public static ValueInput ValueInput(Node node, string id, Type type) {
				return node.ValueInput(id, type);
			}

			public static ValueInput ValueInput(Node node, string id, Type type, object @default) {
				return node.ValueInput(id, type, @default);
			}

			public static ValueInput ValueInput(Node node, string id, Type type, MemberData @default) {
				return node.ValueInput(id, type, @default);
			}

			public static ValueInput ValueInput(Node node, string id, Func<Type> type, MemberData @default = null) {
				return node.ValueInput(id, type, @default);
			}

			public static ValueInput ValueInput(Node node, string id, Type type, out bool isNew) {
				return node.ValueInput(id, type, out isNew);
			}

			public static ValueInput ValueInput(Node node, string id, Func<Type> type, out bool isNew) {
				return node.ValueInput(id, type, out isNew);
			}

			public static ValueInput ValueInput<T>(Node node, string id) {
				return node.ValueInput<T>(id);
			}

			public static ValueInput ValueInput<T>(Node node, string id, T @default) {
				return node.ValueInput<T>(id, @default);
			}

			public static FlowInput PrimaryFlowInput(Node node, string id, Action<Flow> action) {
				return node.PrimaryFlowInput(id, action);
			}

			public static FlowInput PrimaryFlowInput(Node node, string id, Func<Flow, IEnumerator> action) {
				return node.PrimaryFlowInput(id, action);
			}

			public static FlowInput FlowInput(Node node, string id, Action<Flow> action) {
				return node.FlowInput(id, action);
			}

			public static FlowInput FlowInput(Node node, string id, Func<Flow, IEnumerator> action) {
				return node.FlowInput(id, action);
			}

			public static FlowOutput PrimaryFlowOutput(Node node, string id) {
				return node.PrimaryFlowOutput(id);
			}

			public static FlowOutput FlowOutput(Node node, string id) {
				return node.FlowOutput(id);
			}

			public static bool HasValueInput(Node node, string id) {
				return node.nodeObject.ValueInputs.Any(p => p.id == id);
			}

			public static bool HasValueOutput(Node node, string id) {
				return node.nodeObject.ValueOutputs.Any(p => p.id == id);
			}

			public static bool HasFlowInput(Node node, string id) {
				return node.nodeObject.FlowInputs.Any(p => p.id == id);
			}

			public static bool HasFlowOutput(Node node, string id) {
				return node.nodeObject.FlowOutputs.Any(p => p.id == id);
			}
		}
		#endregion

		#region Operators
		public static implicit operator UGraphElement(Node node) {
			if(node == null)
				return null;
			return node.nodeObject;
		}
		#endregion
	}

	public abstract class BaseFlowNode : Node {
		[NonSerialized]
		public FlowInput enter;

		protected virtual bool IsCoroutine() => IsSelfCoroutine();
		protected virtual bool IsSelfCoroutine() {
			if(this.GetType().IsDefined(typeof(NodeMenu))) {
				var att = this.GetType().GetCustomAttribute<NodeMenu>();
				return att.IsCoroutine;
			}
			return false;
		}

		protected override void OnRegister() {
			RegisterFlowPort();
		}

		protected virtual void RegisterFlowPort() {
			enter = PrimaryFlowInput(nameof(enter), DoExecute);
			enter.actionCoroutine = DoExecuteCoroutine;
			enter.isCoroutine = IsCoroutine;
			enter.isSelfCoroutine = IsSelfCoroutine;
		}

		//(Protect execute from invalid node)
		private void DoExecute(Flow flow) {
#if UNITY_EDITOR
			if(IsValid == false) {
				//For live editing
				var element = flow.GetValidNode(this);
				if(element != null) {
					//We found the valid element and so we get the value from the valid element instead.
					element.OnExecuted(flow);
					return;
				}
				//Log to console if the valid node is not found.
				Debug.Log(exceptionInvalidFlow);
			}
#endif
			OnExecuted(flow);
		}

		//(Protect execute from invalid node)
		private IEnumerator DoExecuteCoroutine(Flow flow) {
#if UNITY_EDITOR
			if(IsValid == false) {
				//For live editing
				var element = flow.GetValidNode(this);
				if(element != null) {
					//We found the valid element and so we get the value from the valid element instead.
					yield return element.OnExecutedCoroutine(flow);
					yield break;
				}
				//Log to console if the valid node is not found.
				Debug.Log(exceptionInvalidFlow);
			}
#endif
			yield return OnExecutedCoroutine(flow);
		}

		protected abstract void OnExecuted(Flow flow);

		protected virtual IEnumerator OnExecutedCoroutine(Flow flow) {
			OnExecuted(flow);
			yield break;
		}
	}

	public abstract class BaseCoroutineNode : BaseFlowNode {
		protected override bool IsSelfCoroutine() => true;

		protected override void OnExecuted(Flow data) {
			throw new InvalidOperationException("unexpected operation.");
		}
	}

	public abstract class ValueNode : Node {
		[NonSerialized]
		public ValueOutput output;

		protected override void OnRegister() {
			output = PrimaryValueOutput(nameof(output)).SetName("Out");
		}
	}

	public abstract class FlowAndValueNode : FlowNode {
		[NonSerialized]
		public ValueOutput output;

		protected override void OnRegister() {
			base.OnRegister();
			RegisterValuePort();
		}

		protected void RegisterValuePort() {
			output = PrimaryValueOutput(nameof(output)).SetName("Out");
		}
	}

	public abstract class FlowNode : BaseFlowNode {
		[NonSerialized]
		public FlowOutput exit;

		protected virtual bool AutoExit => true;

		protected override bool IsCoroutine() => IsSelfCoroutine() || exit.IsCoroutine();

		protected override void RegisterFlowPort() {
			enter = PrimaryFlowInput(nameof(enter), DoExecute);
			enter.actionCoroutine = DoExecuteCoroutine;
			enter.isCoroutine = IsCoroutine;
			enter.isSelfCoroutine = IsSelfCoroutine;
			exit = PrimaryFlowOutput(nameof(exit));
		}

		//(Protect execute from invalid node)
		private void DoExecute(Flow flow) {
#if UNITY_EDITOR
			if(IsValid == false) {
				//For live editing
				var element = flow.GetValidNode(this);
				if(element != null) {
					//We found the valid element and so we get the value from the valid element instead.
					element.OnExecuted(flow);
					var js = flow.jumpStatement;
					if(js == null && AutoExit) {
						flow.Next(exit);
					}
					return;
				}
				//Log to console if the valid node is not found.
				Debug.Log(exceptionInvalidFlow);
			}
#endif
			{
				OnExecuted(flow);
				var js = flow.jumpStatement;
				if(js == null && AutoExit) {
					flow.Next(exit);
				}
			}
		}

		//(Protect execute from invalid node)
		private IEnumerator DoExecuteCoroutine(Flow flow) {
#if UNITY_EDITOR
			if(IsValid == false) {
				//For live editing
				var element = flow.GetValidNode(this);
				if(element != null) {
					//We found the valid element and so we get the value from the valid element instead.
					yield return element.OnExecutedCoroutine(flow);
					if(flow.jumpStatement == null && AutoExit) {
						flow.Next(exit);
					}
					yield break;
				}
				//Log to console if the valid node is not found.
				Debug.Log(exceptionInvalidFlow);
			}
#endif
			yield return OnExecutedCoroutine(flow);
			if(flow.jumpStatement == null && AutoExit) {
				flow.Next(exit);
			}
		}
	}

	public abstract class CoroutineNode : FlowNode {
		protected override bool IsSelfCoroutine() => true;

		protected override void OnExecuted(Flow flow) {
			throw new InvalidOperationException(this.GetType().ToString());
		}
	}
}