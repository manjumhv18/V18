using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraControl : MonoBehaviour
{
    public Transform target; // Target object to rotate around
    public float rotationSpeed = 100f;
    public float zoomSpeed = 10f;
    public float minY = 0.5f; // Minimum Y position for the camera
    public float maxY = 80f;  // Maximum Y position for the camera
    public float maxZoom = 40f;
    public float minZoom = 5f;
    public float smoothTime = 0.2f; // Smooth time for transitions

    // Orbit height options
    public bool lockOrbitHeight = false; // NEW: when false, orbit height is not forced so pitch can reach minY/maxY
    public float orbitYHeight = 9.125f;

    // When true and camera is perspective, orbit logic is suspended (script stays enabled for edit/draw)
    public bool suspendInPerspective = false;

    private float distance;
    private float targetX;
    private float targetY;
    private float smoothX;
    private float smoothY;
    private Vector2 previousPointerPos; // unified pointer position (mouse/touch)
    private float targetDistance;
    private float distanceVelocity;
    private float rotationXVelocity;
    private float rotationYVelocity;

    [SerializeField] Camera cam;

    void Start()
    {
        if (cam.orthographic)
        {
            targetDistance = cam.orthographicSize;
        }
        else
        {
            Vector3 direction = transform.position - target.position;
            distance = direction.magnitude;
            targetDistance = distance;

            // Only enforce initial orbit height if locking is enabled
            if (lockOrbitHeight)
            {
                var p = transform.position;
                p.y = orbitYHeight;
                transform.position = p;
            }
        }

        targetX = transform.eulerAngles.y;
        targetY = transform.eulerAngles.x;
        smoothX = targetX;
        smoothY = targetY;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // If we are in perspective and suspended (FP owns the camera or blending), skip orbit logic entirely
        if (!cam.orthographic && suspendInPerspective)
            return;

//#if UNITY_ANDROID
//        HandleTouchInput();
//#else
//        HandleMouseInput();
//#endif

        HandleMouseInput();

        SmoothMovement();
    }

    void HandleMouseInput()
    {
        // Skip if interacting with UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            previousPointerPos = Input.mousePosition;
        }

        if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 normalizedDelta = GetNormalizedDelta(previousPointerPos, mousePos);

            // Normalize by screen size so rotation stays consistent across window sizes
            targetX += normalizedDelta.x * rotationSpeed;
            targetY -= normalizedDelta.y * rotationSpeed;
            targetY = Mathf.Clamp(targetY, minY, maxY);

            previousPointerPos = mousePos;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            if (cam.orthographic)
            {
                targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minZoom, maxZoom);
            }
            else
            {
                targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minZoom, maxZoom);
            }
        }
    }

    void HandleTouchInput()
    {
        // Skip if touching UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(0))
            return;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                previousPointerPos = touch.position;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 normalizedDelta = GetNormalizedDelta(previousPointerPos, touch.position);

                targetX += normalizedDelta.x * rotationSpeed;
                targetY -= normalizedDelta.y * rotationSpeed;
                targetY = Mathf.Clamp(targetY, minY, maxY);

                previousPointerPos = touch.position;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            if (cam.orthographic)
            {
                targetDistance = Mathf.Clamp(targetDistance + deltaMagnitudeDiff * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
            }
            else
            {
                targetDistance = Mathf.Clamp(targetDistance + deltaMagnitudeDiff * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
            }
        }
    }

    // Normalize delta by current screen size; avoids "faster orbit on larger canvas" in WebGL
    private static Vector2 GetNormalizedDelta(Vector2 previous, Vector2 current)
    {
        float w = Mathf.Max(Screen.width, 1);
        float h = Mathf.Max(Screen.height, 1);

        Vector2 pixelDelta = current - previous;
        return new Vector2(pixelDelta.x / w, pixelDelta.y / h);
    }

    void SmoothMovement()
    {
        smoothX = Mathf.SmoothDamp(smoothX, targetX, ref rotationXVelocity, smoothTime);
        smoothY = Mathf.SmoothDamp(smoothY, targetY, ref rotationYVelocity, smoothTime);

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetDistance, ref distanceVelocity, smoothTime);
        }
        else
        {
            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, smoothTime);

            Quaternion rotation = Quaternion.Euler(smoothY, smoothX, 0);
            Vector3 direction = new Vector3(0, 0, -distance);
            Vector3 pos = target.position + rotation * direction;

            // Only force fixed orbit height if locking is enabled
            if (lockOrbitHeight)
                pos.y = orbitYHeight;

            transform.position = pos;
            transform.LookAt(target.position);
        }
    }

    // Align internal smoothing/targets to current camera transform
    public void ResetControls()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        targetX = transform.eulerAngles.y;
        targetY = Mathf.Clamp(transform.eulerAngles.x, minY, maxY);
        smoothX = targetX;
        smoothY = targetY;

        rotationXVelocity = 0f;
        rotationYVelocity = 0f;
        distanceVelocity = 0f;

        if (cam.orthographic)
        {
            targetDistance = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            if (target != null)
                distance = Vector3.Distance(transform.position, target.position);
            else
                distance = 0f;

            targetDistance = Mathf.Clamp(distance, minZoom, maxZoom);

            if (lockOrbitHeight)
            {
                var p = transform.position;
                p.y = orbitYHeight;
                transform.position = p;
            }
        }
    }

    // Public API to suspend orbit logic only in perspective
    public void SetOrbitSuspended(bool suspended)
    {
        suspendInPerspective = suspended;
    }
}