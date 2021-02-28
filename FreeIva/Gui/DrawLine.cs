using UnityEngine;

namespace FreeIva.Gui
{
    public static class DrawLine
    {
        /* Example:
            if (myLineRenderer == null)
                myLineRenderer = CreateLine(myGameObject, Color.white, Color.green, 0.5f, 0f);
            myLineRenderer.SetPosition(0, myGameObject.transform.localPosition);
            myLineRenderer.SetPosition(1, myGameObject.transform.localPosition + myCoolOffset);
        */
        public static LineRenderer CreateLine(GameObject obj, Color startColor, Color endColor, float startWidth, float endWidth)
        {
            GameObject o = new GameObject();
            o.transform.parent = obj.transform;
            LineRenderer line = o.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Particles/Alpha Blended"));
            line.startColor = startColor;
            line.endColor = endColor;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.positionCount = 2;

            return line;
        }
    }
}
