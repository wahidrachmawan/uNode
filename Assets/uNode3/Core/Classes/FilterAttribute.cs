using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	/// <summary>
	/// Filters the list of items displayed in the Item Selector / Type Selector.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public class FilterAttribute : Attribute {
		/// <summary>
		/// Custom type validator.
		/// Note: when this is filled the Filter will ignore default type filter.
		/// </summary>
		public Func<Type, bool> ValidateType;
		/// <summary>
		/// Display Inherited members.
		/// </summary>
		public bool Inherited { get; set; } = true;

		/// <summary>
		/// Display instance members.
		/// </summary>
		public bool Instance { get; set; } = true;

		/// <summary>
		/// Display static members.
		/// </summary>
		public bool Static { get; set; } = true;

		/// <summary>
		/// Display public members.
		/// </summary>
		public bool Public { get; set; } = true;

		/// <summary>
		/// Display private and protected members.
		/// </summary>
		public bool NonPublic { get; set; }

		/// <summary>
		/// Display private members ( NonPublic must be true ).
		/// </summary>
		public bool Private { get; set; }

		/// <summary>
		/// Display Global Type member
		/// </summary>
		public bool DisplayDefaultStaticType { get; set; } = true;

		/// <summary>
		/// Display only member that can be set.
		/// </summary>
		public bool SetMember { get; set; }

		protected bool? boxing = true;
		/// <summary>
		/// Allow Select Member inside struct value by default is true.
		/// </summary>
		public bool Boxing {
			get {
				return boxing == null && !SetMember || boxing != null && boxing.Value;
			}
			set {
				boxing = value;
			}
		}

		protected bool? _selectBaseType;
		/// <summary>
		/// if true, Base Type can be selected.
		/// </summary>
		public bool SelectBaseType {
			get {
				return _selectBaseType == null && SetMember || _selectBaseType != null && _selectBaseType.Value;
			}
			set {
				_selectBaseType = value;
			}
		}

		/// <summary>
		/// Display nesteed type member (class) in static variable.
		/// </summary>
		public bool NestedType { get; set; } = true;

		/// <summary>
		/// If true, Filtered interface type will be allowed.
		/// </summary>
		public bool AllowInterface { get; set; }

		/// <summary>
		/// Include instance member when targeting static type.
		/// </summary>
		public bool DisplayInstanceOnStatic { get; set; } = true;
		/// <summary>
		/// Include Generic Type
		/// </summary>
		public bool DisplayGenericType { get; set; } = true;
		/// <summary>
		/// Include Interface Type
		/// </summary>
		public bool DisplayInterfaceType { get; set; } = true;
		/// <summary>
		/// Include Struct/Value Type
		/// </summary>
		public bool DisplayValueType { get; set; } = true;
		/// <summary>
		/// Include Class/Reference Type
		/// </summary>
		public bool DisplayReferenceType { get; set; } = true;
		/// <summary>
		/// Include Sealed Type
		/// </summary>
		public bool DisplaySealedType { get; set; } = true;
		/// <summary>
		/// Include Sealed Type
		/// </summary>
		public bool DisplayAbstractType { get; set; } = true;
		/// <summary>
		/// Include Native (CLR) C# Types
		/// </summary>
		/// <value></value>
		public bool DisplayNativeType { get; set; } = true;
		/// <summary>
		/// Display uNode runtime type ( generated and native )
		/// </summary>
		public bool DisplayRuntimeType {
			get => DisplayGeneratedRuntimeType || DisplayNativeRuntimeType;
			set {
				DisplayGeneratedRuntimeType = value;
				DisplayNativeRuntimeType = true;
			}
		}
		public bool DisplayGeneratedRuntimeType { get; set; } = true;
		public bool DisplayNativeRuntimeType { get; set; } = true;

		/// <summary>
		/// Indicate type can be selected, this will be ignore when OnlyGetType is true.
		/// </summary>
		public bool CanSelectType {
			get {
				return _canSelectType || OnlyGetType;
			}
			set {
				_canSelectType = value;
			}
		}
		protected bool _canSelectType = false;

		/// <summary>
		/// Only can select type.
		/// </summary>
		public bool OnlyGetType { get; set; }
		/// <summary>
		/// Display unity target reference
		/// </summary>
		public bool UnityReference { get; set; } = true;
		/// <summary>
		/// Only display array type
		/// </summary>
		public bool OnlyArrayType { get; set; }
		/// <summary>
		/// Only display generic type
		/// </summary>
		public bool OnlyGenericType { get; set; }

		/// <summary>
		/// Array manipulator when getting the type.
		/// </summary>
		public bool ArrayManipulator { get; set; } = true;

		/// <summary>
		/// If true can select void return type.
		/// </summary>
		public bool VoidType { get; set; }

		/// <summary>
		/// The minimum method parameter to include
		/// </summary>
		public int MinMethodParam { get; set; }
		/// <summary>
		/// The maximum method parameter to include
		/// </summary>
		public int MaxMethodParam { get; set; }

		private List<Type> _types = new List<Type>();
		/// <summary>
		/// The types to display, or empty for any.
		/// </summary>
		public List<Type> Types {
			get {
				return _types;
			}
			set {
				_tooltip = null;
				_types = value;
			}
		}
		/// <summary>
		/// The list of type to mark as invalid for this filter.
		/// </summary>
		public List<Type> InvalidTypes;
		/// <summary>
		/// Hide sub class type from HideTypes.
		/// </summary>
		public bool HideSubClassType = true;

		/// <summary>
		/// The valid member type to select
		/// </summary>
		public MemberTypes ValidMemberType = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method | MemberTypes.Constructor | MemberTypes.Event | MemberTypes.NestedType;
		/// <summary>
		/// The valid member type to access.
		/// </summary>
		public MemberTypes ValidNextMemberTypes = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method | MemberTypes.Event | MemberTypes.NestedType;
		public BindingFlags validBindingFlags {
			get {
				BindingFlags flags = (BindingFlags)0;

				if(Public)
					flags |= BindingFlags.Public;
				if(NonPublic)
					flags |= BindingFlags.NonPublic;
				if(Instance)
					flags |= BindingFlags.Instance;
				if(Static)
					flags |= BindingFlags.Static;
				if(!Inherited)
					flags |= BindingFlags.DeclaredOnly;
				if(Static && Inherited) {
					flags |= BindingFlags.FlattenHierarchy;
				} else if(Static) {
					flags |= BindingFlags.Static;
				}
				return flags;
			}
		}
		/// <summary>
		/// The invalid member Type, None mean any.
		/// </summary>
		public MemberData.TargetType ValidTargetType = MemberData.TargetType.None;
		/// <summary>
		/// The invalid member Type.
		/// </summary>
		public MemberData.TargetType InvalidTargetType = MemberData.TargetType.None;
		/// <summary>
		/// The valid Attribute target type in all member.
		/// </summary>
		public AttributeTargets attributeTargets = AttributeTargets.All;

		private static FilterAttribute _default;
		/// <summary>
		/// Default Filter
		/// </summary>
		public static FilterAttribute Default {
			get {
				if(_default == null) {
					_default = new FilterAttribute();
				}
				return _default;
			}
		}

		private static FilterAttribute _defaultInheritFilter;
		/// <summary>
		/// Default filter for selecting inherit type.
		/// </summary>
		public static FilterAttribute DefaultInheritFilter {
			get {
				if(_defaultInheritFilter == null) {
					_defaultInheritFilter = new FilterAttribute() {
						OnlyGetType = true,
						DisplaySealedType = false,
						DisplayValueType = false,
						DisplayInterfaceType = false,
						UnityReference = false,
						ArrayManipulator = false,
						DisplayRuntimeType = false
					};
				}
				return _defaultInheritFilter;
			}
		}

		private static FilterAttribute _defaultTypeFilter;
		/// <summary>
		/// Default filter for selecting type.
		/// </summary>
		public static FilterAttribute DefaultTypeFilter {
			get {
				if(_defaultTypeFilter == null) {
					_defaultTypeFilter = new FilterAttribute() {
						OnlyGetType = true,
					};
				}
				return _defaultTypeFilter;
			}
		}

		#region Constructors
		/// <summary>
		/// Filters the list of members displayed in the inspector drawer.
		/// </summary>
		public FilterAttribute() {

		}

		/// <summary>
		/// Filters the list of members displayed in the inspector drawer.
		/// </summary>
		/// <param name="types">The types to display, or none for any.</param>
		public FilterAttribute(params Type[] types) {
			this.Types.AddRange(types);
			if(Types.Count > 0 /*&& !Types.Contains(typeof(object))*/) {
				ArrayManipulator = false;
			}
		}

		public FilterAttribute(FilterAttribute other) {
			if(other == null) return;
			this.CanSelectType = other.CanSelectType;
			this.DisplayDefaultStaticType = other.DisplayDefaultStaticType;
			this.DisplayGenericType = other.DisplayGenericType;
			this.DisplayInstanceOnStatic = other.DisplayInstanceOnStatic;
			this.HideSubClassType = other.HideSubClassType;
			if(other.InvalidTypes != null)
				this.InvalidTypes = new List<Type>(other.InvalidTypes);
			this.Inherited = other.Inherited;
			this.Instance = other.Instance;
			this.MaxMethodParam = other.MaxMethodParam;
			this.MinMethodParam = other.MinMethodParam;
			this.NestedType = other.NestedType;
			this.NonPublic = other.NonPublic;
			this.OnlyArrayType = other.OnlyArrayType;
			this.UnityReference = other.UnityReference;
			this.DisplayGeneratedRuntimeType = other.DisplayGeneratedRuntimeType;
			this.DisplayNativeRuntimeType = other.DisplayNativeRuntimeType;
			this.OnlyGenericType = other.OnlyGenericType;
			this.OnlyGetType = other.OnlyGetType;
			this.Public = other.Public;
			this.SetMember = other.SetMember;
			this.Static = other.Static;
			this.Types = new List<Type>(other.Types);
			this.ValidMemberType = other.ValidMemberType;
			this.ValidNextMemberTypes = other.ValidNextMemberTypes;
			this.ArrayManipulator = other.ArrayManipulator;
			this.SelectBaseType = other.SelectBaseType;
			this.boxing = other.boxing;
			this.DisplayInterfaceType = other.DisplayInterfaceType;
			this.DisplayValueType = other.DisplayValueType;
			this.DisplayReferenceType = other.DisplayReferenceType;
			this.DisplaySealedType = other.DisplaySealedType;
			this.DisplayAbstractType = other.DisplayAbstractType;
			this.DisplayNativeType = other.DisplayNativeType;
			this.attributeTargets = other.attributeTargets;
			this.ValidTargetType = other.ValidTargetType;
			this.InvalidTargetType = other.InvalidTargetType;
			this.VoidType = other.VoidType;
			this.AllowInterface = other.AllowInterface;
			this.ValidateType = other.ValidateType;
		}
		#endregion

		public void SetType(Type type) {
			this.Types = new List<Type>();
			this.Types.Add(type);
		}

		public void SetType(params Type[] types) {
			this.Types = new List<Type>(types);
		}

		public Type GetActualType() {
			if(OnlyGetType) {
				return typeof(Type);
			} else if(Types != null && Types.Count > 0) {
				return Types[0];
			}
			return typeof(object);
		}

		public IList<Type> GetFilteredTypes() {
			if(OnlyGetType) {
				return new[] { typeof(Type) };
			} else if(Types != null && Types.Count > 0) {
				return Types;
			}
			return new[] { typeof(object) };
		}

		public bool IsValidTarget(MemberData.TargetType targetType) {
			bool flag = ValidTargetType != MemberData.TargetType.None;
			bool flag2 = InvalidTargetType != MemberData.TargetType.None;
			if(flag || flag2) {
				if(flag2 && InvalidTargetType.HasFlags(targetType)) {
					return false;
				}
				if(flag && !ValidTargetType.HasFlags(targetType)) {
					return false;
				}
			}
			return true;
		}

		public bool IsValidTarget(MemberTypes targetType) {
			bool flag = ValidTargetType != MemberData.TargetType.None;
			bool flag2 = InvalidTargetType != MemberData.TargetType.None;
			if(flag || flag2) {
				switch(targetType) {
					case MemberTypes.Field:
						if(flag2 && InvalidTargetType.HasFlags(MemberData.TargetType.Field)) {
							return false;
						}
						if(flag && !ValidTargetType.HasFlags(MemberData.TargetType.Field)) {
							return false;
						}
						break;
					case MemberTypes.Property:
						if(flag2 && InvalidTargetType.HasFlags(MemberData.TargetType.Property)) {
							return false;
						}
						if(flag && !ValidTargetType.HasFlags(MemberData.TargetType.Property)) {
							return false;
						}
						break;
					case MemberTypes.Method:
						if(flag2 && InvalidTargetType.HasFlags(MemberData.TargetType.Method)) {
							return false;
						}
						if(flag && !ValidTargetType.HasFlags(MemberData.TargetType.Method)) {
							return false;
						}
						break;
					case MemberTypes.Event:
						if(flag2 && InvalidTargetType.HasFlags(MemberData.TargetType.Event)) {
							return false;
						}
						if(flag && !ValidTargetType.HasFlags(MemberData.TargetType.Event)) {
							return false;
						}
						break;
					case MemberTypes.NestedType:
						if(flag2 && InvalidTargetType.HasFlags(MemberData.TargetType.Type)) {
							return false;
						}
						if(flag && !ValidTargetType.HasFlags(MemberData.TargetType.Type)) {
							return false;
						}
						break;
				}
			}
			return true;
		}

		/// <summary>
		/// True indicate all types filter are value type.
		/// </summary>
		/// <returns></returns>
		public bool IsValueTypes() {
			if(Types != null && Types.Count > 0) {
				for(int i = 0; i < Types.Count; i++) {
					Type type = Types[i];
					if(type == null) continue;
					if(type.IsByRef) {
						type = type.GetElementType();
					}
					if(!type.IsValueType) return false;
				}
				return true;
			}
			return false;
		}

		private readonly HashSet<Type> allowedNonSerializableTypesForValue = new HashSet<Type>() {
			typeof(string),
			typeof(Vector2),
			typeof(Vector2Int),
			typeof(Vector3),
			typeof(Vector3Int),
			typeof(Vector4),
			typeof(AnimationCurve),
			typeof(Bounds),
			typeof(BoundsInt),
			typeof(Color),
			typeof(Color32),
			typeof(Gradient),
			typeof(LayerMask),
			typeof(Quaternion),
			typeof(Rect),
			typeof(RectInt),
		};

		/// <summary>
		/// The valid type for edits ( Valid only for string, value types and any serializable type )
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public bool IsValidTypeForValueConstant(Type t) {
			if(t == null) return true;
			if(t.IsByRef) {
				t = t.GetElementType();
			}
			if(t.IsCastableTo(typeof(UnityEngine.Object))) return true;
			if(t.IsInterface || t.IsAbstract) return false;
			if(!DisplayValueType && t.IsValueType) return false;
			if(t is RuntimeType) {
				if(!DisplayGeneratedRuntimeType && ReflectionUtils.IsNativeType(t) == false)
					return false;
				if(!DisplayNativeRuntimeType && ReflectionUtils.IsNativeType(t))
					return false;
			}
			if(!ReflectionUtils.CanCreateInstance(t)) return false;
			if(!t.IsEnum && !t.IsSerializable) {
				if(!allowedNonSerializableTypesForValue.Contains(t)) {
					return false;
				}
			}
			if(InvalidTypes != null && InvalidTypes.Count > 0) {
				for(int i = 0; i < InvalidTypes.Count; i++) {
					Type type = InvalidTypes[i];
					if(type == null) continue;
					if(type.IsByRef) {
						type = type.GetElementType();
					}
					if(type == t || (HideSubClassType && t.IsSubclassOf(type))) {
						return false;
					}
				}
			}
			if(attributeTargets != AttributeTargets.All && t.IsCastableTo(typeof(System.Attribute))) {
				if(t.IsDefined(typeof(AttributeUsageAttribute), true)) {
					var a = t.GetCustomAttributes(typeof(AttributeUsageAttribute), true)[0] as AttributeUsageAttribute;
					if(!a.ValidOn.HasFlags(attributeTargets)) return false;
				}
			}
			if(t == typeof(void) && !VoidType) {
				return false;
			}
			if(ValidateType != null) {
				return ValidateType(t);
			}
			if(Types == null || Types.Count == 0) {
				return true;
			}
			bool hasType = false;
			for(int i = 0; i < Types.Count; i++) {
				Type type = Types[i];
				if(type == null) continue;
				if(type.IsByRef) {
					type = type.GetElementType();
				}
				if(t.IsCastableTo(type)) {
					hasType = true;
					break;
				}
				if(SelectBaseType && t.IsAssignableFrom(type)) {
					hasType = true;
					break;
				}
			}
			return hasType;
		}

		/// <summary>
		/// Is the type is a valid for this filter ( no type filter ).
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public bool IsValidTypeSimple(Type t) {
			if(t == null)
				return true;
			if(t.IsByRef) {
				t = t.GetElementType();
			}
			if(!DisplayNativeType && !(t is RuntimeType))
				return false;
			if(!DisplayInterfaceType && t.IsInterface)
				return false;
			if(DisplayInterfaceType && AllowInterface && t.IsInterface)
				return true;
			if(!DisplayReferenceType && !t.IsValueType && !t.IsInterface)
				return false;
			if(!DisplayValueType && t.IsValueType)
				return false;
			if(!DisplaySealedType && t.IsSealed)
				return false;
			if(!DisplayAbstractType && t.IsAbstract)
				return false;
			if(t is RuntimeType) {
				if(!DisplayGeneratedRuntimeType && ReflectionUtils.IsNativeType(t) == false)
					return false;
				if(!DisplayNativeRuntimeType && ReflectionUtils.IsNativeType(t))
					return false;
			}
			if(OnlyGetType && t.IsAbstract && t.IsSealed)
				return false;//Ensure the static type is not valid when only get type
			if(InvalidTypes != null && InvalidTypes.Count > 0) {
				for(int i = 0; i < InvalidTypes.Count; i++) {
					Type type = InvalidTypes[i];
					if(type == null)
						continue;
					if(type.IsByRef) {
						type = type.GetElementType();
					}
					if(type == t || (HideSubClassType && t.IsSubclassOf(type))) {
						return false;
					}
				}
			}
			if(t == typeof(void) && !VoidType) {
				return false;
			}
			if(attributeTargets != AttributeTargets.All && t.IsCastableTo(typeof(System.Attribute))) {
				if(t.IsDefined(typeof(AttributeUsageAttribute), true)) {
					var a = t.GetCustomAttributes(typeof(AttributeUsageAttribute), true)[0] as AttributeUsageAttribute;
					if(!a.ValidOn.HasFlags(attributeTargets))
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Is the type is a valid for this filter.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public bool IsValidType(Type t) {
			if(IsValidTypeSimple(t) == false) return false;
			if(ValidateType != null) {
				return ValidateType(t);
			}
			if(Types == null || Types.Count == 0) {
				return true;
			}
			bool hasType = false;
			for(int i = 0; i < Types.Count; i++) {
				Type type = Types[i];
				if(type == null)
					continue;
				if(type.IsByRef) {
					type = type.GetElementType();
				}
				if(t.IsCastableTo(type)) {
					hasType = true;
					break;
				}
				if(SelectBaseType && t.IsAssignableFrom(type)) {
					hasType = true;
					break;
				}
			}
			return hasType;
		}

		public bool IsValidMember(MemberInfo member) {
			if(member != null) {
				if(OnlyGetType) {
					if(!(member is Type)) {
						return false;
					}
					return IsValidType(member as Type);
				}
				if(!IsValidTarget(member.MemberType)) {
					return false;
				}
				if(member is Type) {
					return IsValidType(member as Type);
				}
				switch(member.MemberType) {
					case MemberTypes.Field:
						var field = member as FieldInfo;
						if(!NonPublic && field.IsPrivate) {
							return false;
						}
						break;
					case MemberTypes.Property:
						var prop = member as PropertyInfo;
						if(SetMember && !prop.CanWrite) {
							return false;
						}
						var propM = prop.GetGetMethod() ?? prop.GetSetMethod();
						if(propM == null || !NonPublic && propM.IsPrivate) {
							return false;
						}
						break;
					case MemberTypes.Method:
						var method = member as MethodInfo;
						if(!NonPublic && method.IsPrivate) {
							return false;
						}
						if(!ReflectionUtils.IsValidMethod(method, MaxMethodParam, MinMethodParam, this)) {
							return false;
						}
						break;
					case MemberTypes.Constructor:
						var ctor = member as ConstructorInfo;
						if(!ReflectionUtils.IsValidConstructor(ctor, MaxMethodParam, MinMethodParam)) {
							return false;
						}
						break;
				}
				return true;
			}
			return false;
		}

		public bool CanManipulateArray() {
			//return ArrayManipulator && (Types == null || Types.Count == 0 || Types.Contains(typeof(object)));
			return ArrayManipulator;
		}

		string _tooltip;
		public string Tooltip {
			get {
				if(OnlyGetType) {
					return "System.Type";
				}
				if(Types != null && Types.Count > 0) {
					if(_tooltip == null) {
						_tooltip = string.Join("\n", Types.ConvertAll(t => t.PrettyName()));
					}
					return _tooltip;
				}
				return null;
			}
		}

		/// <summary>
		/// Convert filter to only filter type with generic parameter constraints
		/// </summary>
		/// <param name="genericParameterType"></param>
		public void ToFilterGenericConstraints(Type genericParameterType) {
			if(genericParameterType.IsGenericParameter) {
				var constraints = genericParameterType.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
				if((constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0) {//class constraint
					DisplayValueType = false;
					DisplayInterfaceType = false;
					DisplayReferenceType = true;
				} 
				else if((constraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {//struct constraint
					DisplayValueType = true;
					DisplayInterfaceType = false;
					DisplayReferenceType = false;
				}
				//else if((constraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0) {//new constraint
				//	return false;
				//}
				var pType = genericParameterType.GetGenericParameterConstraints();
				if(pType != null && pType.Length > 0) {
					Types.Clear();
					foreach(var p in pType) {
						if(p != null) {
							if(p == typeof(ValueType)) {
								DisplayValueType = true;
								DisplayReferenceType = false;
							} else {
								Types.Add(p);
								if(p.IsInterface) {
									DisplayInterfaceType = true;
								}
							}
						}
					}
				}
			}
		}
	}
}