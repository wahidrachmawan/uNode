using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace MaxyGames.UNode {
	public abstract class RuntimeType<T> : RuntimeType {
		public readonly T target;

		public RuntimeType(T target) {
			this.target = target;
		}

		public override Type BaseType => typeof(T);
	}

	public class MissingType : RuntimeType, ICustomIcon {
		public string missingType;

		public override Type BaseType => typeof(object);

		public override string Name => missingType;

		public override string ToString() {
			if(string.IsNullOrEmpty(missingType)) {
				return "MissingType";
			}
			return "MissingType: " + missingType;
		}

		internal MissingType() {
			missingType = "Missing Type";
		}

		internal MissingType(string missingType) {
			if(string.IsNullOrEmpty(missingType)) {
				missingType = "Missing Type";
			}
			this.missingType = missingType;
		}

		public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
			return null;
		}

		public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
			return new FieldInfo[0];
		}

		public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
			return new MethodInfo[0];
		}

		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
			return new PropertyInfo[0];
		}

		protected override TypeAttributes GetAttributeFlagsImpl() {
			return TypeAttributes.NotPublic;
		}

		public Texture GetIcon() {
			return Resources.Load<Texture2D>("Icons/IconMissing");
		}
	}

	public class MissingGraphType : MissingType {
		public UnityEngine.Object graph;

		public MissingGraphType(UnityEngine.Object graph, string nativeType) {
			if(object.ReferenceEquals(graph, null))
				throw new ArgumentNullException(nameof(graph));
			this.graph = graph;
			this.missingType = nativeType;
		}

		public BaseReference GetReference() {
			return BaseReference.FromValue(graph);
		}
	}

	/// <summary>
	/// The base class for all RuntimeType
	/// </summary>
	public abstract class RuntimeType : Type, IRuntimeMember {
		public const string CompanyNamespace = "MaxyGames";
		public const string RuntimeNamespace = "MaxyGames.Generated";

		#region Operators
		public static bool operator ==(RuntimeType x, RuntimeType y) {
			if(ReferenceEquals(x, null)) {
				return ReferenceEquals(y, null);
			}
			else if(ReferenceEquals(y, null)) {
				return ReferenceEquals(x, null);
			}
			return x.FullName == y.FullName;//This will ensure the type should be same when the name is same.
		}

		public static bool operator !=(RuntimeType x, RuntimeType y) {
			return !(x == y);
		}


		public override bool Equals(Type o) {
			return Equals(o as object);
		}

		public override bool Equals(object obj) {
			var val = obj as RuntimeType;
			return !ReferenceEquals(val, null) && FullName == val.FullName;
		}
		#endregion

		#region Default Type
		class DefaultRuntimeType : RuntimeType {
			public override Type BaseType => typeof(object);

			public override string Name => "Default";

			public override string ToString() {
				return "default";
			}

			public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
				return null;
			}

			public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
				return new FieldInfo[0];
			}

			public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
				return new MethodInfo[0];
			}

			public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
				return new PropertyInfo[0];
			}

			protected override TypeAttributes GetAttributeFlagsImpl() {
				return TypeAttributes.NotPublic;
			}
		}

		private static RuntimeType _Default;
		public static RuntimeType Default {
			get {
				if(_Default == null) {
					_Default = new DefaultRuntimeType();
				}
				return _Default;
			}
		}
		#endregion

		#region Missing Type
		static readonly Dictionary<string, Type> missingTypeMap = new Dictionary<string, Type>();
		static readonly Dictionary<UnityEngine.Object, Type> missingGraphMap = new Dictionary<UnityEngine.Object, Type>();

		public static Type FromMissingType(string typeName) {
			if(string.IsNullOrEmpty(typeName))
				return RuntimeType.Default;
			if(!missingTypeMap.TryGetValue(typeName, out var result)) {
				result = new MissingType(typeName);
				missingTypeMap[typeName] = result;
			}
			return result;
		}

		public static Type FromMissingType(UnityEngine.Object graph, string typeName) {
			if(string.IsNullOrEmpty(typeName) || object.ReferenceEquals(graph, null))
				return RuntimeType.Default;
			if(!missingGraphMap.TryGetValue(graph, out var result)) {
				result = new MissingGraphType(graph, typeName);
				missingGraphMap[graph] = result;
			}
			return result;
		}
		#endregion

		#region ByRef Type
		class ByRefRuntimeType : RuntimeType {
			public readonly RuntimeType target;

			public ByRefRuntimeType(RuntimeType target) {
				if(target.IsByRef) {
					throw new InvalidOperationException();
				}
				this.target = target;
			}

			public override Type BaseType => target.BaseType;

			public override string Name => target.Name + "&";

			protected override bool HasElementTypeImpl() {
				return true;
			}

			protected override bool IsByRefImpl() {
				return true;
			}

			public override string ToString() {
				return target.ToString() + "$";
			}

			public override Type GetElementType() {
				return target;
			}

			public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
				return null;
			}

			public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
				return new FieldInfo[0];
			}

			public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
				return new MethodInfo[0];
			}

			public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
				return new PropertyInfo[0];
			}

			protected override TypeAttributes GetAttributeFlagsImpl() {
				return TypeAttributes.NotPublic;
			}

			public override Type MakeArrayType() {
				throw new InvalidOperationException();
			}

			public override Type MakeArrayType(int rank) {
				throw new InvalidOperationException();
			}

			public override Type MakeGenericType(params Type[] typeArguments) {
				throw new InvalidOperationException();
			}
		}

		private ByRefRuntimeType byRefType;

		public override Type MakeByRefType() {
			if(byRefType == null) {
				byRefType = new ByRefRuntimeType(this);
			}
			return byRefType;
		}
		#endregion

		#region Utility
		protected static void SplitName(string fullname, out string name, out string ns) {
			name = null;
			ns = null;
			if(fullname == null) {
				return;
			}
			int num = fullname.LastIndexOf(".", StringComparison.Ordinal);
			if(num != -1) {
				ns = fullname.Substring(0, num);
				int num2 = fullname.Length - ns.Length - 1;
				if(num2 != 0) {
					name = fullname.Substring(num + 1, num2);
				}
				else {
					name = "";
				}
			}
			else {
				name = fullname;
			}
			int genericIndex = name.IndexOf('`');
			if(genericIndex >= 0) {
				name = name.Remove(genericIndex);
			}
		}

		protected static bool FilterApplyPrefixLookup(MemberInfo memberInfo, string name, bool ignoreCase) {
			if(ignoreCase) {
				if(!memberInfo.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)) {
					return false;
				}
			}
			else if(!memberInfo.Name.StartsWith(name, StringComparison.Ordinal)) {
				return false;
			}
			return true;
		}
		#endregion

		/// <summary>
		/// Update Runtime Type so it is up to date.
		/// Note: Only call this on main thread.
		/// </summary>
		public virtual void Update() { }

		/// <summary>
		/// Is the RuntimeType is valid?
		/// </summary>
		/// <returns></returns>
		public virtual bool IsValid() {
			return true;
		}

		public override Assembly Assembly => null;

		public override string AssemblyQualifiedName => string.Empty;

		public override string FullName {
			get {
				var ns = Namespace;
				if(!string.IsNullOrEmpty(ns)) {
					return ns + "." + Name;
				}
				return Name;
			}
		}

		public override Guid GUID => Guid.Empty;

		public override Module Module => null;

		public override string Namespace {
			get {
				return RuntimeNamespace;
			}
		}

		public override Type UnderlyingSystemType => BaseType ?? this;

		public override Type DeclaringType => null;

		public override Type ReflectedType => null;

		public override GenericParameterAttributes GenericParameterAttributes => GenericParameterAttributes.None;

		public override Type[] GenericTypeArguments => Type.EmptyTypes;

		public override bool IsEnum => false;

		public override bool IsGenericParameter => false;

		public override bool IsGenericType => false;

		public override bool IsGenericTypeDefinition => false;

		public override bool ContainsGenericParameters => false;

		public override bool IsSerializable => true;

		public override bool IsSecurityCritical => false;
		public override bool IsSecuritySafeCritical => false;
		public override bool IsSecurityTransparent => true;
		public override int MetadataToken => 0;

		public override StructLayoutAttribute StructLayoutAttribute => null;

		public override RuntimeTypeHandle TypeHandle => default;

		public override bool IsConstructedGenericType => true;

		public override int GetHashCode() {
			return RuntimeHelpers.GetHashCode(this);
		}

		public override Type[] GetGenericArguments() {
			return Type.EmptyTypes;
		}

		public override Type[] GetGenericParameterConstraints() {
			return Type.EmptyTypes;
		}

		public override Type GetGenericTypeDefinition() {
			return null;
		}

		// public virtual bool IsAssignableTo(Type type) {
		// 	if(type == null) return false;
		// 	if(this == type) return true;
		// 	if(type is RuntimeType) {
		// 		return type.IsAssignableFrom(this);
		// 	}
		// 	if(IsSubclassOf(type)) {
		// 		return true;
		// 	}
		// 	if(type.IsInterface) {
		// 		return HasImplementInterface(this, type);
		// 	}
		// 	if (type.IsGenericParameter) {
		// 		Type[] genericParameterConstraints = type.GetGenericParameterConstraints();
		// 		for (int i = 0; i < genericParameterConstraints.Length; i++) {
		// 			if (!genericParameterConstraints[i].IsAssignableFrom(this)) {
		// 				return false;
		// 			}
		// 		}
		// 		return true;
		// 	}
		// 	return BaseType != null && type.IsAssignableFrom(BaseType);
		// }

		public override bool IsAssignableFrom(Type c) {
			if(c == null) {
				return false;
			}
			if(this == c) {
				return true;
			}
			// RuntimeType runtimeType = UnderlyingSystemType as RuntimeType;
			// if (runtimeType != null) {
			// 	return runtimeType.IsAssignableFrom(c);
			// }
			if(c.IsSubclassOf(this)) {
				return true;
			}
			if(IsInterface) {
				return HasImplementInterface(c, this);
			}
			if(IsGenericParameter) {
				Type[] genericParameterConstraints = GetGenericParameterConstraints();
				for(int i = 0; i < genericParameterConstraints.Length; i++) {
					if(!genericParameterConstraints[i].IsAssignableFrom(c)) {
						return false;
					}
				}
				return true;
			}
			if(c is RuntimeType) {
				return false;
			}
			else if(this is IFakeType fakeType) {
				//If this is a FakeType then try compare the actual native type
				var nativeType = fakeType.GetNativeType();
				if(nativeType != null) {
					return nativeType.IsAssignableFrom(c);
				}
				else {
					return false;
				}
			}
			else {
				return BaseType != null && BaseType.IsAssignableFrom(c);
			}
		}

		public static bool HasImplementInterface(Type type, Type ifaceType) {
			while(type != null) {
				Type[] interfaces = type.GetInterfaces();
				if(interfaces != null) {
					for(int i = 0; i < interfaces.Length; i++) {
						if(interfaces[i] == ifaceType || (interfaces[i] != null && HasImplementInterface(interfaces[i], ifaceType))) {
							return true;
						}
					}
				}
				type = type.BaseType;
			}
			return false;
		}

		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
			return Array.Empty<ConstructorInfo>();
		}

		public override object[] GetCustomAttributes(bool inherit) {
			return Array.Empty<object>();
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return Array.Empty<object>();
		}

		public override IList<CustomAttributeData> GetCustomAttributesData() {
			return new CustomAttributeData[0];
		}

		public override Type GetElementType() {
			return null;
		}

		public override EventInfo GetEvent(string name, BindingFlags bindingAttr) {
			return null;
		}

		public override EventInfo[] GetEvents(BindingFlags bindingAttr) {
			return Array.Empty<EventInfo>();
		}

		public override Type GetInterface(string name, bool ignoreCase) {
			return null;
		}

		public override Type[] GetInterfaces() {
			return Type.EmptyTypes;
		}

		public IEnumerable<ConstructorInfo> GetRuntimeConstructors() {
			return GetConstructors().Where(p => p is IRuntimeMember);
		}

		public IEnumerable<FieldInfo> GetRuntimeFields() {
			return GetFields().Where(p => p is IRuntimeMember);
		}

		public IEnumerable<PropertyInfo> GetRuntimeProperties() {
			return GetProperties().Where(p => p is IRuntimeMember);
		}

		public IEnumerable<MethodInfo> GetRuntimeMethods() {
			return GetMethods().Where(p => p is IRuntimeMember);
		}

		public virtual IEnumerable<MemberInfo> GetRuntimeMembers(BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in GetRuntimeConstructors()) {
				if(ReflectionUtils.IsValidMember(member, bindingAttr, this)) {
					yield return member;
				}
			}
			foreach(var member in GetRuntimeFields()) {
				if(ReflectionUtils.IsValidMember(member, bindingAttr, this)) {
					yield return member;
				}
			}
			foreach(var member in GetRuntimeProperties()) {
				if(ReflectionUtils.IsValidMember(member, bindingAttr, this)) {
					yield return member;
				}
			}
			foreach(var member in GetRuntimeMethods()) {
				if(ReflectionUtils.IsValidMember(member, bindingAttr, this)) {
					yield return member;
				}
			}
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
			var constructors = GetConstructors(bindingAttr);
			var fields = GetFields(bindingAttr);
			var properties = GetProperties(bindingAttr);
			var methods = GetMethods(bindingAttr);
			var members = new MemberInfo[constructors.Length + fields.Length + properties.Length + methods.Length];
			for(int i = 0; i < constructors.Length; i++) {
				members[i] = constructors[i];
			}
			int idx = constructors.Length;
			for(int i = 0; i < fields.Length; i++) {
				members[i + idx] = fields[i];
			}
			idx += fields.Length;
			for(int i = 0; i < properties.Length; i++) {
				members[i + idx] = properties[i];
			}
			idx += properties.Length;
			for(int i = 0; i < methods.Length; i++) {
				members[i + idx] = methods[i];
			}
			return members;
		}

		public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr) {
			List<MemberInfo> members = new List<MemberInfo>();
			var list = GetMembers(bindingAttr);
			foreach(var m in list) {
				if(m.Name == name && m.MemberType.HasFlags(type)) {
					members.Add(m);
				}
			}
			return members.ToArray();
		}

		public override Type GetNestedType(string name, BindingFlags bindingAttr) {
			return null;
		}

		public override Type[] GetNestedTypes(BindingFlags bindingAttr) {
			return Type.EmptyTypes;
		}

		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) {
			throw new NotImplementedException();
		}

		public override bool IsDefined(Type attributeType, bool inherit) {
			return false;
		}

		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
			if(types == null) {
				types = Type.EmptyTypes;
			}
			var members = GetConstructors(bindingAttr);
			for(int i = 0; i < members.Length; i++) {
				var ctor = members[i];
				var parameters = ctor.GetParameters();
				if(types.Length == parameters.Length) {
					bool flag = true;
					for(int x = 0; x < parameters.Length; x++) {
						if(types[x] != parameters[x].ParameterType) {
							flag = false;
							break;
						}
					}
					if(flag) {
						return ctor;
					}
				}
			}
			return null;
		}

		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
			if(types == null) {
				types = Type.EmptyTypes;
			}
			var members = GetMethods(bindingAttr);
			for(int i = 0; i < members.Length; i++) {
				var method = members[i];
				if(method.Name == name) {
					var parameters = method.GetParameters();
					if(types.Length == parameters.Length) {
						bool flag = true;
						for(int x = 0; x < parameters.Length; x++) {
							if(types[x] != parameters[x].ParameterType) {
								flag = false;
								break;
							}
						}
						if(flag) {
							return method;
						}
					}
				}
			}
			return null;
		}

		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) {
			var members = GetProperties(bindingAttr);
			for(int i = 0; i < members.Length; i++) {
				var member = members[i];
				if(member.Name == name) {
					return member;
				}
			}
			return null;
		}

		protected override bool HasElementTypeImpl() {
			return false;
		}

		protected override bool IsArrayImpl() {
			return false;
		}

		protected override bool IsByRefImpl() {
			return false;
		}

		public override bool IsByRefLike => false;

		protected override bool IsCOMObjectImpl() {
			return false;
		}

		protected override bool IsPointerImpl() {
			return false;
		}

		protected override bool IsPrimitiveImpl() {
			return false;
		}

		public override Type MakeArrayType() {
			return ReflectionUtils.MakeArrayType(this);
		}

		public override Type MakeArrayType(int rank) {
			if(rank > 1) {
				throw new NotSupportedException();
			}
			return ReflectionUtils.MakeArrayType(this);
		}

		public override Type MakeGenericType(params Type[] typeArguments) {
			return ReflectionUtils.MakeGenericType(this, typeArguments);
		}
	}
}