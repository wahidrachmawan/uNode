using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MaxyGames.UNode {
	abstract class BaseFakeGraphType<T> : FakeType {
		public BaseFakeGraphType(Type target) : base(target) { }

		#region Overrides
		public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
			return target.GetField(name, bindingAttr);
		}

		public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
			return target.GetFields(bindingAttr);
		}

		public override Type[] GetInterfaces() {
			return target.GetInterfaces();
		}

		public override bool IsAssignableFrom(Type c) {
			return target.IsAssignableFrom(c);
		}

		public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
			return target.GetMethods(bindingAttr);
		}

		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
			return target.GetProperties(bindingAttr);
		}

		public override bool IsInstanceOfType(object o) {
			return target.IsInstanceOfType(o);
		}
		#endregion

		public override int GetHashCode() {
			return target.GetHashCode();
		}
	}

	public abstract class FakeType : RuntimeType<Type>, IFakeType {
		private bool hasInitialize = false;
		protected Dictionary<MemberInfo, MemberInfo> declaredMembers = new Dictionary<MemberInfo, MemberInfo>();

		public FakeType(Type target) : base(target) { }

		protected void EnsureInitialized() {
			if(!hasInitialize) {
				Initialize();
				hasInitialize = true;
			}
		}

		protected abstract void Initialize();

		public override string Name => target.Name;
		public override string FullName => target.FullName;
		public override string Namespace => target.Namespace;
		public override Type BaseType => target.BaseType;

		public override bool IsGenericType => target.IsGenericType;
		public override bool IsConstructedGenericType => target.IsConstructedGenericType;
		public override Assembly Assembly => target.Assembly;
		public override string AssemblyQualifiedName => target.AssemblyQualifiedName;
		public override IEnumerable<CustomAttributeData> CustomAttributes => target.CustomAttributes;
		public override bool ContainsGenericParameters => target.ContainsGenericParameters;
		public override Type DeclaringType => target.DeclaringType;
		public override GenericParameterAttributes GenericParameterAttributes => target.GenericParameterAttributes;
		public override Type[] GenericTypeArguments => target.GenericTypeArguments;
		public override bool IsEnum => target.IsEnum;
		public override MemberTypes MemberType => target.MemberType;
		public override Type ReflectedType => target.ReflectedType;

		public override Type[] GetGenericArguments() {
			return target.GetGenericArguments();
		}

		public override Type GetGenericTypeDefinition() {
			return target.GetGenericTypeDefinition();
		}

		public override Type MakeArrayType() {
			return ReflectionFaker.FakeArrayType(this);
		}

		public override Type MakeArrayType(int rank) {
			return ReflectionFaker.FakeArrayType(this);
		}

		public override FieldInfo GetField(string name, BindingFlags bindingAttr) {
			EnsureInitialized();
			var field = target.GetField(name, bindingAttr);
			if(field != null && declaredMembers.TryGetValue(field, out var result)) {
				return result as FieldInfo;
			}
			return field;
		}

		public override FieldInfo[] GetFields(BindingFlags bindingAttr) {
			EnsureInitialized();
			var fields = target.GetFields(bindingAttr);
			bool flag = false;
			for(int i = 0; i < fields.Length; i++) {
				if(fields[i] != null && declaredMembers.TryGetValue(fields[i], out var field)) {
					fields[i] = field as FieldInfo;
					if(field == null) {
						flag = true;
					}
				}
			}
			if(flag) {
				return fields.Where(f => f != null).ToArray();
			}
			return fields;
		}

		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
			if(types == null)
				types = Type.EmptyTypes;
			EnsureInitialized();
			var method = target.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
			if(method != null && declaredMembers.TryGetValue(method, out var result)) {
				return result as MethodInfo;
			}
			return method;
		}

		public override MethodInfo[] GetMethods(BindingFlags bindingAttr) {
			EnsureInitialized();
			var methods = target.GetMethods(bindingAttr);
			bool flag = false;
			for(int i = 0; i < methods.Length; i++) {
				if(methods[i] != null && declaredMembers.TryGetValue(methods[i], out var method)) {
					methods[i] = method as MethodInfo;
					if(method == null) {
						flag = true;
					}
				}
			}
			if(flag) {
				return methods.Where(f => f != null).ToArray();
			}
			return methods;
		}

		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) {
			if(types == null)
				types = Type.EmptyTypes;
			EnsureInitialized();
			var property = target.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
			if(property != null && declaredMembers.TryGetValue(property, out var result)) {
				return result as PropertyInfo;
			}
			return property;
		}

		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) {
			EnsureInitialized();
			var properties = target.GetProperties(bindingAttr);
			bool flag = false;
			for(int i = 0; i < properties.Length; i++) {
				if(properties[i] != null && declaredMembers.TryGetValue(properties[i], out var property)) {
					properties[i] = property as PropertyInfo;
					if(property == null) {
						flag = true;
					}
				}
			}
			if(flag) {
				return properties.Where(f => f != null).ToArray();
			}
			return properties;
		}

		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) {
			if(types == null)
				types = Type.EmptyTypes;
			EnsureInitialized();
			var ctor = target.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
			if(ctor != null && declaredMembers.TryGetValue(ctor, out var result)) {
				return result as ConstructorInfo;
			}
			return ctor;
		}

		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) {
			EnsureInitialized();
			var ctors = target.GetConstructors(bindingAttr);
			bool flag = false;
			for(int i = 0; i < ctors.Length; i++) {
				if(ctors[i] != null && declaredMembers.TryGetValue(ctors[i], out var ctor)) {
					ctors[i] = ctor as ConstructorInfo;
					if(ctor == null) {
						flag = true;
					}
				}
			}
			if(flag) {
				return ctors.Where(f => f != null).ToArray();
			}
			return ctors;
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr) {
			EnsureInitialized();
			var members = target.GetMembers(bindingAttr);
			bool flag = false;
			for(int i = 0; i < members.Length; i++) {
				if(members[i] != null && declaredMembers.TryGetValue(members[i], out var member)) {
					members[i] = member;
					if(member == null) {
						flag = true;
					}
				}
			}
			if(flag) {
				return members.Where(f => f != null).ToArray();
			}
			return members;
		}

		public override IEnumerable<MemberInfo> GetRuntimeMembers(BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			return GetMembers(bindingAttr);
		}

		protected override TypeAttributes GetAttributeFlagsImpl() {
			return target.Attributes;
		}

		public override int GetArrayRank() {
			return target.GetArrayRank();
		}

		public override object[] GetCustomAttributes(bool inherit) {
			return target.GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return target.GetCustomAttributes(attributeType, inherit);
		}

		public override Type GetInterface(string name, bool ignoreCase) {
			return target.GetInterface(name, ignoreCase);
		}

		public override Type[] GetInterfaces() {
			return target.GetInterfaces();
		}

		protected override bool IsArrayImpl() {
			return target.IsArray;
		}

		public abstract Type GetNativeType();
	}
}