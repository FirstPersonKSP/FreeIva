using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Reuseable controls to aid with making UI.
    /// </summary>
    public static class GuiUtils
    {
        public static void label(string text, object obj)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            GUILayout.Label(obj == null ? "null" : obj.ToString(), GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }
        public static float editFloat(string text, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            string oldVal = value.ToString();
            if (!oldVal.Contains("."))
            {
                oldVal += ".0";
            }
            string newVal = GUILayout.TextField(oldVal);
            float f = value;
            float.TryParse(newVal, out f);
            GUILayout.EndHorizontal();
            return f;
        }
        public static int editInt(string text, int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            string oldVal = value.ToString();
            string newVal = GUILayout.TextField(oldVal);
            int i = value;
            int.TryParse(newVal, out i);
            GUILayout.EndHorizontal();
            return i;
        }

        public static void slider(string label, ref float variable, float from, float to)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ": " + variable.ToString());
            GUILayout.FlexibleSpace();
            variable = GUILayout.HorizontalSlider(variable, from, to, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }
        public static Color rgbaSlider(string label, ref float r, ref float g, ref float b, ref float a, float from, float to)
        {
            GUILayout.Label(label);
            slider("r", ref r, from, to);
            slider("g", ref g, from, to);
            slider("b", ref b, from, to);
            slider("a", ref a, from, to);
            return new Color(r, g, b, a);

        }


        static float x = 0;
        static float y = 0;
        public static KeyValuePair<float, float> GetSliderXY()
        {
            return new KeyValuePair<float, float>(x, y);
        }

        public static bool DrawGui = true;
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FreeIvaGui : MonoBehaviour
    {
        private static Rect windowPos = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);

        /// <summary>
        /// GUI draw event. Called (at least once) each frame.
        /// </summary>
        public void OnGUI()
        {
            if (GuiUtils.DrawGui)
                windowPos = GUILayout.Window(GetInstanceID(), windowPos, MainGUI, "Free IVA", GUILayout.Width(600), GUILayout.Height(50));
        }
        private static void MainGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GuiManager.Gui();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
