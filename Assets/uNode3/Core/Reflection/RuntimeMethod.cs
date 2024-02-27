using System;
using System.Reflection;

namespace MaxyGames.UNode {
	/// <summary>
	/// A generic implementation of RuntimeMethod with T is a instance of the method
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class RuntimeMethod<T> : RuntimeMethod {
		public T target;
		public override Type ReturnType => typeof(T);

		public RuntimeMethod(RuntimeType owner, T target) : base(owner) {
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

	// public class RuntimeDelegateEvent : GenericRuntimeMethod<Func<object, object[], object>> {
	// 	private MethodInfo Method => target.Method;

	// 	public override string Name => Method.Name;

	// 	public RuntimeDelegateEvent(RuntimeType owner, Func<object, object[], object> target) : base(owner, target) { }

	// 	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
	// 		return target.Invoke(obj, parameters);
	// 	}

	// 	public override ParameterInfo[] GetParameters() {
	// 		return Method.GetParameters();
	// 	}
	// }


	// public class RuntimeDelegate : RuntimeDelegate<System.Delegate> {
	// 	public RuntimeDelegate(RuntimeType owner, Delegate target) : base(owner, target) {
	// 	}
	// }

	// public class RuntimeDelegate<T> : GenericRuntimeMethod<T> where T : System.Delegate {
	// 	private MethodInfo Method => target.Method;

	// 	public override string Name => Method.Name;
		
	// 	public override Type ReturnType => Method.ReturnType;

	// 	public RuntimeDelegate(RuntimeType owner, T target) : base(owner, target) { }

	// 	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
	// 		return target.DynamicInvoke(parameters);
	// 	}

	// 	public override ParameterInfo[] GetParameters() {
	// 		return Method.GetParameters();
	// 	}
	// }

	public abstract class RuntimeMethod : MethodInfo, IRuntimeMember {
		public readonly RuntimeType owner;

		public RuntimeMethod(RuntimeType owner) {
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

		public override Type ReturnType => throw new NotImplementedException();

		public override ICustomAttributeProvider ReturnTypeCustomAttributes => null;
		public override RuntimeMethodHandle MethodHandle => default;
		public override int MetadataToken => 0;

		public override MethodInfo GetBaseDefinition() {
			return this;
		}

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

		public override ParameterInfo ReturnParameter => null;

		public override Type[] GetGenericArguments() {
			return Type.EmptyTypes;
		}

		public override Delegate CreateDelegate(Type delegateType) {
			throw new NotSupportedException();
		}

		public override Delegate CreateDelegate(Type delegateType, object target) {
			throw new NotSupportedException();
		}

		public override MethodInfo GetGenericMethodDefinition() {
			return null;
		}

		public override MethodInfo MakeGenericMethod(params Type[] typeArguments) {
			throw new NotSupportedException();
		}
	}
}