using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	//This node is not relevance now as uNode can handle GetComponent<MyGraph> etc

    //[NodeMenu("Data", "GetComponent", typeof(Component), inputs = new[] { typeof(GameObject), typeof(Component) })]
	public class GetComponentNode : ValueNode {
		[Filter(typeof(Component), OnlyGetType = true, ArrayManipulator = false, AllowInterface =true)]
		public SerializedType type = typeof(Component);
		[System.NonSerialized]
		public ValueInput target;
		public GetComponentKind getComponentKind;
		[Hide(nameof(getComponentKind), GetComponentKind.GetComponent)]
		public bool includeInactive;

		public enum GetComponentKind {
			GetComponent,
			GetComponentInChildren,
			GetComponentInParent,
		}

		protected override void OnRegister() {
			base.OnRegister();
			target = ValueInput(nameof(target), typeof(UnityEngine.Object));
			target.filter = new FilterAttribute(typeof(Component), typeof(GameObject));
		}

		public override System.Type ReturnType() {
			if(type.isFilled) {
				try {
					System.Type t = type.type;
					if(!object.ReferenceEquals(t, null)) {
						return t;
					}
				}
				catch { }
			}
			return typeof(object);
		}

		public override object GetValue(Flow flow) {
			var value = target.GetValue(flow);
			System.Type t = type.nativeType;
			if (value != null) {
				if(value.GetType() == t) return value;
				if(t.IsInterface || t.IsCastableTo(typeof(Component))) {
					if(value is GameObject gameObject) {
						if(t is RuntimeType) {
							switch(getComponentKind) {
								case GetComponentKind.GetComponent:
									return gameObject.GetGeneratedComponent(t as RuntimeType);
								case GetComponentKind.GetComponentInChildren:
									return gameObject.GetGeneratedComponentInChildren(t as RuntimeType, includeInactive);
								case GetComponentKind.GetComponentInParent:
									return gameObject.GetGeneratedComponentInParent(t as RuntimeType, includeInactive);
							}
						}
						return gameObject.GetComponent(t);
					} else if(value is Component component) {
						if(t is RuntimeType) {
							switch(getComponentKind) {
								case GetComponentKind.GetComponent:
									return component.GetGeneratedComponent(t as RuntimeType);
								case GetComponentKind.GetComponentInChildren:
									return component.GetGeneratedComponentInChildren(t as RuntimeType, includeInactive);
								case GetComponentKind.GetComponentInParent:
									return component.GetGeneratedComponentInParent(t as RuntimeType, includeInactive);
							}
						}
						return component.GetComponent(t);
					}
				} else {
					throw new System.InvalidOperationException("The type is not supported to use GetComponent: " + t.FullName);
				}
			}
			return value;
		}

		protected override string GenerateValueCode() {
			if(target.isAssigned && type.isAssigned) {
				System.Type t = type.type;
				if(t != null) {
					if(t.IsInterface || t.IsCastableTo(typeof(Component))) {
						if(t is RuntimeType runtimeType && runtimeType is not INativeMember) {
							CG.RegisterUsingNamespace("MaxyGames.UNode");//Register namespace to make sure Extensions work for GameObject or Component target type.
							if(CG.generatePureScript) {
								switch(getComponentKind) {
									case GetComponentKind.GetComponent:
										return CG.Value(target).CGInvoke(
											nameof(uNodeHelper.GetGeneratedComponent),
											new[] { t },
											null
										);
									case GetComponentKind.GetComponentInChildren:
										return CG.Value(target).CGInvoke(
											nameof(uNodeHelper.GetGeneratedComponentInChildren),
											new[] { t },
											new[] { includeInactive.CGValue() }
										);
									case GetComponentKind.GetComponentInParent:
										return CG.Value(target).CGInvoke(
											nameof(uNodeHelper.GetGeneratedComponentInChildren),
											new[] { t },
											new[] { includeInactive.CGValue() }
										);
								}
							}
							else {
								switch(getComponentKind) {
									case GetComponentKind.GetComponent:
										return CG.Value(target).CGInvoke(
											nameof(uNodeHelper.GetGeneratedComponent),
											new[] { CG.GetUniqueNameForType(runtimeType)
										});
									case GetComponentKind.GetComponentInChildren:
										return CG.Value(target).CGInvoke(
											nameof(uNodeHelper.GetGeneratedComponentInChildren),
											new[] { CG.GetUniqueNameForType(runtimeType), includeInactive.CGValue() }
										);
									case GetComponentKind.GetComponentInParent:
										return CG.Value(target).CGInvoke(
											nameof(uNodeHelper.GetGeneratedComponentInChildren),
											new[] { CG.GetUniqueNameForType(runtimeType), includeInactive.CGValue()
										});
								}
							}
						}
						else {
							switch(getComponentKind) {
								case GetComponentKind.GetComponent:
									return CG.Value(target).CGInvoke(nameof(Component.GetComponent), new[] { t }, null);
								case GetComponentKind.GetComponentInChildren:
									return CG.Value(target).CGInvoke(nameof(Component.GetComponentInChildren), new[] { t }, new[] { includeInactive.CGValue() });
								case GetComponentKind.GetComponentInParent:
									return CG.Value(target).CGInvoke(nameof(Component.GetComponentInParent), new[] { t }, new[] { includeInactive.CGValue() });
							}
						}
					}
					else {
						throw new System.InvalidOperationException("The type is not supported to use GetComponent: " + t.FullName);
					}
				}
			}
			throw new System.Exception("Target or Type is unassigned.");
		}

		public override string GetTitle() {
			return getComponentKind.ToString();
		}

		public override string GetRichName() {
			return $"{getComponentKind}({type.GetRichName()})" + target.GetRichName();
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(!analizer.CheckValue(type, nameof(type), this) && !analizer.CheckPort(target)) {
				System.Type t = type.type;
				System.Type targetType = target.ValueType;
				if(t != null && targetType != null) {
					if(t.IsCastableTo(typeof(Component)) || t.IsInterface) {
						bool valid = false;
						if(targetType.IsCastableTo(typeof(Component)) || targetType == typeof(GameObject)) {
							valid = true;
						}
						if(!valid) {
							analizer.RegisterError(this, $"The target type:{targetType.PrettyName()} is not castable to type:{t.PrettyName()}");
						}
					} else {
						analizer.RegisterError(this, $"The type must targeting 'UnityEngine.Component' or a interface");
					}
				}
			}
		}
	}
}