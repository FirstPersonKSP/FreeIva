#if Experimental
using UnityEngine;

namespace FreeIva
{
    /// <summary>
    /// Experimental implementation of moving objects around, with a view to carrying objects in IVA.
    /// Can be used for moving any object that has a collider, without respect for physics.
    /// Made obsolete by InteractionCollider.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ColliderManipulator : MonoBehaviour
    {
        private Vector3 screenPoint;
        private Vector3 offset;
        //bool _mouseWasDown = false; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! TODO

        public static bool MovingObject = false;
		
        public void Update()
        {
            // Tempcode
            if (Input.GetMouseButton(0))
            {
                if (Camera.main != null)
                {
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, float.PositiveInfinity, (int)Layers.Kerbals))
                    {
                        ScreenMessages.PostScreenMessage("Selected " + hit.collider.name, 1f, ScreenMessageStyle.LOWER_CENTER);
                        FreeIva.SelectedObject = hit.collider.gameObject;
                    }
                }
            }

            return; /*/ !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! TODO



            if (Input.GetKeyDown(KeyCode.Space))
            {
                ScreenMessages.PostScreenMessage((MovingObject ? "Starting" : "Stopping") + " move object.", 1f, ScreenMessageStyle.LOWER_CENTER);
                MovingObject = !MovingObject;
                if (FreeIva.SelectedObject != null)
                    offset = FreeIva.SelectedObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
                else
                    offset = Vector3.zero;
            }

            if (Input.GetMouseButton(0))
            {
                GetObjectUnderMouse();
            }

            if (MovingObject)
                MoveSelectedTransform();


            /* Mouse movement code
             * 
            if (Input.GetMouseButton(0))
            {
                Debug.Log("Mouse is down");
                if (!_mouseWasDown)
                {
                    Debug.Log("Mouse click");
                    MouseDown();
                    _mouseWasDown = true;
                }
                else
                {
                    Debug.Log("Mouse drag");
                    MouseDrag();
                }
            }
            else
            {
                if (_mouseWasDown)
                {
                    Debug.Log("Mouse raising");
                    MouseUp();
                    _mouseWasDown = false;
                }
            }*/
        }

        private GameObject _draggedObject = null;

        public void MouseDown()
        {
            ScreenMessages.PostScreenMessage("MouseDown", 1f, ScreenMessageStyle.LOWER_CENTER);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) // float.PositiveInfinity
            {
                ScreenMessages.PostScreenMessage(hit.collider.name, 1f, ScreenMessageStyle.LOWER_CENTER);

                _draggedObject = hit.collider.gameObject;
                screenPoint = Camera.main.WorldToScreenPoint(_draggedObject.transform.position);
                offset = _draggedObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
            }
        }

        public void MouseDrag()
        {
            if (_draggedObject != null)
            {
                Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;
                _draggedObject.transform.position = /*InternalSpace.WorldToInternal(*/cursorPosition;
            }
        }

        public void MouseUp()
        {
            _draggedObject = null;
        }

        // Doesn't work
        public void OnMouseDown()
        {
            ScreenMessages.PostScreenMessage("OnMouseDown", 1f, ScreenMessageStyle.LOWER_CENTER);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                ScreenMessages.PostScreenMessage("Hit " + hit.collider, 1f, ScreenMessageStyle.LOWER_CENTER);
            }
        }

        public void GetObjectUnderMouse()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) // float.PositiveInfinity
            {
                ScreenMessages.PostScreenMessage("Selected " + hit.collider.name, 1f, ScreenMessageStyle.LOWER_CENTER);
                FreeIva.SelectedObject = hit.collider.gameObject;
            }
        }

        public void MoveSelectedTransform()
        {
            if (FreeIva.SelectedObject == null) return;

            Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint)/10 + offset;
            FreeIva.SelectedObject.transform.position = cursorPosition;
        }
    }
}
#endif