using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MaxyGames.UNode {
	class FakeField : RuntimeField<FieldInfo>, IFakeMember {
		private readonly Type fieldType;
		public FakeField(FakeType owner, FieldInfo target, Type fieldType) : base(owner, target) {
			//if(fieldType is RuntimeGraphType) {
			//	fieldType = ReflectionFaker.FakeGraphType(fieldType as RuntimeGraphType);
			//}
			this.fieldType = fieldType;
		}

		public override Type FieldType => fieldType ?? target.FieldType;

		public override string Name => target.Name;

		public override object GetValue(object obj) {
			return target.GetValue(obj);
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
			target.SetValue(obj, value);
		}

		public override string ToString() {
			return FieldType.ToString() + " " + Name;
		}
	}
}