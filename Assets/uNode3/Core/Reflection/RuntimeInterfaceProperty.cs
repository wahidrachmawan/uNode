using System;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	//public class RuntimeInterfaceProperty : RuntimeProperty<InterfaceProperty>, ISummary {
 //       public RuntimeInterfaceProperty(RuntimeType owner, InterfaceProperty target) : base(owner, target) { }

	//	public override string Name => target.name;
	//	public override Type PropertyType => target.ReturnType();

	//	public string GetSummary() {
	//		return target.GetSummary();
	//	}

	//	private RuntimePropertyGetMethod _getMethod;
	//	public override MethodInfo GetGetMethod(bool nonPublic) {
	//		if(_getMethod == null && target.CanGetValue()) {
	//			_getMethod = new RuntimePropertyGetMethod(owner, this);
	//		}
	//		return target.CanGetValue() ? _getMethod : null;
	//	}

	//	private RuntimePropertySetMethod _setMethod;
	//	public override MethodInfo GetSetMethod(bool nonPublic) {
	//		if(_setMethod == null && target.CanSetValue()) {
	//			_setMethod = new RuntimePropertySetMethod(owner, this);
	//		}
	//		return target.CanSetValue() ? _setMethod : null;
	//	}

	//	public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
	//		if(obj == null) {
	//			throw new NullReferenceException("The instance member cannot be null");
	//		}
	//		return DoGetValue(obj);
	//	}

	//	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
	//		if(obj == null) {
	//			throw new NullReferenceException("The instance member cannot be null");
	//		}
	//		DoSetValue(obj, value);
	//	}

	//	protected object DoGetValue(object obj) {
	//		if(obj is IRuntimeProperty runtime) {
	//			return runtime.GetProperty(target.name);
	//		}
	//		var data = (obj as IGraph).GetPropertyData(target.name);
	//		if(data == null) {
	//			throw new Exception($"Property: {target.name} not found in object: {obj}");
	//		}
	//		return data.Get();
	//	}

	//	protected void DoSetValue(object obj, object value) {
	//		if(obj is IRuntimeProperty runtime) {
	//			runtime.SetProperty(target.name, value);
	//			return;
	//		}
	//		var data = (obj as IGraph).GetPropertyData(target.name);
	//		if(data == null) {
	//			throw new Exception($"Property: {target.name} not found in object: {obj}");
	//		}
	//		data.Set(value);
	//	}
	//}
}