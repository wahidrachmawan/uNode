#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace MaxyGames.UNode.Nodes {
	[EventMenu("Input System", "On Input System Event")]
	[StateEvent]
	public class EventOnInputSystemButton : BaseComponentEvent {
		[Serializable]
		public class Data {
			public string id;
			public string name;
			public string assetGuid;
		}
		public enum OutputType {
			Button = 0,
			Float = 1,
			Int = 4,
			Vector2 = 2,
			Vector3 = 3,
			Object = 99,
			Custom = 100,
		}
		public enum UpdateEvent {
			Update,
			FixedUpdate,
		}

		public enum InputActionOption {
			OnPressed,
			OnHold,
			OnReleased,
		}

		public UpdateEvent updateEvent;
		public InputActionOption inputActionChangeType;
		public OutputType outputType;
		[Hide(nameof(outputType), OutputType.Custom, hideOnSame = false)]
		[Filter(DisplayReferenceType = false)]
		public SerializedType customOutputType = typeof(int);

		[HideInInspector]
		public Data data = new Data();

		[NonSerialized]
		public ValueInput target;
		[NonSerialized]
		public ValueOutput output;

		private InputAction m_Action;
		private bool m_WasRunning;

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(UnityEngine.Object));
			target.filter = new FilterAttribute(typeof(PlayerInput), typeof(Component), typeof(GameObject));
			switch(outputType) {
				case OutputType.Custom: {
					output = ValueOutput(nameof(output), () => customOutputType);
					output.AssignGetCallback(flow => Operator.Convert(m_Action.ReadValueAsObject(), customOutputType));
					break;
				}
				case OutputType.Object: {
					output = ValueOutput(nameof(output), typeof(object));
					output.AssignGetCallback(flow => m_Action.ReadValueAsObject());
					break;
				}
				case OutputType.Float:
					output = ValueOutput(nameof(output), typeof(float));
					output.AssignGetCallback(flow => m_Action.ReadValue<float>());
					break;
				case OutputType.Int:
					output = ValueOutput(nameof(output), typeof(int));
					output.AssignGetCallback(flow => m_Action.ReadValue<int>());
					break;
				case OutputType.Vector2:
					output = ValueOutput(nameof(output), typeof(Vector2));
					output.AssignGetCallback(flow => m_Action.ReadValue<Vector2>());
					break;
				case OutputType.Vector3:
					output = ValueOutput(nameof(output), typeof(Vector3));
					output.AssignGetCallback(flow => m_Action.ReadValue<Vector3>());
					break;
				//case OutputType.Button:
				//	output = ValueOutput(nameof(output), typeof(bool));
				//	output.AssignGetCallback(flow => m_Action.ReadValue<bool>());
				//	break;
			}
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			instance.eventData.onStart += obj => {
				var o = target.GetValue(obj.defaultFlow) ?? obj.target;
				if(o != null) {
					PlayerInput pi = null;
					if(o is PlayerInput) {
						pi = o as PlayerInput;
					}
					else if(o is GameObject) {
						pi = (o as GameObject).GetComponent<PlayerInput>();
					}
					else if(o is Component) {
						pi = (o as Component).GetComponent<PlayerInput>();
					}
					if(pi != null) {
						m_Action = pi.actions.FindAction(data.id, false);
						if(m_Action == null) {
							throw new Exception($"No action with id: '{data.id}' in {pi}.\nAction name: {data.name} ( For reference only )");
						}
					}
					else {
						throw new NullReferenceException("target PlayerInput is null");
					}
				}
			};
			if(updateEvent == UpdateEvent.Update) {
				UEvent.Register(UEventID.Update, instance.target as Component, () => OnUpdate(instance));
			}
			else {
				UEvent.Register(UEventID.FixedUpdate, instance.target as Component, () => OnUpdate(instance));
			}
		}

		void OnUpdate(GraphInstance instance) {
			if(m_Action == null)
				return;
			bool shouldtrigger;
			// "Started" is true while the button is held, triggered is true only one frame. hence what looks like a bug but isn't
			switch(inputActionChangeType) {
				case InputActionOption.OnPressed:
					shouldtrigger = m_Action.triggered; // started is true too long
					break;
				case InputActionOption.OnHold:
					shouldtrigger = m_Action.phase == InputActionPhase.Started; // triggered is only true one frame
					break;
				case InputActionOption.OnReleased:
					shouldtrigger = m_WasRunning && m_Action.phase != InputActionPhase.Started; // never equal to InputActionPhase.Cancelled when polling
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			m_WasRunning = m_Action.phase == InputActionPhase.Started;

			if(shouldtrigger)
				Trigger(instance);
		}

		public override void OnGeneratorInitialize() {
			string actionName;
			CG.RegisterUserObject(
				new[] {
					actionName = CG.RegisterPrivateVariable(CG.GenerateNewName(nameof(m_Action)), typeof(InputAction)),
					inputActionChangeType == InputActionOption.OnReleased ? CG.RegisterPrivateVariable(CG.GenerateNewName(nameof(m_WasRunning)), typeof(bool)) : "",
				},
				this
			);
			if(output != null && output.hasValidConnections) {
				CG.RegisterPort(output, () => {
					switch(outputType) {
						case OutputType.Custom:
							return actionName.CGInvoke(nameof(m_Action.ReadValueAsObject)).CGConvert(customOutputType);
						case OutputType.Object:
							return actionName.CGInvoke(nameof(m_Action.ReadValueAsObject));
						case OutputType.Float:
							return actionName.CGInvoke(nameof(m_Action.ReadValue), new[] { typeof(float) });
						case OutputType.Int:
							return actionName.CGInvoke(nameof(m_Action.ReadValue), new[] { typeof(int) });
						//case OutputType.Button:
						//	return actionName.CGInvoke(nameof(m_Action.ReadValue), new[] { typeof(bool) });
						case OutputType.Vector2:
							return actionName.CGInvoke(nameof(m_Action.ReadValue), new[] { typeof(Vector2) });
						case OutputType.Vector3:
							return actionName.CGInvoke(nameof(m_Action.ReadValue), new[] { typeof(Vector3) });
					}
					return null;
				});
			}
		}

		public override void GenerateEventCode() {
			var contents = GenerateRunFlows();
			if(!string.IsNullOrEmpty(contents)) {
				var names = CG.GetUserObject<string[]>(this);
				var startMethod = CG.GetOrRegisterFunction("Start", typeof(void));
				if(target.isAssigned) {
					if(target.ValueType == typeof(PlayerInput)) {
						startMethod.AddCodeForEvent(
							CG.WrapWithInformation(
								CG.Set(
									names[0],
									target.CGValue()
										.CGAccess(nameof(PlayerInput.actions))
										.CGInvoke(
											nameof(PlayerInput.actions.FindAction),
											data.id.CGValue(),
											true.CGValue()
										)
								)
								, this)
						);
					}
					else {
						startMethod.AddCodeForEvent(
							CG.WrapWithInformation(
								CG.Set(
									names[0],
									target.CGValue()
										.CGInvoke(
											nameof(Component.GetComponent),
											new[] { typeof(PlayerInput) })
										.CGAccess(nameof(PlayerInput.actions))
										.CGInvoke(
											nameof(PlayerInput.actions.FindAction),
											data.id.CGValue(),
											true.CGValue()
										)
								)
								, this)
						);
					}
				}
				else {
					startMethod.AddCodeForEvent(
						CG.WrapWithInformation(
							CG.Set(
								names[0],
								CG.This
									.CGInvoke(
										nameof(Component.GetComponent),
										new[] { typeof(PlayerInput) })
									.CGAccess(nameof(PlayerInput.actions))
									.CGInvoke(
										nameof(PlayerInput.actions.FindAction),
										data.id.CGValue(),
										true.CGValue()
									)
							)
							, this)
					);
				}
				var mData = CG.GetOrRegisterFunction(updateEvent.ToString(), typeof(void));
				mData.AddCodeForEvent(
					CG.WrapWithInformation(
						CG.If(
							CG.Compare(names[0], CG.Null, ComparisonType.NotEqual),
							CG.Flow(
								inputActionChangeType == InputActionOption.OnPressed ?
									CG.If(
										names[0].CGAccess(nameof(InputAction.triggered)),
										contents
									)
								: inputActionChangeType == InputActionOption.OnHold ?
									CG.If(
										names[0].CGAccess(nameof(InputAction.phase)).CGCompare(InputActionPhase.Started.CGValue()),
										contents
									)
								: inputActionChangeType == InputActionOption.OnReleased ?
									CG.Flow(
										CG.If(
											CG.And(
												names[1],
												names[0].CGAccess(nameof(InputAction.phase)).CGCompare(InputActionPhase.Started.CGValue(), ComparisonType.NotEqual)
											),
											contents
										),
										CG.Set(
											names[1],
											names[0].CGAccess(nameof(InputAction.phase)).CGCompare(InputActionPhase.Started.CGValue())
										)
									)
								: throw new InvalidOperationException()
							)
						)
						, this)
				);
			}
		}

		public override Type GetNodeIcon() {
			return typeof(PlayerInput);
		}

		public override string GetTitle() {
			switch(outputType) {
				case OutputType.Custom:
					return "On Input System: " + customOutputType.prettyName;
				case OutputType.Object:
					return "On Input System: Object";
				case OutputType.Button:
					return "On Input System: Button";
				case OutputType.Float:
					return "On Input System: Float";
				case OutputType.Int:
					return "On Input System: Int";
				case OutputType.Vector2:
					return "On Input System: Vector2";
				case OutputType.Vector3:
					return "On Input System: Vector3";
				default:
					throw new InvalidOperationException();
			}
		}
	}
}

#if UNITY_EDITOR
namespace MaxyGames.UNode.Editors {
	using UnityEditor;
	using MaxyGames.UNode.Nodes;
	using System.Linq;

	public class EventOnInputSystemButtonEditor : NodeDrawer<EventOnInputSystemButton> {
		public override void DrawLayouted(DrawerOption option) {
			var node = GetNode(option);

			DrawChilds(option);
			
			var rect = EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), new GUIContent("Action"));
			var inputAsset = uNodeEditorUtility.LoadAssetByGuid<InputActionAsset>(node.data.assetGuid);
			{//For Update action name if the original is changed.
				if(inputAsset != null) {
					foreach(var actionMap in inputAsset.actionMaps) {
						foreach(var action in actionMap.actions) {
							if(action.id.ToString() == node.data.id) {
								node.data.name = action.name;
							}
						}
					}
				}
			}
			if(GUI.Button(rect, string.IsNullOrEmpty(node.data.name) ? "<None>" : node.data.name)) {
				var assets = uNodeEditorUtility.FindAssetsByType<InputActionAsset>();
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("<None>"), string.IsNullOrEmpty(node.data.id), () => {
					node.data.id = "";
					node.data.name = "";
				});
				foreach(var asset in assets) {
					foreach(var actionMap in asset.actionMaps) {
						for(int i = 0; i < actionMap.actions.Count; i++) {
							var action = actionMap.actions[i];
							menu.AddItem(new GUIContent($"{asset.name}/{actionMap.name} > {action.name}"), node.data.id == action.id.ToString(), () => {
								node.data.id = action.id.ToString();
								node.data.name = action.name;
								node.data.assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
							});
						}
						//Debug.Log(actionMap);
						//Debug.Log(actionMap + string.Join(", ", actionMap.actions.Select(a => a.name)));
					}
				}
				menu.ShowAsContext();
			}
			if(inputAsset != null) {
				var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(inputAsset));
				foreach(var asset in assets) {
					if(asset is InputActionReference action && action.action?.id.ToString() == node.data.id) {
						EditorGUI.BeginDisabledGroup(true);
						EditorGUI.indentLevel++;
						EditorGUILayout.ObjectField("Action Asset", action, typeof(InputActionReference), false);
						EditorGUI.indentLevel--;
						EditorGUI.EndDisabledGroup();
						break;
					}
				}
			}
			DrawInputs(option);
			DrawOutputs(option);
			DrawErrors(option);
		}
	}
}
#endif
#endif