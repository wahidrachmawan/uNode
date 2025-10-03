﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[System.Serializable]
	public class InterfaceModifier : AccessModifier {

	}

	[Serializable]
	public sealed class SerializedValue : ISerializationCallbackReceiver, IGetValue {
		[SerializeField]
		private OdinSerializedData serializedData;
		[SerializeField]
		public SerializedType serializedType;
		[NonSerialized]
		private bool failedDeserialize;

		public SerializedValue() {

		}

		public SerializedValue(object value) {
			this.value = value;
			if(value != null) {
				serializedType = value.GetType();
			}
			else {
				serializedType = typeof(object);
			}
		}

		public SerializedValue(object value, Type type) {
			serializedType = type;
			this.value = value;
		}

		public static SerializedValue CreateFromType(Type type) {
			if(ReflectionUtils.CanCreateInstance(type)) {
				return new SerializedValue(ReflectionUtils.CreateInstance(type), type);
			}
			else {
				return new SerializedValue(null, type);
			}
		}

		public Type type {
			get {
				return serializedType?.type;
			}
			set {
				serializedType = value;
				if(this.value != null && !this.value.GetType().IsCastableTo(value)) {
					if(value.IsValueType && ReflectionUtils.CanCreateInstance(value)) {
						//If type is struct then, create a new instance because a struct cannot have null value.
						this.value = ReflectionUtils.CreateInstance(value);
					}
					else {
						//Change value to null in case type is not a struct.
						this.value = null;
					}
				}
			}
		}

		private object _value;
		public object value {
			get {
				if(_value is IGetValue) {
					return (_value as IGetValue).Get();
				}
				return _value;
			}
			set {
				_value = value;
				if(value != null) {
					if(value is Type) {
						serializedType = typeof(Type);
					}
					else {
						serializedType = value.GetType();
					}
				}
				failedDeserialize = false;
				Serialize();
			}
		}

		public object serializedValue => _value;

		internal OdinSerializedData SerializedData => serializedData;

		public void ChangeValue(object value) {
			_value = value;
			failedDeserialize = false;
			Serialize();
		}

		public void OnAfterDeserialize() {
			try {
				_value = SerializerUtility.Deserialize(serializedData);
			}
			catch {
				failedDeserialize = true;
				//Debug.LogException(new Exception($"Error on trying to deserialize value: {serializedType.typeName}", ex));
			}
		}

		private void Serialize() {
			if(failedDeserialize) return;
			if(_value is Type) {
                serializedData = SerializerUtility.SerializeValue(new SerializedType(_value as Type));
            }
			else {
                serializedData = SerializerUtility.SerializeValue(_value);
            }
		}

        object IGetValue.Get() {
			return value;
        }

		void ISerializationCallbackReceiver.OnBeforeSerialize() {

		}
	}

	public sealed class JumpStatement {
		public readonly NodeObject from;
		/// <summary>
		/// The jump statement type that's one of the following ( continue, break, or return )
		/// </summary>
		public readonly JumpStatementType jumpType;
		/// <summary>
		/// The value of a return jump statement
		/// </summary>
		public readonly object value;

		public JumpStatement(NodeObject from, JumpStatementType jumpType, object value = null) {
			this.jumpType = jumpType;
			this.from = from;
			this.value = value;
		}
	}

	[Serializable]
	public abstract class UReference : BaseGraphReference {
		protected UReference(IGraph graph) : base(graph) {
		}

		public abstract UGraphElement GetGraphElement();
		public abstract void SetGraphElement(UGraphElement element);

		public override object ReferenceValue => GetGraphElement();
	}

	public abstract class UReference<T> : UReference where T : UGraphElement {
		public override bool isValid => reference != null && _referece.IsValid;

		[NonSerialized]
		private T _referece;
		public T reference {
			get {
				//if(_referece?.graph != graph) {
				//	_referece = null;
				//}
				if((_referece == null || _referece.IsValid == false) && unityObject is IGraph g) {
					_referece = g.GetGraphElement<T>(id);
				}
				return _referece;
			}
		}

		public override UGraphElement GetGraphElement() => reference;

		public override void SetGraphElement(UGraphElement value) {
			if(value is not T) {
				throw new ArgumentException("The value is null or not type of: " + typeof(T));
			}
			_referece = value as T;
			_name = _referece.name;
			_id = _referece.id;
			unityObject = _referece.graphContainer as UnityEngine.Object;
		}

		public override string name {
			get {
				if(reference != null) {
					_name = _referece.name;
					return _referece.name;
				}
				return base.name;
			}
		}

		public UReference(T reference, IGraph graph) : base(graph) {
			_referece = reference;
			if(reference != null) {
				_name = reference.name;
				_id = reference.id;
			}
		}
	}

	[Serializable]
	public class VariableRef : UReference<Variable>, ISummary, IGraphValue {
		[SerializeField]
		private SerializedType _type = typeof(object);

		public VariableRef(Variable variable) : base(variable, variable.graphContainer) {
			_type = variable.type;
		}

		public Type type {
			get {
				return reference != null ? reference.type : _type.type;
			}
		}

		public object Get(Flow flow) {
			return reference.Get(flow);
		}

		public void Set(Flow flow, object value) {
			reference.Set(flow, value);
		}

		public string GetSummary() {
			return reference?.GetSummary();
		}
	}

	[Serializable]
	public class ParameterRef : UReference<UGraphElement>, IIcon {
		[SerializeField]
		private SerializedType _type = typeof(object);
		[SerializeField]
		private string _parameterID;

		public ParameterRef(UGraphElement parameterSystem, ParameterData parameterData) : base(parameterSystem, parameterSystem.graphContainer) {
			if(parameterSystem is not IParameterSystem)
				throw new InvalidOperationException();
			_parameterID = parameterData.id;
			_type = parameterData.type;
			this._parameter = parameterData;
		}

		[NonSerialized]
		private ParameterData _parameter;
		public ParameterData parameter {
			get {
				if(_parameter == null) {
					var system = GetGraphElement() as IParameterSystem;
					if(system != null) {
						_parameter = system.GetParameterByID(_parameterID);
					}
				}
				return _parameter;
			}
		}

		public override string name {
			get {
				if(parameter != null) {
					_name = _parameter.name;
					return _parameter.name;
				}
				return base.name;
			}
		}

		public Type type {
			get {
				return parameter != null ? parameter.type : _type.type;
			}
		}

		public override object ReferenceValue => parameter;

		public override void SetGraphElement(UGraphElement value) {
			base.SetGraphElement(value);
			if(_parameter != null) {
				var oldParam = _parameter;
				_parameter = null;
				if(value is IParameterSystem system) {
					_parameter = system.GetParameterByID(oldParam.id);
					if(_parameter == null) {
						_parameter = system.GetParameterByName(oldParam.name);
					}
					if(_parameter != null) {
						_parameterID = _parameter.id;
						_type = new(_parameter.type);
					}
				}
			}
		}

		public Type GetIcon() {
			return type;
		}
	}

	[Serializable]
	public class FunctionRef : UReference<Function>, ISummary {
		[SerializeField]
		private SerializedType _type = typeof(object);

		public FunctionRef(Function function) : base(function, function.graphContainer) {
			_type = function.ReturnType();
		}

		public Type type {
			get {
				return reference != null ? reference.ReturnType() : _type.type;
			}
		}

		public string GetSummary() {
			return reference?.comment;
		}

		public Type ReturnType() {
			return reference?.ReturnType();
		}
	}

	[Serializable]
	public class ConstructorRef : UReference<Constructor>, ISummary {
		[SerializeField]
		private SerializedType _type = typeof(object);

		public ConstructorRef(Constructor ctor) : base(ctor, ctor.graphContainer) {
			_type = ctor.ReturnType();
		}

		public Type type {
			get {
				return _type.type;
			}
		}

		public object Get() {
			return reference;
		}

		public string GetSummary() {
			return reference?.comment;
		}

		public Type ReturnType() {
			return reference?.ReturnType();
		}
	}

	[Serializable]
	public class UGraphElementRef : UReference<UGraphElement> {
		public T GetElement<T>() where T : UGraphElement {
			return reference as T;
		}

		public UGraphElementRef(UGraphElement graphObject) : base(graphObject, graphObject?.graphContainer) {
		}

		public static implicit operator UGraphElementRef(UGraphElement element) {
			if(element == null)
				return null;
			return new(element);
		}
	}

	[Serializable]
	public class UPortRef : UReference<UGraphElement> {
		public string portID;
		public PortKind kind;

		public UPortRef(UPort reference) : base(reference.node, reference.node.graphContainer) {
			portID = reference.id;
			if(reference is FlowInput) {
				kind = PortKind.FlowInput;
			}
			else if(reference is FlowOutput) {
				kind = PortKind.FlowOutput;
			}
			else if(reference is ValueInput) {
				kind = PortKind.ValueInput;
			}
			else if(reference is ValueOutput) {
				kind = PortKind.ValueOutput;
			}
		}

		public UPortRef(string portID, PortKind kind, NodeObject node) : base(node, node.graphContainer) {
			this.portID = portID;
			this.kind = kind;
		}

		public UPort GetPort() {
			switch(kind) {
				case PortKind.FlowInput:
					return (reference as NodeObject)?.GetFlowInput(portID);
				case PortKind.FlowOutput:
					return (reference as NodeObject)?.GetFlowOutput(portID);
				case PortKind.ValueInput:
					return (reference as NodeObject)?.GetValueInput(portID);
				case PortKind.ValueOutput:
					return (reference as NodeObject)?.GetValueOutput(portID);
			}
			throw null;
		}

		public static implicit operator UPortRef(UPort value) {
			return new UPortRef(value);
		}

		public override object ReferenceValue => GetPort();
	}

	[Serializable]
	public class PropertyRef : UReference<Property>, IGraphValue, ISummary {
		[SerializeField]
		private SerializedType _type = typeof(object);

		public PropertyRef(Property property) : base(property, property.graphContainer) {
			_type = property.type;
		}

		public Type type => ReturnType();

		public string GetSummary() {
			return reference?.GetSummary();
		}

		public Type ReturnType() {
			return reference?.ReturnType() ?? _type.type;
		}

		public bool CanGetValue() {
			return reference.CanGetValue();
		}

		public bool CanSetValue() {
			return reference.CanSetValue();
		}

		public object Get(Flow flow) {
			return reference.Get(flow);
		}

		public void Set(Flow flow, object value) {
			reference.Set(flow, value);
		}
	}

	public abstract class URoot<T> : UGraphElement where T : UGraphElement {
		public virtual IEnumerable<T> GetObjects(bool recursive = true) {
			return GetObjectsInChildren<T>(recursive);
		}

		public virtual IEnumerable<T> GetObjects(System.Predicate<T> predicate, bool recursive = true) {
			return GetObjectsInChildren<T>(predicate, recursive);
		}
	}

	public abstract class NodeContainer : URoot<NodeObject> {
		private VariableContainer _variableContainer;
		public VariableContainer variableContainer {
			get {
				if(_variableContainer == null) {
					_variableContainer = GetObjectInChildren<VariableContainer>();
					if(_variableContainer == null) {
						_variableContainer = AddChild(new VariableContainer());
					}
				}
				return _variableContainer;
			}
		}

		public virtual bool AllowCoroutine() {
			return false;
		}
	}

	public abstract class NodeContainerWithEntry : NodeContainer, IElementWithEntry {
		[NonSerialized]
		protected NodeObject entryObject;

		public virtual BaseEntryNode Entry {
			get {
				if(this == null) return null;
				if(entryObject == null || entryObject.node is not Nodes.FunctionEntryNode) {
					entryObject = this.GetNodeInChildren<Nodes.FunctionEntryNode>();
					if(entryObject == null) {
						AddChild(entryObject = new NodeObject(new Nodes.FunctionEntryNode()));
						entryObject.EnsureRegistered();
					}
				}
				return entryObject.node as BaseEntryNode;
			}
		}

		public virtual void RegisterEntry(BaseEntryNode node) { }
	}

	[Serializable]
	public sealed class VariableContainer : URoot<Variable> {
		private VariableCollections _collections;

		public VariableCollections collections {
			get {
				if(_collections == null) {
					_collections = new VariableCollections(this);
				}
				return _collections;
			}
		}

		protected override void OnChildrenChanged() {
			if(_collections != null) {
				_collections.SetDirty();
			}
			base.OnChildrenChanged();
		}

		public Variable AddVariable(string name, Type type, object value = null) {
			var variables = GetObjects(true);
			string fixName = name;
			int index = 0;
			while(variables.Any(v => v.name == fixName)) {
				fixName = name + (++index);
			}
			return AddChild(new Variable() {
				name = fixName,
				type = type,
				defaultValue = value ?? ReflectionUtils.CreateInstance(type),
				resetOnEnter = GetObjectInParent<ILocalVariableSystem>() != null,
			});
		}

		public Variable GetVariable(string name) {
			var variables = GetObjects(true);
			foreach(var v in variables) {
				if(v.name == name)
					return v;
			}
			return null;
		}
	}

	[Serializable]
	public sealed class FunctionContainer : URoot<Function> {
		private FunctionCollections _collections;

		public FunctionCollections collections {
			get {
				if(_collections == null) {
					_collections = new FunctionCollections(this);
				}
				return _collections;
			}
		}

		protected override void OnChildrenChanged() {
			if(_collections != null) {
				_collections.SetDirty();
			}
			base.OnChildrenChanged();
		}
	}

	[Serializable]
	public sealed class EventGraphContainer : URoot<NodeContainer> { }

	[Serializable]
	public sealed class MainGraphContainer : NodeContainer, IPrettyName, IIcon {
		public override bool AllowCoroutine() {
			var container = graphContainer;
			if(container is IMacroGraph || container is IStateGraph) {
				return true;
			}
			else if(container is ICustomMainGraph mainGraph) {
				return mainGraph.AllowCoroutine;
			}
			return false;
		}

		Type IIcon.GetIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public string GetPrettyName() {
			var container = graphContainer;
			if(container is IMacroGraph) {
				return "Macro Graph";
			}
			else if(container is IStateGraph) {
				return "Event Graph";
			}
			else if(container is ICustomMainGraph mainGraph) {
				return mainGraph.MainGraphTitle;
			}
			else {
				return "None";
			}
		}
	}

	[Serializable]
	public sealed class ConstructorContainer : URoot<Constructor> {
		private ConstructorCollections _collections;

		public ConstructorCollections collections {
			get {
				if(_collections == null) {
					_collections = new ConstructorCollections(this);
				}
				return _collections;
			}
		}

		protected override void OnChildrenChanged() {
			if(_collections != null) {
				_collections.SetDirty();
			}
			base.OnChildrenChanged();
		}
	}

	[Serializable]
	public sealed class PropertyContainer : URoot<Property> {
		private PropertyCollections _collections;

		public PropertyCollections collections {
			get {
				if(_collections == null) {
					_collections = new PropertyCollections(this);
				}
				return _collections;
			}
		}

		protected override void OnChildrenChanged() {
			if(_collections != null) {
				_collections.SetDirty();
			}
			base.OnChildrenChanged();
		}

		public Property NewProperty(string name, Type type) {
			var variables = GetObjectsInChildren<Property>(true);
			string fixName = name;
			int index = 0;
			while(variables.Any(v => v.name == fixName)) {
				fixName = name + (++index);
			}
			return AddChild(new Property() {
				name = fixName,
				type = type,
			});
		}
	}

	[Serializable]
	public abstract class UCollection<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection {
		[SerializeField]
		private List<T> items = new List<T>();

		protected List<T> Items {
			get {
				if(items == null)
					items = new();
				return items;
			}
		}

		internal List<T> Data {
			get => items;
			set => items = value;
		}

		public T this[int index] {
			get => items[index];
			set {
				if(index < 0 || index >= items.Count) {
					throw new ArgumentOutOfRangeException();
				}
				SetItem(index, value);
			}
		}
		object IList.this[int index] {
			get => ((IList)items)[index];
			set {
				if(index < 0 || index >= items.Count) {
					throw new ArgumentOutOfRangeException();
				}
				SetItem(index, (T)value);
			}
		}

		public int Count => items.Count;

		bool IList.IsFixedSize => ((IList)items).IsFixedSize;

		bool IList.IsReadOnly => ((IList)items).IsReadOnly;

		bool ICollection.IsSynchronized => ((ICollection)items).IsSynchronized;

		object ICollection.SyncRoot => ((ICollection)items).SyncRoot;

		bool ICollection<T>.IsReadOnly => ((ICollection<T>)items).IsReadOnly;

		public void Add(T item) {
			int count = items.Count;
			InsertItem(count, item);
		}

		int IList.Add(object value) {
			try {
				Add((T)value);
			}
			catch(InvalidCastException) {
				throw new ArgumentException("Wrong Type", nameof(value));
			}
			return Count - 1;
		}

		public void Clear() {
			ClearItems();
		}

		public bool Contains(T item) {
			return items.Contains(item);
		}

		bool IList.Contains(object value) {
			return ((IList)items).Contains(value);
		}

		public void CopyTo(T[] array, int arrayIndex) {
			items.CopyTo(array, arrayIndex);
		}

		void ICollection.CopyTo(Array array, int index) {
			((ICollection)items).CopyTo(array, index);
		}

		public IEnumerator<T> GetEnumerator() {
			return items.GetEnumerator();
		}

		public int IndexOf(T item) {
			return items.IndexOf(item);
		}

		int IList.IndexOf(object value) {
			return ((IList)items).IndexOf(value);
		}

		public void Insert(int index, T item) {
			if(index < 0 || index > items.Count) {
				throw new ArgumentOutOfRangeException();
			}
			InsertItem(index, item);
		}

		void IList.Insert(int index, object value) {
			Insert(index, (T)value);
		}

		public bool Remove(T item) {
			int num = items.IndexOf(item);
			if(num < 0) {
				return false;
			}
			RemoveItem(num);
			return true;
		}

		void IList.Remove(object value) {
			if(IsCompatibleObject(value)) {
				Remove((T)value);
			}
		}

		public void RemoveAt(int index) {
			if(index < 0 || index >= items.Count) {
				throw new ArgumentOutOfRangeException();
			}
			RemoveItem(index);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return items.GetEnumerator();
		}

		private static bool IsCompatibleObject(object value) {
			if(!(value is T)) {
				if(value == null) {
					return default(T) == null;
				}
				return false;
			}
			return true;
		}


		#region Protecteds
		protected virtual void ClearItems() {
			items.Clear();
		}

		protected virtual void InsertItem(int index, T item) {
			items.Insert(index, item);
		}

		protected virtual void RemoveItem(int index) {
			items.RemoveAt(index);
		}

		protected virtual void SetItem(int index, T item) {
			items[index] = item;
		}
		#endregion
	}

	public abstract class UReadOnlyCollection<T> : IReadOnlyList<T> {
		[SerializeField]
		private List<T> m_items = new List<T>();
		protected List<T> Items {
			get {
				if(dirty) {
					dirty = false;
					m_items.Clear();
					BuildItems(m_items);
				}
				return m_items;
			}
		}
		protected bool dirty = true;

		protected abstract void BuildItems(List<T> items);

		public void SetDirty() {
			dirty = true;
		}

		public int Count => Items.Count;

		public T this[int index] => Items[index];

		public IEnumerator<T> GetEnumerator() {
			return Items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return Items.GetEnumerator();
		}
	}

	public sealed class VariableCollections : UReadOnlyCollection<Variable> {
		public readonly VariableContainer container;

		public VariableCollections(VariableContainer container) {
			this.container = container;
		}

		protected override void BuildItems(List<Variable> items) {
			items.AddRange(container.GetObjects());
		}
	}

	public sealed class PropertyCollections : UReadOnlyCollection<Property> {
		public readonly PropertyContainer container;

		public PropertyCollections(PropertyContainer container) {
			this.container = container;
		}

		protected override void BuildItems(List<Property> items) {
			items.AddRange(container.GetObjects());
		}
	}

	public sealed class FunctionCollections : UReadOnlyCollection<Function> {
		public readonly FunctionContainer container;

		public FunctionCollections(FunctionContainer container) {
			this.container = container;
		}

		protected override void BuildItems(List<Function> items) {
			items.AddRange(container.GetObjects());
		}
	}

	public sealed class ConstructorCollections : UReadOnlyCollection<Constructor> {
		public readonly ConstructorContainer container;

		public ConstructorCollections(ConstructorContainer container) {
			this.container = container;
		}

		protected override void BuildItems(List<Constructor> items) {
			items.AddRange(container.GetObjects());
		}
	}

	[Serializable]
	public sealed class NodePorts<T> : UCollection<T> where T : UPort {
		private NodeObject _node;
		public NodeObject node {
			get => _node;
			set {
				_node = value;
				for(int i = 0; i < Items.Count; i++) {
					Items[i].node = value;
				}
			}
		}

		public NodePorts(NodeObject node) {
			if(node == null)
				throw new ArgumentNullException(nameof(node));
			this.node = node;
		}

		protected override void RemoveItem(int index) {
			var port = Items[index];
			if(port != null) {
				port.node = null;
			}
			base.RemoveItem(index);
		}

		protected override void InsertItem(int index, T item) {
			if(item == null)
				throw new ArgumentNullException(nameof(item));
			item.node = node;
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, T item) {
			throw new Exception("Set item is forbidden, use Add or Insert instead.");
		}

		protected override void ClearItems() {
			foreach(var item in Items) {
				item.node = null;
			}
			base.ClearItems();
		}
	}

	public abstract class ValuePort : UPort {
		protected Type _type;
		[NonSerialized]
		internal Func<Type> dynamicType;
		[NonSerialized]
		private Func<bool> autoType;
		/// <summary>
		/// The port type
		/// </summary>
		public Type type {
			get {
				if(IsAutoType && connections.Count > 0) {
					for(int i = 0; i < connections.Count; i++) {
						if(connections[i].isValid) {
							if(connections[i].input != this) {
								if(connections[i].input.IsAutoType == false) {
									//var other = connections[i].input;
									//Type actualType = null;
									//if(other.filter != null) {
									//	actualType = other.filter.GetActualType();
									//}
									//if(actualType == null) {
									//	actualType = other.type;
									//}
									return connections[i].input.type;
								}
							}
							else {
								if(connections[i].output.IsAutoType == false) {
									return connections[i].output.type;
								}
							}
							break;
						}
					}
				}
				return _type ?? dynamicType?.Invoke() ?? typeof(object);
			}
			set {
				_type = value;
			}
		}
		[NonSerialized]
		public Func<bool> canGetValue;
		[NonSerialized]
		public Func<bool> canSetValue;
		[SerializeReference]
		public List<ValueConnection> connections = new List<ValueConnection>();

		public Type StaticType {
			get => _type;
			set => _type = value;
		}

		/// <summary>
		/// True if the port type is dynamic
		/// </summary>
		public bool IsDynamicType => dynamicType != null || IsAutoType;
		/// <summary>
		/// If true, the <see cref="type"/> will be auto based on connected port, if there's no connection fallback to default.
		/// </summary>
		public bool IsAutoType => autoType?.Invoke() == true;

		/// <summary>
		/// Set the type of the port
		/// </summary>
		/// <param name="func"></param>
		public void SetType(Func<Type> func) {
			dynamicType = func;
			_type = null;
		}

		/// <summary>
		/// Set the auto type. If true, the <see cref="type"/> will be auto based on connected port, if there's no connection fallback to default.
		/// </summary>
		/// <param name="value"></param>
		public void SetAutoType(bool value) {
			static bool FUNC() => true;
			if(value) {
				autoType = FUNC;
			}
			else {
				autoType = null;
			}
		}

		/// <summary>
		/// Set the auto type. If true, the <see cref="type"/> will be auto based on connected port, if there's no connection fallback to default.
		/// </summary>
		/// <param name="func"></param>
		public void SetAutoType(Func<bool> func) {
			autoType = func;
		}

		protected ValuePort(NodeObject node) : base(node) {
		}

		public override IEnumerable<Connection> Connections => connections;

		public bool CanSetValue() => canSetValue != null && canSetValue();
		public bool CanGetValue() => canGetValue == null || canGetValue();

		public override void ClearConnections() {
			base.ClearConnections();
			if(connections.Count > 0) {
				for(int i = 0; i < connections.Count; i++) {
					if(connections[i] == null) {
						connections.RemoveAt(i);
						i--;
					}
				}
			}
		}
	}

	public abstract class FlowPort : UPort {
		[SerializeReference]
		public List<FlowConnection> connections = new List<FlowConnection>();

		protected FlowPort(NodeObject node) : base(node) { }

		public override IEnumerable<Connection> Connections => connections;

		public override void ClearConnections() {
			base.ClearConnections();
			if(connections.Count > 0) {
				for(int i = 0; i < connections.Count; i++) {
					if(connections[i] == null) {
						connections.RemoveAt(i);
						i--;
					}
				}
			}
		}
	}

	//[Serializable]
	//public class InvalidPort {
	//	public string id;
	//	public SerializedType portKind;
	//	public List<UndirrectConnection> connections;

	//	public InvalidPort(FlowPort port) {
	//		portKind = port.GetType();
	//		id = port.id;
	//		connections = new List<UndirrectConnection>();
	//		foreach(var c in port.Connections) {
	//			connections.Add(new UndirrectConnection(c));
	//		}
	//	}
	//}

	//public class PortRef : UReference<NodeObject> {
	//	public string id;
	//	public PortKind portKind;

	//	public PortRef(NodeObject reference, IGraph graph, UPort port) : base(reference, graph) {
	//		id = port.id;
	//		if(port is FlowInput) {
	//			portKind = PortKind.FlowInput;
	//		} else if(port is FlowOutput) {
	//			portKind = PortKind.FlowOutput;
	//		} else if(port is ValueInput) {
	//			portKind = PortKind.ValueInput;
	//		} else if(port is ValueOutput) {
	//			portKind = PortKind.ValueOutput;
	//		} else {
	//			throw new InvalidOperationException();
	//		}
	//	}
	//}

	//public class UndirrectConnection {
	//	public PortRef input;
	//	public PortRef output;

	//	public UndirrectConnection(Connection connection) {

	//	}
	//}


	//[Serializable]
	//public abstract class UObjectRef : IObjectReference {
	//	public abstract object ReferenceValue { get; }
	//}

	//[Serializable]
	//public class UObjectReference : UObjectRef {
	//	[SerializeReference]
	//	public object value;

	//	public override object ReferenceValue => value;
	//}

	//[Serializable]
	//public class UObjectReference<T> : UObjectRef {
	//	public T value;

	//	public override object ReferenceValue => value;
	//}

	//[Serializable]
	//public class UObjectUnityReference : UObjectRef {
	//	[Serializable]
	//	public class Data {
	//		public UnityEngine.Object value;
	//	}
	//	public Data data;

	//	public override object ReferenceValue => data;

	//	public UObjectUnityReference(UnityEngine.Object obj) {
	//		data = new Data() { value = obj };
	//	}
	//}

	[Serializable]
	public sealed class USerializedValueWrapper {
		[SerializeReference]
		public object value;
		public UnityEngine.Object unityObject;

		public USerializedValueWrapper(object value) {
			if(value is UnityEngine.Object obj) {
				unityObject = obj;
			}
			else {
				this.value = value;
			}
		}
	}
}