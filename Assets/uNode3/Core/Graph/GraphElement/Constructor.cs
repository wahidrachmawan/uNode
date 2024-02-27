using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public sealed class Constructor : BaseFunction, IIcon {
		public ConstructorModifier modifier = new ConstructorModifier();

		public override bool AllowCoroutine() {
			return false;
		}

		public override Type ReturnType() {
			return typeof(void);
		}

		Type IIcon.GetIcon() {
			return typeof(TypeIcons.MethodIcon);
		}
	}
}