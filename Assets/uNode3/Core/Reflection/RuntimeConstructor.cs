using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	/// <summary>
	/// A generic implementation of RuntimeMethod with T is a instance of the method
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class RuntimeConstructor<T> : RuntimeConstructor {
		public T target;

		public RuntimeConstructor(RuntimeType owner, T target) : base(owner) {
			this.target = target;
		}

		public override object[] GetCustomAttributes(bool inherit) {
			if (target is IAttributeSystem) {
				return (target as IAttributeSystem).GetAttributes();
			}
			return new object[0];
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			if (target is IAttributeSystem) {
				return (target as IAttributeSystem).GetAttributes(attributeType);
			}
			return new object[0];
		}
	}

	public abstract class RuntimeConstructor : ConstructorInfo, IRuntimeMember {
		public readonly RuntimeType owner;

		public RuntimeConstructor(RuntimeType owner) {
			this.owner = owner;
		}

		public override MethodAttributes Attributes {
			get {
				if(owner.IsAbstract && owner.IsSealed) {
					return MethodAttributes.Public | MethodAttributes.Static;
				}
				return MethodAttributes.Public;
			}
		}
		
		public override Type DeclaringType => owner;
		public override Type ReflectedType => DeclaringType;
		public override RuntimeMethodHandle MethodHandle => default;
		public override int MetadataToken => 0;

		public override object[] GetCustomAttributes(bool inherit) {
			return new object[0];
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return new object[0];
		}

		public override MethodImplAttributes GetMethodImplementationFlags() {
			return MethodImplAttributes.Runtime;
		}

		public override bool IsDefined(Type attributeType, bool inherit) {
			return false;
		}

		public override Type[] GetGenericArguments() {
			return Type.EmptyTypes;
		}
	}

	internal class RuntimeDefaultConstructor : ConstructorInfo {
		public readonly Type type;
		
		public RuntimeDefaultConstructor(Type type) {
			this.type = type;
		}

		public override MethodAttributes Attributes => MethodAttributes.Public | MethodAttributes.PrivateScope | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
		public override CallingConventions CallingConvention => CallingConventions.Standard | CallingConventions.HasThis;
		public override bool ContainsGenericParameters => false;
		public override IEnumerable<CustomAttributeData> CustomAttributes => System.Linq.Enumerable.Empty<CustomAttributeData>();
		public override Type[] GetGenericArguments() => Type.EmptyTypes;


		public override RuntimeMethodHandle MethodHandle { get; }
		public override Type DeclaringType => type;
		public override string Name => "ctor";
		public override Type ReflectedType { get; }

		public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();

		public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.Runtime;

		public override ParameterInfo[] GetParameters() => Array.Empty<ParameterInfo>();

		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return Activator.CreateInstance(type, true);
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return Activator.CreateInstance(type, true);
		}

		public override bool IsDefined(Type attributeType, bool inherit) => false;
	}
}