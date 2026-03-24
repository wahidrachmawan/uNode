using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MaxyGames.UNode {
	class FakeProperty : RuntimeProperty<PropertyInfo>, IFakeMember {
		private readonly Type propertyType;

		public FakeProperty(FakeType owner, PropertyInfo target, Type propertyType) : base(owner, target) {
			//if(propertyType is RuntimeGraphType) {
			//	propertyType = ReflectionFaker.FakeGraphType(propertyType as RuntimeGraphType);
			//}
			this.propertyType = propertyType;
		}

		public override Type PropertyType => propertyType ?? target.PropertyType;
		public override string Name => target.Name;

		public object Original => target;
		public bool IsNativeMember => ReflectionUtils.IsNativeType(owner);

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			return target.GetValueOptimized(obj);
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			target.SetValueOptimized(obj, value);
		}

		public override MethodInfo GetGetMethod(bool nonPublic) {
			//TODO: proper handling for GetGetMethod for fake properties
			return target.GetGetMethod(nonPublic);
		}

		public override MethodInfo GetSetMethod(bool nonPublic) {
			//TODO: proper handling for GetSetMethod for fake properties
			return target.GetSetMethod(nonPublic);
		}

		public override string ToString() {
			return PropertyType.ToString() + " " + Name;
		}
	}

}