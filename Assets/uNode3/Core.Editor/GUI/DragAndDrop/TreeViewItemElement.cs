using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors.UI {
    internal interface ITreeViewItemElement {
        int index { get; set; }
        VisualElement element => this as VisualElement;

        Dictionary<string, object> GetDragGenericData();
        IEnumerable<Object> GetDraggedReferences();

		bool CanHaveChilds();
        bool CanDragInsideParent();
        bool CanDrag();
    }
}