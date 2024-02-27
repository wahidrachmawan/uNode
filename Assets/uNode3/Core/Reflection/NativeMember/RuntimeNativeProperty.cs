using System;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	public class RuntimeNativeProperty : RuntimeProperty<PropertyRef>, ISummary, IRuntimeMemberWithRef, INativeProperty {
        public RuntimeNativeProperty(RuntimeNativeType owner, PropertyRef target) : base(owner, target) { }

		private Exception throwIfNotCompiled => new Exception($"The graph: {target.reference.graphContainer} need to be compiled first.");

		public override string Name => target.name;

		public override Type PropertyType => target.ReturnType();

		public string GetSummary() {
			return target.GetSummary();
		}

		private RuntimeNativePropertyGetMethod _getMethod;
		public override MethodInfo GetGetMethod(bool nonPublic) {
			if(_getMethod == null && target.CanGetValue()) {
				_getMethod = new RuntimeNativePropertyGetMethod(owner, this);
			}
			return target.CanGetValue() ? _getMethod : null;
		}

		private RuntimeNativePropertySetMethod _setMethod;
		public override MethodInfo GetSetMethod(bool nonPublic) {
			if(_setMethod == null && target.CanSetValue()) {
				_setMethod = new RuntimeNativePropertySetMethod(owner, this);
			}
			return target.CanSetValue() ? _setMethod : null;
		}

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			var member = GetNativeProperty();
			if(member != null) {
				return member.GetValueOptimized(obj);
			}
			else {
				throw throwIfNotCompiled;
			}
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			var member = GetNativeProperty();
			if(member != null) {
				member.SetValueOptimized(obj, value);
			}
			else {
				throw throwIfNotCompiled;
			}
		}

		public BaseReference GetReference() {
			return target;
		}

		private PropertyInfo m_nativeProperty;
		public PropertyInfo GetNativeProperty() {
			if(m_nativeProperty == null) {
				var type = (owner as RuntimeNativeType).GetNativeType();
				if(type != null) {
					m_nativeProperty = type.GetPropertyCached(Name);
				}
			}
			return m_nativeProperty;
		}

		public MemberInfo GetNativeMember() {
			return GetNativeProperty();
		}
	}

	public class RuntimeNativePropertyGetMethod : RuntimeMethod<RuntimeProperty> {
		private readonly RuntimeNativeProperty nativeMember;

		public override string Name => target.Name.AddFirst("get_");
		public override Type ReturnType => target.PropertyType;

		public override MethodAttributes Attributes {
			get {
				var att = MethodAttributes.Public;
				var modifier = nativeMember.target.reference?.modifier;
				if(modifier != null) {
					if(modifier.Private) {
						att = MethodAttributes.Private;
					}
					if(modifier.Static) {
						att |= MethodAttributes.Static;
					}
					if(modifier.Abstract) {
						att |= MethodAttributes.Abstract;
					}
				}
				if(owner.IsAbstract && owner.IsSealed) {
					att |= MethodAttributes.Static;
				}
				return att;
			}
		}

		public RuntimeNativePropertyGetMethod(RuntimeType owner, RuntimeNativeProperty target) : base(owner, target) {
			nativeMember = target;
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return target.GetValue(obj);
		}

		public override ParameterInfo[] GetParameters() {
			return new ParameterInfo[0];
		}
	}

	public class RuntimeNativePropertySetMethod : RuntimeMethod<RuntimeProperty> {
		private readonly RuntimeNativeProperty nativeMember;
		public override string Name => target.Name.AddFirst("set_");
		public override Type ReturnType => target.PropertyType;

		public override MethodAttributes Attributes {
			get {
				var att = MethodAttributes.Public;
				var modifier = nativeMember.target.reference?.modifier;
				if(modifier != null) {
					if(modifier.Private) {
						att = MethodAttributes.Private;
					}
					if(modifier.Static) {
						att |= MethodAttributes.Static;
					}
					if(modifier.Abstract) {
						att |= MethodAttributes.Abstract;
					}
				}
				if(owner.IsAbstract && owner.IsSealed) {
					att |= MethodAttributes.Static;
				}
				return att;
			}
		}

		public RuntimeNativePropertySetMethod(RuntimeType owner, RuntimeNativeProperty target) : base(owner, target) {
			nativeMember = target;
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			target.SetValueOptimized(obj, parameters[0]);
			return null;
		}

		public override ParameterInfo[] GetParameters() {
			return new ParameterInfo[] { new RuntimeParameterInfo("value", target.PropertyType) };
		}
	}
}