using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MaxyGames.UNode.Editors;
using UnityEditor;
using UnityEngine;
using static MaxyGames.UNode.Editors.Commands.SurroundCommands;

namespace MaxyGames.UNode.Editors {
    public class SurroundWithWindow : EditorWindow {
        private Action<SurroundCommand> action;
        HashSet<Type> surroundCommands = new HashSet<Type>();
        private GraphEditorData graphData;
        private bool positionSet;
        Vector2 scrollPos = new Vector2();
        public static void ShowWindow(Action<SurroundCommand> action, GraphEditorData graphData, Vector2 mousePosition) {
            var window = ScriptableObject.CreateInstance<SurroundWithWindow>();
            window.action = action;
            window.graphData = graphData;
            window.position = new Rect(mousePosition, new Vector2(250, 250));
            window.surroundCommands = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(SurroundCommand).IsAssignableFrom(type) && !type.IsAbstract)
            .ToHashSet();
            window.ShowPopup();
        }

        void OnGUI() {
            if(surroundCommands == null) Close();
            if(!positionSet) {
                minSize = new Vector2(250, (EditorGUIUtility.singleLineHeight * (surroundCommands.Count + 2)) + EditorStyles.boldLabel.CalcSize(new GUIContent("Surround With [...]:")).y);
                maxSize = new Vector2(250, (EditorGUIUtility.singleLineHeight * (surroundCommands.Count + 2)) + EditorStyles.boldLabel.CalcSize(new GUIContent("Surround With [...]:")).y);
                positionSet = true;
            }
            if(focusedWindow != this) Close();
            if(surroundCommands == null || surroundCommands.Count == 0) {
                EditorGUILayout.LabelField("No Surround With Commands Found");
                return;
            }
            EditorGUILayout.LabelField("Surround With Commands:", EditorStyles.boldLabel);
            GUILayout.Space(4);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach(var command in surroundCommands) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("[...]", GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent("[...]")).x));
                var instance = (SurroundCommand)Activator.CreateInstance(command, new object[] { graphData.currentCanvas });
                if(GUILayout.Button(instance.DisplayName)) {
                    action?.Invoke(instance);
                    Close();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
}