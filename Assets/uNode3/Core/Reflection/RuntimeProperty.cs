using System;
using System.Reflection;

namespace MaxyGames.UNode {
	/// <summary>
	/// A generic implementation of RuntimeProperty with T is a instance of the property
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class RuntimeProperty<T> : RuntimeProperty {
		public readonly T target;

        public RuntimeProperty(RuntimeType owner, T target) : base(owner) {
			this.target = target;
		}
	}

	/// <summary>
	/// The base class for all RuntimeProperty
	/// </summary>
	public abstract class RuntimeProperty : PropertyInfo, IRuntimeMember {
		public readonly RuntimeType owner;

        public RuntimeProperty(RuntimeType owner) {
			this.owner = owner;
		}

		public override PropertyAttributes Attributes => PropertyAttributes.None;

		public override bool CanRead => GetGetMethod(false) != null;

		public override bool CanWrite => GetSetMethod(false) != null;

		public override Type DeclaringType => owner;

		public override Type ReflectedType => DeclaringType;
		public override int MetadataToken => 0;

		public override MethodInfo[] GetAccessors(bool nonPublic) {
			return new MethodInfo[0];
		}

		public override object[] GetCustomAttributes(bool inherit) {
			return new object[0];
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return new object[0];
		}

		public override MethodInfo GetGetMethod(bool nonPublic) {
			return null;
		}

		public override MethodInfo GetSetMethod(bool nonPublic) {
			return null;
		}

		public override ParameterInfo[] GetIndexParameters() {
			return new ParameterInfo[0];
		}

		public override bool IsDefined(Type attributeType, bool inherit) {
			return false;
		}
	}
}