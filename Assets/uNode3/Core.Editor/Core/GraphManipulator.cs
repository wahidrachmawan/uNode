using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;
using System.Collections;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public abstract class GraphManipulator {
		private uNodeEditor.TabData m_tabData;
		public uNodeEditor.TabData tabData {
			get => m_tabData;
			set {
				m_tabData = value;
				graphData = value.selectedGraphData;
			}
		}

		public GraphEditorData graphData { get; private set; }
		public IGraph graph => graphData.graph;

		public virtual int order => 0;

		public virtual bool IsValid(string action) => false;

		public virtual bool CreateNewVariable(Vector2 mousePosition, Action postAction) => false;
		public virtual bool CreateNewProperty(Vector2 mousePosition, Action postAction) => false;
		public virtual bool CreateNewFunction(Vector2 mousePosition, Action postAction) => false;
		public virtual bool CreateNewLocalVariable(Vector2 mousePosition, Action postAction) => false;
		public virtual bool CreateNewClass(Vector2 mousePosition, Action postAction) => false;
		public virtual IEnumerable<DropdownMenuItem> ContextMenuForGraph(Vector2 mousePosition) => null;
		public virtual IEnumerable<DropdownMenuItem> ContextMenuForVariable(Vector2 mousePosition, Variable variable) => null;
		public virtual IEnumerable<DropdownMenuItem> ContextMenuForProperty(Vector2 mousePosition, Property variable) => null;
		public virtual IEnumerable<DropdownMenuItem> ContextMenuForFunction(Vector2 mousePosition, Function variable) => null;

		/// <summary>
		/// Call this to mark that the graph has been changed
		/// </summary>
		protected void GraphChanged() {
			uNodeGUIUtility.GUIChanged(graph, UIChangeType.Important);
		}

		protected void ShowTypeMenu(Vector2 position, Action<Type> onClick, Type[] generalTypes = null, FilterAttribute filter = null) {
			if(generalTypes == null) {
				generalTypes = new Type[] {
					typeof(string),
					typeof(float),
					typeof(bool),
					typeof(int),
					typeof(Vector2),
					typeof(Vector3),
					typeof(Transform),
					typeof(GameObject),
					//typeof(IRuntimeClass),
					typeof(List<>),
				};
			}
			if(filter == null) {
				filter = FilterAttribute.DefaultTypeFilter;
			}
			var customItmes = ItemSelector.MakeCustomTypeItems(generalTypes, "General");
			var window = ItemSelector.ShowType(
				graphData.graph,
				filter, 
				(m) => {
					onClick(m.startType);
				},
				customItmes).ChangePosition(position);
			window.displayNoneOption = false;
			window.displayGeneralType = false;
		}
	}

	class DefaultGraphManipulator : GraphManipulator {
		public override int order => int.MaxValue;

		public override bool IsValid(string action) {
			return true;
		}

		public override bool CreateNewVariable(Vector2 mousePosition, Action postAction) {
			ShowTypeMenu(mousePosition, type => {
				var variable = graphData.graphData.variableContainer.NewVariable("newVariable", type);
				if(graph is IClassModifier classModifier) {
					if(classModifier.GetModifier().ReadOnly) {
						variable.modifier.ReadOnly = true;
					}
				}
				postAction?.Invoke();
			});
			return true;
		}

		public override bool CreateNewProperty(Vector2 mousePosition, Action postAction) {
			ShowTypeMenu(mousePosition, type => {
				graphData.graphData.propertyContainer.NewProperty("newProperty", type);
				postAction?.Invoke();
			});
			return true;
		}

		public override bool CreateNewLocalVariable(Vector2 mousePosition, Action postAction) {
			ShowTypeMenu(mousePosition, type => {
				graphData.selectedRoot.variableContainer.NewVariable("localVariable", type);
				postAction?.Invoke();
			});
			return true;
		}

		public override bool CreateNewFunction(Vector2 mousePosition, Action postAction) {
			GenericMenu menu = new GenericMenu();
			var functionSystem = graphData.graph;
			menu.AddItem(new GUIContent("Add new"), false, () => {
				NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "NewFunction", typeof(void), f => {
					if(uNodePreference.preferenceData.newVariableAccessor == uNodePreference.DefaultAccessor.Private) {
						f.modifier.SetPrivate();
					}
				});
				GraphChanged();
			});
			menu.AddItem(new GUIContent("Add new coroutine"), false, () => {
				NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "NewFunction", typeof(IEnumerator), f => {
					if(uNodePreference.preferenceData.newVariableAccessor == uNodePreference.DefaultAccessor.Private) {
						f.modifier.SetPrivate();
					}
				});
				GraphChanged();
			});
			Type inheritType = null;
			if(graphData.graph is IClassGraph co) {
				inheritType = co.InheritType;
			}
			if(inheritType != null) {
				#region UnityEvent
				if(typeof(MonoBehaviour).IsAssignableFrom(inheritType)) {
					menu.AddSeparator("");
					{//Start Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("Start", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/Start()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Start", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//Awake Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("Awake", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/Awake()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Awake", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnDestroy Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnDestroy", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/OnDestroy()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDestroy", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnDisable Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnDisable", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/OnDisable()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDisable", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnEnable Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnEnable", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/OnEnable()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnEnable", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//Update Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("Update", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/Update()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Update", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//FixedUpdate Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("FixedUpdate", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/FixedUpdate()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "FixedUpdate", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//LateUpdate Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("LateUpdate", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/LateUpdate()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "LateUpdate", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnGUI Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnGUI", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/OnGUI()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnGUI", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnAnimatorIK Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnAnimatorIK", 0, typeof(int))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Animation/OnAnimatorIK(int)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnAnimatorIK", typeof(void), new string[] { "layerIndex" }, new Type[] { typeof(int) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnAnimatorMove Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnAnimatorMove", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Animation/OnAnimatorMove()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnAnimatorMove", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnApplicationFocus Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnApplicationFocus", 0, typeof(bool))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Game Event/OnApplicationFocus(bool)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnApplicationFocus", typeof(void), new string[] { "focusStatus" }, new Type[] { typeof(bool) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnApplicationPause Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnApplicationPause", 0, typeof(bool))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Game Event/OnApplicationPause(bool)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnApplicationPause", typeof(void), new string[] { "pauseStatus" }, new Type[] { typeof(bool) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnApplicationQuit Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnApplicationQuit", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Game Event/OnApplicationQuit()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnApplicationQuit", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionEnter Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnCollisionEnter", 0, typeof(Collision))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionEnter(Collision)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionEnter", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionEnter2D Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnCollisionEnter2D", 0, typeof(Collision2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionEnter2D(Collision2D)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionEnter2D", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionExit Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnCollisionExit", 0, typeof(Collision))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionExit(Collision)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionExit", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionExit2D Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnCollisionExit2D", 0, typeof(Collision2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionExit2D(Collision2D)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionExit2D", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionStay Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnCollisionStay", 0, typeof(Collision))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionStay(Collision)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionStay", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionStay2D Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnCollisionStay2D", 0, typeof(Collision2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionStay2D(Collision2D)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionStay2D", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnParticleCollision Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnParticleCollision", 0, typeof(GameObject))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnParticleCollision(GameObject)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnParticleCollision", typeof(void), new string[] { "other" }, new Type[] { typeof(GameObject) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerEnter Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTriggerEnter", 0, typeof(Collider))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerEnter(Collider)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerEnter", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerEnter2D Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTriggerEnter2D", 0, typeof(Collider2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerEnter2D(Collider2D)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerEnter2D", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerExit Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTriggerExit", 0, typeof(Collider))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerExit(Collider)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerExit", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerExit2D Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTriggerExit2D", 0, typeof(Collider2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerExit2D(Collider2D)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerExit2D", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerStay Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTriggerStay", 0, typeof(Collider))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerStay(Collider)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerStay", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerStay2D Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTriggerStay2D", 0, typeof(Collider2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerStay2D(Collider2D)"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerStay2D", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTransformChildrenChanged Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTransformChildrenChanged", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Transfrom/OnTransformChildrenChanged()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTransformChildrenChanged", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTransformParentChanged Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnTransformParentChanged", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Transfrom/OnTransformParentChanged()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTransformParentChanged", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseDown Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnMouseDown", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseDown()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseDown", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseDrag Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnMouseDrag", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseDrag()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseDrag", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseEnter Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnMouseEnter", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseEnter()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseEnter", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseExit Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnMouseExit", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseExit()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseExit", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseOver Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnMouseOver", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseOver()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseOver", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseUp Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnMouseUp", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseUp()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseUp", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseUpAsButton Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnMouseUpAsButton", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseUpAsButton()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseUpAsButton", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnBecameInvisible Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnBecameInvisible", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnBecameInvisible()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnBecameInvisible", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnBecameVisible Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnBecameVisible", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnBecameVisible()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnBecameVisible", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnPostRender Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnPostRender", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnPostRender()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnPostRender", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnPreCull Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnPreCull", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnPreCull()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnPreCull", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnPreRender Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnPreRender", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnPreRender()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnPreRender", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnRenderObject Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnRenderObject", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnRenderObject()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnRenderObject", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnRenderImage Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnRenderImage", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnRenderImage()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnRenderImage", typeof(void), new[] { "src", "dest" }, new[] { typeof(RenderTexture), typeof(RenderTexture) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnWillRenderObject Event
						bool hasFunction = false;
						if(functionSystem.GetFunction("OnWillRenderObject", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnWillRenderObject()"), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnWillRenderObject", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					//if(editorData.graph is uNodeClass) 
					{
						{//OnDrawGizmos Event
							bool hasFunction = false;
							if(functionSystem.GetFunction("OnDrawGizmos", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/OnDrawGizmos()"), hasFunction, () => {
								if(!hasFunction) {
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDrawGizmos", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
						{//OnDrawGizmosSelected Event
							bool hasFunction = false;
							if(functionSystem.GetFunction("OnDrawGizmosSelected", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/OnDrawGizmosSelected()"), hasFunction, () => {
								if(!hasFunction) {
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDrawGizmosSelected", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
						{//OnValidate Event
							bool hasFunction = false;
							if(functionSystem.GetFunction("OnValidate", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/OnValidate()"), hasFunction, () => {
								if(!hasFunction) {
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnValidate", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
						{//Reset Event
							bool hasFunction = false;
							if(functionSystem.GetFunction("Reset", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/Reset()"), hasFunction, () => {
								if(!hasFunction) {
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Reset", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
					}
				}
				#endregion

				#region Override
				{
					MethodInfo[] methods = inheritType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate (MethodInfo info) {
						if(!info.IsAbstract && !info.IsVirtual)
							return false;
						if(info.IsStatic)
							return false;
						if(info.IsSpecialName)
							return false;
						if(info.IsPrivate)
							return false;
						if(info.IsConstructor)
							return false;
						if(info.Name.StartsWith("get_", StringComparison.Ordinal))
							return false;
						if(info.Name.StartsWith("set_", StringComparison.Ordinal))
							return false;
						if(info.ContainsGenericParameters)
							return false;
						if(!info.IsPublic && !info.IsFamily)
							return false;
						if(info.IsFamilyAndAssembly)
							return false;
						if(info.IsDefinedAttribute(typeof(ObsoleteAttribute)))
							return false;
						if(info.GetCustomAttributes(true).Length > 0) {
							if(info.IsDefinedAttribute(typeof(System.Runtime.ConstrainedExecution.ReliabilityContractAttribute)))
								return false;
						}
						return true;
					}).ToArray();
					foreach(var method in methods) {
						bool hasFunction = false;
						if(functionSystem.GetFunction(method.Name, method.GetGenericArguments().Length,
							method.GetParameters()
							.Select(item => item.ParameterType).ToArray())) {
							hasFunction = true;
						}
						var m = method;
						menu.AddItem(new GUIContent("Override Function/" + EditorReflectionUtility.GetPrettyMethodName(method)), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, m.Name, m.ReturnType,
									m.GetParameters().Select(item => item.Name).ToArray(),
									m.GetParameters().Select(item => item.ParameterType).ToArray(),
									m.GetGenericArguments().Select(item => item.Name).ToArray(),
									(function) => {
										function.modifier.Override = true;
										function.modifier.Private = m.IsPrivate;
										function.modifier.Public = m.IsPublic;
										function.modifier.Internal = m.IsAssembly;
										function.modifier.Protected = m.IsFamily;
										if(m.IsFamilyOrAssembly) {
											function.modifier.Internal = true;
											function.modifier.Protected = true;
										}
									});
								GraphChanged();
							}
						});
					}
				}
				#endregion

				#region Hide Function
				{
					MethodInfo[] methods = inheritType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate (MethodInfo info) {
						if(info.IsStatic)
							return false;
						if(info.IsPrivate)
							return false;
						if(info.IsAbstract)
							return false;
						if(info.IsConstructor)
							return false;
						if(info.IsSpecialName)
							return false;
						if(info.Name.StartsWith("get_", StringComparison.Ordinal))
							return false;
						if(info.Name.StartsWith("set_", StringComparison.Ordinal))
							return false;
						if(info.ContainsGenericParameters)
							return false;
						if(!info.IsPublic && !info.IsFamily)
							return false;
						if(info.IsDefinedAttribute(typeof(ObsoleteAttribute)))
							return false;
						if(info.GetCustomAttributes(true).Length > 0) {
							if(info.IsDefinedAttribute(typeof(System.Runtime.ConstrainedExecution.ReliabilityContractAttribute)))
								return false;
						}
						return true;
					}).ToArray();
					foreach(var method in methods) {
						bool hasFunction = false;
						if(functionSystem.GetFunction(method.Name, method.GetGenericArguments().Length,
							method.GetParameters()
							.Select(item => item.ParameterType).ToArray())) {
							hasFunction = true;
						}
						var m = method;
						menu.AddItem(new GUIContent("Hide Function/" + EditorReflectionUtility.GetPrettyMethodName(method)), hasFunction, () => {
							if(!hasFunction) {
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, m.Name, m.ReturnType,
									m.GetParameters().Select(item => item.Name).ToArray(),
									m.GetParameters().Select(item => item.ParameterType).ToArray(),
									m.GetGenericArguments().Select(item => item.Name).ToArray(),
									(function) => {
										function.modifier.New = true;
										function.modifier.Private = m.IsPrivate;
										function.modifier.Public = m.IsPublic;
										function.modifier.Internal = m.IsAssembly;
										function.modifier.Protected = m.IsFamily;
										if(m.IsFamilyOrAssembly) {
											function.modifier.Internal = true;
											function.modifier.Protected = true;
										}
									});
								GraphChanged();
							}
						});
					}
				}
				#endregion

				#region Implement Interfaces
				var interfaceSystem = graphData.graph as IInterfaceSystem;
				if(interfaceSystem != null && interfaceSystem.Interfaces.Count > 0) {
					foreach(var inter in interfaceSystem.Interfaces) {
						if(inter == null || !inter.isFilled)
							continue;
						Type t = inter.type;
						if(t != null) {
							MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate (MethodInfo info) {
								if(info.Name.StartsWith("get_", StringComparison.Ordinal))
									return false;
								if(info.Name.StartsWith("set_", StringComparison.Ordinal))
									return false;
								return true;
							}).ToArray();
							foreach(var method in methods) {
								bool hasFunction = false;
								if(functionSystem.GetFunction(method.Name, method.GetGenericArguments().Length,
									method.GetParameters()
									.Select(item => item.ParameterType).ToArray())) {
									hasFunction = true;
								}

								var m = method;
								menu.AddItem(new GUIContent("Interface " + t.Name + "/" + EditorReflectionUtility.GetPrettyMethodName(method)), hasFunction, () => {
									if(!hasFunction) {
										NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, m.Name, m.ReturnType,
											m.GetParameters().Select(item => item.Name).ToArray(),
											m.GetParameters().Select(item => item.ParameterType).ToArray(),
											m.GetGenericArguments().Select(item => item.Name).ToArray());
										GraphChanged();
									}
								});
							}
						}
					}
				}
				#endregion
			}
			menu.ShowAsContext();
			return true;
		}

		public override bool CreateNewClass(Vector2 mousePosition, Action postAction) {
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Class"), false, () => {
				if(tabData.owner is IScriptGraph scriptGraph) {
					var newAsset = ScriptableObject.CreateInstance<ClassScript>();
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newClass";
					}
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
					postAction?.Invoke();
				}
				else {
					throw new InvalidOperationException();
				}
			});
			menu.AddItem(new GUIContent("Struct"), false, () => {
				if(tabData.owner is IScriptGraph scriptGraph) {
					var newAsset = ScriptableObject.CreateInstance<ClassScript>();
					newAsset.inheritType = typeof(ValueType);
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newStruct";
					}
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
				}
				else {
					throw new InvalidOperationException();
				}
				postAction?.Invoke();
			});
			if(tabData.owner is IScriptGraph scriptGraph) {
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Interface"), false, () => {
					var newAsset = ScriptableObject.CreateInstance<InterfaceScript>();
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newInterface";
					}
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
					postAction?.Invoke();
				});
				menu.AddItem(new GUIContent("Enum"), false, () => {
					var newAsset = ScriptableObject.CreateInstance<EnumScript>();
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newEnum";
					}
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
					postAction?.Invoke();
				});
			}
			menu.ShowAsContext();
			return true;
		}

		public override IEnumerable<DropdownMenuItem> ContextMenuForGraph(Vector2 mousePosition) {
			if(graph != null) {
				var converters = GraphUtility.FindGraphConverters();
				var current = graph;
				for(int x = 0; x < converters.Count; x++) {
					var converter = converters[x];
					if(!converter.IsValid(graph)) continue;
					yield return new DropdownMenuAction("Convert/" + converter.GetMenuName(current), evt => {
						converter.Convert(current);
						uNodeGUIUtility.GUIChangedMajor(null);
					}, DropdownMenuAction.AlwaysEnabled);
				}
			}
			yield break;
		}
	}
}