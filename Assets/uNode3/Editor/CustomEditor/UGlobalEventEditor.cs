using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MaxyGames.UNode.Editors {
	[CustomEditor(typeof(UGlobalEvent), true)]
	public class UGlobalEventEditor : Editor {
		static ConditionalWeakTable<Delegate, Delegate[]> listenerMap = new();

		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
			DrawFindReferences();

			if(Application.isPlaying) {
				DrawEvents((target as UGlobalEvent).GetDelegate());
			}
		}

		protected void DrawFindReferences() {
			if(GUILayout.Button("Find All References")) {
				GraphUtility.ShowUnityReferenceUsages(target);
			}
		}

		protected void DrawEvents(Delegate evt) {
			if(evt == null) return;
			if(!listenerMap.TryGetValue(evt, out var delegates)) {
				delegates = evt.GetInvocationList();
				listenerMap.AddOrUpdate(evt, delegates);
			}
			DrawEventListeners(delegates);
		}

		protected void DrawEventListeners(Delegate[] events) {
			EditorGUILayout.LabelField("Listeners", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.BeginVertical("Box");
			foreach(var evt in events) {
				DrawDelegate(evt);
				EditorGUILayout.Separator();
			}
			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
		}

		protected void DrawDelegate(Delegate @delegate) {
			EditorGUILayout.LabelField(EditorReflectionUtility.GetPrettyMethodName(@delegate.Method), EditorStyles.centeredGreyMiniLabel);
			EditorGUI.indentLevel++;
			if(@delegate.Target != null) {
				var target = @delegate.Target;
				var type = target.GetType();
				DrawListenerTarget(target, type);
			}
			EditorGUI.indentLevel--;
		}

		private void DrawListenerTarget(object target, Type type) {
			if(target is Node) {
				target = (target as Node).nodeObject;
			}
			if(target is UGraphElement) {
				uNodeGUI.DrawReference(uNodeGUIUtility.GetRect(), target, type); 
			}
			EditorGUILayout.LabelField(target.GetType().ToString(), EditorStyles.centeredGreyMiniLabel);

			EditorGUI.BeginDisabledGroup(true);
			if(type.IsDefinedAttribute<CompilerGeneratedAttribute>()) {
				var fields = EditorReflectionUtility.GetFields(type, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Instance);
				foreach(var field in fields) {
					var val = field.GetValueOptimized(target);
					if(val is Delegate) {
						EditorGUILayout.LabelField(new GUIContent(ObjectNames.NicifyVariableName(field.Name)), EditorStyles.boldLabel);
						EditorGUI.indentLevel++;
						DrawDelegate(val as Delegate);
						EditorGUI.indentLevel--;
					}
					else {
						uNodeGUIUtility.EditValueLayouted(new GUIContent(ObjectNames.NicifyVariableName(field.Name)), val, field.FieldType, null, new uNodeUtility.EditValueSettings() { nullable = true });
					}
				}
			}
			else {
				uNodeGUIUtility.EditValueLayouted(new GUIContent("value"), target, type, null, new uNodeUtility.EditValueSettings() { nullable = true });
			}
			EditorGUI.BeginDisabledGroup(false);
		}
	}

    [CustomEditor(typeof(UGlobalEventCustom), true)]
    class UGlobalEventCustomEditor : UGlobalEventEditor {
		public override void OnInspectorGUI() {
			var monoScript = uNodeEditorUtility.GetMonoScript(target);
			if(monoScript != null) {
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), true);
				EditorGUI.EndDisabledGroup();
			}
			var asset = target as UGlobalEventCustom;
			uNodeGUI.DrawCustomList(asset.parameters, "Parameters",
				drawElement: (pos, index, parameter) => {
					var name = EditorGUI.DelayedTextField(new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight), "Name", parameter.name);
					if(name != parameter.name) {
						parameter.name = name;
					}
					uNodeGUIUtility.DrawTypeDrawer(
						new Rect(pos.x, pos.y + EditorGUIUtility.singleLineHeight, pos.width, EditorGUIUtility.singleLineHeight),
						parameter.type,
						new GUIContent("Type"),
						type => {
							uNodeEditorUtility.RegisterUndo(asset);
							parameter.type = type;
						}, null, asset);
				},
				add: position => {
					asset.parameters.Add(new ParameterData("newParameter", typeof(string)));
				},
				remove: index => {
					asset.parameters.RemoveAt(index);
				},
				elementHeight: index => {
					return EditorGUIUtility.singleLineHeight * 2;
				});
			DrawFindReferences();

			if(Application.isPlaying) {
				DrawEvents(asset.GetDelegate());
			}
		}
	}
}