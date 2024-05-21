using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MaxyGames.UNode {
	public class MemberData : IGraphValue, IValueReference {
		#region Classes
		public class ItemData {
			public string name;
			public BaseReference reference;

			public TypeData[] parameters;
			public TypeData[] genericArguments;

			public T GetReference<T>() where T : BaseReference {
				return reference as T;
			}

			public object GetReferenceValue() {
				return reference?.ReferenceValue;
			}

			public string GetActualName() {
				if(reference != null)
					return reference.name;
				return name;
			}

			public ItemData() { }

			public ItemData(string name) {
				this.name = name;
			}

			public ItemData(BaseReference reference) {
				name = reference.name;
				this.reference = reference;
			}
		}

		public class Event {
			public readonly EventInfo eventInfo;
			public readonly object instance;

			public Event(EventInfo eventInfo, object instance) {
				this.eventInfo = eventInfo;
				this.instance = instance;
			}
		}

		public delegate object EventCallback(object[] arg);

		public enum TargetType {
			//No target
			None = 0,
			//Null target
			Null = 1 << 0,
			//target is Serialized Values.
			Values = 1 << 1,
			//The target is a instance value.
			Self = 1 << 2,
			//field target - using reflection
			Field = 1 << 3,
			//property target - using reflection
			Property = 1 << 4,
			//method target - using reflection
			Method = 1 << 5,
			//constructor target - using reflection
			Constructor = 1 << 6,
			//event target - using reflection
			Event = 1 << 7,
			//target is a type
			Type = 1 << 8,
			//VariableData target (Using reflection when in deep variable otherwise use direct get and set)
			uNodeVariable = 1 << 9,
			//target is uNodeRoot LocalVariable.
			uNodeLocalVariable = 1 << 10,
			//target is Function in uNode (Runtime, Class, or Struct)
			uNodeFunction = 1 << 11,
			//target is Property in uNode (Runtime, Class, or Struct)
			uNodeProperty = 1 << 12,
			//target is Constructor in uNode (Runtime, Class, or Struct)
			uNodeConstructor = 1 << 13,
			//target is Indexer in uNode (Runtime, Class, or Struct)
			uNodeIndexer = 1 << 14,
			//target is Parameter in uNode (Function, Constructor, or Indexer)
			uNodeParameter = 1 << 15,
			//target is GenericParameter in uNode (Function, or Class)
			uNodeGenericParameter = 1 << 16,
			//target is uNode Type.
			uNodeType = 1 << 17,
			//target is runtime member (variable, property or method)
			RuntimeMember = 1 << 18,
			//Target is node ports ( only for editor )
			NodePort = 1 << 19,
		}

		public static class Utilities {
			private static void InitializeArguments(MemberData member, out Type[][] parameters, out Type[][] genericParameters, bool throwError = true) {
				if(member.Items != null) {
					Type[][] paramTypes = new Type[member.Items.Length][];
					Type[][] genericTypes = new Type[member.Items.Length][];
					for(int i = 0; i < member.Items.Length; i++) {
						ItemData iData = member.Items[i];
						if(iData != null) {
							try {
								Type[] paramsType;
								Type[] genericType;
								MemberDataUtility.DeserializeMemberItem(member.Items[i],
									out genericType,
									out paramsType,
									throwError);
								paramTypes[i] = paramsType;
								genericTypes[i] = genericType;
							}
							catch {
								if(uNodeUtility.isPlaying) {
									throw;
								}
								throw;
								// return null;
							}
						}
					}
					parameters = paramTypes;
					genericParameters = genericTypes;
					return;
				}
				parameters = null;
				genericParameters = null;
			}

			public static Type[][] GetParameterTypes(MemberData member) {
				if(member._parameterTypes == null) {
					InitializeArguments(member, out member._parameterTypes, out member._genericTypes);
				}
				return member._parameterTypes;
			}

			public static Type[][] GetGenericTypes(MemberData member) {
				if(member._genericTypes == null) {
					InitializeArguments(member, out member._parameterTypes, out member._genericTypes);
				}
				return member._genericTypes;
			}

			public static Type[][] SafeGetGenericTypes(MemberData member) {
				if(member._genericTypes == null && member.Items != null) {
					bool cacheValue = true;
					InitializeArguments(member, out var paramTypes, out var genericTypes, false);
					if(paramTypes != null) {
						for(int x = 0; x < paramTypes.Length; x++) {
							if(!cacheValue)
								break;
							if(paramTypes[x] != null) {
								var types = paramTypes[x];
								for(int y = 0; y < types.Length; y++) {
									if(types[y] is MissingType) {
										cacheValue = false;
										break;
									}
								}
							}
						}
					}
					if(genericTypes != null) {
						for(int x = 0; x < genericTypes.Length; x++) {
							if(!cacheValue)
								break;
							if(genericTypes[x] != null) {
								var types = genericTypes[x];
								for(int y = 0; y < types.Length; y++) {
									if(types[y] is MissingType) {
										cacheValue = false;
										break;
									}
								}
							}
						}
					}
					if(cacheValue) {
						member._parameterTypes = paramTypes;
						member._genericTypes = genericTypes;
					}
					else {
						return genericTypes;
					}
				}
				return member._genericTypes;
			}

			public static Type[][] SafeGetParameterTypes(MemberData member) {
				if(member._parameterTypes == null && member.Items != null) {
					bool cacheValue = true;
					InitializeArguments(member, out var paramTypes, out var genericTypes, false);
					if(paramTypes != null) {
						for(int x = 0; x < paramTypes.Length; x++) {
							if(!cacheValue)
								break;
							if(paramTypes[x] != null) {
								var types = paramTypes[x];
								for(int y = 0; y < types.Length; y++) {
									if(types[y] is MissingType) {
										cacheValue = false;
										break;
									}
								}
							}
						}
					}
					if(genericTypes != null) {
						for(int x = 0; x < genericTypes.Length; x++) {
							if(!cacheValue)
								break;
							if(genericTypes[x] != null) {
								var types = genericTypes[x];
								for(int y = 0; y < types.Length; y++) {
									if(types[y] is MissingType) {
										cacheValue = false;
										break;
									}
								}
							}
						}
					}
					if(cacheValue) {
						member._parameterTypes = paramTypes;
						member._genericTypes = genericTypes;
					}
					else {
						return paramTypes;
					}
				}
				return member._parameterTypes;
			}
		}
		#endregion

		#region Variables
		[SerializeField]
		private bool _isStatic;
		public bool isStatic { get => _isStatic; set => _isStatic = value; }

		[SerializeField]
		private TargetType _targetType;
		public TargetType targetType { get => _targetType; set => _targetType = value; }

		[SerializeField]
		private SerializedType startSerializedType;
		[SerializeField]
		private SerializedType targetSerializedType;

		public SerializedType StartSerializedType {
			get {
				if(startSerializedType == null) {
					startSerializedType = new SerializedType(typeof(object));
				}
				return startSerializedType;
			}
			set {
				startSerializedType = value;
			}
		}

		public SerializedType TargetSerializedType {
			get {
				if(targetSerializedType == null) {
					targetSerializedType = new SerializedType(typeof(object));
				}
				return targetSerializedType;
			}
			set {
				targetSerializedType = value;
			}
		}

		public ItemData startItem {
			get {
				var items = Items;
				if(items.Length > 0) {
					return items[0];
				}
				return null;
			}
		}

		[SerializeField]
		private ItemData[] _items;
		/// <summary>
		/// The Items currently have
		/// </summary>
		public ItemData[] Items {
			get {
				if(_items == null)
					_items = Array.Empty<ItemData>();
				return _items;
			}
			set {
				_items = value;
				ResetCache();
			}
		}

		[SerializeField]
		private SerializedValue _instance;

		public SerializedValue serializedInstance => _instance;

		/// <summary>
		/// Flags ( Public | Instance | Static | NonPublic | FlattenHierarchy)
		/// </summary>
		public static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
		#endregion

		#region Properties
		/// <summary>
		/// The object containing the member.
		/// </summary>
		public object instance {
			get {
				return _instance?.serializedValue;
			}
			set {
				_instance = new SerializedValue(value);
			}
		}

		/// <summary>
		/// True if the member required instance to perform an action
		/// </summary>
		/// <returns></returns>
		public bool IsRequiredInstance() {
			switch(targetType) {
				case TargetType.Constructor:
				case TargetType.None:
				case TargetType.Null:
				case TargetType.Type:
				case TargetType.uNodeConstructor:
				case TargetType.uNodeType:
					return false;
				case TargetType.Event:
				case TargetType.Field:
				case TargetType.Method:
				case TargetType.Property:
					return !isStatic;
			}
			return true;
		}

		/// <summary>
		/// Get preferred instance of this member.
		/// </summary>
		/// <returns></returns>
		public object GetInstance() {
			object obj = instance;
			if(obj is MemberData) {
				MemberData member = obj as MemberData;
				if(member.targetType == TargetType.Values || member.targetType == TargetType.Self) {
					return member.Get(null);
				}
			}
			else if(obj is IGetValue) {
				return (obj as IGetValue).Get();
			}
			else if(obj is IReference) {
				return (obj as IReference).ReferenceValue;
			}
			return obj;
		}

		/// <summary>
		/// The object that required to perform action, this return StartType if target is static member.
		/// if instance value is <see cref="IGetValue"/> it will try to call <see cref="IGetValue.Get"/> from the target instance.
		/// The change of this value will not be serialized.
		/// </summary>
		public object startTarget {
			get {
				if(!object.ReferenceEquals(_startTarget, null))
					return _startTarget;
				if(isStatic && targetType != TargetType.uNodeGenericParameter || targetType == TargetType.Constructor) {
					_startTarget = startType;
				}
				else {
					object obj = instance;
					if(obj != null) {
						if(obj is IGetValue) {
							obj = (obj as IGetValue).Get();
						}
						if(!IsTargetingUNode && targetType != TargetType.Self)
							obj = AutoConvertValue(obj, startType);
					}
					return obj;
				}
				return _startTarget;
			}
			set {
				_startTarget = value;
			}
		}

		/// <summary>
		/// The start name of this member
		/// </summary>
		public string startName {
			get {
				if(Items.Length > 0) {
					return Items[0].GetActualName();
				}
				else {
					return string.Empty;
				}
			}
			set {
				if(Items.Length > 0) {
					Items[0].name = value;
				}
				else {
					Items = new[]{
						new ItemData() {
							name = value,
						}
					};
				}
			}
		}

		/// <summary>
		/// The name of the member.
		/// </summary>
		public string name {
			get {
				if(string.IsNullOrEmpty(_name)) {
					if(Items.Length == 0) {
						return string.Empty;
					}
					else if(Items.Length == 1) {
						return _name = Items[0].GetActualName();
					}
					_name = string.Join(".", Items.Select(i => i.GetActualName()));
				}
				return _name;
			}
		}

		/// <summary>
		/// The Display name for this member
		/// </summary>
		/// <param name="longName"></param>
		/// <returns></returns>
		public string DisplayName(bool longName = false, bool typeTargetWithTypeof = true) {
			switch(targetType) {
				case TargetType.None:
					return "(none)";
				case TargetType.Self:
					return "this";
				case TargetType.Null:
					return "null";
				case TargetType.uNodeVariable:
				case TargetType.uNodeLocalVariable:
				case TargetType.uNodeProperty:
				case TargetType.uNodeParameter:
					if(longName)
						break;
					_name = null;
					return "$" + name;
				case TargetType.uNodeFunction:
				case TargetType.uNodeIndexer:
					if(longName)
						break;
					return startItem.GetActualName();
				case TargetType.uNodeConstructor:
					if(longName)
						break;
					return "new" + name;
				case TargetType.Values:
					if(type != null) {
						return "v: " + type.PrettyName();
					}
					return "Missing Type";
				case TargetType.Constructor:
					if(longName)
						break;
					if(type != null) {
						return "new " + type.PrettyName() + "()";
					}
					return !string.IsNullOrEmpty(name) ? name : "ctor";
				case TargetType.Method:
					if(longName)
						break;
					return !string.IsNullOrEmpty(name) ? name : "()";
			}
			if(string.IsNullOrEmpty(_displayName) && isTargeted && Items != null && Items.Length > 0) {
				string result = null;
				if(targetType == TargetType.Constructor) {
					result += "new " + type.PrettyName();
				}
				bool cache = true;
				for(int i = 0; i < Items.Length; i++) {
					if(i != 0 && (targetType != TargetType.Constructor)) {
						result += ".";
					}
					if(targetType != TargetType.uNodeGenericParameter && targetType != TargetType.Type && targetType != TargetType.Constructor) {
						if(i == 0) {
							switch(targetType) {
								case TargetType.uNodeVariable:
								case TargetType.uNodeLocalVariable:
								case TargetType.uNodeProperty:
								case TargetType.uNodeParameter:
									result += "$" + Items[i].GetActualName();
									cache = false;
									break;
								default:
									if(startType is RuntimeType runtimeType) {
										startName = runtimeType.Name;
										result += runtimeType.PrettyName();
										cache = false;
									}
									else {
										result += Items[i].GetActualName();
									}
									break;
							}
						}
						else {
							result += Items[i].GetActualName();
						}
					}
					ItemData iData = Items[i];
					if(iData != null) {
						string[] paramsType;
						string[] genericType;
						MemberDataUtility.GetItemName(Items[i],
							out genericType,
							out paramsType);
						if(genericType.Length > 0) {
							for(int x = 0; x < genericType.Length; x++) {
								genericType[x] = genericType[x].PrettyName();
							}
							if(targetType != TargetType.uNodeGenericParameter && targetType != TargetType.Type) {
								result += string.Format("<{0}>", string.Join(", ", genericType));
							}
							else {
								result += string.Format("{0}", string.Join(", ", genericType));
								if(Items[i].GetActualName().Contains("[")) {
									bool valid = false;
									for(int x = 0; x < Items[i].GetActualName().Length; x++) {
										if(!valid) {
											if(Items[i].GetActualName()[x] == '[') {
												valid = true;
											}
										}
										if(valid) {
											result += Items[i].GetActualName()[x];
										}
									}
								}
							}
						}
						if(paramsType.Length > 0 ||
							targetType == TargetType.uNodeFunction ||
							targetType == TargetType.uNodeConstructor ||
							targetType == TargetType.Constructor ||
							targetType == TargetType.Method && !isDeepTarget) {
							for(int x = 0; x < paramsType.Length; x++) {
								paramsType[x] = paramsType[x].PrettyName();
							}
							result += string.Format("({0})", string.Join(", ", paramsType));
						}
					}
				}
				if(cache == false) {
					if(targetType.IsTargetingType()) {
						if(!typeTargetWithTypeof) {
							return result;
						}
						return "typeof(" + result + ")";
					}
					return result;
				}
				_displayName = result;
			}
			if(!string.IsNullOrEmpty(_displayName)) {
				if(targetType.IsTargetingType()) {
					if(!typeTargetWithTypeof) {
						return _displayName;
					}
					return "typeof(" + _displayName + ")";
				}
				return _displayName;
			}
			if(targetType.IsTargetingType()) {
				if(!typeTargetWithTypeof) {
					return startType.PrettyName();
				}
				return "typeof(" + startType.PrettyName() + ")";
			}
			return !string.IsNullOrEmpty(name) ? name : "(none)";
		}

		public string Tooltip {
			get {
				if(isAssigned) {
					switch(targetType) {
						case TargetType.None:
						case TargetType.Type:
							return name +
								"\nTarget	: " + targetType.ToString() +
								"\nType	: " + typeof(System.Type).PrettyName(true);
						default:
							Type t = type;
							if(t == null) {
								return DisplayName(true) +
									"\nTarget	: " + targetType.ToString() +
									"\nType	: " + "Missing Type";
							}
							return DisplayName(true) +
								"\nTarget	: " + targetType.ToString() +
								"\nType	: " + type.PrettyName(true);
					}
				}
				return "Unassigned";
			}
		}

		/// <summary>
		/// Indicates whether the reflection has been found and cached.
		/// </summary>
		public bool isReflected { get; private set; }
		public bool isDeepTarget {
			get {
				if(IsTargetingUNode) {
					return Items.Length > 1;
				}
				return Items.Length > 2;
			}
		}

		/// <summary>
		/// Indicates whether the member has been targeted.
		/// </summary>
		public bool isTargeted {
			get {
				switch(targetType) {
					case TargetType.None:
						return false;
					case TargetType.uNodeGenericParameter:
					case TargetType.Type:
					case TargetType.Null:
					case TargetType.Values:
						return true;
					case TargetType.Self:
						return instance != null;
					default:
						return !string.IsNullOrEmpty(name);
				}
			}
		}

		/// <summary>
		/// Indicates whether the member has been properly assigned.
		/// </summary>
		public bool isAssigned {
			get {
				switch(targetType) {
					case TargetType.None:
						return false;
					case TargetType.Null:
					case TargetType.Values:
						return true;
					case TargetType.Self:
						return instance != null;
					case TargetType.uNodeType:
						return startType != null;
					case TargetType.uNodeGenericParameter:
					case TargetType.Type:
					case TargetType.Constructor:
					default:
						return !string.IsNullOrEmpty(name);
				}
			}
		}

		/// <summary>
		/// Indicate is targeting uNode Member whether it's graph, pin, or nodes.
		/// </summary>
		public bool IsTargetingUNode => targetType.IsTargetingUNode();

		/// <summary>
		/// True if targetType is uNodeVariable or uNodeLocalVariable
		/// </summary>
		/// <value></value>
		public bool IsTargetingVariable => targetType.IsTargetingVariable();

		/// <summary>
		/// The target is targeting graph that's return a value except targeting pin or nodes.
		/// </summary>
		/// <returns></returns>
		public bool IsTargetingGraph => targetType.IsTargetingGraphValue();

		/// <summary>
		/// True if targetType is Type or uNodeType
		/// </summary>
		/// <value></value>
		public bool IsTargetingType => targetType.IsTargetingType();

		/// <summary>
		///  if targetType is Values, SelfTarget, or Null
		/// </summary>
		public bool IsTargetingValue => targetType.IsTargetingValue();

		/// <summary>
		/// True if targetType is Constructor, Event, Field, Property, or Method
		/// </summary>
		/// <returns></returns>
		public bool IsTargetingReflection => targetType.IsTargetingReflection();

		/// <summary>
		/// The all parameters type this member have, null if does not.
		/// </summary>
		public Type[][] ParameterTypes {
			get {
				return Utilities.GetParameterTypes(this);
			}
		}

		/// <summary>
		/// The all generic type this member have, null if does not.
		/// </summary>
		public Type[][] GenericTypes {
			get {
				return Utilities.GetGenericTypes(this);
			}
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Create new member
		/// </summary>
		public MemberData() { }

		/// <summary>
		/// Create new member
		/// </summary>
		/// <param name="members"></param>
		public MemberData(IList<MemberInfo> members) {
			if(members == null) {
				throw new ArgumentNullException("members");
			}
			if(!(members[0] is Type)) {
				MemberInfo[] memberInfos = new MemberInfo[members.Count + 1];
				for(int i = 0; i < members.Count; i++) {
					memberInfos[i + 1] = members[i];
				}
				memberInfos[0] = members[0].DeclaringType;
				members = memberInfos;
			}
			var lastMember = members[members.Count - 1];
			isStatic = ReflectionUtils.GetMemberIsStatic(lastMember);
			List<ItemData> itemDatas = new List<ItemData>();
			foreach(var m in members) {
				if(m == null)
					throw new NullReferenceException("There's some null members");
				itemDatas.Add(MemberDataUtility.CreateItemData(m));
			}
			type = ReflectionUtils.GetMemberType(lastMember);
			startType = ReflectionUtils.GetMemberType(members[0]);
			if(lastMember is FieldInfo) {
				targetType = TargetType.Field;
			}
			else if(lastMember is PropertyInfo) {
				targetType = TargetType.Property;
			}
			else if(lastMember is MethodInfo) {
				targetType = TargetType.Method;
			}
			else if(lastMember is ConstructorInfo) {
				targetType = TargetType.Constructor;
				isStatic = true;
			}
			else if(lastMember is EventInfo) {
				targetType = TargetType.Event;
			}
			else if(lastMember is Type) {
				targetType = TargetType.Type;
				isStatic = true;
			}
			if(members[0] is RuntimeType) {
				startType = members[0] as Type;
			}
			if(!isStatic && members.Count > 1) {
				for(int i = 1; i < members.Count; i++) {
					if(i + 1 < members.Count && !(members[i] is Type)) {
						isStatic = ReflectionUtils.GetMemberIsStatic(members[i]);
						break;
					}
				}
			}
			Items = itemDatas.ToArray();
		}

		/// <summary>
		/// Create new member
		/// </summary>
		/// <param name="memberInfo"></param>
		public MemberData(MemberInfo memberInfo) {
			if(memberInfo == null) {
				throw new ArgumentNullException("memberInfo");
			}
			if(memberInfo is Type) {
				Type type = memberInfo as Type;
				startType = type;
				targetType = TargetType.Type;
				isStatic = true;
				Items = new[] { MemberDataUtility.CreateItemData(memberInfo) };
				return;
			}
			else {
				if(memberInfo.MemberType == MemberTypes.Field) {
					FieldInfo field = memberInfo as FieldInfo;
					startType = field.DeclaringType;
					type = field.FieldType;
					isStatic = field.IsStatic;
					targetType = TargetType.Field;
				}
				else if(memberInfo.MemberType == MemberTypes.Property) {
					PropertyInfo property = memberInfo as PropertyInfo;
					startType = property.DeclaringType;
					type = property.PropertyType;
					if(property.GetGetMethod() != null) {
						isStatic = property.GetGetMethod().IsStatic;
					}
					else if(property.GetSetMethod() != null) {
						isStatic = property.GetSetMethod().IsStatic;
					}
					targetType = TargetType.Property;
				}
				else if(memberInfo.MemberType == MemberTypes.Method) {
					MethodInfo method = memberInfo as MethodInfo;
					startType = method.DeclaringType;
					type = method.ReturnType;
					isStatic = method.IsStatic;
					targetType = TargetType.Method;
				}
				else if(memberInfo.MemberType == MemberTypes.Event) {
					EventInfo eventInfo = memberInfo as EventInfo;
					startType = eventInfo.DeclaringType;
					type = eventInfo.EventHandlerType;
					isStatic = eventInfo.GetAddMethod().IsStatic;
					targetType = TargetType.Event;
				}
				else if(memberInfo.MemberType == MemberTypes.Constructor) {
					ConstructorInfo ctor = memberInfo as ConstructorInfo;
					startType = ctor.DeclaringType;
					type = ctor.DeclaringType;
					isStatic = true;
					targetType = TargetType.Constructor;
				}
				else {
					throw new Exception("Unsupported MemberType:" + memberInfo.MemberType.ToString());
				}
				Items = new[] {
					MemberDataUtility.CreateItemData(memberInfo.DeclaringType),
					MemberDataUtility.CreateItemData(memberInfo)
				};
			}
		}

		/// <summary>
		/// Create new member
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="instance"></param>
		/// <param name="targetType"></param>
		public MemberData(string name, Type type, object instance, TargetType targetType) {
			var strs = name.Split('.');
			Items = new ItemData[strs.Length];
			for(int i = 0; i < strs.Length; i++) {
				Items[i] = new ItemData(strs[i]);
			}
			this.targetType = targetType;
			startType = type;
			this.instance = instance;
		}

		/// <summary>
		/// Create new member
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		public MemberData(object value, TargetType targetType = TargetType.Values) {
			if(value != null) {
				this.targetType = targetType;
				if(targetType == TargetType.Values) {
					instance = value;
					startType = value.GetType();
					if(value is UnityEngine.Object) {
						if(value is Node) {
							startType = typeof(Node);
						}
						else if(value is GraphAsset) {
							startType = typeof(GraphAsset);
						}
					}
				}
				else if(targetType == TargetType.Self) {
					if(value != null) {
						startType = value.GetType();
					}
					//startName = "self";
					instance = value;
				}
				else if(targetType == TargetType.Type) {
					if(value is Type) {
						startName = (value as Type).PrettyName();
						startType = value as Type;
						isStatic = true;
					}
					else if(value is string) {
						var t = TypeSerializer.Deserialize(value as string);
						startName = t.PrettyName();
						startType = t;
						isStatic = true;
					}
					else if(value is MemberData) {
						object o = (value as MemberData).Get(null);
						if(o is Type) {
							startName = (o as Type).PrettyName();
							startType = (o as Type);
							isStatic = true;
						}
						else if(o is string) {
							Type t = TypeSerializer.Deserialize(o as string);
							startName = t.PrettyName();
							startType = t;
							isStatic = true;
						}
					}
					else if(value is SerializedType) {
						var type = value as SerializedType;
						startName = type.prettyName;
						startType = value as SerializedType;
						isStatic = true;
					}
					else {
						throw new Exception("Invalid value to create type member.\nThe value should be System.Type or string type\nType:" + value.GetType());
					}
				}
				else {
					throw new Exception("Target type must be Values, SelfTarget, or Type");
				}
			}
			else {
				startName = "null";
				this.targetType = targetType;
			}
		}

		/// <summary>
		/// Create new member
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="targetType"></param>
		/// <param name="reflectedType"></param>
		public MemberData(string name, Type type, TargetType targetType = TargetType.None, Type reflectedType = null) {
			this.targetType = targetType;
			switch(targetType) {
				case TargetType.Constructor:
					isStatic = true;
					break;
			}
			startType = type;
			if(reflectedType != null) {
				this.type = reflectedType;
			}
			var strs = name.Split('.');
			Items = new ItemData[strs.Length];
			for(int i = 0; i < strs.Length; i++) {
				Items[i] = new ItemData(strs[i]);
			}
		}

		/// <summary>
		/// Create new member from other member
		/// </summary>
		/// <param name="member"></param>
		public MemberData(MemberData member) {
			CopyFrom(member);
		}
		#endregion

		#region General Functions
		/// <summary>
		/// Reset this member.
		/// </summary>
		/// <param name="fullReset">if true instanced value will be reset too.</param>
		public void Reset(bool fullReset = false) {
			fieldInfo = null;
			propertyInfo = null;
			isStatic = false;
			if(fullReset) {
				instance = null;
			}
			isReflected = false;
			startSerializedType = SerializedType.Default;
			targetSerializedType = SerializedType.Default;
			Items = new ItemData[0];
			targetType = TargetType.None;
			if(targetType == TargetType.Self ||
				targetType == TargetType.Values) {
				instance = null;
			}
		}

		/// <summary>
		/// Get MemberInfo from this member, null if does not using reflection.
		/// </summary>
		/// <returns></returns>
		public MemberInfo[] GetMembers(bool throwOnFail = true) {
			if(_hasInitializeMembers)
				return memberInfo;
			switch(targetType) {
				case TargetType.uNodeVariable: {
					if(!isDeepTarget)
						return null;
					var reference = startItem.GetReference<VariableRef>();
					if(reference != null) {
						if(reference.reference == null) {
							if(throwOnFail) {
								throw new Exception("Variable with name: " + startName + " cannot be found.");
							}
							else {
								return null;
							}
						}
						memberInfo = ReflectionUtils.GetMemberInfo(reference.type, this, flags, throwOnFail);
					}
					else if(startTarget is ILocalVariableSystem) {
						goto case TargetType.uNodeLocalVariable;
					}
					break;
				}
				case TargetType.uNodeLocalVariable: {
					if(!isDeepTarget)
						return null;
					var reference = startItem.GetReference<VariableRef>();
					if(reference != null) {
						if(reference.reference == null) {
							if(throwOnFail) {
								throw new Exception("Loval Variable with name: " + startName + " cannot be found.");
							}
							else {
								return null;
							}
						}
						memberInfo = ReflectionUtils.GetMemberInfo(reference.type, this, flags, throwOnFail);
					}
					else {
						if(throwOnFail) {
							throw new Exception("Cannot get local variable system from target: " + startTarget);
						}
						else {
							return null;
						}
					}
					break;
				}
				case TargetType.uNodeProperty:
					if(isDeepTarget) {
						var reference = startItem.GetReference<PropertyRef>();
						if(reference != null) {
							if(reference.reference == null) {
								if(throwOnFail) {
									throw new Exception("Property with name: " + startName + " cannot be found.");
								}
								else {
									return null;
								}
							}
							memberInfo = ReflectionUtils.GetMemberInfo(
								reference.ReturnType(),
								this,
								flags,
								throwOnFail
							);
						}
						else {
							if(throwOnFail) {
								throw new Exception("Missing target property: " + startTarget);
							}
							else {
								return null;
							}
						}
					}
					else {
						return null;
					}
					break;
				case TargetType.uNodeParameter:
					if(isDeepTarget) {
						var param = startItem.GetReferenceValue() as ParameterData;
						if(param != null) {
							memberInfo = ReflectionUtils.GetMemberInfo(
								param.Type,
								this,
								flags,
								throwOnFail
							);
						}
						else {
							if(throwOnFail) {
								throw new Exception("Parameter with name: " + startItem.name + " cannot be found.");
							}
							else {
								return null;
							}
						}
					}
					else {
						return null;
					}
					break;
				case TargetType.uNodeGenericParameter:
				case TargetType.Type:
				case TargetType.uNodeType:
				case TargetType.None:
				case TargetType.Null:
				case TargetType.Self:
				case TargetType.Values:
					return null;
				case TargetType.uNodeFunction: {
					var function = startItem.GetReferenceValue() as Function;
					if(function != null) {
						_hasRefOrOut = function.HasRefOrOut;
					}
					return null;
				}
				default:
					if(!uNodeUtility.isPlaying) {
						if(startType != null) {
							memberInfo = ReflectionUtils.GetMemberInfo(startType, this, flags, throwOnFail);
						}
						else {
							return null;
						}
						break;
					}
					memberInfo = ReflectionUtils.GetMemberInfo(startType, this, flags, throwOnFail);
					break;
			}
			if(memberInfo == null) {
				return null;
			}
			if(memberInfo.Length == 0) {
				//return memberInfo;
				if(!uNodeUtility.isPlaying) {
					return memberInfo;
				}
				throw new Exception(string.Format("No matching member found: '{0}.{1}'", startType.Name, name));
			}
			fieldInfo = memberInfo[memberInfo.Length - 1] as FieldInfo;
			propertyInfo = memberInfo[memberInfo.Length - 1] as PropertyInfo;
			methodInfo = memberInfo[memberInfo.Length - 1] as MethodInfo;
			constructorInfo = memberInfo[memberInfo.Length - 1] as ConstructorInfo;
			eventInfo = memberInfo[memberInfo.Length - 1] as EventInfo;
			_hasInitializeMembers = true;
			return memberInfo;
		}

		/// <summary>
		/// Are this member can get a value.
		/// </summary>
		/// <returns></returns>
		public bool CanGetValue() {
			if(isAssigned) {
				switch(targetType) {
					case TargetType.uNodeFunction:
					case TargetType.Method:
						return type != typeof(void);
					case TargetType.uNodeProperty:
						if(isDeepTarget) {
							return true;
						}
						else {
							var reference = startItem.GetReferenceValue() as Property;
							if(reference != null) {
								return reference.CanGetValue();
							}
							return true;
						}
					default:
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Are this member can set a value.
		/// </summary>
		/// <returns></returns>
		public bool CanSetValue() {
			if(isAssigned) {
				MemberInfo[] members;
				switch(targetType) {
					case TargetType.uNodeParameter:
						if(isDeepTarget) {
							members = GetMembers(false);
							if(members != null && members.Length > 0) {
								return ReflectionUtils.CanSetMemberValue(members[members.Length - 1]);
							}
							return false;
						}
						return true;
					case TargetType.Field:
					case TargetType.Event:
						return true;
					case TargetType.Property:
						members = GetMembers(false);
						if(members != null && members.Length > 0) {
							return ReflectionUtils.CanSetMemberValue(members[members.Length - 1]);
						}
						break;
					case TargetType.uNodeProperty:
						if(isDeepTarget) {
							members = GetMembers(false);
							if(members != null && members.Length > 0) {
								return ReflectionUtils.CanSetMemberValue(members[members.Length - 1]);
							}
						}
						else {
							var reference = startItem.GetReferenceValue() as Property;
							if(reference != null) {
								return reference.CanSetValue();
							}
							return false;
						}
						break;
					case TargetType.uNodeVariable:
					case TargetType.uNodeLocalVariable:
						if(isDeepTarget) {
							members = GetMembers(false);
							if(members != null && members.Length > 0) {
								return ReflectionUtils.CanSetMemberValue(members[members.Length - 1]);
							}
						}
						else
							return true;
						break;
				}
			}
			return false;
		}

		public RuntimeEvent CreateRuntimeEvent() {
			return new RuntimeEventValue(RuntimeType.Default, this);
		}

		public Property GetProperty() {
			switch(targetType) {
				case TargetType.uNodeProperty: {
					return startItem.GetReferenceValue() as Property;
				}
				default:
					return null;
			}
		}

		//public T GetGraphElement<T>() where T : UGraphElement {
		//	switch(targetType) {
		//		case TargetType.FlowPort:
		//		case TargetType.ValuePort:
		//			return (startTarget as GraphElementRef).GetElement<T>();
		//		default:
		//			return default;
		//	}
		//}

		/// <summary>
		/// Copy data from another member.
		/// </summary>
		/// <param name="member"></param>
		public void CopyFrom(MemberData member) {
			if(member == null) {
				Reset();
				return;
			}
			if(member.targetType == TargetType.None) {
				Reset();
				return;
			}
			else if(member.targetType == TargetType.Null) {
				Items = SerializerUtility.Duplicate(member.Items);
				startSerializedType = SerializedType.Default;
				targetSerializedType = SerializedType.Default;
				instance = null;
				isStatic = false;
				targetType = member.targetType;
				return;
			}
			targetType = member.targetType;
			instance = member.instance;
			isStatic = member.isStatic;
			StartSerializedType = member.StartSerializedType;
			targetSerializedType = member.targetSerializedType;
			Items = SerializerUtility.Duplicate(member.Items);
			ResetCache();
		}

		/// <summary>
		/// Copy data to another member.
		/// </summary>
		/// <param name="member"></param>
		public void CopyTo(MemberData member) {
			if(member != null)
				member.CopyFrom(this);
		}

		private void EnsureIntialized() {
			if(!uNodeUtility.isPlaying) {
				isReflected = false;
			}
			if(!isReflected) {
				switch(targetType) {
					case TargetType.uNodeVariable:
					case TargetType.uNodeLocalVariable:
					case TargetType.uNodeProperty:
						isReflected = true;
						if(isDeepTarget)
							GetMembers();
						break;
					case TargetType.uNodeParameter:
						if(startItem.GetReferenceValue() == null) {
							var reference = startItem.GetReference<ParameterRef>();
							throw new Exception($"Cannot find parameter: {reference.name} from object: {reference.GetGraphElement()}, with parameter ID: {reference.id}");
						}
						isReflected = true;
						if(isDeepTarget) {
							GetMembers();
							break;
						}
						return;
					case TargetType.uNodeGenericParameter:
						if(Items == null || Items.Length == 0) {
							if(startTarget is IGenericParameterSystem) {
								_genericParameterData = new GenericParameterData[1];
								if(name.Contains('[')) {
									_genericParameterData[0] = (startTarget as IGenericParameterSystem).GetGenericParameter(name.Replace("[]", ""));
								}
								else {
									_genericParameterData[0] = (startTarget as IGenericParameterSystem).GetGenericParameter(name);
								}
							}
						}
						isReflected = true;
						return;
					case TargetType.uNodeFunction:
						isReflected = true;
						break;
					case TargetType.Type:
					case TargetType.Values:
					case TargetType.None:
					case TargetType.Self:
					case TargetType.uNodeType:
						isReflected = true;
						return;
					default:
						GetMembers();
						break;
				}
			}
			if(!uNodeUtility.isPlaying) {
				isReflected = false;
			}
		}
		#endregion

		#region Get
		/// <summary>
		/// Retrieves the value of the member or call it.
		/// </summary>
		public object Get(Flow flow) {
			return Invoke(flow, null);
		}

		/// <summary>
		/// Retrieves the value of the member or call it.
		/// </summary>
		public object Get(Flow flow, Type convertType) {
			if(convertType != null) {
				object resultValue = Get(flow);
				if(resultValue != null) {
					if(resultValue.GetType() == convertType)
						return resultValue;
					return Operator.Convert(resultValue, convertType);
				}
				return resultValue;
			}
			return Invoke(flow, null);
		}

		/// <summary>
		/// Generic Wrapper to get value for class type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Get<T>(Flow graph) {
			//if(type == null) return null;
			object resultValue = Get(graph);
			if(!object.ReferenceEquals(resultValue, null)) {
				return Operator.Convert<T>(resultValue);
			}
			return default;
		}
		#endregion

		#region Invoke
		public object Invoke(Flow flow, object[] paramValues) {
			EnsureIntialized();
			AutoConvertParameters(ref paramValues);
			return DoInvoke(flow, paramValues);
		}

		private object DoReflect(Flow flow, object reflectionTarget, object[] paramValues) {
			if(methodInfo != null) {
				reflectionTarget = AutoConvertValue(reflectionTarget, methodInfo.DeclaringType);//Ensure the value is valid
				int paramsLength = methodInfo.GetParameters().Length;
				if(paramValues != null && paramsLength != paramValues.Length) {
					object[] obj = new object[paramsLength];
					for(int x = 0; x < paramsLength; x++) {
						obj[x] = paramValues[(paramValues.Length - paramsLength) + x];
					}
					if(_hasRefOrOut) {
						object retVal = methodInfo.InvokeOptimized(reflectionTarget, obj);
						for(int x = 0; x < paramsLength; x++) {
							paramValues[(paramValues.Length - paramsLength) + x] = obj[x];
						}
						return retVal;
					}
					return methodInfo.InvokeOptimized(reflectionTarget, obj);
				}
				else {
					return methodInfo.InvokeOptimized(reflectionTarget, paramValues);
				}
			}
			else if(constructorInfo != null) {
				int paramsLength = constructorInfo.GetParameters().Length;
				if(paramValues != null && paramsLength != paramValues.Length) {
					object[] obj = new object[paramsLength];
					for(int x = 0; x < paramsLength; x++) {
						obj[x] = paramValues[(paramValues.Length - paramsLength) + x];
					}
					if(_hasRefOrOut) {
						object retVal = constructorInfo.Invoke(obj);
						for(int x = 0; x < paramsLength; x++) {
							paramValues[(paramValues.Length - paramsLength) + x] = obj[x];
						}
						return retVal;
					}
					return constructorInfo.Invoke(obj);
				}
				else {
					if((paramValues == null || paramValues.Length == 0) && constructorInfo.DeclaringType.IsValueType) {
						return Activator.CreateInstance(constructorInfo.DeclaringType);
					}
					return constructorInfo.Invoke(paramValues);
				}
			}
			else if(fieldInfo != null) {
				reflectionTarget = AutoConvertValue(reflectionTarget, fieldInfo.ReflectedType);//Ensure the value is valid
				return fieldInfo.GetValueOptimized(reflectionTarget);
			}
			else if(propertyInfo != null) {
				reflectionTarget = AutoConvertValue(reflectionTarget, propertyInfo.ReflectedType);//Ensure the value is valid
				return propertyInfo.GetValueOptimized(reflectionTarget);
			}
			else {
				switch(targetType) {
					case TargetType.uNodeVariable:
					case TargetType.uNodeLocalVariable:
						return (startItem.GetReferenceValue() as Variable).Get(flow);
					case TargetType.Property:
						return (startItem.GetReferenceValue() as Property).Get(flow);
					default:
						throw new Exception(targetType.ToString());
				}
			}
		}

		private void DoReflectSet(object reflectionTarget, object value, object parentTarget) {
			if(fieldInfo != null) {
				if(type.IsValueType && memberInfo.Length > 1) {
					ReflectionUtils.SetBoxedMemberValue(parentTarget, memberInfo[memberInfo.Length - 2], reflectionTarget, fieldInfo, value);
				}
				else {
					fieldInfo.SetValueOptimized(reflectionTarget, value);
				}
				return;
			}
			if(propertyInfo != null) {
				if(type.IsValueType && memberInfo.Length > 1) {
					ReflectionUtils.SetBoxedMemberValue(parentTarget, memberInfo[memberInfo.Length - 2], reflectionTarget, propertyInfo, value);
				}
				else {
					propertyInfo.SetValueOptimized(reflectionTarget, value);
				}
				return;
			}
			else if(methodInfo != null) {
				throw new Exception("Method can't be set.");
			}
		}

		/// <summary>
		/// Invoke Member Method with parameter or get field/property
		/// </summary>
		/// <param name="paramValues"></param>
		/// <returns></returns>
		private object DoInvoke(Flow flow, object[] paramValues) {
			//object startObj = null;
			//if(instance == graph.graph) {
			//	startObj = graph.target;
			//}
			//else {
			//	startObj = startTarget;
			//}
			switch(targetType) {
				case TargetType.Null:
				case TargetType.None:
					return null;
				case TargetType.uNodeType:
					object startObj = startTarget;
					if(startObj is RuntimeType) {
						return startObj as RuntimeType;
					}
					else if(startObj is IReflectionType) {
						return (startObj as IReflectionType).ReflectionType;
					}
					return startType;
				case TargetType.uNodeConstructor:
					throw new Exception("uNodeConstructor doesn't support in runtime, its only for code generation.");
				case TargetType.uNodeLocalVariable:
				case TargetType.uNodeVariable:
					if(isDeepTarget) {
						return DoReflect(flow, ReflectionUtils.GetMemberTargetRef(memberInfo, (startItem.GetReferenceValue() as Variable).Get(flow), out _, paramValues), paramValues);
					}
					return (startItem.GetReferenceValue() as Variable).Get(flow);
				case TargetType.uNodeProperty:
					if(isDeepTarget) {
						return DoReflect(flow, ReflectionUtils.GetMemberTargetRef(memberInfo, (startItem.GetReferenceValue() as Property).Get(flow), out _, paramValues), paramValues);
					}
					return (startItem.GetReferenceValue() as Property).Get(flow);
				case TargetType.Type:
					return startType;
				case TargetType.Values: {
					var obj = instance;
					if(obj != null && (obj is UnityEngine.Object || obj.GetType().IsValueType || obj is string || obj is MemberData)) {
						return obj;
					}
					return SerializerUtility.Duplicate(obj);
				}
				case TargetType.Self:
					return flow?.target ?? startTarget;
				case TargetType.uNodeParameter:
					if(isDeepTarget) {
						return DoReflect(flow, ReflectionUtils.GetMemberTargetRef(memberInfo, flow.GetLocalData(null, startItem.GetReferenceValue()), out _, paramValues), paramValues);
					}
					return flow.GetLocalData(null, startItem.GetReferenceValue());
				case TargetType.uNodeGenericParameter:
					if(Items != null && Items.Length > 0) {
						ItemData iData = Items[0];
						if(iData != null) {
							Type[] genericTypes = GenericTypes[0];
							if(genericTypes.Length == 1) {
								if(name.Contains('[')) {
									int arrayCount = 0;
									foreach(var n in name) {
										if(n == '[') {
											arrayCount++;
										}
									}
									Type t = genericTypes[0];
									for(int i = 0; i < arrayCount; i++) {
										t = ReflectionUtils.MakeArrayType(t);
									}
									return t;
								}
								return genericTypes[0];
							}

						}
					}
					if(_genericParameterData != null) {
						//if(type != null && type.IsGenericTypeDefinition) {
						//	return type.MakeGenericType(_genericParameterData.Select(item => item.value).ToArray());
						//}
						if(_genericParameterData.Length == 1 && _genericParameterData[0] != null) {
							if(name.Contains('[')) {
								int arrayCount = 0;
								foreach(var n in name) {
									if(n == '[') {
										arrayCount++;
									}
								}
								Type t = _genericParameterData[0].value;
								for(int i = 0; i < arrayCount; i++) {
									t = ReflectionUtils.MakeArrayType(t);
								}
								return t;
							}
							return _genericParameterData[0].value;
						}
					}
					return type;
				case TargetType.uNodeFunction:
					return startItem.GetReference<FunctionRef>().reference.Invoke(flow, paramValues);
				case TargetType.Event:
					return new Event(eventInfo, ReflectionUtils.GetMemberTargetRef(memberInfo, startTarget, out _, paramValues));
				case TargetType.Constructor:
					if(constructorInfo != null) {
						goto default;
					}
					return Activator.CreateInstance(startType);
				case TargetType.NodePort:
					return startItem?.GetReferenceValue();
				default:
					return DoReflect(flow, ReflectionUtils.GetMemberTargetRef(memberInfo, startTarget, out _, paramValues), paramValues);
			}
		}
		#endregion

		#region AutoConvert
		private void AutoConvertValue(ref object value) {
			value = AutoConvertValue(value, type);
		}

		private void AutoConvertParameters(ref object[] values) {
			if(values == null || values.Length == 0 || ParameterTypes == null)
				return;
			int count = 0;
			for(int a = 0; a < ParameterTypes.Length; a++) {
				if(ParameterTypes[a] == null)
					continue;
				for(int b = 0; b < ParameterTypes[a].Length; b++) {
					Type t = ParameterTypes[a][b];
					if(t != null && values.Length > count) {
						object val = values[count];
						if(val == null)
							continue;
						values[count] = AutoConvertValue(val, t);
					}
					count++;
				}
			}
		}

		private static object AutoConvertValue(object value, Type type) {
			if(type == null)
				return value;
			if(type is RuntimeType && ReflectionUtils.IsNativeType(type)) {
				//For runtime native type, use the original c# type.
				type = ReflectionUtils.GetNativeType(type);
			}
			if(value is Delegate) {
				if(value.GetType() != type && type.IsCastableTo(typeof(Delegate))) {
					if(value is EventCallback func) {
						var method = type.GetMethod("Invoke");
						if(method.ReturnType == typeof(void)) {
							var parameters = method.GetParameters();
							Type[] types = new Type[parameters.Length];
							for(int i = 0; i < parameters.Length; i++) {
								types[i] = parameters[i].ParameterType;
							}
							return CustomDelegate.CreateActionDelegate((obj) => {
								func(obj);
							}, types);
						}
						else {
							var parameters = method.GetParameters();
							Type[] types = new Type[parameters.Length + 1];
							for(int i = 0; i < parameters.Length; i++) {
								types[i] = parameters[i].ParameterType;
							}
							types[parameters.Length] = method.ReturnType;
							return CustomDelegate.CreateFuncDelegate((obj) => {
								return func(obj);
							}, types);
						}
					}
					else {
						Delegate del = value as Delegate;
						return Delegate.CreateDelegate(type, del.Target, del.Method);
					}
				}
			}
			else if(value != null) {
				if(type.IsByRef)
					type = type.GetElementType();
				Type valType = value.GetType();
				if(valType != type && (valType.IsValueType || !valType.IsSubclassOf(type) && type.IsCastableTo(valType))) {
					return Operator.Convert(value, type);
				}
				else if(type is RuntimeType) {
					return Operator.Convert(value, type);
				}
				// else if(type.IsSubclassOf(typeof(Component))) {
				// 	if(value is GameObject gameObject) {
				// 		return gameObject.GetComponent(type);
				// 	} else if(value is Component component) {
				// 		return component.GetComponent(type);
				// 	}
				// }
			}
			return value;
		}
		#endregion

		#region Set
		/// <summary>
		/// Assigns a new value to the variable.
		/// </summary>
		/// <param name="value"></param>
		public void Set(Flow flow, object value) {
			Set(flow, value, null);
		}

		/// <summary>
		/// Assigns a new value to the variable.
		/// </summary>
		/// <param name="value">The value to assign</param>
		/// <param name="paramValues">the parameter list for invoke method.</param>
		public void Set(Flow flow, object value, object[] paramValues) {
			EnsureIntialized();
			AutoConvertValue(ref value);
			switch(targetType) {
				case TargetType.None:
					return;
				case TargetType.uNodeParameter: {
					if(isDeepTarget) {
						DoReflectSet(ReflectionUtils.GetMemberTargetRef(memberInfo, flow.GetLocalData(null, startItem.GetReferenceValue()), out var parentTarget, paramValues), value, parentTarget);
					}
					else {
						flow.SetLocalData(null, startItem.GetReferenceValue(), value);
					}
					return;
				}
				case TargetType.uNodeGenericParameter:
					throw new Exception("Generic Type can't be set");
				case TargetType.uNodeFunction:
					throw new Exception("Class Function can't be set");
				case TargetType.Type:
					throw new Exception("Target Type : Type can't be set");
				case TargetType.Values:
					throw new Exception("Target Type : Values can't be set");
				case TargetType.Self:
					throw new Exception("Self target can't be set");
				case TargetType.uNodeProperty:
					if(isDeepTarget) {
						goto default;
					}
					else {
						(startItem.GetReferenceValue() as Property).Set(flow, value);
					}
					break;
				case TargetType.uNodeLocalVariable:
				case TargetType.uNodeVariable: {
					if(isDeepTarget) {
						DoReflectSet(ReflectionUtils.GetMemberTargetRef(memberInfo, (startItem.GetReferenceValue() as Variable).Get(flow), out var parentTarget, paramValues), value, parentTarget);
					}
					else {
						(startItem.GetReferenceValue() as Variable).Set(flow, value);
					}
					return;
				}
				default: {
					DoReflectSet(ReflectionUtils.GetMemberTargetRef(memberInfo, startTarget, out var parentTarget, paramValues), value, parentTarget);
					return;
				}
			}
		}
		#endregion

		#region Types
		/// <summary>
		/// The type of the reflected member.
		/// </summary>
		public Type type {
			get {
				if(_type == null || _type.Equals(null)) {
					switch(targetType) {
						case TargetType.None:
							return null;
						case TargetType.Null:
							//Return System.Object type on target type is Null.
							return typeof(object);
						//case TargetType.uNodeGenericParameter:
						//	//if(startTarget is uNodeFunction) {
						//	//	_genericParameterData = new GenericParameterData[1];
						//	//	_genericParameterData[0] = (startTarget as uNodeFunction).GetGenericParameter(name);
						//	//	return _genericParameterData[0].value;
						//	//}
						//	if(_type == null) {
						//		goto default;
						//	}
						//	break;
						case TargetType.uNodeParameter: {
							if(isDeepTarget) {
								goto default;
							}
							var param = startItem.GetReferenceValue() as ParameterData;
							if(param != null) {
								if(param != null) {
									return param.type.type;
								}
							}
							if(_type == null) {
								goto default;
							}
							break;
						}
						case TargetType.uNodeGenericParameter:
						case TargetType.uNodeType:
						case TargetType.Type:
							return typeof(Type);
						case TargetType.Self:
						case TargetType.Values:
							return startType;
						case TargetType.uNodeVariable:
						case TargetType.uNodeLocalVariable:
							if(!isDeepTarget) {
								var reference = startItem?.reference as VariableRef;
								if(reference != null) {
									return type = reference.type;
								}
							}
							goto default;
						case TargetType.uNodeProperty:
							if(!isDeepTarget) {
								var reference = startItem?.reference as PropertyRef;
								if(reference != null) {
									return type = reference.ReturnType();
								}
							}
							goto default;
						case TargetType.uNodeFunction:
							if(!isDeepTarget) {
								var reference = startItem?.reference as FunctionRef;
								if(reference != null) {
									return type = reference.ReturnType();
								}
							}
							goto default;
						case TargetType.NodePort: {
							var port = startItem?.GetReferenceValue() as ValuePort;
							if(port != null) {
								return port.type;
							}
							break;
						}
						default:
							if(_type == null) {
								var members = GetMembers(false);
								if(members != null && members.Length > 0) {
									return type = ReflectionUtils.GetMemberType(members[members.Length - 1]);
									//type = t;
									//_type = t;
								}
								else {
									_type = TargetSerializedType.type;
								}
								if(_type == null && !TargetSerializedType.isFilled) {
									return startType;
								}
							}
							break;
					}
				}
				return _type;
			}
			set {
				if(value == null)
					return;
				targetSerializedType = new SerializedType(value);
				_type = null;
			}
		}

		/// <summary>
		/// The start target type.
		/// </summary>
		public Type startType {
			get {
				if(_startType == null || _startType.Equals(null)) {
					if(isStatic) {
						var GenericTypes = Utilities.SafeGetGenericTypes(this);
						if(GenericTypes != null && GenericTypes.Length > 0 && GenericTypes[0] != null && GenericTypes[0].Length > 0) {
							_startType = GenericTypes[0][0];
						}
						else {
							_startType = StartSerializedType.type;
							if(_startType != null && _startType.IsGenericTypeDefinition &&
								GenericTypes != null && GenericTypes.Length > 0 &&
								GenericTypes[0] != null && GenericTypes[0].Length > 0) {
								_startType = ReflectionUtils.MakeGenericType(_startType, GenericTypes[0]);
							}
						}
					}
					else {
						switch(targetType) {
							case TargetType.Self:
								var instance = this.instance;
								if(instance is IReflectionType reflection) {
									return reflection.ReflectionType;
								}
								else if(instance is IClassGraph classGraph) {
									return classGraph.InheritType;
								}
								else {
									return instance?.GetType() ?? typeof(object);
								}
							case TargetType.uNodeVariable:
							case TargetType.uNodeLocalVariable: {
								var result = startItem.GetReference<VariableRef>()?.type;
								if(result != null) {
									if(StartSerializedType.typeKind != SerialiedTypeKind.None) {
										StartSerializedType.type = null;
									}
									return result;
								}
								goto default;
							}
							case TargetType.uNodeProperty: {
								var result = startItem.GetReference<PropertyRef>()?.type;
								if(result != null) {
									if(StartSerializedType.typeKind != SerialiedTypeKind.None) {
										StartSerializedType.type = null;
									}
									return result;
								}
								goto default;
							}
							case TargetType.uNodeFunction: {
								var result = startItem.GetReference<FunctionRef>()?.type;
								if(result != null) {
									if(StartSerializedType.typeKind != SerialiedTypeKind.None) {
										StartSerializedType.type = null;
									}
									return result;
								}
								goto default;
							}
							case TargetType.uNodeParameter: {
								var result = startItem.GetReference<ParameterRef>()?.type;
								if(result != null) {
									if(StartSerializedType.typeKind != SerialiedTypeKind.None) {
										StartSerializedType.type = null;
									}
									return result;
								}
								goto default;
							}
							default:
								_startType = StartSerializedType.type;
								if(_startType != null && _startType.IsGenericTypeDefinition) {
									var GenericTypes = Utilities.SafeGetGenericTypes(this);
									if(GenericTypes != null && GenericTypes.Length > 0 && GenericTypes[0] != null && GenericTypes[0].Length > 0) {
										_startType = ReflectionUtils.MakeGenericType(_startType, GenericTypes[0]);
									}
								}
								break;
						}
					}
				}
				return _startType;
			}
			set {
				if(value == null)
					return;
				StartSerializedType = new SerializedType(value);
				ResetCache();
			}
		}
		#endregion

		#region Cached Data
		/// <summary>
		/// Used this to reset cached data.
		/// </summary>
		public void ResetCache() {
			isReflected = false;
			_hasRefOrOut = false;
			fieldInfo = null;
			propertyInfo = null;
			constructorInfo = null;
			eventInfo = null;
			methodInfo = null;
			memberInfo = null;
			_genericParameterData = null;
			genericData = null;
			_hasInitializeMembers = false;
			_type = null;
			_startType = null;
			_startTarget = null;
			_displayName = null;
			_parameterTypes = null;
			_genericTypes = null;
			_name = null;
		}

		/// <summary>
		/// The underlying reflected field, or null if the variable is not field.
		/// </summary>
		public FieldInfo fieldInfo { get; private set; }
		/// <summary>
		/// The underlying property field, or null if the variable is not property.
		/// </summary>
		public PropertyInfo propertyInfo { get; private set; }
		/// <summary>
		/// The underlying constructor field, or null if the variable is not constructor.
		/// </summary>
		public ConstructorInfo constructorInfo { get; private set; }
		/// <summary>
		/// The underlying event field, or null if the variable is not event.
		/// </summary>
		public EventInfo eventInfo { get; private set; }
		/// <summary>
		/// The underlying method field, or null if the variable is a method.
		/// </summary>
		public MethodInfo methodInfo { get; private set; }
		/// <summary>
		/// The list of MemberInfo.
		/// </summary>
		public MemberInfo[] memberInfo { get; private set; }

		[NonSerialized]
		private string _name;

		[NonSerialized]
		private bool _hasRefOrOut;
		public bool HasRefOrOut {
			get {
				if(_hasInitializeMembers == false) {
					GetMembers(false);
				}
				return _hasRefOrOut;
			}
			set {
				_hasRefOrOut = value;
			}
		}
		[NonSerialized]
		public TypeData genericData;
		[NonSerialized]
		private GenericParameterData[] _genericParameterData;
		[NonSerialized]
		private object _startTarget;
		[NonSerialized]
		private string _displayName;
		[NonSerialized]
		private bool _hasInitializeMembers;
		[NonSerialized]
		private Type _type;
		[NonSerialized]
		private Type _startType;
		[NonSerialized]
		private Type[][] _parameterTypes;
		[NonSerialized]
		private Type[][] _genericTypes;
		#endregion

		#region Static Functions
		/// <summary>
		/// None target of MemberData.
		/// </summary>
		public static MemberData None {
			get {
				return new MemberData();
			}
		}

		/// <summary>
		/// Empty values of a MemberData.
		/// </summary>
		public static MemberData Null {
			get {
				return new MemberData(null, TargetType.Null);
			}
		}

		/// <summary>
		/// Empty values of a MemberData.
		/// </summary>
		public static MemberData Empty {
			get {
				return new MemberData() { targetType = TargetType.Values };
			}
		}

		/// <summary>
		/// Create MemberData that target this.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MemberData This(object value) {
			return new MemberData("this", value.GetType(), TargetType.Self) { instance = value };
		}

		/// <summary>
		/// Create MemberData that target this.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static MemberData This(object value, Type type) {
			return new MemberData("this", type, TargetType.Self) { instance = value };
		}

		/// <summary>
		/// Clone the MemberData.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static MemberData Clone(MemberData member) {
			return new MemberData(member);
		}

		/// <summary>
		/// Create new MemberData from MemberInfo
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static MemberData CreateFromMember(MemberInfo member) {
			return new MemberData(member);
		}

		/// <summary>
		/// Create new MemberData from MemberInfos
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static MemberData CreateFromMembers(IList<MemberInfo> members) {
			return new MemberData(members);
		}

		/// <summary>
		/// Create a new MemberData that's targeting a type from a full type name
		/// </summary>
		/// <param name="fullTypeName"></param>
		/// <returns></returns>
		public static MemberData CreateFromType(string fullTypeName) {
			var member = new MemberData();
			member.isStatic = true;
			member.startName = fullTypeName;
			member.startSerializedType = fullTypeName.ToType(false);
			member.targetType = MemberData.TargetType.Type;
			return member;
		}

		/// <summary>
		/// Create a new MemberData that's targeting a type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static MemberData CreateFromType(Type type) {
			return new MemberData(type);
		}

		/// <summary>
		/// Create a new MemberData that's targeting a value
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(object value) {
			return new MemberData(value);
		}

		/// <summary>
		/// Create a new MemberData that's targeting a value with given type
		/// </summary>
		/// <param name="value"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(object value, Type type) {
			if(object.ReferenceEquals(value, null) && type.IsValueType) {
				value = ReflectionUtils.CreateInstance(type);
			}
			var m = new MemberData(value);
			m.startType = type;
			return m;
		}

		/// <summary>
		/// Create a new MemberData that's targeting a value with given type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static MemberData CreateValueFromType(Type type) {
			if(type == null)
				return None;
			object value = null;
			if(ReflectionUtils.CanCreateInstance(type)) {
				if(type == typeof(object)) {
					type = typeof(string);
				}
				value = ReflectionUtils.CreateInstance(type);
			}
			var m = new MemberData(value);
			m.startType = type;
			return m;
		}

		/// <summary>
		/// Create a new MemberData for Variable
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(Variable variable) {
			return new MemberData() {
				Items = new[] {
					new ItemData() {
						name = variable.name,
						reference = new VariableRef(variable, variable.graphContainer)
					}
				},
				targetType = TargetType.uNodeVariable,
				startType = variable.type,
				isStatic = false,
				instance = null
			};
		}

		/// <summary>
		/// Use only for editor
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(UPort port) {
			return CreateFromValue(new UPortRef(port));
		}

		/// <summary>
		/// Use only for editor
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(UPortRef port) {
			return new MemberData() {
				Items = new ItemData[] { new ItemData(port) },
				targetType = TargetType.NodePort,
			};
		}

		/// <summary>
		/// Create a new MemberData for Parameter
		/// </summary>
		/// <param name="parameterReference"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(ParameterRef parameterReference) {
			return new MemberData() {
				instance = null,
				targetType = TargetType.uNodeParameter,
				targetSerializedType = parameterReference.type,
				startSerializedType = parameterReference.type,
				isStatic = false,
				Items = new[] {
					MemberDataUtility.CreateItemData(parameterReference)
				}
			};
		}

		/// <summary>
		/// Create a new MemberData for uNodeProperty
		/// </summary>
		/// <param name="value"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(Property value) {
			return new MemberData() {
				type = value.ReturnType(),
				startType = typeof(IGraph),
				isStatic = false,
				targetType = TargetType.uNodeProperty,
				Items = new[] {
					MemberDataUtility.CreateItemData(value)
				},
			};
		}

		/// <summary>
		/// Create a new MemberData for uNodeFunction
		/// </summary>
		/// <param name="value"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(Function value) {
			var mData = new MemberData();
			mData.targetSerializedType = value.ReturnType();
			mData.startSerializedType = typeof(MonoBehaviour);
			mData.isStatic = false;
			mData.targetType = MemberData.TargetType.uNodeFunction;
			mData.Items = new[] {
				MemberDataUtility.CreateItemData(value)
			};
			return mData;
		}

		/// <summary>
		/// Create a new MemberData for uNodeFunction
		/// </summary>
		/// <param name="value"></param>
		/// <param name="genericTypes"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static MemberData CreateFromValue(Function value, Type[] genericTypes) {
			var mData = new MemberData();
			mData.targetSerializedType = new SerializedType(value.ReturnType());
			mData.startType = typeof(MonoBehaviour);
			mData.isStatic = false;
			mData.targetType = MemberData.TargetType.uNodeFunction;
			mData.Items = new[] {
				MemberDataUtility.CreateItemData(value, genericTypes)
			};
			return mData;
		}

		public static MemberData Default(Type type) {
			if(FilterAttribute.DefaultTypeFilter.IsValidTypeForValueConstant(type)) {
				return CreateValueFromType(type);
			}
			return None;
		}

		public static bool CanApplyAutoConvert(MemberData member, Type type) {
			if(member == null || type == null)
				return false;
			var mType = member.type;
			if(mType != null && mType != type && mType.IsCastableTo(type)) {
				return true;
			}
			return false;
		}
		#endregion

	}
}