using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MaxyGames.UNode;
using MaxyGames.UNode.GenericResolver;
using UnityEngine;

//GameObject.GetComponentInParent<T>()
//GameObject.GetComponentInParent<T>(bool)
[assembly: RegisterGenericMethodResolver(typeof(GetComponentInParentResolver), typeof(GameObject), nameof(GameObject.GetComponentInParent))]
[assembly: RegisterGenericMethodResolver(typeof(GetComponentInParentResolver), typeof(GameObject), nameof(GameObject.GetComponentInParent), new[] { typeof(bool) })]
[assembly: RegisterGenericMethodResolver(typeof(GetComponentInParentResolver), typeof(Component), nameof(Component.GetComponentInParent))]
[assembly: RegisterGenericMethodResolver(typeof(GetComponentInParentResolver), typeof(Component), nameof(Component.GetComponentInParent), new[] { typeof(bool) })]

namespace MaxyGames.UNode.GenericResolver {
	public class GetComponentInParentResolver : GenericMethodResolver {
		private Func<object, object[], object> func;

		protected override void OnRuntimeInitialize() {
			var type = OpenMethodInfo.DeclaringType;
			var compType = RuntimeMethodInfo.GetGenericArguments()[0];
			if(type == typeof(GameObject)) {
				func = (obj, parameters) => {
					switch(parameters.Length) {
						case 0:
							return obj.ConvertTo<GameObject>().GetGeneratedComponentInParent(compType);
						case 1:
							return obj.ConvertTo<GameObject>().GetGeneratedComponentInParent(compType, parameters[0].ConvertTo<bool>());
					}
					throw new InvalidOperationException();
				};
			}
			else if(type == typeof(Component)) {
				func = (obj, parameters) => {
					switch(parameters.Length) {
						case 0:
							return obj.ConvertTo<Component>().GetGeneratedComponentInParent(compType);
						case 1:
							return obj.ConvertTo<Component>().GetGeneratedComponentInParent(compType, parameters[0].ConvertTo<bool>());
					}
					throw new InvalidOperationException();
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
				if(parameters.Length == 0) {
					var result = CG.Invoke(string.Empty, nameof(uNodeHelper.GetGeneratedComponentInParent), new[] { compType });
					members.Add(result);
				}
				else if(parameters.Length == 1) {
					var result = CG.Invoke(string.Empty, nameof(uNodeHelper.GetGeneratedComponentInParent), new[] { compType as RuntimeType }, new[] { parameters[0] });
					members.Add(result);
				}
			}
			else {
				if(parameters.Length == 0) {
					var result = CG.Invoke(string.Empty, nameof(uNodeHelper.GetGeneratedComponentInParent), new[] { CG.GetUniqueNameForType(compType as RuntimeType) });
					members.Add(result);
				}
				else if(parameters.Length == 1) {
					var result = CG.Invoke(string.Empty, nameof(uNodeHelper.GetGeneratedComponentInParent), new[] { CG.GetUniqueNameForType(compType as RuntimeType), parameters[0] });
					members.Add(result);
				}
			}
		}
	}
}