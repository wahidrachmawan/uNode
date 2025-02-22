using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public sealed class NodeSerializedData {
		public byte[] data;
		public List<object> references;
		public List<object> References {
			get {
				if(references == null && serializedReferences != null) {
					references = new List<object>(serializedReferences.Count);
					ReloadReferences();
				}
				return references;
			}
			set {
				references = value;
				if(value == null) {
					serializedReferences = null;
				}
				else {
					if(serializedReferences == null) {
						serializedReferences = new(value.Count);
					}
					else {
						if(serializedReferences.Capacity < value.Count) {
							serializedReferences.Capacity = value.Capacity;
						}
						serializedReferences.Clear();
					}
					for(int i = 0; i < value.Count; i++) {
						serializedReferences.Add(new USerializedValueWrapper(value[i]));
					}
				}
			}
		}

		internal void ReloadReferences() {
			if(references == null) return;
			if(serializedReferences == null) return;
			if(serializedReferences.Count == 0) return;
			if(references.Capacity < serializedReferences.Capacity) {
				references.Capacity = serializedReferences.Capacity;
			}
			//clear the references so we get the newers data
			references.Clear();
			for(int i = 0; i < serializedReferences.Count; i++) {
				if(serializedReferences[i].value != null) {
					references.Add(serializedReferences[i].value);
				}
				else {
					references.Add(serializedReferences[i].unityObject);
				}
			}
		}

		[SerializeField]
		private List<USerializedValueWrapper> serializedReferences;

		[SerializeReference, DoNotSerialize]
		public List<ValueInput> serializedValueInput;
		[SerializeReference, DoNotSerialize]
		public List<ValueOutput> serializedValueOutput;
		[SerializeReference, DoNotSerialize]
		public List<FlowInput> serializedFlowInput;
		[SerializeReference, DoNotSerialize]
		public List<FlowOutput> serializedFlowOutput;


		public string serializedType;

		private System.Type _type;
		public System.Type type {
			get {
				if(_type == null) {
					if(string.IsNullOrEmpty(serializedType)) {
						return typeof(object);
					}
					_type = TypeSerializer.Deserialize(serializedType, false);
				}
				return _type;
			}
			set {
				_type = type;
				serializedType = TypeSerializer.Serialize(value);
			}
		}

		public bool isFilled => data != null && data.Length > 0;
	}

	public sealed class NodeObject : UGraphElement, ISerializationCallbackReceiver, IErrorCheck, IPrettyName, IRichName, IIcon {
		public bool nodeExpanded = true;
		/// <summary>
		/// The node position in graph.
		/// </summary>
		public Rect position;

		[SerializeField]
		private NodeSerializedData _nodeSerializedData;

		public NodeSerializedData serializedData => _nodeSerializedData;

		private Node _node;
		/// <summary>
		/// The actual node instance.
		/// </summary>
		public Node node {
			get {
				return _node;
			}
			set {
				if(_node != null) {
					_node.nodeObject = null;
				}
				_node = value;
				if(value != null) {
					value.nodeObject = this;
				}
			}
		}

		#region Constructors
		public NodeObject() { }

		public NodeObject(Node node) {
			this.node = node;
		}
		#endregion

		#region Ports
		[NonSerialized]
		public FlowInput primaryFlowInput;
		[NonSerialized]
		public FlowOutput primaryFlowOutput;
		[NonSerialized]
		public ValueOutput primaryValueOutput;

		private NodePorts<ValueInput> _valueInputs;
		/// <summary>
		/// The list of all value input ports
		/// </summary>
		[SerializeField]
		public NodePorts<ValueInput> ValueInputs {
			get {
				if(_valueInputs == null)
					_valueInputs = new NodePorts<ValueInput>(this);
				return _valueInputs;
			}
			set {
				_valueInputs = value;
				_valueInputs.node = this;
			}
		}

		private NodePorts<ValueOutput> _valueOutputs;
		/// <summary>
		/// The list of all value output ports
		/// </summary>
		[SerializeField]
		public NodePorts<ValueOutput> ValueOutputs {
			get {
				if(_valueOutputs == null)
					_valueOutputs = new NodePorts<ValueOutput>(this);
				return _valueOutputs;
			}
			set {
				_valueOutputs = value;
				_valueOutputs.node = this;
			}
		}

		private NodePorts<FlowInput> _flowInputs;
		/// <summary>
		/// The list of all flow input ports
		/// </summary>
		[SerializeField]
		public NodePorts<FlowInput> FlowInputs {
			get {
				if(_flowInputs == null)
					_flowInputs = new NodePorts<FlowInput>(this);
				return _flowInputs;
			}
			set {
				_flowInputs = value;
				_flowInputs.node = this;
			}
		}

		private NodePorts<FlowOutput> _flowOutputs;
		/// <summary>
		/// The list of all flow output ports
		/// </summary>
		[SerializeField]
		public NodePorts<FlowOutput> FlowOutputs {
			get {
				if(_flowOutputs == null)
					_flowOutputs = new NodePorts<FlowOutput>(this);
				return _flowOutputs;
			}
			set {
				_flowOutputs = value;
				_flowOutputs.node = this;
			}
		}
		[SerializeReference]
		private List<UPort> _invalidPorts;
		/// <summary>
		/// The list of all invalid ports
		/// </summary>
		public List<UPort> InvalidPorts {
			get {
				if(_invalidPorts == null)
					_invalidPorts = new List<UPort>();
				return _invalidPorts;
			}
		}

		/// <summary>
		/// Get the value input port by id
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public ValueInput GetValueInput(string key) {
			return ValueInputs.FirstOrDefault(p => p.id == key);
		}

		/// <summary>
		/// Get the value output port by id
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public ValueOutput GetValueOutput(string key) {
			return ValueOutputs.FirstOrDefault(p => p.id == key);
		}

		/// <summary>
		/// Get the flow input port by id
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public FlowInput GetFlowInput(string key) {
			return FlowInputs.FirstOrDefault(p => p.id == key);
		}

		/// <summary>
		/// Get the flow output port by id
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public FlowOutput GetFlowOutput(string key) {
			return FlowOutputs.FirstOrDefault(p => p.id == key);
		}
		#endregion

		#region Initialization
		/// <summary>
		/// True if the node has been registered.
		/// </summary>
		[field: NonSerialized]
		public bool isRegistered { get; internal set; }
		[field: NonSerialized]
		public bool isPreviousPortRestored { get; private set; }
		[field: NonSerialized]
		public Exception exceptionRegister { get; private set; }
		private NodePreservation preservation;

		internal void OnGeneratorInitialize() {
			if (node == null)
				return;
			node.OnGeneratorInitialize();
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			try {
				EnsureRegistered();
				node?.OnRuntimeInitialize(instance);
			}
			catch(Exception ex) {
				if(ex is GraphException) {
					throw;
				}
				else {
					throw new GraphException(ex, this);
				}
			}
		}

		/// <summary>
		/// Register the node if it not registered.
		/// </summary>
		public void EnsureRegistered() {
			if(!isRegistered) {
				Register();
			}
		}

		/// <summary>
		/// Register the node
		/// </summary>
		public void Register() {
			if(_node == null)
				return;//Skip register if the node is null / missing.
			primaryFlowInput = null;
			primaryFlowOutput = null;
			primaryValueOutput = null;
			isPreviousPortRestored = false;
			preservation = NodePreservation.Preserve(this);
			Unregister();
			try {
				Node.Utilities.DoRegister(_node);
				exceptionRegister = null;
				isRegistered = true;
				preservation.RestoreInvalid();
			}
			catch(Exception ex) {
				RestorePreviousPort();
				exceptionRegister = ex;
				Debug.LogError(new GraphException(ex, this));
			}
			preservation = null;
		}

		internal void Unregister() {
			ValueInputs.Clear();
			ValueOutputs.Clear();
			FlowInputs.Clear();
			FlowOutputs.Clear();
			isRegistered = false;
		}

		/// <summary>
		/// Can only be call inside <see cref="Node.OnRegister"/>
		/// </summary>
		public void RestorePreviousPort() {
			preservation.RestoreCauseOfErrors();
			isPreviousPortRestored = true;
		}

		public ValueInput RegisterPort(ValueInput port) {
			if(preservation != null) {
				return preservation.RestorePort(port);
			} else {
				ValueInputs.Add(port);
				return port;
			}
		}

		public ValueInput RegisterPort(ValueInput port, out bool isNew) {
			if(preservation != null) {
				return preservation.RestorePort(port, out isNew);
			}
			else {
				isNew = true;
				ValueInputs.Add(port);
				return port;
			}
		}

		public ValueOutput RegisterPort(ValueOutput port) {
			if(preservation != null) {
				return preservation.RestorePort(port);
			} else {
				ValueOutputs.Add(port);
				return port;
			}
		}

		public ValueOutput RegisterPort(ValueOutput port, out bool isNew) {
			if(preservation != null) {
				return preservation.RestorePort(port, out isNew);
			}
			else {
				isNew = true;
				ValueOutputs.Add(port);
				return port;
			}
		}

		public FlowInput RegisterPort(FlowInput port) {
			if(preservation != null) {
				return preservation.RestorePort(port);
			} else {
				FlowInputs.Add(port);
				return port;
			}
		}

		public FlowInput RegisterPort(FlowInput port, out bool isNew) {
			if(preservation != null) {
				return preservation.RestorePort(port, out isNew);
			}
			else {
				isNew = true;
				FlowInputs.Add(port);
				return port;
			}
		}

		public FlowOutput RegisterPort(FlowOutput port) {
			if(preservation != null) {
				return preservation.RestorePort(port);
			} else {
				FlowOutputs.Add(port);
				return port;
			}
		}

		public FlowOutput RegisterPort(FlowOutput port, out bool isNew) {
			if(preservation != null) {
				return preservation.RestorePort(port, out isNew);
			}
			else {
				isNew = true;
				FlowOutputs.Add(port);
				return port;
			}
		}
		#endregion

		#region Functions
		/// <summary>
		/// Get the title of the node
		/// </summary>
		/// <returns></returns>
		public string GetTitle() {
			try {
				return node?.GetTitle() ?? name;
			}
			catch(Exception ex) {
				Debug.LogError(new GraphException(ex, this));
				return name;
			}
		}

		/// <summary>
		/// Get rich title of the node
		/// </summary>
		/// <returns></returns>
		public string GetRichTitle() {
			try {
				return node?.GetRichTitle() ?? name;
			}
			catch(Exception ex) {
				Debug.LogError(new GraphException(ex, this));
				return name;
			}
		}

		/// <summary>
		/// Get rich name of the node
		/// </summary>
		/// <returns></returns>
		public string GetRichName() {
			try {
				return node?.GetRichName() ?? name;
			}
			catch(Exception ex) {
				Debug.LogError(new GraphException(ex, this));
				return name;
			}
		}

		public bool CanSetValue() {
			return node?.CanSetValue() ?? false;
		}

		public bool CanGetValue() {
			return node?.CanGetValue() ?? false;
		}

		/// <summary>
		/// The return type of a primary value output port
		/// </summary>
		/// <returns></returns>
		public Type ReturnType() {
			return node?.ReturnType();
		}

		/// <summary>
		/// Get the node icon
		/// </summary>
		/// <returns></returns>
		public Type GetNodeIcon() {
			return node?.GetNodeIcon() ?? typeof(object);
		}

		public bool IsNodeContainer() {
			return false;
		}

		public void SetPosition(Vector2 position) {
			this.position.x = position.x;
			this.position.y = position.y;
		}

		/// <summary>
		/// The get action for the primary value output.
		/// (This function is protected from invalid node for live editing)
		/// </summary>
		/// <returns></returns>
		public object GetPrimaryValue(Flow flow) {
#if UNITY_EDITOR
			if(IsValid == false) {
				//For live editing
				var element = flow.GetValidElement(this);
				if(element != null) {
					//We found the valid element and so we get the value from the valid element instead.
					return element.node.GetValue(flow);
				}
				//Log to console if the valid node is not found.
				Debug.Log(GraphException.GetMessage("Live editing: trying to get value from invalid node." + "Node: " + GetTitle() + " - id:" + id, this));
			}
#endif
			return node.GetValue(flow);
		}

		/// <summary>
		/// The set action for the primary value output.
		/// (This function is protected from invalid node for live editing)
		/// </summary>
		/// <param name="value"></param>
		public void SetPrimaryValue(Flow flow, object value) {
#if UNITY_EDITOR
			if(IsValid == false) {
				//For live editing
				var element = flow.GetValidElement(this);
				if(element != null) {
					//We found the valid element and so we get the value from the valid element instead.
					element.node.SetValue(flow, value);
					return;
				}
				//Log to console if the valid node is not found.
				Debug.Log(GraphException.GetMessage("Live editing: trying to set value to invalid node." + "Node: " + GetTitle() + " - id:" + id, this));
			}
#endif
			node.SetValue(flow, value);
		}

		#endregion

		protected override void OnDestroy() {
			//foreach(var con in Connections.ToArray()) {
			//	con.Disconnect();
			//}
			Unregister();
			base.OnDestroy();
		}

		public IEnumerable<Connection> Connections {
			get {
				foreach(var port in ValueInputs) {
					foreach(var c in port.connections) {
						yield return c;
					}
				}
				foreach(var port in ValueOutputs) {
					foreach(var c in port.connections) {
						yield return c;
					}
				}
				foreach(var port in FlowInputs) {
					foreach(var c in port.connections) {
						yield return c;
					}
				}
				foreach(var port in FlowOutputs) {
					foreach(var c in port.connections) {
						yield return c;
					}
				}
			}
		}

		public static implicit operator Node(NodeObject node) {
			if(node == null)
				return null;
			return node.node;
		}

		public static implicit operator NodeObject(Node node) {
			if(node == null || node.nodeObject == null)
				return null;
			return node.nodeObject;
		}

		public override string ToString() {
			if(!object.ReferenceEquals(_node, null)) {
				return _node.GetTitle() + $" ({_node.GetType()})";
			}
			return base.ToString();
		}

		public void ValidatePorts() {
			foreach(var port in ValueInputs) {
				port.Validate();
			}
		}

		void IErrorCheck.CheckError(ErrorAnalyzer analizer) {
			EnsureRegistered();
			if(node == null) {
				analizer.RegisterError(this, "Missing type: " + serializedData.serializedType);
			}
		}

		string IPrettyName.GetPrettyName() {
			return GetTitle();
		}

		string IRichName.GetRichName() {
			return node.GetRichName();
		}

		Type IIcon.GetIcon() {
			return GetNodeIcon();
		}

		#region Serialization
		void ISerializationCallbackReceiver.OnBeforeSerialize() {
			if(_node != null) {
				if(_nodeSerializedData == null) {
					_nodeSerializedData = new NodeSerializedData();
				}
				using(var cache = SerializerUtility.UNodeSerializationContext) {
					using(var resolver = OdinSerializer.Utilities.Cache<SerializerUtility.GraphReferenceResolver>.Claim()) {
						//This for make sure the reference is same.
						resolver.Value.PrepareForSerialization(graph);

						cache.Value.IndexReferenceResolver = resolver.Value;
						var bytes = SerializerUtility.Serialize(_node, cache);
						_nodeSerializedData.data = bytes;
						_nodeSerializedData.References = resolver.Value.GetReferencedObjects();
						_nodeSerializedData.serializedType = TypeSerializer.Serialize(_node.GetType());
					}
				}
				foreach(var p in ValueInputs.Data) {
					if(p.connections.Count > 0) {
						p.defaultValue = null;
					}
				}
				_nodeSerializedData.serializedFlowInput = FlowInputs.Data;
				_nodeSerializedData.serializedFlowOutput = FlowOutputs.Data;
				_nodeSerializedData.serializedValueInput = ValueInputs.Data;
				_nodeSerializedData.serializedValueOutput = ValueOutputs.Data;
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() {
			if(_nodeSerializedData != null && _nodeSerializedData.isFilled) {
				using(var cache = SerializerUtility.UNodeDeserializationContext) {
					using(var resolver = OdinSerializer.Utilities.Cache<SerializerUtility.GraphReferenceResolver>.Claim()) {
						_nodeSerializedData.ReloadReferences();
						resolver.Value.SetReferencedObjects(_nodeSerializedData.References);
						cache.Value.IndexReferenceResolver = resolver.Value;
						node = SerializerUtility.DeserializeWeak(_nodeSerializedData.data, _nodeSerializedData.type, cache) as Node;
						//Ensure that we need to register it again
						isRegistered = false;
					}
				}
				if(_nodeSerializedData.serializedFlowInput != null) {
					if(_flowInputs == null) {
						foreach(var p in _nodeSerializedData.serializedFlowInput) {
							p.node = this;
						}
						foreach(var p in _nodeSerializedData.serializedFlowOutput) {
							p.node = this;
						}
						foreach(var p in _nodeSerializedData.serializedValueInput) {
							p.node = this;
						}
						foreach(var p in _nodeSerializedData.serializedValueOutput) {
							p.node = this;
						}
					}
					FlowInputs.node = this;
					FlowInputs.Data = _nodeSerializedData.serializedFlowInput;
					FlowOutputs.node = this;
					FlowOutputs.Data = _nodeSerializedData.serializedFlowOutput;
					ValueInputs.node = this;
					ValueInputs.Data = _nodeSerializedData.serializedValueInput;
					ValueOutputs.node = this;
					ValueOutputs.Data = _nodeSerializedData.serializedValueOutput;
				}
			}
		}
		#endregion
	}

	class NodePreservation {
		NodeObject node;
		public Dictionary<string, ValueInput> valueInputs = new Dictionary<string, ValueInput>();
		public Dictionary<string, ValueOutput> valueOutputs = new Dictionary<string, ValueOutput>();
		public Dictionary<string, FlowInput> flowInputs = new Dictionary<string, FlowInput>();
		public Dictionary<string, FlowOutput> flowOutputs = new Dictionary<string, FlowOutput>();

		public static NodePreservation Preserve(NodeObject node) {
			var value = new NodePreservation();
			value.node = node;
			foreach(var p in node.InvalidPorts) {
				if(p is ValueInput vi) {
					value.valueInputs[p.id] = vi;
				} else if(p is ValueOutput vo) {
					value.valueOutputs[p.id] = vo;
				} else if(p is FlowInput fi) {
					value.flowInputs[p.id] = fi;
				} else if(p is FlowOutput fo) {
					value.flowOutputs[p.id] = fo;
				}
			}
			foreach(var port in node.ValueInputs) {
				value.valueInputs[port.id] = port;
			}
			foreach(var port in node.ValueOutputs) {
				value.valueOutputs[port.id] = port;
			}
			foreach(var port in node.FlowInputs) {
				value.flowInputs[port.id] = port;
			}
			foreach(var port in node.FlowOutputs) {
				value.flowOutputs[port.id] = port;
			}
			return value;
		}

		public void RestoreInvalid() {
			node.InvalidPorts.Clear();
			foreach(var port in valueInputs) {
				node.InvalidPorts.Add(port.Value);
			}
			foreach(var port in valueOutputs) {
				node.InvalidPorts.Add(port.Value);
			}
			foreach(var port in flowInputs) {
				node.InvalidPorts.Add(port.Value);
			}
			foreach(var port in flowOutputs) {
				node.InvalidPorts.Add(port.Value);
			}
		}

		public void RestoreCauseOfErrors() {
			foreach(var port in valueInputs) {
				if(node.ValueInputs.Any((p) => p.id == port.Key) == false && node.InvalidPorts.Contains(port.Value) == false) {
					node.ValueInputs.Add(port.Value);
				}
			}
			foreach(var port in valueOutputs) {
				if(node.ValueOutputs.Any((p) => p.id == port.Key) == false && node.InvalidPorts.Contains(port.Value) == false) {
					node.ValueOutputs.Add(port.Value);
				}
			}
			foreach(var port in flowInputs) {
				if(node.FlowInputs.Any((p) => p.id == port.Key) == false && node.InvalidPorts.Contains(port.Value) == false) {
					node.FlowInputs.Add(port.Value);
				}
			}
			foreach(var port in flowOutputs) {
				if(node.FlowOutputs.Any((p) => p.id == port.Key) == false && node.InvalidPorts.Contains(port.Value) == false) {
					node.FlowOutputs.Add(port.Value);
				}
			}
			valueInputs.Clear();
			valueOutputs.Clear();
			flowInputs.Clear();
			flowOutputs.Clear();
		}

		public ValueInput RestorePort(ValueInput port) {
			if(valueInputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.ValueInputs.Add(val);
				valueInputs.Remove(port.id);
				return val;
			} else {
				node.ValueInputs.Add(port);
				return port;
			}
		}

		public ValueInput RestorePort(ValueInput port, out bool isNew) {
			if(valueInputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.ValueInputs.Add(val);
				valueInputs.Remove(port.id);
				isNew = false;
				return val;
			}
			else {
				node.ValueInputs.Add(port);
				isNew = true;
				return port;
			}
		}

		public ValueOutput RestorePort(ValueOutput port) {
			if(valueOutputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.ValueOutputs.Add(val);
				valueOutputs.Remove(port.id);
				return val;
			} else {
				node.ValueOutputs.Add(port);
				return port;
			}
		}

		public ValueOutput RestorePort(ValueOutput port, out bool isNew) {
			if(valueOutputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.ValueOutputs.Add(val);
				valueOutputs.Remove(port.id);
				isNew = false;
				return val;
			}
			else {
				node.ValueOutputs.Add(port);
				isNew = true;
				return port;
			}
		}

		public FlowInput RestorePort(FlowInput port) {
			if(flowInputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.FlowInputs.Add(val);
				flowInputs.Remove(port.id);
				return val;
			} else {
				node.FlowInputs.Add(port);
				return port;
			}
		}

		public FlowInput RestorePort(FlowInput port, out bool isNew) {
			if(flowInputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.FlowInputs.Add(val);
				flowInputs.Remove(port.id);
				isNew = false;
				return val;
			}
			else {
				node.FlowInputs.Add(port);
				isNew = true;
				return port;
			}
		}

		public FlowOutput RestorePort(FlowOutput port) {
			if(flowOutputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.FlowOutputs.Add(val);
				flowOutputs.Remove(port.id);
				return val;
			} else {
				node.FlowOutputs.Add(port);
				return port;
			}
		}

		public FlowOutput RestorePort(FlowOutput port, out bool isNew) {
			if(flowOutputs.TryGetValue(port.id, out var val)) {
				val.Restore(port);
				node.FlowOutputs.Add(val);
				flowOutputs.Remove(port.id);
				isNew = false;
				return val;
			}
			else {
				node.FlowOutputs.Add(port);
				isNew = true;
				return port;
			}
		}
	}
}