using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode
{
	public sealed class Constructor : BaseFunction, IIcon
	{
		public ConstructorModifier modifier = new ConstructorModifier();
		public ConstructorInitializer InitializerType = ConstructorInitializer.None;
		public override bool AllowCoroutine()
		{
			return false;
		}

		public override Type ReturnType()
		{
			return typeof(void);
		}

		Type IIcon.GetIcon()
		{
			return typeof(TypeIcons.MethodIcon);
		}
	}

	public enum ConstructorInitializer
	{
		None,
		Base,
		This
	}
}