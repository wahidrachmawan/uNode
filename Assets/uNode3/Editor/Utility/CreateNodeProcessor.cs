using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	class DefaultCreateNodeProcessor : CreateNodeProcessor {
		public override bool Process(MemberData member, GraphEditorData editorData, Vector2 position, Action<Node> onCreated) {
			var members = member.GetMembers(false);
			if(members != null && members.Length > 0 && members[members.Length - 1] is MethodInfo method) {
				//For operators
				if(method.Name.StartsWith("op_", StringComparison.Ordinal)) {
					string name = method.Name;
					switch(name) {
						case "op_Addition":
						case "op_Subtraction":
						case "op_Multiply":
						case "op_Division":
							NodeEditorUtility.AddNewNode<Nodes.MultiArithmeticNode>(editorData, null, null, position, n => {
								switch(name) {
									case "op_Addition":
										n.operatorKind = ArithmeticType.Add;
										break;
									case "op_Subtraction":
										n.operatorKind = ArithmeticType.Subtract;
										break;
									case "op_Multiply":
										n.operatorKind = ArithmeticType.Multiply;
										break;
									case "op_Division":
										n.operatorKind = ArithmeticType.Divide;
										break;
								}
								n.EnsureRegistered();
								var param = method.GetParameters();
								for(int i = 0; i < param.Length; i++) {
									var paramType = param[i].ParameterType.IsByRef ? param[i].ParameterType.GetElementType() : param[i].ParameterType;
									n.inputs[i].type = paramType;
									n.inputs[i].port.AssignToDefault(MemberData.Default(paramType));
								}
								n.nodeObject.Register();
								onCreated?.Invoke(n);
							});
							return true;
					}
				}
				//For list
				if(method.DeclaringType.HasImplementInterface(typeof(System.Collections.IList))) {
					if(method.Name == "get_Item") {
						NodeEditorUtility.AddNewNode<Nodes.GetListItem>(editorData, position, n => {
							n.EnsureRegistered();
							onCreated?.Invoke(n);
						});
						return true;
					}
					else if(method.Name == "set_Item") {
						NodeEditorUtility.AddNewNode<Nodes.SetListItem>(editorData, position, n => {
							n.EnsureRegistered();
							onCreated?.Invoke(n);
						});
						return true;
					}
					else if(method.Name == "Add") {
						if(method.GetParameters().Length == 1) {
							NodeEditorUtility.AddNewNode<Nodes.AddListItem>(editorData, position, n => {
								n.EnsureRegistered();
								onCreated?.Invoke(n);
							});
							return true;
						}
					}
					else if(method.Name == "Insert") {
						if(method.GetParameters().Length == 2) {
							NodeEditorUtility.AddNewNode<Nodes.InsertListItem>(editorData, position, n => {
								n.EnsureRegistered();
								onCreated?.Invoke(n);
							});
							return true;
						}
					}
					else if(method.Name == "Remove") {
						if(method.GetParameters().Length == 1) {
							NodeEditorUtility.AddNewNode<Nodes.RemoveListItem>(editorData, position, n => {
								n.EnsureRegistered();
								onCreated?.Invoke(n);
							});
							return true;
						}
					}
				}
				//For array
				if(method.DeclaringType.IsArray && method.DeclaringType.GetArrayRank() == 1) {
					if(method.Name == "Get") {
						NodeEditorUtility.AddNewNode<Nodes.GetListItem>(editorData, position, n => {
							n.EnsureRegistered();
							onCreated?.Invoke(n);
						});
						return true;
					}
					else if(method.Name == "Set") {
						NodeEditorUtility.AddNewNode<Nodes.SetListItem>(editorData, position, n => {
							n.EnsureRegistered();
							onCreated?.Invoke(n);
						});
						return true;
					}
				}
			}
			if(member.targetType.IsTargetingReflection() &&
				member.isStatic == false &&
				member.isDeepTarget == false &&
				member.instance is IClassGraph classGraph &&
				(member.startType == classGraph.InheritType || classGraph.InheritType.IsSubclassOf(member.startType))) {

				NodeEditorUtility.AddNewNode<NodeBaseCaller>(editorData, position, n => {
					n.target = member;
					n.EnsureRegistered();
					onCreated?.Invoke(n);
				});
				return true;
			}
			return false;
		}
	}
}