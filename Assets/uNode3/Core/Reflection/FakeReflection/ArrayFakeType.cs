using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MaxyGames.UNode {
	class ArrayFakeType : RuntimeType, IFakeType {
		private readonly Type target;

		public ArrayFakeType(Type elementType) {
			target = elementType;
		}

		public override string Name => target.Name + "[]";
		public override string FullName => target.FullName + "[]";
		public override string Namespace => target.Namespace;
		public override Type BaseType => typeof(Array);
		public override bool IsGenericType => false;
		public override bool IsConstructedGenericType => false;
		public override Assembly Assembly => target.Assembly;
		public override string AssemblyQualifiedName => target.AssemblyQualifiedName;
		public override IEnumerable<CustomAttributeData> CustomAttributes => Enumerable.Empty<CustomAttributeData>();
		public override bool ContainsGenericParameters => target.IsGenericParameter || target.IsGenericParameter && target.ContainsGenericParameters;
		public override Type DeclaringType => null;
		public override GenericParameterAttributes GenericParameterAttributes => throw null;
		public override Type[] GenericTypeArguments => Type.EmptyTypes;
		public override bool IsEnum => false;
		public override MemberTypes MemberType => MemberTypes.TypeInfo;
		public override Type ReflectedType => null;

		public override int GetArrayRank() => 1;
		public override Type GetElementType() => target;
		protected override bool HasElementTypeImpl() => true;
		public override Type[] GetGenericArguments() => Type.EmptyTypes;
		public override Type GetGenericTypeDefinition() => null;

		public override Type MakeArrayType() {
			return ReflectionFaker.FakeArrayType(this);
		}

		public override Type MakeArrayType(int rank) {
			return ReflectionFaker.FakeArrayType(this);
		}

		public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
			//Skip since array doesn't have fields.
			return null;
		}

		public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
			return Array.Empty<FieldInfo>();
		}

		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
			if(types == null)
				types = Type.EmptyTypes;
			EnsureInitialized();
			if(name == "Get") {
				return m_getMethod;
			}
			else if(name == "Set") {
				return m_setMethod;
			}
			else {
				return typeof(Array).GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
			}
		}

		class ArrayGetMethod : RuntimeMethod, IFakeMember {
			private ParameterInfo[] parameters;

			public ArrayGetMethod(RuntimeType owner) : base(owner) {
				parameters = new[] { new RuntimeParameterInfo("index", typeof(int)) };
			}

			public override string Name => "Get";

			public override Type ReturnType => owner.GetElementType();

			public override ParameterInfo[] GetParameters() {
				var result = new ParameterInfo[parameters.Length];
				Array.Copy(parameters, result, parameters.Length);
				return result;
			}

			public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
				Array array = (Array)obj;
				return array.GetValue((int)parameters[0]);
			}
		}

		class ArraySetMethod : RuntimeMethod, IFakeMember {
			private ParameterInfo[] parameters;

			public ArraySetMethod(RuntimeType owner) : base(owner) {
				parameters = new[] {
					new RuntimeParameterInfo("index", typeof(int)),
					new RuntimeParameterInfo("value", owner.GetElementType()),
				};
			}

			public override string Name => "Set";

			public override Type ReturnType => typeof(void);

			public override ParameterInfo[] GetParameters() {
				var result = new ParameterInfo[parameters.Length];
				Array.Copy(parameters, result, parameters.Length);
				return result;
			}

			public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
				Array array = (Array)obj;
				array.SetValue(parameters[1], (int)parameters[0]);
				return null;
			}
		}


		private MethodInfo m_getMethod;
		private MethodInfo m_setMethod;
		private void EnsureInitialized() {
			if(m_getMethod == null) {
				m_getMethod = new ArrayGetMethod(this);
				m_setMethod = new ArraySetMethod(this);
			}
		}

		public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
			EnsureInitialized();
			//if(bindingAttr.HasFlags(BindingFlags.DeclaredOnly)) {
			//	return new[] { m_getMethod, m_setMethod };
			//}
			//var methods = typeof(Array).GetMethods(bindingAttr);
			//var result = new MethodInfo[methods.Length + 2];
			//Array.Copy(methods, 0, result, 2, methods.Length);
			//result[0] = m_getMethod;
			//result[1] = m_setMethod;
			//return methods;
			return new[] { m_getMethod, m_setMethod };
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
			EnsureInitialized();
			//if(bindingAttr.HasFlags(BindingFlags.DeclaredOnly)) {
			//	return new[] { m_getMethod, m_setMethod };
			//}
			//var members = typeof(Array).GetMembers(bindingAttr);
			//var result = new MemberInfo[members.Length + 2];
			//Array.Copy(members, 0, result, 2, members.Length);
			//result[0] = m_getMethod;
			//result[1] = m_setMethod;
			//return members;
			return new[] { m_getMethod, m_setMethod };
		}

		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) {
			if(types == null)
				types = Type.EmptyTypes;
			return typeof(Array).GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
		}

		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
			return typeof(Array).GetProperties(bindingAttr);
		}

		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
			return null;
		}

		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
			return Array.Empty<ConstructorInfo>();
		}

		public override IEnumerable<MemberInfo> GetRuntimeMembers(BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			return GetMembers(bindingAttr);
		}

		protected override TypeAttributes GetAttributeFlagsImpl() {
			return typeof(Array).Attributes;
		}

		public override object[] GetCustomAttributes(bool inherit) {
			return typeof(Array).GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return typeof(Array).GetCustomAttributes(attributeType, inherit);
		}

		private Type[] _arrayInterfaces;
		private Type[] arrayInterfaces {
			get {
				if(_arrayInterfaces == null) {
					var types = typeof(Array).GetInterfaces();
					_arrayInterfaces = new Type[types.Length + 1];
					Array.Copy(types, _arrayInterfaces, types.Length);
					_arrayInterfaces[types.Length] = ReflectionUtils.MakeGenericType(typeof(IEnumerable<>), target);
				}
				return _arrayInterfaces;
			}
		}

		public override Type GetInterface(string fullname, bool ignoreCase) {
			SplitName(fullname, out var name, out var ns);
			var ifaces = arrayInterfaces;
			for(int i = 0; i < ifaces.Length; i++) {
				if(!FilterApplyPrefixLookup(ifaces[i], name, ignoreCase)) {
					continue;
				}
				if(ns != null && !ifaces[i].Namespace.Equals(ns)) {
					continue;
				}
				return ifaces[i];
			}
			return null;
		}

		public override Type[] GetInterfaces() {
			return arrayInterfaces;
		}

		protected override bool IsArrayImpl() => true;

		public Type GetNativeType() {
			var elementType = ReflectionUtils.GetNativeType(target);
			if(elementType != null && elementType is not IRuntimeMember) {
				return elementType.MakeArrayType(GetArrayRank());
			}
			return null;
		}
	}
}