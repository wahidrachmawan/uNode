using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Behavior", "On Enable")]
    [StateEvent]
    [Description("This function is called when the object becomes enabled and active.")]
    public class OnEnableEvent : BaseComponentEvent {
        public override void GenerateEventCode() {
            DoGenerateCode(UEventID.OnEnable);
        }
    }
}