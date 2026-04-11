using UnityEngine;

public class CameraPanFollow : MonoBehaviour
{
    public Transform target;
    public Camera cam;
    public float dragSensitivity = 1f;

    Vector2 _pan;
    Vector3 _lastMouseScreen;
    bool _dragging;

    void Awake()
    {
        if (!cam)
            cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!cam || !target)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
            _pan = Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            _lastMouseScreen = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            _dragging = false;

        if (_dragging && cam.orthographic)
        {
            Vector3 cur = ScreenToWorldPlane(cam, Input.mousePosition);
            Vector3 prev = ScreenToWorldPlane(cam, _lastMouseScreen);
            _pan += dragSensitivity * (Vector2)(prev - cur);
            _lastMouseScreen = Input.mousePosition;
        }

        Vector3 p = target.position + (Vector3)_pan;
        p.z = cam.transform.position.z;
        cam.transform.position = p;
    }

    static Vector3 ScreenToWorldPlane(Camera c, Vector3 screen)
    {
        screen.z = Mathf.Abs(c.transform.position.z);
        return c.ScreenToWorldPoint(screen);
    }
}
