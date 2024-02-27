using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using UnityEngine.Pool;
using System.Text;

namespace MaxyGames.UNode.Editors.Control {
	class UnityEventFieldControl : FieldControl<UnityEventBase> {

		struct ValidMethodMap {
			public Object target;
			public MethodInfo methodInfo;
			public PersistentListenerMode mode;
		}

		struct UnityEventFunction {
			readonly object m_PersistentCall;
			readonly Object m_Target;
			readonly MethodInfo m_Method;
			readonly PersistentListenerMode m_Mode;

			public UnityEventFunction(object persistentCall, Object target, MethodInfo method, PersistentListenerMode mode) {
				m_PersistentCall = persistentCall;
				m_Target = target;
				m_Method = method;
				m_Mode = mode;
			}

			public void Assign() {
				// find the current event target...
				var arguments = PersistentCall.Arguments.GetValueOptimized(m_PersistentCall);

				PersistentCall.SetTarget(m_PersistentCall, m_Target);
				PersistentCall.SetTargetAssemblyTypeName(m_PersistentCall, m_Method.DeclaringType.AssemblyQualifiedName);
				PersistentCall.SetMethodName(m_PersistentCall, m_Method.Name);
				PersistentCall.SetMode(m_PersistentCall, m_Mode);

				if(m_Mode == PersistentListenerMode.Object) {
					var argParams = m_Method.GetParameters();
					if(argParams.Length == 1 && typeof(Object).IsAssignableFrom(argParams[0].ParameterType))
						ArgumentCache.ObjectArgumentAssemblyTypeName.SetValueOptimized(arguments, argParams[0].ParameterType.AssemblyQualifiedName);
					else
						ArgumentCache.ObjectArgumentAssemblyTypeName.SetValueOptimized(arguments, typeof(Object).AssemblyQualifiedName);
				}

				ValidateObjectParamater(arguments, m_Mode);
			}

			private void ValidateObjectParamater(object arguments, PersistentListenerMode mode) {
				var argumentObj = ArgumentCache.ObjectArgument.GetValueOptimized(arguments).ConvertTo<Object>();

				if(mode != PersistentListenerMode.Object) {
					ArgumentCache.ObjectArgumentAssemblyTypeName.SetValueOptimized(arguments, typeof(Object).AssemblyQualifiedName);
					ArgumentCache.ObjectArgument.SetValueOptimized(arguments, null);
					return;
				}

				if(argumentObj == null)
					return;

				Type t = Type.GetType(ArgumentCache.ObjectArgumentAssemblyTypeName.GetValueOptimized(arguments).ConvertTo<string>(), false);
				if(!typeof(Object).IsAssignableFrom(t) || !t.IsInstanceOfType(argumentObj))
					ArgumentCache.ObjectArgument.SetValueOptimized(arguments, null);
			}

			public void Clear() {
				PersistentCall.SetMethodName(m_PersistentCall, null);
				PersistentCall.SetMode(m_PersistentCall, PersistentListenerMode.Void);
			}
		}
		static class UnityEventBaseInfo {
			public static readonly FieldInfo PersistentCalls = typeof(UnityEventBase).GetField("m_PersistentCalls", MemberData.flags);
			public static readonly MethodInfo FindMethod = typeof(UnityEventBase).GetMethod("FindMethod", MemberData.flags, null, new[] { typeof(string), typeof(Type), typeof(PersistentListenerMode), typeof(Type) }, null);
		}

		static class PersistentCallGroup {
			public static readonly Type Type = "UnityEngine.Events.PersistentCallGroup".ToType(false);
			public static readonly FieldInfo Calls = Type.GetField("m_Calls", MemberData.flags);
		}

		static class PersistentCall {
			public static readonly Type Type = "UnityEngine.Events.PersistentCall".ToType(false);
			public static readonly FieldInfo MethodName = Type.GetField("m_MethodName", MemberData.flags);
			public static readonly FieldInfo Target = Type.GetField("m_Target", MemberData.flags);
			public static readonly FieldInfo TargetAssemblyTypeName = Type.GetField("m_TargetAssemblyTypeName", MemberData.flags);
			public static readonly FieldInfo Mode = Type.GetField("m_Mode", MemberData.flags);
			public static readonly FieldInfo Arguments = Type.GetField("m_Arguments", MemberData.flags);
			public static readonly FieldInfo State = Type.GetField("m_CallState", MemberData.flags);

			public static string GetMethodName(object persistentCall) {
				return MethodName.GetValueOptimized(persistentCall).ConvertTo<string>();
			}

			public static void SetMethodName(object persistentCall, string value) {
				MethodName.SetValueOptimized(persistentCall, value);
			}

			public static string GetTargetAssemblyTypeName(object persistentCall) {
				return TargetAssemblyTypeName.GetValueOptimized(persistentCall).ConvertTo<string>();
			}

			public static void SetTargetAssemblyTypeName(object persistentCall, string value) {
				TargetAssemblyTypeName.SetValueOptimized(persistentCall, value);
			}

			public static Object GetTarget(object persistentCall) {
				return Target.GetValueOptimized(persistentCall).ConvertTo<Object>();
			}

			public static void SetTarget(object persistentCall, Object value) {
				Target.SetValueOptimized(persistentCall, value);
			}

			public static PersistentListenerMode GetMode(object persistentCall) {
				return Mode.GetValueOptimized(persistentCall).ConvertTo<PersistentListenerMode>();
			}

			public static void SetMode(object persistentCall, PersistentListenerMode value) {
				Mode.SetValueOptimized(persistentCall, value);
			}

			public static UnityEventCallState GetState(object persistentCall) {
				return State.GetValueOptimized(persistentCall).ConvertTo<UnityEventCallState>();
			}

			public static void SetState(object persistentCall, UnityEventCallState value) {
				State.SetValueOptimized(persistentCall, value);
			}
		}

		static class ArgumentCache {
			public static readonly Type Type = "UnityEngine.Events.ArgumentCache".ToType(false);
			public static readonly FieldInfo ObjectArgument = Type.GetField("m_ObjectArgument", MemberData.flags);
			public static readonly FieldInfo ObjectArgumentAssemblyTypeName = Type.GetField("m_ObjectArgumentAssemblyTypeName", MemberData.flags);
			public static readonly FieldInfo IntArgument = Type.GetField("m_IntArgument", MemberData.flags);
			public static readonly FieldInfo FloatArgument = Type.GetField("m_FloatArgument", MemberData.flags);
			public static readonly FieldInfo StringArgument = Type.GetField("m_StringArgument", MemberData.flags);
			public static readonly FieldInfo BoolArgument = Type.GetField("m_BoolArgument", MemberData.flags);
		}


		private const string kNoFunctionString = "No Function";

		public override bool IsValidControl(Type type, bool layouted) {
			return layouted && type.IsSubclassOf(typeof(UnityEventBase));
		}

		public override void DrawLayouted(object value, GUIContent label, Type type, Action<object> onChanged, uNodeUtility.EditValueSettings settings) {
			EditorGUI.BeginChangeCheck();
			ValidateValue(ref value, type);
			var newValue = value.ConvertTo<UnityEventBase>();
			if(newValue != null) {
				var persistenceCalls = UnityEventBaseInfo.PersistentCalls.GetValueOptimized(value);
				var callList = PersistentCallGroup.Calls.GetValueOptimized(persistenceCalls) as IList;
				uNodeGUI.DrawCustomList(callList, GetHeaderText(label.text, newValue),
					drawElement: (position, index, value) => {
						var subRects = GetRowRects(position);
						Rect enabledRect = subRects[0];
						Rect goRect = subRects[1];
						Rect functionRect = subRects[2];
						Rect argRect = subRects[3];

						uNodeGUIUtility.EditValue(enabledRect, GUIContent.none, "m_CallState", value, settings.unityObject);
						uNodeGUIUtility.EditValue(goRect, GUIContent.none, "m_Target", value, settings.unityObject);

						var functionName = GetFunctionDropdownText(newValue, value);
						var targetReference = newValue.GetPersistentTarget(index);
						EditorGUI.BeginDisabledGroup(targetReference == null);
						if(GUI.Button(functionRect, string.IsNullOrEmpty(functionName) ? "No Function" : functionName)) {
							var menu = BuildPopupList(targetReference, newValue, value);
							menu.ShowAsContext();
						}
						EditorGUI.EndDisabledGroup();

						var listenerTarget = newValue.GetPersistentTarget(index);
						var methodName = newValue.GetPersistentMethodName(index);
						var modeEnum = PersistentCall.GetMode(value);
						var argument = PersistentCall.Arguments.GetValueOptimized(value);

						//only allow argument if we have a valid target / method
						if(listenerTarget == null || string.IsNullOrEmpty(methodName))
							modeEnum = PersistentListenerMode.Void;

						switch(modeEnum) {
							case PersistentListenerMode.Object: {
								var desiredArgTypeName = ArgumentCache.ObjectArgumentAssemblyTypeName.GetValueOptimized(argument) as string;
								var desiredType = typeof(Object);
								if(!string.IsNullOrEmpty(desiredArgTypeName))
									desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);
								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.ObjectField(argRect, GUIContent.none, ArgumentCache.ObjectArgument.GetValueOptimized(argument).ConvertTo<Object>(), desiredType, true);
								if(EditorGUI.EndChangeCheck()) {
									ArgumentCache.ObjectArgument.SetValueOptimized(argument, result);
									onChanged(newValue);
								}
								break;
							}
							case PersistentListenerMode.Bool: {
								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.Toggle(argRect, GUIContent.none, ArgumentCache.BoolArgument.GetValueOptimized(argument).ConvertTo<bool>());
								if(EditorGUI.EndChangeCheck()) {
									ArgumentCache.BoolArgument.SetValueOptimized(argument, result);
									onChanged(newValue);
								}
								break;
							}
							case PersistentListenerMode.Float: {
								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.FloatField(argRect, GUIContent.none, ArgumentCache.FloatArgument.GetValueOptimized(argument).ConvertTo<float>());
								if(EditorGUI.EndChangeCheck()) {
									ArgumentCache.FloatArgument.SetValueOptimized(argument, result);
									onChanged(newValue);
								}
								break;
							}
							case PersistentListenerMode.Int: {
								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.IntField(argRect, GUIContent.none, ArgumentCache.IntArgument.GetValueOptimized(argument).ConvertTo<int>());
								if(EditorGUI.EndChangeCheck()) {
									ArgumentCache.IntArgument.SetValueOptimized(argument, result);
									onChanged(newValue);
								}
								break;
							}
							case PersistentListenerMode.String: {
								EditorGUI.BeginChangeCheck();
								var result = EditorGUI.DelayedTextField(argRect, GUIContent.none, ArgumentCache.StringArgument.GetValueOptimized(argument).ConvertTo<string>());
								if(EditorGUI.EndChangeCheck()) {
									ArgumentCache.StringArgument.SetValueOptimized(argument, result);
									onChanged(newValue);
								}
								break;
							}
						}
					},
					add: _ => {
						uNodeEditorUtility.RegisterUndo(settings.unityObject);
						callList.Add(ReflectionUtils.CreateInstance(PersistentCall.Type));
						onChanged(newValue);
					},
					remove: index => {
						uNodeEditorUtility.RegisterUndo(settings.unityObject);
						callList.RemoveAt(index);
						onChanged(newValue);
					},
					elementHeight: index => {
						return (EditorGUIUtility.singleLineHeight + 2) * 2;
					});
			}
			if(EditorGUI.EndChangeCheck()) {
				onChanged(newValue);
			}
		}

		private static string GetHeaderText(string text, UnityEventBase unityEvent) {
			return (string.IsNullOrEmpty(text) ? "Event" : text) + GetEventParams(unityEvent);
		}

		static string GetEventParams(UnityEventBase evt) {
			var methodInfo = UnityEventBaseInfo.FindMethod.InvokeOptimized(evt, "Invoke", evt.GetType(), PersistentListenerMode.EventDefined, null) as MethodInfo;

			var sb = new StringBuilder();
			sb.Append(" (");

			var types = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
			for(int i = 0; i < types.Length; i++) {
				sb.Append(types[i].Name);
				if(i < types.Length - 1) {
					sb.Append(", ");
				}
			}
			sb.Append(")");
			return sb.ToString();
		}

		private static string GetFunctionDropdownText(UnityEventBase unityEvent, object listener) {
			var listenerTarget = PersistentCall.GetTarget(listener);
			var methodName = PersistentCall.GetMethodName(listener);
			var mode = PersistentCall.GetMode(listener);
			var desiredArgTypeName = ArgumentCache.ObjectArgumentAssemblyTypeName.GetValueOptimized(PersistentCall.Arguments.GetValueOptimized(listener)).ConvertTo<string>();
			var desiredType = typeof(Object);
			if(!string.IsNullOrEmpty(desiredArgTypeName))
				desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);

			var buttonLabel = new StringBuilder();
			if(listenerTarget == null || string.IsNullOrEmpty(methodName)) {
				buttonLabel.Append(kNoFunctionString);
			}
			else if(!IsPersistantListenerValid(unityEvent, methodName, listenerTarget, mode, desiredType)) {
				var instanceString = "UnknownComponent";
				var instance = listenerTarget;
				if(instance != null)
					instanceString = instance.GetType().Name;

				buttonLabel.Append(string.Format("<Missing {0}.{1}>", instanceString, methodName));
			}
			else {
				buttonLabel.Append(listenerTarget.GetType().Name);

				if(!string.IsNullOrEmpty(methodName)) {
					buttonLabel.Append(".");
					if(methodName.StartsWith("set_"))
						buttonLabel.Append(methodName.Substring(4));
					else
						buttonLabel.Append(methodName);
				}
			}

			return buttonLabel.ToString();
		}

		public static bool IsPersistantListenerValid(UnityEventBase unityEvent, string methodName, Object uObject, PersistentListenerMode modeEnum, Type argumentType) {
			if(uObject == null || string.IsNullOrEmpty(methodName))
				return false;

			return UnityEventBaseInfo.FindMethod.InvokeOptimized(unityEvent, new object[] { methodName, uObject.GetType(), modeEnum, argumentType }) != null;
		}

		Rect[] GetRowRects(Rect rect) {
			Rect[] rects = new Rect[4];

			rect.height = EditorGUIUtility.singleLineHeight;
			rect.y += 2;

			Rect enabledRect = rect;
			enabledRect.width *= 0.3f;

			Rect goRect = enabledRect;
			goRect.y += EditorGUIUtility.singleLineHeight + 2;

			Rect functionRect = rect;
			functionRect.xMin = goRect.xMax + 5;

			Rect argRect = functionRect;
			argRect.y += EditorGUIUtility.singleLineHeight + 2;

			rects[0] = enabledRect;
			rects[1] = goRect;
			rects[2] = functionRect;
			rects[3] = argRect;
			return rects;
		}

		internal static GenericMenu BuildPopupList(Object target, UnityEventBase unityEvent, object listener) {
			//special case for components... we want all the game objects targets there!
			var targetToUse = target;
			if(targetToUse is Component)
				targetToUse = (target as Component).gameObject;

			// find the current event target...
			var methodName = PersistentCall.GetMethodName(listener);

			var menu = new GenericMenu();
			menu.AddItem(new GUIContent(kNoFunctionString),
				string.IsNullOrEmpty(methodName),
				() => {
					new UnityEventFunction(listener, null, null, PersistentListenerMode.EventDefined).Clear();
				});

			if(targetToUse == null)
				return menu;

			menu.AddSeparator("");

			// figure out the signature of this delegate...
			// The property at this stage points to the 'container' and has the field name
			Type delegateType = unityEvent.GetType();

			// check out the signature of invoke as this is the callback!
			MethodInfo delegateMethod = delegateType.GetMethod("Invoke");
			var delegateArgumentsTypes = delegateMethod.GetParameters().Select(x => x.ParameterType).ToArray();

			var duplicateNames = DictionaryPool<string, int>.Get();
			var duplicateFullNames = DictionaryPool<string, int>.Get();

			GeneratePopUpForType(menu, targetToUse, targetToUse.GetType().Name, listener, delegateArgumentsTypes);
			duplicateNames[targetToUse.GetType().Name] = 0;
			if(targetToUse is GameObject) {
				Component[] comps = (targetToUse as GameObject).GetComponents<Component>();

				// Collect all the names and record how many times the same name is used.
				foreach(Component comp in comps) {
					if(comp == null)
						continue;

					var duplicateIndex = 0;
					if(duplicateNames.TryGetValue(comp.GetType().Name, out duplicateIndex))
						duplicateIndex++;
					duplicateNames[comp.GetType().Name] = duplicateIndex;
				}

				foreach(Component comp in comps) {
					if(comp == null)
						continue;

					var compType = comp.GetType();
					string targetName = compType.Name;
					int duplicateIndex = 0;

					// Is this name used multiple times? If so then use the full name plus an index if there are also duplicates of this. (case 1309997)
					if(duplicateNames[compType.Name] > 0) {
						if(duplicateFullNames.TryGetValue(compType.FullName, out duplicateIndex))
							targetName = $"{compType.FullName} ({duplicateIndex})";
						else
							targetName = compType.FullName;
					}
					GeneratePopUpForType(menu, comp, targetName, listener, delegateArgumentsTypes);
					duplicateFullNames[compType.FullName] = duplicateIndex + 1;
				}

				DictionaryPool<string, int>.Release(duplicateNames);
				DictionaryPool<string, int>.Release(duplicateFullNames);
			}
			return menu;
		}

		private static void GeneratePopUpForType(GenericMenu menu, Object target, string targetName, object listener, Type[] delegateArgumentsTypes) {
			var methods = new List<ValidMethodMap>();
			bool didAddDynamic = false;

			// skip 'void' event defined on the GUI as we have a void prebuilt type!
			if(delegateArgumentsTypes.Length != 0) {
				GetMethodsForTargetAndMode(target, delegateArgumentsTypes, methods, PersistentListenerMode.EventDefined);
				if(methods.Count > 0) {
					menu.AddDisabledItem(new GUIContent(targetName + "/Dynamic " + string.Join(", ", delegateArgumentsTypes.Select(e => GetTypeName(e)).ToArray())));
					AddMethodsToMenu(menu, listener, methods, targetName);
					didAddDynamic = true;
				}
			}

			methods.Clear();
			GetMethodsForTargetAndMode(target, new[] { typeof(float) }, methods, PersistentListenerMode.Float);
			GetMethodsForTargetAndMode(target, new[] { typeof(int) }, methods, PersistentListenerMode.Int);
			GetMethodsForTargetAndMode(target, new[] { typeof(string) }, methods, PersistentListenerMode.String);
			GetMethodsForTargetAndMode(target, new[] { typeof(bool) }, methods, PersistentListenerMode.Bool);
			GetMethodsForTargetAndMode(target, new[] { typeof(Object) }, methods, PersistentListenerMode.Object);
			GetMethodsForTargetAndMode(target, new Type[] { }, methods, PersistentListenerMode.Void);
			if(methods.Count > 0) {
				if(didAddDynamic)
					// AddSeperator doesn't seem to work for sub-menus, so we have to use this workaround instead of a proper separator for now.
					menu.AddItem(new GUIContent(targetName + "/ "), false, null);
				if(delegateArgumentsTypes.Length != 0)
					menu.AddDisabledItem(new GUIContent(targetName + "/Static Parameters"));
				AddMethodsToMenu(menu, listener, methods, targetName);
			}
		}

		static void AddFunctionsForScript(GenericMenu menu, object listener, ValidMethodMap method, string targetName) {
			PersistentListenerMode mode = method.mode;

			// find the current event target...
			var listenerTarget = PersistentCall.GetTarget(listener);
			var methodName = PersistentCall.GetMethodName(listener);
			var setMode = PersistentCall.GetMode(listener);
			var typeName = ArgumentCache.ObjectArgumentAssemblyTypeName.GetValueOptimized(PersistentCall.Arguments.GetValueOptimized(listener)).ConvertTo<string>();

			var args = new StringBuilder();
			var count = method.methodInfo.GetParameters().Length;
			for(int index = 0; index < count; index++) {
				var methodArg = method.methodInfo.GetParameters()[index];
				args.Append(string.Format("{0}", GetTypeName(methodArg.ParameterType)));

				if(index < count - 1)
					args.Append(", ");
			}

			var isCurrentlySet = listenerTarget == method.target
				&& methodName == method.methodInfo.Name
				&& mode == setMode;

			if(isCurrentlySet && mode == PersistentListenerMode.Object && method.methodInfo.GetParameters().Length == 1) {
				isCurrentlySet &= (method.methodInfo.GetParameters()[0].ParameterType.AssemblyQualifiedName == typeName);
			}

			string path = GetFormattedMethodName(targetName, method.methodInfo.Name, args.ToString(), mode == PersistentListenerMode.EventDefined);
			menu.AddItem(new GUIContent(path),
				isCurrentlySet,
				() => {
					var func = new UnityEventFunction(listener, method.target, method.methodInfo, mode);
					func.Assign();
				});
		}

		private static string GetTypeName(Type t) {
			if(t == typeof(int))
				return "int";
			if(t == typeof(float))
				return "float";
			if(t == typeof(string))
				return "string";
			if(t == typeof(bool))
				return "bool";
			return t.Name;
		}

		static string GetFormattedMethodName(string targetName, string methodName, string args, bool dynamic) {
			if(dynamic) {
				if(methodName.StartsWith("set_"))
					return string.Format("{0}/{1}", targetName, methodName.Substring(4));
				else
					return string.Format("{0}/{1}", targetName, methodName);
			}
			else {
				if(methodName.StartsWith("set_"))
					return string.Format("{0}/{2} {1}", targetName, methodName.Substring(4), args);
				else
					return string.Format("{0}/{1} ({2})", targetName, methodName, args);
			}
		}

		private static void AddMethodsToMenu(GenericMenu menu, object listener, List<ValidMethodMap> methods, string targetName) {
			// Note: sorting by a bool in OrderBy doesn't seem to work for some reason, so using numbers explicitly.
			IEnumerable<ValidMethodMap> orderedMethods = methods.OrderBy(e => e.methodInfo.Name.StartsWith("set_") ? 0 : 1).ThenBy(e => e.methodInfo.Name);
			foreach(var validMethod in orderedMethods)
				AddFunctionsForScript(menu, listener, validMethod, targetName);
		}

		private static void GetMethodsForTargetAndMode(Object target, Type[] delegateArgumentsTypes, List<ValidMethodMap> methods, PersistentListenerMode mode) {
			IEnumerable<ValidMethodMap> newMethods = CalculateMethodMap(target, delegateArgumentsTypes, mode == PersistentListenerMode.Object);
			foreach(var m in newMethods) {
				var method = m;
				method.mode = mode;
				methods.Add(method);
			}
		}

		static IEnumerable<ValidMethodMap> CalculateMethodMap(Object target, Type[] t, bool allowSubclasses) {
			var validMethods = new List<ValidMethodMap>();
			if(target == null || t == null)
				return validMethods;

			// find the methods on the behaviour that match the signature
			Type componentType = target.GetType();
			var componentMethods = componentType.GetMethods().Where(x => !x.IsSpecialName).ToList();

			var wantedProperties = componentType.GetProperties().AsEnumerable();
			wantedProperties = wantedProperties.Where(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0 && x.GetSetMethod() != null);
			componentMethods.AddRange(wantedProperties.Select(x => x.GetSetMethod()));

			foreach(var componentMethod in componentMethods) {
				//Debug.Log ("Method: " + componentMethod);
				// if the argument length is not the same, no match
				var componentParamaters = componentMethod.GetParameters();
				if(componentParamaters.Length != t.Length)
					continue;

				// Don't show obsolete methods.
				if(componentMethod.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
					continue;

				if(componentMethod.ReturnType != typeof(void))
					continue;

				// if the argument types do not match, no match
				bool paramatersMatch = true;
				for(int i = 0; i < t.Length; i++) {
					if(!componentParamaters[i].ParameterType.IsAssignableFrom(t[i]))
						paramatersMatch = false;

					if(allowSubclasses && t[i].IsAssignableFrom(componentParamaters[i].ParameterType))
						paramatersMatch = true;
				}

				// valid method
				if(paramatersMatch) {
					var vmm = new ValidMethodMap {
						target = target,
						methodInfo = componentMethod
					};
					validMethods.Add(vmm);
				}
			}
			return validMethods;
		}
	}
}