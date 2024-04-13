using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MaxyGames.UNode;
using MaxyGames.UNode.GenericResolver;
using UnityEngine;

//GameObject.GetComponent<T>()
[assembly: RegisterGenericMethodResolver(typeof(GetComponentResolver), typeof(GameObject), nameof(GameObject.GetComponent))]
[assembly: RegisterGenericMethodResolver(typeof(GetComponentResolver), typeof(Component), nameof(Component.GetComponent))]

namespace MaxyGames.UNode.GenericResolver {
	public class GetComponentResolver : GenericMethodResolver {
		private Func<object, object[], object> func;

		protected override void OnRuntimeInitialize() {
			var type = OpenMethodInfo.DeclaringType;
			var compType = RuntimeMethodInfo.GetGenericArguments()[0];
			if(type == typeof(GameObject)) {
				func = (obj, parameters) => {
					return obj.ConvertTo<GameObject>().GetGeneratedComponent(compType);
				};
			}
			else if(type == typeof(Component)) {
				func = (obj, parameters) => {
					return obj.ConvertTo<Component>().GetGeneratedComponent(compType);
				};
			}
			else {
				throw new InvalidOperationException();
			}
		}


		protected override object DoInvoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) {
			return func(obj, parameters);
		}

		protected override void DoGenerateCode(List<string> members, string[] parameters) {
			//Register namespace to make sure Extensions work for GameObject or Component target type.
			CG.RegisterUsingNamespace("MaxyGames.UNode");
			//Get the component type
			var compType = RuntimeMethodInfo.GetGenericArguments()[0];
			//Do generate code and add it to member list
			if(CG.generatePureScript) {
				members.Add(CG.Invoke(string.Empty, nameof(uNodeHelper.GetGeneratedComponent), new[] { compType }));
			}
			else {
				members.Add(CG.Invoke(string.Empty, nameof(uNodeHelper.GetGeneratedComponent), new[] { CG.GetUniqueNameForType(compType as RuntimeType) }));
			}
		}
	}
}