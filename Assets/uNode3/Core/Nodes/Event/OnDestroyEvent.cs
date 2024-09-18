using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Behavior", "On Destroy")]
    [StateEvent]
    [Description("Destroying the attached Behaviour will result in the game or Scene receiving OnDestroy.")]
    public class OnDestroyEvent : BaseComponentEvent {

        public override void GenerateEventCode() {
            DoGenerateCode(UEventID.OnDestroy);
        }
    }
}