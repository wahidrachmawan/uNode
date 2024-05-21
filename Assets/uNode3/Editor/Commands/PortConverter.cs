using System;
using System.Linq;
using UnityEngine;
using System.Reflection;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.PortConverter {
	class CastConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			if(leftType == typeof(object) && output != null && output.GetNode() is MultipurposeNode outputNode) {
				if(outputNode.target.targetType == MemberData.TargetType.Null) {
					//Skip if the target is null.
					action?.Invoke(outputNode);
					return true;
				}
			}
			NodeEditorUtility.AddNewNode<Nodes.NodeValueConverter>(
				canvas,
				new Vector2(position.x - 250, position.y),
				(nod) => {
					nod.input.ConnectTo(output);
					nod.type = rightType;
					action?.Invoke(nod);
				});
			return true;
			//return NodeEditorUtility.AddNewNode<ASNode>(
			//	graph, parent,
			//		new Vector2(position.x - 250, position.y),
			//		(nod) => {
			//			nod.compactDisplay = true;
			//			nod.type = new MemberData(rightType);
			//			nod.target = GetConnection();
			//		});
		}

		public override bool IsValid() {
			if(force) {
				return rightType is not IFakeType;
			}
			return rightType.IsCastableTo(leftType) || rightType == typeof(string) || 
				(leftType == typeof(GameObject) || leftType.IsCastableTo(typeof(Component))) && (rightType == typeof(GameObject) || rightType.IsCastableTo(typeof(Component)));
		}

		public override int order {
			get {
				return int.MaxValue;
			}
		}
	}


	class EnumToNumberConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			NodeEditorUtility.AddNewNode<Nodes.NodeValueConverter>(
				canvas,
				new Vector2(position.x - 250, position.y),
				(nod) => {
					nod.input.ConnectTo(output);
					nod.type = rightType;
					action?.Invoke(nod);
				});
			return true;
		}

		public override bool IsValid() {
			if(rightType == null || leftType == null) return false;
			if(rightType == typeof(int) || rightType == typeof(byte) || rightType == typeof(sbyte) || rightType == typeof(short) || rightType == typeof(long)) {
				return leftType.IsEnum;
			}
			return false;
		}
	}

	class LambdaConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			var lambda = output.node.node as NodeLambda;
			lambda.delegateType = rightType;
			lambda.Register();
			action?.Invoke(lambda);
			return true;
		}

		public override bool IsValid() {
			if(canvas != null && output != null && output.node.node is NodeLambda lambda && lambda.autoDelegateType) {
				return rightType.IsSubclassOf(typeof(Delegate));
			}
			return false;
		}

		public override int order {
			get {
				return int.MaxValue - 100;
			}
		}
	}

	class StringToPrimitiveConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			NodeEditorUtility.AddNewNode<MultipurposeNode>(
				canvas,
				new Vector2(position.x - 250, position.y),
				(nod) => {
					nod.target = new MemberData(rightType.GetMethod("Parse", new Type[] { typeof(string) }));
					nod.parameters.First().input.ConnectTo(output);
					action?.Invoke(nod);
				});
			return true;
		}

		public override bool IsValid() {
			if(leftType == typeof(string)) {
				if(rightType == typeof(float) ||
					rightType == typeof(int) ||
					rightType == typeof(double) ||
					rightType == typeof(decimal) ||
					rightType == typeof(short) ||
					rightType == typeof(ushort) ||
					rightType == typeof(uint) ||
					rightType == typeof(long) ||
					rightType == typeof(byte) ||
					rightType == typeof(sbyte)) {
					return true;
				}
			}
			return false;
		}

		public override int order {
			get {
				return 1000000;
			}
		}
	}

	class ElementToArray : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			NodeEditorUtility.AddNewNode<MakeArrayNode>(canvas, new Vector2(position.x - 250, position.y),
				(nod) => {
					nod.elementType = rightType.GetElementType();
					nod.elements[0].port.ConnectTo(output);
					action?.Invoke(nod);
				});
			return true;
		}

		public override bool IsValid() {
			if(rightType == null || leftType == null) return false;
			return rightType.IsArray && leftType.IsCastableTo(rightType.GetElementType());
		}

		public override int order {
			get {
				return -1;
			}
		}
	}

	// class StringConverter : AutoConvertPort {
	// 	public override Node CreateNode() {
	// 		Node node = leftNode;
	// 		NodeEditorUtility.AddNewNode<MultipurposeNode>(
	// 			graph, parent,
	// 				new Vector2(position.x - 250, position.y),
	// 				(nod) => {
	// 					nod.target.target = new MemberData(typeof(object).GetMethod("ToString", Type.EmptyTypes));
	// 					nod.target.target.instance = GetLeftConnection();
	// 					node = nod;
	// 				});
	// 		return node;
	// 	}

	// 	public override bool IsValid() {
	// 		return rightType == typeof(string);
	// 	}
	// }

	// class GameObjectConverter : AutoConvertPort {
	// 	public override Node CreateNode() {
	// 		Node node = leftNode;
	// 		if(rightType is RuntimeType) {
	// 			return node;
	// 		}
	// 		if(rightType.IsCastableTo(typeof(Component))) {
	// 			NodeEditorUtility.AddNewNode<MultipurposeNode>(
	// 				graph, parent,
	// 				new Vector2(position.x - 250, position.y),
	// 				(nod) => {
	// 					nod.target.target = new MemberData(
	// 						typeof(GameObject).GetMethod("GetComponent", Type.EmptyTypes).MakeGenericMethod(rightType)
	// 					);
	// 					nod.target.target.instance = GetLeftConnection();
	// 					node = nod;
	// 				});
	// 		}
	// 		return node;
	// 	}

	// 	public override bool IsValid() {
	// 		return leftType == typeof(GameObject);
	// 	}
	// }

	class ComponentConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			if(leftType == typeof(Transform)) {
				if(rightType == typeof(Vector3)) {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(
						canvas,
						new Vector2(position.x - 250, position.y),
						(nod) => {
							nod.target = new MemberData(typeof(Transform).GetProperty("position"));
							nod.instance.ConnectTo(output);
							action?.Invoke(nod);
						});
					return true;
				} else if(rightType == typeof(Quaternion)) {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(
						canvas,
						new Vector2(position.x - 250, position.y),
						(nod) => {
							nod.target = new MemberData(typeof(Transform).GetProperty("rotation"));
							nod.instance.ConnectTo(output);
							action?.Invoke(nod);
						});
					return true;
				}
			}
			if(rightType == typeof(GameObject)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(typeof(Component).GetProperty("gameObject"));
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(Transform)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
						canvas,
						new Vector2(position.x - 250, position.y),
						(nod) => {
							nod.target = new MemberData(typeof(Component).GetProperty("transform"));
							nod.instance.ConnectTo(output);
							action?.Invoke(nod);
						});
				return true;
			} else if(rightType.IsCastableTo(typeof(Component))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							ReflectionUtils.MakeGenericMethod(typeof(Component).GetMethod("GetComponent", Type.EmptyTypes), rightType)
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			}
			return false;
		}

		public override bool IsValid() {
			if(leftType == typeof(Transform)) {
				if(rightType == typeof(Vector3)) {
					return true;
				} else if(rightType == typeof(Quaternion)) {
					return true;
				}
			}
			return false;
			// return leftType.IsCastableTo(typeof(Component));
		}
	}

	class QuaternionConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			if(rightType.IsCastableTo(typeof(Vector3))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(typeof(Quaternion).GetProperty("eulerAngles"));
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			}
			return false;
		}

		public override bool IsValid() {
			return leftType == typeof(Quaternion);
		}
	}

	class Vector3Converter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			if(rightType == typeof(Quaternion)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(Quaternion).GetMethod("Euler", new Type[] { typeof(Vector3) })
						);
						nod.parameters.First().input.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			}
			return false;
		}

		public override bool IsValid() {
			return leftType == typeof(Vector3) && rightType == typeof(Quaternion);
		}
	}

	class RaycastHitConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			#region RaycastHit
			if(rightType == typeof(Collider)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit).GetProperty("collider")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(Transform)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit).GetProperty("transform")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(Rigidbody)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit).GetProperty("rigidbody")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(GameObject)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit),
								typeof(RaycastHit).GetProperty("collider"),
								typeof(Collider).GetProperty("gameObject"),
							}
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType.IsCastableTo(typeof(Component))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit),
								typeof(RaycastHit).GetProperty("collider"),
								ReflectionUtils.MakeGenericMethod(typeof(Collider).GetMethod("GetComponent", Type.EmptyTypes), rightType),
							}
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType.IsCastableTo(typeof(float))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit).GetProperty("distance")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(Vector3)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit).GetProperty("point")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			}
			#endregion
			return false;
		}

		public override bool IsValid() {
			return leftType == typeof(RaycastHit) && (
				rightType == typeof(Collider) ||
				rightType == typeof(Transform) ||
				rightType == typeof(Rigidbody) ||
				rightType == typeof(GameObject) ||
				rightType == typeof(Vector3) ||
				rightType == typeof(Rigidbody) ||
				rightType.IsCastableTo(typeof(float)) ||
				rightType.IsCastableTo(typeof(Component))
			);
		}
	}

	class RaycastHit2DConverter : AutoConvertPort {
		public override bool CreateNode(System.Action<Node> action) {
			#region RaycastHit2D
			if(rightType == typeof(Collider2D)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("collider")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(Transform)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("transform")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(Rigidbody2D)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("rigidbody")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(GameObject)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit2D),
								typeof(RaycastHit2D).GetProperty("collider"),
								typeof(Collider).GetProperty("gameObject"),
							}
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType.IsCastableTo(typeof(Component))) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							new MemberInfo[] {
								typeof(RaycastHit2D),
								typeof(RaycastHit2D).GetProperty("collider"),
								ReflectionUtils.MakeGenericMethod(typeof(Collider).GetMethod("GetComponent", Type.EmptyTypes), rightType),
							}
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(float)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("distance")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			} else if(rightType == typeof(Vector3)) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(
					canvas,
					new Vector2(position.x - 250, position.y),
					(nod) => {
						nod.target = new MemberData(
							typeof(RaycastHit2D).GetProperty("point")
						);
						nod.instance.ConnectTo(output);
						action?.Invoke(nod);
					});
				return true;
			}
			#endregion
			return false;
		}

		public override bool IsValid() {
			return leftType == typeof(RaycastHit2D) && (
				rightType == typeof(Collider2D) ||
				rightType == typeof(Transform) ||
				rightType == typeof(Rigidbody2D) ||
				rightType == typeof(GameObject) ||
				rightType == typeof(Vector3) ||
				rightType.IsCastableTo(typeof(float)) ||
				rightType.IsCastableTo(typeof(Component))
			);
		}
	}
}