using System;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
    public class RuntimeNativeField : RuntimeField<VariableRef>, ISummary, IRuntimeMemberWithRef, INativeField {
		public RuntimeNativeField(RuntimeNativeType owner, VariableRef target) : base(owner, target) {

		}

		private Exception throwIfNotCompiled => new Exception($"The graph: {target.reference.graphContainer} need to be compiled first.");

		#region Functions
		private FieldInfo m_nativeField;
		public FieldInfo GetNativeField() {
			if(m_nativeField == null) {
				var type = (owner as RuntimeNativeType).GetNativeType();
				if(type != null) {
					m_nativeField = type.GetFieldCached(Name);
				}
			}
			return m_nativeField;
		}

		public MemberInfo GetNativeMember() {
			return GetNativeField();
		}

		public BaseReference GetReference() {
			return target;
		}

		public string GetSummary() {
			return target.GetSummary();
		}
		#endregion

		public override Type FieldType => target.type;

		public override string Name => target.name;

		public override FieldAttributes Attributes {
			get {
				var reference = target.reference;
				if(reference != null) {
					FieldAttributes att = FieldAttributes.Public;
					if(reference.modifier.Private) {
						att = FieldAttributes.Private;
					}
					if(reference.modifier.Static) {
						att |= FieldAttributes.Static;
					}
					else if(owner.IsAbstract && owner.IsSealed) {
						att |= FieldAttributes.Static;
					}
					if(reference.modifier.ReadOnly) {
						att |= FieldAttributes.InitOnly;
					}
					return att;
				}
				return base.Attributes;
			}
		}

		public override object GetValue(object obj) {
			var field = GetNativeField();
			if(field != null) {
				return field.GetValueOptimized(obj);
			} else {
				throw throwIfNotCompiled;
			}
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
			var field = GetNativeField();
			if(field != null) {
				field.SetValueOptimized(obj, value);
			}
			else {
				throw throwIfNotCompiled;
			}
		}
	}
}