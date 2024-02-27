using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(Nodes.ManualEvent))]
	public class ManualEventView : BaseNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			Nodes.ManualEvent node = targetNode as Nodes.ManualEvent;
			ControlView control = new ControlView();
			control.style.alignSelf = Align.Center;
			control.Add(new Button() { 
				text = "Trigger",
				clickable = new Clickable(evt => {
					var graph = node.nodeObject.graphContainer;
					var target = graphData.debugTarget;
					if(target == null) {
						bool ShowInstances(Type type) {
							if(type != null) {
								if(type.IsCastableTo(typeof(UnityEngine.Object))) {
									var items = ItemSelector.MakeCustomItemsForInstancedType(type, target => {
										if(target is IInstancedGraph instancedGraph) {
											GraphInstance instance;
											if(instancedGraph.Instance != null) {
												instance = instancedGraph.Instance;
											}
											else {
												instance = RuntimeGraphUtility.GetObjectGraphInstance(graph, instancedGraph);
												if(target is IRuntimeGraphWrapper wrapper) {
													RuntimeGraphUtility.InitializeInstanceGraphValue(graph, instance, wrapper.WrappedVariables);
												}
											}
											node.Trigger(instance);
											//instance.eventData.ExecuteCustomEvent("@ManualEvent_" + node.id, instance);
										}
										else if(target is IRuntimeClass runtime) {
											runtime.InvokeFunction("M_ManualEvent_" + node.id, null);
										}
										else {
											var method = target.GetType().GetMemberCached("M_ManualEvent_" + node.id) as MethodInfo;
											if(method != null) {
												method.InvokeOptimized(target);
											}
										}
										if(target is UnityEngine.Object) {
											EditorGUIUtility.PingObject(target as UnityEngine.Object);
										}
									}, true);
									ItemSelector.ShowCustomItem(items).ChangePosition(evt.originalMousePosition);
									return true;
								}
							}
							return false;
						}
						if(graph is IScriptGraphType scriptGraph) {
							var type = scriptGraph.TypeName.ToType(false);
							if(ShowInstances(type)) return;
						}
						else if(graph is IReflectionType reflectionType) {
							var type = reflectionType.ReflectionType;
							if(ShowInstances(type)) return;
						}
						uNodeEditorUtility.DisplayErrorMessage("No selected instance, please select the instance from the `Debug` menu");
					}
					else {
						if(target is IInstancedGraph instancedGraph) {
							GraphInstance instance;
							if(instancedGraph.Instance != null) {
								instance = instancedGraph.Instance;
							}
							else {
								instance = RuntimeGraphUtility.GetObjectGraphInstance(graph, instancedGraph);
								if(target is IRuntimeGraphWrapper wrapper) {
									RuntimeGraphUtility.InitializeInstanceGraphValue(graph, instance, wrapper.WrappedVariables);
								}
							}
							node.Trigger(instance);
							//instance.eventData.ExecuteCustomEvent("@ManualEvent_" + node.id, instance);
						}
						else if(target is IRuntimeClass runtime) {
							runtime.InvokeFunction("M_ManualEvent_" + node.id, null);
						}
						else {
							var method = target.GetType().GetMemberCached("M_ManualEvent_" + node.id) as MethodInfo;
							if(method != null) {
								method.InvokeOptimized(target);
							}
						}
					}
				})
			});
			AddControl(Direction.Input, control);
		}
	}
}