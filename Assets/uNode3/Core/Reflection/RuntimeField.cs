using System;
using System.Reflection;

namespace MaxyGames.UNode {
	/// <summary>
	/// A generic implementation of RuntimeField with T is a instance of the field
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class RuntimeField<T> : RuntimeField {
		public readonly T target;

		public override Type FieldType => typeof(T);

		public RuntimeField(RuntimeType owner, T target) : base(owner) {
			this.target = target;
		}

		public override object[] GetCustomAttributes(bool inherit) {
			if(typeof(T) is IAttributeSystem) {
				return (target as IAttributeSystem).GetAttributes();
			}
			return new object[0];
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			if(typeof(T) is IAttributeSystem) {
				return (target as IAttributeSystem).GetAttributes(attributeType);
			}
			return new object[0];
		}
	}

	public abstract class RuntimeField : FieldInfo, IRuntimeMember {
		public readonly RuntimeType owner;

        public RuntimeField(RuntimeType owner) {
			this.owner = owner;
		}

		public override FieldAttributes Attributes {
			get {
				if(owner.IsAbstract && owner.IsSealed) {
					return FieldAttributes.Public | FieldAttributes.Static;
				}
				return FieldAttributes.Public;
			}
		}

		public override RuntimeFieldHandle FieldHandle => default;

		public override Type DeclaringType => owner;

		public override Type ReflectedType => DeclaringType;
		
		public override int MetadataToken => 0;

		public override bool IsDefined(Type attributeType, bool inherit) {
			return false;
		}
	}
}