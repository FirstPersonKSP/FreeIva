using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Used for triggering actions on interactive objects, such as clicking on a handle to unlock a hatch.
    /// </summary>
    public class ClickWatcher : MonoBehaviour
    {
        public static bool Debug = false;
        List<Action> mouseOverActions = new List<Action>();
        List<Action> mouseDownActions = new List<Action>();
        List<Action> mouseUpActions = new List<Action>();

        public void AddMouseDownAction(Action action)
        {
            mouseDownActions.Add(action);
        }

        public void AddMouseOverAction(Action action)
        {
            mouseOverActions.Add(action);
        }

        public void AddMouseUpAction(Action action)
        {
            mouseUpActions.Add(action);
        }

        public void OnMouseOver()
        {
            if (Debug)
                ScreenMessages.PostScreenMessage("OnMouseOver", 1f, ScreenMessageStyle.LOWER_CENTER);
            foreach (Action a in mouseOverActions) { a(); }
        }

        public void OnMouseDown()
        {
            if (Debug)
                ScreenMessages.PostScreenMessage("OnMouseDown", 1f, ScreenMessageStyle.LOWER_CENTER);
            foreach (Action a in mouseDownActions) { a(); }
        }

        public void OnMouseUp()
        {
            if (Debug)
                ScreenMessages.PostScreenMessage("OnMouseUp", 1f, ScreenMessageStyle.LOWER_CENTER);
            foreach (Action a in mouseUpActions) { a(); }
        }
    }
}
