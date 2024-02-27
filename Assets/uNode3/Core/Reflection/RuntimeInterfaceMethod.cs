using System;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace MaxyGames.UNode {
	//public class RuntimeInterfaceMethod : RuntimeMethod<InterfaceFunction>, ISummary {
	//	private Type[] functionTypes;
		
	//	public RuntimeInterfaceMethod(RuntimeType owner, InterfaceFunction target) : base(owner, target) {
	//		this.target = target;
	//	}

	//	public override string Name => target.name;
	//	public override Type ReturnType => target.ReturnType();

	//	public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
	//		if(IsStatic) {
	//			if(owner is RuntimeGraphType runtimeGraph) {
	//				if(runtimeGraph.target != null) {
	//					return DoInvoke(runtimeGraph.target, parameters);
	//				}
	//				throw new NullReferenceException("The graph reference cannot be null");
	//			}
	//			throw new NotImplementedException();
	//		} else if(obj == null) {
	//			throw new NullReferenceException("The instance member cannot be null");
	//		}
	//		return DoInvoke(obj, parameters);
	//	}

	//	protected object DoInvoke(object obj, object[] parameters) {
	//		if(obj is IRuntimeFunction runtime) {
	//			return runtime.InvokeFunction(target.name, GetParamTypes(), parameters);
	//		}
	//		var data = (obj as IGraph).GetFunction(target.name, GetParamTypes());
	//		if(data == null) {
	//			throw new Exception($"Function: {target.name} not found in object: {obj}");
	//		}
	//		return data.Invoke(parameters);
	//	}

	//	public override ParameterInfo[] GetParameters() {
	//		var types = target.parameters;
	//		if(types != null) {
	//			var param = new ParameterInfo[types.Length];
	//			for (int i = 0; i < types.Length;i++) {
	//				param[i] = new RuntimeGraphParameter(types[i]);
	//			}
	//			return param;
	//		}
	//		return new ParameterInfo[0];
	//	}

	//	public string GetSummary() {
	//		return target.GetSummary();
	//	}

	//	private Type[] GetParamTypes() {
	//		if(functionTypes == null) {
	//			functionTypes = target.parameters.Select(p => p.Type).ToArray();
	//		}
	//		return functionTypes;
	//	}
	//}
}