using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MaxyGames.UNode {
	class FakeMethod : RuntimeMethod<MethodInfo>, IFakeMember {
		private readonly Type returnType;
		private readonly ParameterInfo[] parameters;

		public FakeMethod(FakeType owner, MethodInfo target, Type returnType, ParameterInfo[] parameters) : base(owner, target) {
			//if(returnType is RuntimeGraphType) {
			//	returnType = ReflectionFaker.FakeGraphType(returnType as RuntimeGraphType);
			//}
			this.returnType = returnType;
			this.parameters = parameters;
		}

		public override string Name => target.Name;

		public override ParameterInfo[] GetParameters() {
			return parameters ?? target.GetParameters();
		}

		public override Type ReturnType => returnType ?? target.ReturnType;

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return target.Invoke(obj, invokeAttr, binder, parameters, culture);
		}

		public override string ToString() {
			return ReturnType.ToString() + " " + Name + "(" + string.Join(", ", GetParameters().Select(p => p.ParameterType.ToString())) + ")";
		}
	}

	class FakeNativeMethod : MethodInfo, IRuntimeMember, IFakeMember, INativeMethod, IGenericMethodWithResolver {
		public readonly MethodInfo target;
		public readonly Type[] typeArguments;

		private ParameterInfo[] parameters;
		private Type returnType;

		public override ICustomAttributeProvider ReturnTypeCustomAttributes => target.ReturnTypeCustomAttributes;
		public override MethodAttributes Attributes => target.Attributes;
		public override RuntimeMethodHandle MethodHandle => target.MethodHandle;
		public override Type DeclaringType => target.DeclaringType;
		public override string Name => target.Name;
		public override Type ReflectedType => target.ReflectedType;
		public override Type ReturnType => returnType ?? target.ReturnType;
		public override bool IsGenericMethod => true;

		public FakeNativeMethod(MethodInfo nativeMethod, Type[] typeArguments) {
			if(nativeMethod.IsConstructedGenericMethod)
				throw new InvalidOperationException();
			var rawTypeArguments = nativeMethod.GetGenericArguments();
			if(typeArguments.Length != rawTypeArguments.Length) {
				throw new InvalidOperationException("Invalid given type arguments");
			}
			target = nativeMethod;
			this.typeArguments = typeArguments;

			var param = nativeMethod.GetParameters();
			parameters = new ParameterInfo[param.Length];
			for(int i = 0; i < param.Length; i++) {
				var ptype = param[i].ParameterType;
				if(ptype.ContainsGenericParameters || ptype.IsGenericParameter) {
					var constructedType = ReflectionUtils.ReplaceUnconstructedType(ptype, type => {
						for(int i = 0; i < rawTypeArguments.Length; i++) {
							if(rawTypeArguments[i] == type) {
								return typeArguments[i];
							}
						}
						throw new Exception("Couldn't resolve generic parameter of: " + type + "\nNo matching arguments found");
					});
					parameters[i] = new RuntimeParameterInfo(param[i].Name, constructedType, param[i].Attributes);
				}
				else {
					parameters[i] = param[i];
				}
			}
			returnType = ReflectionUtils.ReplaceUnconstructedType(nativeMethod.ReturnType, type => {
				for(int i = 0; i < rawTypeArguments.Length; i++) {
					if(rawTypeArguments[i] == type) {
						return typeArguments[i];
					}
				}
				throw new Exception("Couldn't resolve generic parameter of: " + type + "\nNo matching arguments found");
			});
		}

		public override MethodInfo GetBaseDefinition() {
			return target.GetBaseDefinition();
		}

		public override object[] GetCustomAttributes(bool inherit) {
			return target.GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return target.GetCustomAttributes(attributeType, inherit);
		}

		public override MethodImplAttributes GetMethodImplementationFlags() {
			return target.GetMethodImplementationFlags();
		}

		public override ParameterInfo[] GetParameters() {
			return parameters;
		}

		public override Type[] GetGenericArguments() {
			var result = new Type[typeArguments.Length];
			Array.Copy(typeArguments, result, result.Length);
			return result;
		}

		public override MethodInfo GetGenericMethodDefinition() {
			return target;
		}

		public override bool IsConstructedGenericMethod => true;
		public override bool IsGenericMethodDefinition => false;

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			if(IsNativeMethod) {
				return GetNativeMethod().InvokeOptimized(obj, parameters);
			}
			var resolver = GetResolver();
			resolver.EnsureRuntimeInitialized();
			return resolver.Invoke(obj, invokeAttr, binder, parameters, culture);
		}

		private bool? m_isNativeMethod;
		/// <summary>
		/// True if this method is native method
		/// </summary>
		public bool IsNativeMethod {
			get {
				if(m_isNativeMethod == null) {
					m_isNativeMethod = true;
					for(int i = 0; i < typeArguments.Length; i++) {
						if(ReflectionUtils.IsNativeType(typeArguments[i]) == false) {
							m_isNativeMethod = false;
							break;
						}
					}
				}
				return m_isNativeMethod.Value;
			}
		}

		private GenericMethodResolver m_runtimeResolver;
		public GenericMethodResolver GetResolver() {
			if(m_runtimeResolver == null) {
				var method = GetNativeMethod();
				//if(method.IsGenericMethodDefinition) {
				//	throw new Exception($"The generic method of {Name} is not supported in `Reflection` mode.");
				//}
				m_runtimeResolver = GenericMethodResolver.GetResolver(method, this);
			}
			//else {
			//	GenericMethodResolver.GetResolver(GetNativeMethod(), this);
			//}
			return m_runtimeResolver;
		}

		public override bool IsDefined(Type attributeType, bool inherit) {
			return target.IsDefined(attributeType, inherit);
		}

		private MethodInfo m_nativeMethod;
		public MethodInfo GetNativeMethod() {
			if(m_nativeMethod == null) {
				Type[] types = new Type[typeArguments.Length];
				for(int i = 0; i < typeArguments.Length; i++) {
					types[i] = ReflectionUtils.GetNativeType(typeArguments[i], true);
				}
				m_nativeMethod = target.MakeGenericMethod(types);
			}
			return m_nativeMethod;
		}

		public MemberInfo GetNativeMember() {
			return GetNativeMethod();
		}
	}
}