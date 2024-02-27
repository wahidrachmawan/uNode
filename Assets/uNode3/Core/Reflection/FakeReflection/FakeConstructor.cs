using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MaxyGames.UNode {
	class FakeConstructor : RuntimeConstructor<ConstructorInfo> {
		private readonly ParameterInfo[] parameters;

		public FakeConstructor(FakeType owner, ConstructorInfo target, ParameterInfo[] parameters) : base(owner, target) {
			this.parameters = parameters;
		}

		public override string Name => target.Name;
		public override Type DeclaringType => owner;

		public override ParameterInfo[] GetParameters() {
			return parameters ?? target.GetParameters();
		}

		public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return target.Invoke(invokeAttr, binder, parameters, culture);
		}

		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return target.Invoke(obj, invokeAttr, binder, parameters, culture);
		}

		public override string ToString() {
			return "ctor" + Name + "(" + string.Join(", ", GetParameters().Select(p => p.ParameterType.ToString())) + ")";
		}
	}
}