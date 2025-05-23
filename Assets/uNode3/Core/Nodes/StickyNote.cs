using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;

namespace MaxyGames.UNode.Nodes {
	public class StickyNote : Node {
		public Color backgroundColor = Color.clear;
		public Color textColor = Color.clear;
		public bool hideTitle;
		public float titleSize;
		[AllowAssetReference]
		public Font titleFont;

		public float descriptionSize;
		[AllowAssetReference]
		public Font descriptionFont;

		protected override void OnRegister() { }

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.NoteIcon);
		}
	}
}