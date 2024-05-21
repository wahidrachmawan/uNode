using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace MaxyGames.UNode {
	public static class DefaultInstance<T> where T : new() {
		private static T _default;
		public static T Default {
			get {
				if(_default == null)
					_default = new T();
				return _default;
			}
		}
	}

	[Serializable]
	public class ScriptInformation {
		public string id;
		public string ghostID;
		public int ownerID;
		public int startLine;
		public int startColumn;
		public int endLine;
		public int endColumn;

		public int lineRange => endLine - startLine;
		public int columnRange {
			get {
				if(startLine == endLine) {
					return endColumn - startColumn;
				}
				else {
					return (lineRange * 1000) + (endColumn - startColumn);
				}
			}
		}
	}

	public abstract class ErrorAnalyzer {
		public bool CheckIsConvertible(ValueInput input) {
			return CheckIsConvertible(input, input.type);
		}

		public bool CheckIsConvertible(ValueInput input, Type type) {
			if(input.isAssigned) {
				if(input.UseDefaultValue) {

				}
				else {
					var con = input.connections.FirstOrDefault();
					if(con != null && con.isValid) {
						if(input.filter != null) {
							if(!input.filter.IsValidType(con.output.type)) {
								RegisterError(input.node, $"Cannot convert type `{con.output.type}` to `{type}`");
							}
						}
						else if(!con.output.type.IsCastableTo(con.input.type)) {
							RegisterError(input.node, $"Cannot convert type `{con.output.type}` to `{type}`");
						}
					}
				}
			}
			return false;
		}

		public virtual bool CheckValue(object value, string name, UGraphElement owner) {
			if(value == null) {
				RegisterError(owner, "Unassigned value: " + name);
				return true;
			}
			else if(value is SerializedType type) {
				if(type.type == null) {
					RegisterError(owner, "Unassigned value: " + name);
					return true;
				}
			}
			else if(value is MemberData member) {
				if(!member.isAssigned) {
					RegisterError(owner, "Unassigned value: " + name);
					return true;
				}
				else {
					if(member.targetType == MemberData.TargetType.uNodeVariable || member.targetType == MemberData.TargetType.uNodeLocalVariable) {
						var reference = member.startItem.GetReference<BaseGraphReference>();
						if(reference != null) {
							if(reference.ReferenceValue == null) {
								RegisterError(owner, name.Add(" is ") + "missing variable reference: " + reference.name + " with id: " + reference.id);
								return true;
							}
							else if(reference.graph != owner.graph) {
								RegisterError(owner, name.Add(" is ") + "invalid variable reference: " + reference.name + ", please re-assign it");
								return true;
							}
						}
					}
					else if(member.targetType == MemberData.TargetType.uNodeProperty) {
						var reference = member.startItem.GetReference<BaseGraphReference>();
						if(reference != null) {
							if(reference.ReferenceValue == null) {
								RegisterError(owner, name.Add(" is ") + "missing property reference: " + reference.name + " with id: " + reference.id);
								return true;
							}
							else if(reference.graph != owner.graph) {
								RegisterError(owner, name.Add(" is ") + "invalid property reference: " + reference.name + ", please re-assign it");
								return true;
							}
						}
					}
					else if(member.targetType == MemberData.TargetType.uNodeFunction) {
						var reference = member.startItem.GetReference<BaseGraphReference>();
						if(reference != null) {
							if(reference.ReferenceValue == null) {
								RegisterError(owner, name.Add(" is ") + "missing function reference: " + reference.name + " with id: " + reference.id);
								return true;
							}
							else if(reference.graph != owner.graph) {
								RegisterError(owner, name.Add(" is ") + "invalid function reference: " + reference.name + ", please re-assign it");
								return true;
							}
						}
					}
					else if(member.targetType == MemberData.TargetType.uNodeParameter) {
						var reference = member.startItem.GetReference<ParameterRef>();
						if(reference != null) {
							if(reference.ReferenceValue == null) {
								RegisterError(owner, name.Add(" is ") + "missing parameter reference: " + reference.name + " with id: " + reference.id);
								return true;
							}
							else if(reference.graph != owner.graph) {
								RegisterError(owner, name.Add(" is ") + "invalid parameter reference: " + reference.name + ", please re-assign it");
								return true;
							}
						}
					}
					else if(member.IsTargetingReflection) {
						foreach(var item in member.Items) {
							if(item.reference != null) {
								if(item.reference is VariableRef) {
									var reference = item.reference as VariableRef;
									if(reference.ReferenceValue == null) {
										string graphName;
										if(reference.UnityObject is IGraph) {
											graphName = (reference.UnityObject as IGraph).GetFullGraphName();
										} 
										else if(reference.UnityObject != null) {
											graphName = reference.UnityObject.name;
										}
										else if(object.ReferenceEquals(reference.UnityObject, null) == false) {
											graphName = reference.UnityObject.GetHashCode().ToString();
										}
										else {
											graphName = "Null";
										}
										RegisterError(owner, $"graph: {graphName} is missing variable: " + reference.name + " with id: " + reference.id);
										return true;
									}
								}
								else if(item.reference is PropertyRef) {
									var reference = item.reference as PropertyRef;
									if(reference.ReferenceValue == null) {
										string graphName;
										if(reference.UnityObject is IGraph) {
											graphName = (reference.UnityObject as IGraph).GetFullGraphName();
										}
										else if(reference.UnityObject != null) {
											graphName = reference.UnityObject.name;
										}
										else if(object.ReferenceEquals(reference.UnityObject, null) == false) {
											graphName = reference.UnityObject.GetHashCode().ToString();
										}
										else {
											graphName = "Null";
										}
										RegisterError(owner, $"graph: {graphName} is missing property: " + reference.name + " with id: " + reference.id);
										return true;
									}
								}
								else if(item.reference is FunctionRef) {
									var reference = item.reference as FunctionRef;
									if(reference.ReferenceValue == null) {
										string graphName;
										if(reference.UnityObject is IGraph) {
											graphName = (reference.UnityObject as IGraph).GetFullGraphName();
										}
										else if(reference.UnityObject != null) {
											graphName = reference.UnityObject.name;
										}
										else if(object.ReferenceEquals(reference.UnityObject, null) == false) {
											graphName = reference.UnityObject.GetHashCode().ToString();
										}
										else {
											graphName = "Null";
										}
										RegisterError(owner, $"graph: {graphName} is missing function: " + reference.name + " with id: " + reference.id);
										return true;
									}
								}
							}
						}
					}
				}
			}
			return false;
		}

		public bool CheckPort(params UPort[] ports) {
			if(ports == null)
				return false;
			bool flag = false;
			for(int i = 0; i < ports.Length; i++) {
				flag |= CheckPort(ports[i]);
			}
			return flag;
		}

		public bool CheckPort(IEnumerable<UPort> ports) {
			if(ports == null)
				return false;
			bool flag = false;
			foreach(var p in ports) {
				flag |= CheckPort(p);
			}
			return flag;
		}

		public virtual bool CheckPort(UPort port) {
			if(port == null || port.node == null) return false;
			if(port is ValueInput) {
				var p = port as ValueInput;
				if(p.UseDefaultValue) {
					if(p.defaultValue == null || !p.defaultValue.isAssigned) {
						RegisterError(port.node, new uNodeUtility.ErrorMessage() {
							message = "Unassigned port: " + port.GetPrettyName(),
						});
						return true;
					}
					else {
						var member = p.defaultValue;
						var inputType = member.type;
						var targetType = p.type;
						if(member.IsTargetingValue) {
							if(inputType != null && targetType != null) {
								if(inputType.IsCastableTo(targetType) == false) {
									//For auto fix incorrect literal value
									if(inputType.IsCastableTo(targetType, true)) {
										member.CopyFrom(MemberData.CreateFromValue(member.Get(null, targetType), targetType));
									}
									else {
										member.CopyFrom(MemberData.CreateFromValue(null, targetType));
									}
								}
							}
						}
						else {
							if(inputType.IsCastableTo(targetType) == false) {
								RegisterError(port.node, new uNodeUtility.ErrorMessage() {
									message = $"Cannot convert: {inputType.PrettyName(true)} to {targetType.PrettyName(true)}. Error from port: {port.GetPrettyName()}",
								});
							}
						}
						//if(!member.isStatic && member.targetType != MemberData.TargetType.Null &&
						//	member.targetType != MemberData.TargetType.Type &&
						//	member.targetType != MemberData.TargetType.Values && member.instance == null) {
						//	RegisterError(port.node, "Instance of " + port.name + " is unassigned/null");
						//	return true;
						//}
						//else if(!allowNull && !member.isStatic) {
						//	if(member.targetType == MemberData.TargetType.Null ||
						//		member.targetType == MemberData.TargetType.Values && member.Get() == null) {
						//		RegisterError(port.node, "Port: " + port.name + " value cannot be null");
						//		return true;
						//	}
						//}
					}
					if(CheckValue(p.defaultValue, port.GetPrettyName(), port.node)) {
						return true;
					}
				}
				else if(p.isConnected) {
					if(p.connections[0].isValid) {
						return CheckIsConvertible(p);
					}
					else if(!p.connections[0].isValid) {
						RegisterError(port.node, $"{p.GetPrettyName()} - port connection is missing or invalid.");
					}
				}
			}
			else if(port is ValueOutput) {
				var p = port as ValueOutput;
				if(!p.isConnected) {
					RegisterError(port.node, new uNodeUtility.ErrorMessage() {
						message = "Unassigned port: " + port.GetPrettyName(),
					});
					return true;
				}
				else {
					if(p.connections.All(c => !c.isValid)) {
						RegisterError(port.node, $"{p.GetPrettyName()} - port connection is missing or invalid.");
					}
				}
			}
			else if(port is FlowOutput) {
				var p = port as FlowOutput;
				if(!p.isConnected) {
					RegisterError(port.node, new uNodeUtility.ErrorMessage() {
						message = "Unassigned port: " + port.GetPrettyName(),
					});
					return true;
				}
				else {
					if(!p.connections[0].isValid) {
						RegisterError(port.node, $"{p.GetPrettyName()} - port connection is missing or invalid.");
					}
				}
			}
			else if(port is FlowInput) {
				var p = port as FlowInput;
				if(!p.isConnected) {
					RegisterError(port.node, new uNodeUtility.ErrorMessage() {
						message = "Unassigned port: " + port.GetPrettyName(),
					});
					return true;
				}
				else {
					if(p.connections.All(c => !c.isValid)) {
						RegisterError(port.node, $"{p.GetPrettyName()} - port connection is missing or invalid.");
					}
				}
			}
			return false;
		}

		public void RegisterError(UGraphElement owner, string message) {
			RegisterError(owner, new uNodeUtility.ErrorMessage() {
				message = message,
			});
		}

		public void RegisterError(UGraphElement owner, string message, Action autoFix) {
			RegisterError(owner, new uNodeUtility.ErrorMessage() {
				message = message,
				autoFix = (_) => autoFix?.Invoke(),
			});
		}

		public void RegisterError(UGraphElement owner, string message, Action<Vector2> autoFix) {
			RegisterError(owner, new uNodeUtility.ErrorMessage() {
				message = message,
				autoFix = autoFix,
			});
		}

		public void RegisterWarning(UGraphElement owner, string message) {
			RegisterWarning(owner, new uNodeUtility.ErrorMessage() {
				message = message,
			});
		}

		public void RegisterWarning(UGraphElement owner, string message, Action autoFix) {
			RegisterWarning(owner, new uNodeUtility.ErrorMessage() {
				message = message,
				autoFix = (_) => autoFix?.Invoke(),
			});
		}

		public void RegisterWarning(UGraphElement owner, string message, Action<Vector2> autoFix) {
			RegisterWarning(owner, new uNodeUtility.ErrorMessage() {
				message = message,
				autoFix = autoFix,
			});
		}

		public void RegisterWarning(UGraphElement owner, uNodeUtility.ErrorMessage error) {
			error.type = InfoType.Warning;
			RegisterError(owner, error);
		}

		public abstract void RegisterError(UGraphElement owner, uNodeUtility.ErrorMessage error);
		public abstract void ClearErrors(UGraphElement owner);
		public abstract void ClearErrors(IGraph graph);
		public abstract void ClearErrors();
		public abstract void CheckErrors(IGraph graph);
	}

	[System.Serializable]
	public class GraphException : Exception {
		public const string KEY_REFERENCE = "[GRAPH_REFERENCE:";
		public const char KEY_REFERENCE_SEPARATOR = '#';
		public const char KEY_REFERENCE_TAIL = ']';

		public UGraphElement graphReference;

		public override string StackTrace {
			get {
				if(graphReference == null) {
					return base.StackTrace;
				}
				return base.StackTrace.AddFirst(GetMessage(graphReference)).AddLineInFirst();
			}
		}

		public GraphException(UGraphElement graphReference) {
			this.graphReference = graphReference;
		}
		public GraphException(Exception inner, UGraphElement graphReference) : base("", inner) {
			this.graphReference = graphReference;
		}
		public GraphException(string message, UGraphElement graphReference) : base(message) {
			this.graphReference = graphReference;
		}
		public GraphException(string message, Exception inner, UGraphElement graphReference) : base(message, inner) {
			this.graphReference = graphReference;
		}
		protected GraphException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

		public override string ToString() {
			string msg;
			if(string.IsNullOrEmpty(Message) && InnerException != null) {
				msg = InnerException.ToString();
			}
			else {
				msg = base.ToString();
			}
			if(graphReference != null) {
				msg = msg.AddLineInEnd().Add(GetMessage(graphReference));
			}
			return msg;
		}

		public static string GetMessage(string message, UGraphElement element) {
			if(object.ReferenceEquals(element, null) || element.graphContainer == null)
				return message;
			return message.AddLineInEnd() + GetMessage(element);
		}

		public static string GetMessage(UGraphElement element) {
			if(object.ReferenceEquals(element, null) || element.graphContainer == null)
				return string.Empty;
			return KEY_REFERENCE + element.id + KEY_REFERENCE_SEPARATOR + GetGraphID(element.graphContainer) + KEY_REFERENCE_TAIL;
		}

		public static string GetMessage(string message, UGraphElement element, object debugObject) {
			if(object.ReferenceEquals(element, null) || element.graphContainer == null)
				return message;
			return message.AddLineInEnd() + GetMessage(element, debugObject);
		}

		public static string GetMessage(UGraphElement element, object debugObject) {
			if(object.ReferenceEquals(element, null) || element.graphContainer == null)
				return string.Empty;
			if(debugObject == null) {
				return GetMessage(element);
			}
			return KEY_REFERENCE + element.id + KEY_REFERENCE_SEPARATOR + GetGraphID(element.graphContainer) + KEY_REFERENCE_SEPARATOR + GraphDebug.GetDebugID(debugObject) + KEY_REFERENCE_TAIL;
		}

		public static string GetMessage(string message, int graphID, int elementID, object debugObject) {
			if(debugObject == null) {
				return message.AddLineInEnd() + KEY_REFERENCE + elementID + KEY_REFERENCE_SEPARATOR + graphID + KEY_REFERENCE_TAIL;
			}
			return message.AddLineInEnd() + KEY_REFERENCE + elementID + KEY_REFERENCE_SEPARATOR + graphID + KEY_REFERENCE_SEPARATOR + GraphDebug.GetDebugID(debugObject) + KEY_REFERENCE_TAIL;
		}

		private static string GetGraphID(IGraph graph) {
#if !UNITY_EDITOR
			var db = uNodeDatabase.instance;
			if(db != null) {
				var data = db.graphDatabases.FirstOrDefault(d => object.ReferenceEquals(d.asset, graph));
				if(data != null) {
					return data.assetGuid;
				}
			}
#endif
			return graph.GetHashCode().ToString();
		}
	}

	[Serializable]
	public class SerializedType : ISerializationCallbackReceiver, IValueReference, IGetValue {
		[SerializeField]
		private SerialiedTypeKind kind;

		[SerializeField]
		private string serializedString;
		[SerializeField]
		private byte[] serializedBytes;
		[SerializeField]
		public List<UnityEngine.Object> references;

		public SerializedType() {
			this.type = typeof(object);
		}

		public SerializedType(Type type) {
			this.type = type;
		}

		public bool isFilled {
			get {
				switch(kind) {
					case SerialiedTypeKind.Native:
						return !string.IsNullOrEmpty(serializedString);
					case SerialiedTypeKind.Runtime:
						return serializedBytes != null && serializedBytes.Length > 0;
				}
				return false;
			}
		}

		public bool isAssigned => type != null;
		public bool isOpenGeneric => type != null && type.IsGenericTypeDefinition;

		[NonSerialized]
		private Type _type;
		public Type type {
			get {
				switch(kind) {
					case SerialiedTypeKind.Native:
						if(_type == null) {
							_type = TypeSerializer.Deserialize(serializedString, false);
						}
						break;
					case SerialiedTypeKind.Runtime:
						if(_type == null || _type.Equals(null)) {
							var data = SerializerUtility.Deserialize<TypeData>(serializedBytes, references);
							_type = MemberDataUtility.GetParameterType(data, throwError: false);
						}
						break;
					case SerialiedTypeKind.None:
						return null;
				}
				return _type;
			}
			set {
				_type = value;
				if(value == null) {
					kind = SerialiedTypeKind.None;
				}
				else if(value is RuntimeType) {
					kind = SerialiedTypeKind.Runtime;
					serializedString = string.Empty;
					serializedBytes = SerializerUtility.Serialize(MemberDataUtility.GetTypeData(value), out references);
				}
				else {
					kind = SerialiedTypeKind.Native;
					if(value != null) {
						serializedString = value.FullName;
					}
					else {
						serializedString = string.Empty;
					}
					serializedBytes = new byte[0];
					references = null;
				}
			}
		}

		/// <summary>
		/// Use only in runtime when trying to get, set, or invoke members.
		/// </summary>
		public Type nativeType {
			get {
				//In case it is a `Runtime Native Type` then return the original c# type otherwise it will return the original type.
				return ReflectionUtils.GetNativeType(type);
			}
		}

		public SerialiedTypeKind typeKind => kind;

		public string typeName {
			get {
				switch(kind) {
					case SerialiedTypeKind.Native:
						return serializedString;
					case SerialiedTypeKind.Runtime:
						return MemberDataUtility.GetParameterName(SerializerUtility.Deserialize<TypeData>(serializedBytes, references), null);
				}
				return "None";
			}
		}

		public string prettyName {
			get {
				switch(kind) {
					case SerialiedTypeKind.Native:
						return type?.PrettyName() ?? serializedString;
					case SerialiedTypeKind.Runtime:
						return typeName;
				}
				return "None";
			}
		}

		public bool isNative => kind == SerialiedTypeKind.Native;

		public string GetRichName(bool withTypeOf = false) {
			return uNodeUtility.GetRichTypeName(prettyName, withTypeOf);
		}
		public static SerializedType Default => new SerializedType(typeof(object));
		public static SerializedType None => new SerializedType(null);

		void ISerializationCallbackReceiver.OnBeforeSerialize() {

		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() {
#if UNITY_EDITOR
			_type = null;
#endif
		}

		public override string ToString() {
			switch(kind) {
				case SerialiedTypeKind.None:
					return "None";
				case SerialiedTypeKind.Native:
					return serializedString;
				case SerialiedTypeKind.Runtime:
					return "Runtime Type";
			}
			return base.ToString();
		}

		object IGetValue.Get() {
			return type;
		}

		public static implicit operator SerializedType(Type type) {
			return new SerializedType(type);
		}

		public static implicit operator Type(SerializedType type) {
			if(type == null)
				return null;
			return type.type;
		}
	}

	public abstract class BaseReference {
		[SerializeField]
		protected int _id;
		public virtual int id {
			get {
				return _id;
			}
		}
		[SerializeField]
		protected string _name;
		public virtual string name {
			get {
				return _name;
			}
		}

		public abstract object ReferenceValue { get; }

		public static BaseReference FromValue(object value) {
			if(value == null)
				throw new ArgumentNullException(nameof(value));
			if(value is Node) {
				return new UGraphElementRef((value as Node).nodeObject);
			}
			else if(value is Variable variable) {
				return new VariableRef(variable, variable.graphContainer);
			}
			else if(value is Function function) {
				return new FunctionRef(function, function.graphContainer);
			}
			else if(value is Property property) {
				return new PropertyRef(property, property.graphContainer);
			}
			else if(value is Constructor ctor) {
				return new ConstructorRef(ctor, ctor.graphContainer);
			}
			else if(value is Graph graph) {
				return new GraphRef(graph.graphContainer);
			}
			else if(value is UGraphElement graphElement) {
				return new UGraphElementRef(graphElement);
			}
			else if(value is UnityEngine.Object unityObject) {
				return new UnityObjectReference(unityObject);
			}
			throw new Exception("Unsupported value: " + value.ToString());
		}
	}

	public abstract class BaseUnityObjectReference : BaseReference {
		[SerializeField]
		protected UnityEngine.Object unityObject;

		public abstract bool isValid { get; }

		public UnityEngine.Object UnityObject => unityObject;
		public override object ReferenceValue => unityObject;

		public BaseUnityObjectReference(UnityEngine.Object unityObject) {
			this.unityObject = unityObject;
		}
	}

	[Serializable]
	public class UnityObjectReference : BaseUnityObjectReference {
		public override bool isValid => unityObject != null;

		public UnityObjectReference(UnityEngine.Object unityObject) : base(unityObject) { }
	}

	public abstract class BaseGraphReference : BaseUnityObjectReference {
		public Graph graph {
			get {
				if(unityObject is IGraph g) {
					return g.GraphData;
				}
				return null;
			}
		}

		public override object ReferenceValue => graph;

		public BaseGraphReference(IGraph graph) : base(graph as UnityEngine.Object) { }
	}

	[Serializable]
	public class GraphRef : BaseGraphReference {
		public override bool isValid => UnityObject != null;
		public override object ReferenceValue => UnityObject;

		public override string name {
			get {
				if(object.ReferenceEquals(unityObject, null) == false && unityObject is IGraph graph) {
					return _name = graph.GetGraphName();
				}
				return base.name;
			}
		}

		public GraphRef(IGraph graph) : base(graph) {
			if(graph == null)
				throw new ArgumentNullException(nameof(graph));
			_name = graph.GraphData.name;
		}
	}

	[Serializable]
	public class NativeTypeRef : BaseUnityObjectReference {
		[SerializeField]
		private string _typeName;

		public string typeName {
			get {
				if(object.ReferenceEquals(unityObject, null) == false && unityObject is IReflectionType type) {
					_typeName = type.ReflectionType.FullName;
				}
				return _typeName;
			}
		}

		public override string name {
			get {
				if(object.ReferenceEquals(unityObject, null) == false && unityObject is IReflectionType type) {
					_typeName = type.ReflectionType.FullName;
					return _name = type.ReflectionType.Name;
				}
				return base.name;
			}
		}

		public override bool isValid => unityObject != null;

		private Type _type;
		public Type type {
			get {
				if(_type == null) {
					if(object.ReferenceEquals(unityObject, null) == false && unityObject is IReflectionType type) {
						_typeName = type.ReflectionType.FullName;
						_type = type.ReflectionType;
					}
					else {
						_type = _typeName.ToType(false);
						if(_type == null) {
							_type = object.ReferenceEquals(unityObject, null) ? RuntimeType.FromMissingType(unityObject, _typeName) : RuntimeType.FromMissingType(_typeName);
						}
					}
				}
				return _type;
			}
		}

		public override object ReferenceValue => type;

		public NativeTypeRef(IReflectionType type) : base(type as UnityEngine.Object) {
			_typeName = type.ReflectionType.FullName;
		}

		NativeTypeRef(UnityEngine.Object obj, string typeName) : base(obj) {
			_typeName = typeName;
		}

		public static NativeTypeRef FromMissingType(MissingGraphType type) {
			return new NativeTypeRef(type.graph, type.missingType);
		}
	}

	/// <summary>
	/// This class used to save type data
	/// </summary>
	public class TypeData {
		/// <summary>
		/// The Full Type Name or the name of referenced id.
		/// </summary>
		public string name = "";
		/// <summary>
		/// The reference
		/// </summary>
		public BaseReference reference;
		/// <summary>
		/// The list of type parameters
		/// </summary>
		public TypeData[] parameters;

		/// <summary>
		/// True if the type is native c# type
		/// </summary>
		public bool isNative {
			get {
				if(name != null && name.Length > 0) {
					if(name[0] == '#' || name[0] == '@' || name[0] == '$') {
						return false;
					}
				}
				return true;
			}
		}

		public TypeData() { }

		public TypeData(string name) {
			this.name = name;
		}

		public TypeData(string name, TypeData[] parameters) {
			this.name = name;
			this.parameters = parameters;
		}
	}

	/// <summary>
	/// Class for save type information.
	/// </summary>
	public class TypeInfo {
		public GenericParameterData parameterData;
		public System.Type type;

		public System.Type Type {
			get {
				if(parameterData != null) {
					return parameterData.value;
				}
				return type;
			}
		}

		public static implicit operator TypeInfo(System.Type type) {
			return new TypeInfo() {
				type = type,
			};
		}

		public static implicit operator System.Type(TypeInfo type) {
			return type.Type;
		}
	}

	/// <summary>
	/// This class used to save parameter data
	/// </summary>
	[System.Serializable]
	public class ParameterData {
		[SerializeField]
		private string _id = uNodeUtility.GenerateUID();
		public string id {
			get {
				if(_id == null) {
					_id = uNodeUtility.GenerateUID();
				}
				return _id;
			}
		}
		public string name;
		public SerializedType type;
		public RefKind refKind;

		public bool isByRef {
			get {
				return refKind != RefKind.None;
			}
		}
		public object value;

		public Type Type {
			get {
				return type.type;
			}
		}

		public ParameterData() {
			type = typeof(object);
		}

		public ParameterData(string name, System.Type type) {
			this.name = name;
			if(type.IsByRef) {
				refKind = RefKind.Ref;
				type = type.ElementType();
			}
			this.type = type;
		}

		public ParameterData(ParameterInfo info) {
			this.name = info.Name;
			this.type = info.ParameterType;
			if(info.ParameterType.IsByRef) {
				this.type = info.ParameterType.ElementType();
				if(info.IsOut) {
					refKind = RefKind.Out;
				}
				else if(info.IsIn) {
					refKind = RefKind.In;
				}
				else {
					refKind = RefKind.Ref;
				}
			}
		}
	}

	/// <summary>
	/// This class used to save generic parameter data
	/// </summary>
	[System.Serializable]
	public class GenericParameterData {
		public string name;
		public SerializedType typeConstraint;

		private Type _value;
		public Type value {
			get {
				if(_value == null) {
					if(typeConstraint != null) {
						return typeConstraint.type;
					}
					return typeof(System.Object);
				}
				return _value;
			}
			set {
				_value = value;
			}
		}

		public GenericParameterData() {
		}
		public GenericParameterData(string name) {
			this.name = name;
		}
	}

	/// <summary>
	/// This class used to save attribute data
	/// </summary>
	[System.Serializable]
	public class AttributeData {
		[Filter(typeof(Attribute), ArrayManipulator = false, AllowInterface = false)]
		public SerializedType attributeType = typeof(object);
		public ConstructorValueData constructor;

		public Type type => attributeType?.type;

		public object Get() {
			if(constructor != null && constructor.type == attributeType) {
				return constructor.Get();
			}
			return null;
		}

		public override string ToString() {
			if(attributeType != null && constructor != null) {
				string pInfo = null;
				if(constructor.parameters != null) {
					for(int i = 0; i < constructor.parameters.Length; i++) {
						if(i != 0) {
							pInfo += ", ";
						}
						pInfo += constructor.parameters[i] != null ? constructor.parameters[i].ToString() : "null";
					}
				}
				return attributeType + "(" + pInfo + ")";
			}
			return "null";
		}

		public AttributeData() { }

		public AttributeData(Type attributeType) {
			this.attributeType = attributeType;
			constructor = new ConstructorValueData(attributeType);
		}
	}

	/// <summary>
	/// This class used to save delegate data
	/// </summary>
	[System.Serializable]
	public class DelegateData {
		public string name;
		public EventModifier modifierData;
	}

	/// <summary>
	/// This class used to save class modifier
	/// </summary>
	[System.Serializable]
	public class ClassModifier : AccessModifier {
		public bool Abstract;
		public bool Static;
		public bool Sealed;
		public bool Partial;
		public bool ReadOnly;

		private string GenerateAccessModifier() {
			if(ReadOnly) {
				string result = "readonly ";
				if(Public) {
					result += "public ";
				}
				if(Protected) {
					result += "protected ";
				}
				if(Internal) {
					result += "internal ";
				}
				return result;
			}
			if(Public) {
				return "public ";
			}
			else if(Private) {
				return string.Empty;
			}
			else if(Internal) {
				if(Protected) {
					return "protected internal ";
				}
				return "internal ";
			}
			else if(Protected) {
				return "protected ";
			}
			return string.Empty;
		}

		public override string GenerateCode() {
			string data = GenerateAccessModifier();
			if(Abstract) {
				data += "abstract ";
			}
			if(Sealed) {
				data += "sealed ";
			}
			if(Static) {
				data += "static ";
			}
			if(Partial) {
				data += "partial ";
			}
			return data;
		}
	}

	/// <summary>
	/// This class used to save field modifier
	/// </summary>
	[System.Serializable]
	public class FieldModifier : AccessModifier {
		[Hide("Const", true)]
		public bool Static;
		[Hide("Const", true)]
		public bool Event;
		[Hide("Const", true)]
		public bool ReadOnly;
		[Hide("ReadOnly", true)]
		[Hide("Event", true)]
		[Hide("Static", true)]
		public bool Const;

		public override string GenerateCode() {
			string data = base.GenerateCode();
			if(Const) {
				data += "const ";
			}
			else {
				if(Static) {
					data += "static ";
				}
				if(Event) {
					data += "event ";
				}
				if(ReadOnly) {
					data += "readonly ";
				}
			}
			return data;
		}

		public static FieldModifier PublicModifier => new FieldModifier();

		public static FieldModifier PrivateModifier {
			get {
				return new FieldModifier() {
					Private = true,
					Public = false,
				};
			}
		}

		public static FieldModifier InternalModifier {
			get {
				return new FieldModifier() {
					Private = false,
					Public = false,
					Internal = true,
				};
			}
		}

		public static FieldModifier ProtectedModifier {
			get {
				return new FieldModifier() {
					Private = false,
					Public = false,
					Internal = false,
					Protected = true,
				};
			}
		}

		public static FieldModifier ProtectedInternalModifier {
			get {
				return new FieldModifier() {
					Private = false,
					Public = false,
					Internal = true,
					Protected = true,
				};
			}
		}
	}

	/// <summary>
	/// This class used to save property modifier
	/// </summary>
	[System.Serializable]
	public class PropertyModifier : AccessModifier {
		[Hide("Static", true)]
		[Hide("Virtual", true)]
		public bool Abstract;
		[Hide("Abstract", true)]
		[Hide("Virtual", true)]
		public bool Static;
		[Hide("Static", true)]
		[Hide("Abstract", true)]
		public bool Virtual;
		public bool Override;

		public override string GenerateCode() {
			string data = base.GenerateCode();
			if(Static) {
				data += "static ";
			}
			if(Abstract) {
				data += "abstract ";
			}
			else if(Virtual) {
				data += "virtual ";
			}
			else if(Override) {
				data += "override ";
			}
			return data;
		}
	}

	/// <summary>
	/// This class used to save indexer modifier
	/// </summary>
	[System.Serializable]
	public class IndexerModifier : AccessModifier {

	}

	/// <summary>
	/// This class used to save function modifier
	/// </summary>
	[System.Serializable]
	public class FunctionModifier : AccessModifier {
		public bool Static;
		public bool Unsafe;
		public bool Virtual;
		public bool Abstract;
		public bool Extern;
		public bool New;
		public bool Override;
		public bool Partial;
		public bool Sealed;
		public bool Async;

		public void SetVirtual() {
			Virtual = true;
			Static = false;
			Abstract = false;
			Override = false;
		}

		public void SetOverride() {
			Virtual = false;
			Static = false;
			Abstract = false;
			Override = true;
		}

		public void SetStatic() {
			Virtual = false;
			Static = true;
			Abstract = false;
			Override = false;
		}

		public override string GenerateCode() {
			string data = base.GenerateCode();
			if(Static) {
				data += "static ";
				if(Extern) {
					data += " extern ";
				}
			}
			else if(Unsafe) {
				data += "unsave ";
			}
			else if(Virtual) {
				data += "virtual ";
			}
			else if(Abstract) {
				data += "abstract ";
			}
			else if(Override) {
				data += "override ";
			}
			else if(Async) {
				data += "async ";
			}
			else if(Partial && string.IsNullOrEmpty(data)) {
				data += "partial ";
			}
			if(Sealed) {
				data = data.Insert(0, "sealed ");
			}
			else if(New) {
				data = data.Insert(0, "new ");
			}
			return data;
		}
	}

	/// <summary>
	/// This class used to save constructor modifier
	/// </summary>
	[System.Serializable]
	public class ConstructorModifier : AccessModifier {
		[Hide(nameof(Public), true)]
		[Hide(nameof(Internal), true)]
		[Hide(nameof(Protected), true)]
		public bool Static;

		public override string GenerateCode() {
			string data = base.GenerateCode();
			if(Static) {
				return "static ";
			}
			return data;
		}
	}

	/// <summary>
	/// This class used to save event modifier
	/// </summary>
	[System.Serializable]
	public class EventModifier : AccessModifier {
		public bool Abstract;

		[Hide("Unsafe", true)]
		[Hide("Virtual", true)]
		public bool Static;
		[Hide("Static", true)]
		[Hide("Virtual", true)]
		public bool Unsafe;
		[Hide("Static", true)]
		[Hide("Unsafe", true)]
		public bool Virtual;
	}

	/// <summary>
	/// This class used to save operator modifier
	/// </summary>
	[System.Serializable]
	public class OperatorModifier : AccessModifier {
		[Hide("Unsafe", true)]
		[Hide("Virtual", true)]
		public bool Static;
		[Hide("Static", true)]
		[Hide("Virtual", true)]
		public bool Unsafe;
		[Hide("Static", true)]
		[Hide("Unsafe", true)]
		public bool Virtual;

		public override string GenerateCode() {
			string data = base.GenerateCode().Add(" ");
			if(Static) {
				return data += "static";
			}
			else if(Unsafe) {
				return data += "unsave";
			}
			else if(Virtual) {
				return data += "virtual";
			}
			return data;
		}
	}

	/// <summary>
	/// This class used to save access modifier
	/// </summary>
	[System.Serializable]
	public class AccessModifier {
		[Hide("Private", true)]
		[Hide("Internal", true)]
		[Hide("Protected", true)]
		public bool Public = true;
		[Hide("Public", true)]
		[Hide("Internal", true)]
		[Hide("Protected", true)]
		public bool Private;
		[Hide("Public", true)]
		[Hide("Private", true)]
		public bool Protected;
		[Hide("Public", true)]
		[Hide("Private", true)]
		public bool Internal;

		public bool isPublic => Public;
		public bool isPrivate => Private || !Public && !Protected;
		public bool isProtected => Protected;

		public void SetPublic() {
			Public = true;
			Private = false;
			Protected = false;
			Internal = false;
		}

		public void SetPrivate() {
			Public = false;
			Private = true;
			Protected = false;
			Internal = false;
		}

		public void SetProtected() {
			Public = false;
			Private = false;
			Protected = true;
			Internal = false;
		}

		public void SetProtectedInternal() {
			Public = false;
			Private = false;
			Protected = true;
			Internal = true;
		}

		public virtual string GenerateCode() {
			if(Public) {
				return "public ";
			}
			else if(Private) {
				return "private ";
			}
			else if(Internal) {
				if(Protected) {
					return "protected internal ";
				}
				return "internal ";
			}
			else if(Protected) {
				return "protected ";
			}
			return "private ";
		}
	}

	/// <summary>
	/// This class used to save enum data
	/// </summary>
	[System.Serializable]
	public class EnumData {
		[System.Serializable]
		public class Element {
			public string name;
		}
		public string name;
		public Element[] enumeratorList = new Element[0];
		[Filter(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), Inherited = false, OnlyGetType = true, UnityReference = false)]
		public SerializedType inheritFrom = typeof(int);
		public EnumModifier modifiers = new EnumModifier();
	}

	[System.Serializable]
	public class EnumModifier : AccessModifier {

	}
}