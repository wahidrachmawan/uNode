using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MaxyGames.UNode;
using MaxyGames.UNode.GenericResolver;
using UnityEngine;

//GameObject.GetComponents<T>()
[assembly: RegisterGenericMethodResolver(typeof(GetComponentsResolver), typeof(GameObject), nameof(GameObject.GetComponents))]
[assembly: RegisterGenericMethodResolver(typeof(GetComponentsResolver), typeof(Component), nameof(Component.GetComponents))]

namespace MaxyGames.UNode.GenericResolver {
	public class GetComponentsResolver : GenericMethodResolver {
		private Func<object, object[], object> func;

		protected override void OnRuntimeInitialize() {
			var type = OpenMethodInfo.DeclaringType;
			var nativeCompType = ConstructedMethodInfo.GetGenericArguments()[0];
			var compType = RuntimeMethodInfo.GetGenericArguments()[0];
			if(type == typeof(GameObject)) {
				func = (obj, parameters) => {
					var array = obj.ConvertTo<GameObject>().GetComponents(nativeCompType).Where(item => compType.IsInstanceOfType(item)).ToArray();
					var result = Array.CreateInstance(nativeCompType, array.Length);
					for(int i = 0; i < array.Length; i++) {
						result.SetValue(array[i], i);
					}
					return result;
				};
			}
			else if(type == typeof(Component)) {
				func = (obj, parameters) => {
					var array = obj.ConvertTo<Component>().GetComponents(nativeCompType).Where(item => compType.IsInstanceOfType(item)).ToArray();
					var result = Array.CreateInstance(nativeCompType, array.Length);
					for(int i = 0; i < array.Length; i++) {
						result.SetValue(array[i], i);
					}
					return result;
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
			CG.RegisterUsingNamespace("System.Linq");
			CG.RegisterUsingNamespace("MaxyGames.UNode");
			//Get the component type
			var nativeCompType = ConstructedMethodInfo.GetGenericArguments()[0];
			//Get the component type
			var compType = RuntimeMethodInfo.GetGenericArguments()[0];
			string item = CG.GenerateNewName("item");
			//Do generate code and add it to member list
			members.Add(
				CG.Invoke(string.Empty, nameof(GameObject.GetComponents), new[] { nativeCompType })
				.CGInvoke(nameof(Enumerable.Where), CG.SimplifiedLambda(new string[] { item }, item.CGInvoke(nameof(Extensions.IsTypeOf), CG.GetUniqueNameForType(compType as RuntimeType))))
				//.CGInvoke(nameof(Enumerable.Select), CG.SimplifiedLambda(new string[] { item }, CG.As(item, nativeCompType)))
				.CGInvoke(nameof(Enumerable.ToArray))
			);
		}
	}
}