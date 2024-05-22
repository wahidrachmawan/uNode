using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MaxyGames.UNode;
using MaxyGames.UNode.GenericResolver;
using UnityEngine;
using Object = UnityEngine.Object;

//Object.FindObjectOfType<T>()
//Object.FindObjectOfType<T>(bool)
[assembly: RegisterGenericMethodResolver(typeof(FindObjectOfTypeResolver), typeof(Object), nameof(Object.FindObjectOfType))]
[assembly: RegisterGenericMethodResolver(typeof(FindObjectOfTypeResolver), typeof(Object), nameof(Object.FindObjectOfType), new[] { typeof(bool) })]

namespace MaxyGames.UNode.GenericResolver {
	public class FindObjectOfTypeResolver : GenericMethodResolver {
		private Func<object, object[], object> func;

		protected override void OnRuntimeInitialize() {
			var type = OpenMethodInfo.DeclaringType;
			var nativeCompType = ConstructedMethodInfo.GetGenericArguments()[0];
			var compType = RuntimeMethodInfo.GetGenericArguments()[0];
			func = (obj, parameters) => {
				switch(parameters.Length) {
					case 0: {
						return Object.FindObjectsOfType(nativeCompType).Where(item => compType.IsInstanceOfType(item)).FirstOrDefault();
					}
					case 1: {
						return Object.FindObjectsOfType(nativeCompType, parameters[0].ConvertTo<bool>()).Where(item => compType.IsInstanceOfType(item)).FirstOrDefault();
					}
				}
				throw new InvalidOperationException();

			};
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
				CG.Invoke(string.Empty, nameof(Object.FindObjectsOfType), new[] { nativeCompType }, parameters)
				.CGInvoke(nameof(Enumerable.Where), CG.SimplifiedLambda(new string[] { item }, item.CGInvoke(nameof(Extensions.IsTypeOf), CG.GetUniqueNameForType(compType as RuntimeType))))
				//.CGInvoke(nameof(Enumerable.Select), CG.SimplifiedLambda(new string[] { item }, CG.As(item, nativeCompType)))
				.CGInvoke(nameof(Enumerable.FirstOrDefault))
			);
		}
	}
}