using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace MaxyGames.UNode {
	public abstract class RuntimeParameter<T> : RuntimeParameter {
		public readonly T target;

		public override Type ParameterType => typeof(T);

        public RuntimeParameter(T target) {
			this.target = target;
		}
	}

	public class RuntimeGraphParameter : RuntimeParameter<ParameterData> {
		public RuntimeGraphParameter(ParameterData target) : base(target) { }

		public override string Name => target.name;

		public override Type ParameterType {
			get {
				switch(target.refKind) {
					case RefKind.Out:
					case RefKind.Ref:
					case RefKind.In:
						return ReflectionUtils.MakeByRefType(target.type);
				}
				return target.Type;
			}
		}

		public override ParameterAttributes Attributes {
			get {
				if (target.refKind == RefKind.Out) {
					return ParameterAttributes.Out;
				}
				//else if (target.refKind == RefKind.Ref) {
				//	return ParameterAttributes.Retval;
				//}
				else if(target.refKind == RefKind.In) {
					return ParameterAttributes.In;
				}
				return ParameterAttributes.None;
			}
		}
	}

	public class RuntimeParameterInfo : RuntimeParameter {
		private readonly string name;
		private readonly Type type;
		private readonly ParameterAttributes attributes = ParameterAttributes.None;

		public RuntimeParameterInfo(Type type)  {
			this.name = type.Name.ToLower();
			this.type = type;
		}

		public RuntimeParameterInfo(string name, Type type) {
			this.name = name;
			this.type = type;
		}

		public RuntimeParameterInfo(string name, Type type, ParameterAttributes attributes) {
			this.name = name;
			this.type = type;
			this.attributes = attributes;
		}

		public override string Name => name;
		public override Type ParameterType => type;
		public override ParameterAttributes Attributes => attributes;
	}

	public abstract class RuntimeParameter : ParameterInfo {
		public override ParameterAttributes Attributes => ParameterAttributes.None;

		public override object DefaultValue => null;

		public override bool HasDefaultValue => false;

		public override MemberInfo Member => base.Member;

		public override int MetadataToken => 0;

		public override IEnumerable<CustomAttributeData> CustomAttributes => base.CustomAttributes;

		public override int Position => base.Position;

		public override object RawDefaultValue => base.RawDefaultValue;

		public override object[] GetCustomAttributes(bool inherit) {
			return GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return base.GetCustomAttributes(attributeType, inherit);
		}

		public override IList<CustomAttributeData> GetCustomAttributesData() {
			return base.GetCustomAttributesData();
		}

		public override bool IsDefined(Type attributeType, bool inherit) {
			return base.IsDefined(attributeType, inherit);
		}

		public override string ToString() {
			return Name;
		}
	}
}