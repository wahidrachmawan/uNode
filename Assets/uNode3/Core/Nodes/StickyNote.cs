using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;

namespace MaxyGames.UNode.Nodes {
	public class StickyNote : Node {
		[Tooltip("The background color of the node")]
		public Color backgroundColor = Color.clear;
		[Tooltip("The text color of the note")]
		public Color textColor = Color.clear;
		[Tooltip("If true, the title will be hide")]
		public bool hideTitle;
		[Tooltip("The title text size")]
		public float titleSize;
		[Tooltip("The description text size")]
		public float descriptionSize;

		[Tooltip("If false, will node size will auto fit to the text label")]
		public bool customNodeSize;

		protected override void OnRegister() { }

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.NoteIcon);
		}
	}
}