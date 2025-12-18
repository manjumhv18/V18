using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GlobalTMPKeyboardAvoider : MonoBehaviour
{
    [Tooltip("Optional. If assigned, this RectTransform will be moved for all fields. " +
             "If not set, the script automatically uses the nearest Canvas of the focused field.")]
    public RectTransform moveRoot;

    [Tooltip("Extra space (in screen pixels) between the keyboard top and the input field bottom.")]
    public float padding = 16f;

    [Tooltip("Smoothly animate movement instead of snapping.")]
    public bool animate = true;

    [Tooltip("Animation speed (higher is snappier).")]
    public float lerpSpeed = 14f;

    // Internal state
    private TMP_InputField _currentField;
    private RectTransform _currentRoot;
    private Vector2 _currentRootBasePos;
    private float _currentShift; // applied shift on current root
    private Camera _uiCamera;

    // Track base positions for any root we touch so we can restore them correctly.
    private readonly Dictionary<RectTransform, Vector2> _rootBasePositions = new Dictionary<RectTransform, Vector2>();

    private void Awake()
    {
        // If a fixed moveRoot is provided, cache its base position.
        if (moveRoot != null)
        {
            CacheRootBase(moveRoot);
            _currentRoot = moveRoot;
            _currentRootBasePos = _rootBasePositions[_currentRoot];
        }
    }

    private void OnDisable()
    {
        // Restore any roots we have moved.
        foreach (var kvp in _rootBasePositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.anchoredPosition = kvp.Value;
            }
        }

        _currentField = null;
        _currentRoot = null;
        _currentShift = 0f;
    }

    private void Update()
    {
        // Track currently focused TMP_InputField.
        var focused = GetFocusedTMPInputField();

        if (focused != _currentField)
        {
            SwitchFocusedField(focused);
        }

        // If no field or no root, restore and exit.
        if (_currentField == null || _currentRoot == null)
        {
            ApplyShift(0f);
            return;
        }

        // Determine the visible bottom (keyboard top or safe area bottom).
        var keyboardTop = GetKeyboardTopY();
        var visibleBottom = Mathf.Max(Screen.safeArea.yMin, keyboardTop);

        // Compute input field screen rect.
        var fieldRt = _currentField.textViewport != null
            ? _currentField.textViewport    // more stable than the whole field
            : _currentField.GetComponent<RectTransform>();

        var fieldScreenRect = GetScreenRect(fieldRt, GetCanvasFor(_currentRoot), GetUICamera(GetCanvasFor(_currentRoot)));

        // Required bottom bound for the field (in pixels).
        var requiredBottom = visibleBottom + padding;

        // If field bottom is below required bottom, we need to shift up by the delta in pixels.
        var delta = requiredBottom - fieldScreenRect.yMin;
        var targetShift = Mathf.Max(0f, delta);

        ApplyShift(targetShift);
    }

    private void ApplyShift(float targetShift)
    {
        if (_currentRoot == null)
        {
            return;
        }

        // Smooth or snap towards target shift.
        if (animate)
        {
            _currentShift = Mathf.Lerp(_currentShift, targetShift, 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime));
            // Snap when very close to avoid sub-pixel drift.
            if (Mathf.Abs(_currentShift - targetShift) < 0.5f)
            {
                _currentShift = targetShift;
            }
        }
        else
        {
            _currentShift = targetShift;
        }

        // Apply shift on Y; X remains as base.
        var basePos = _rootBasePositions.TryGetValue(_currentRoot, out var baseAnchored) ? baseAnchored : _currentRoot.anchoredPosition;
        _currentRoot.anchoredPosition = new Vector2(basePos.x, basePos.y + _currentShift);
    }

    private void SwitchFocusedField(TMP_InputField newField)
    {
        // Restore previous root position when switching fields/roots.
        if (_currentRoot != null && _rootBasePositions.TryGetValue(_currentRoot, out var basePos))
        {
            _currentRoot.anchoredPosition = basePos;
        }

        _currentField = newField;
        _currentShift = 0f;

        // Pick the root to move.
        if (moveRoot != null)
        {
            _currentRoot = moveRoot;
        }
        else
        {
            _currentRoot = null;
            if (_currentField != null)
            {
                var canvas = GetCanvasFor(_currentField.GetComponent<RectTransform>());
                if (canvas != null)
                {
                    _currentRoot = canvas.GetComponent<RectTransform>();
                }
            }
        }

        if (_currentRoot != null)
        {
            CacheRootBase(_currentRoot);
            _currentRootBasePos = _rootBasePositions[_currentRoot];
        }
    }

    private void CacheRootBase(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        if (!_rootBasePositions.ContainsKey(root))
        {
            _rootBasePositions[root] = root.anchoredPosition;
        }
        else
        {
            // Refresh the cached base if it has changed externally.
            _rootBasePositions[root] = root.anchoredPosition;
        }
    }

    private static Canvas GetCanvasFor(Component c)
    {
        return c != null ? c.GetComponentInParent<Canvas>(true) : null;
    }

    private static Canvas GetCanvasFor(RectTransform rt)
    {
        return rt != null ? rt.GetComponentInParent<Canvas>(true) : null;
    }

    private Camera GetUICamera(Canvas canvas)
    {
        if (canvas == null)
        {
            return null;
        }

        // Cache once per session (assumes a single UI camera).
        if (_uiCamera == null)
        {
            _uiCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
            if (_uiCamera == null && Camera.main != null)
            {
                // Fallback for world-space or missing camera assignments.
                _uiCamera = Camera.main;
            }
        }

        return _uiCamera;
    }

    private static Rect GetScreenRect(RectTransform rectTransform, Canvas canvas, Camera cam)
    {
        if (rectTransform == null)
        {
            return Rect.zero;
        }

        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        // Convert world -> screen points considering render mode.
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[i]);
            }
            else
            {
                // ScreenSpaceOverlay or fallback
                screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCorners[i]);
            }

            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        return new Rect(min, max - min);
    }

    private static TMP_InputField GetFocusedTMPInputField()
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null)
        {
            return null;
        }

        // Check the object or its parents for a TMP_InputField.
        return go.GetComponentInParent<TMP_InputField>();
    }

    private static float GetKeyboardTopY()
    {
#if UNITY_IOS || UNITY_ANDROID
        // If area is available, use it.
        var area = TouchScreenKeyboard.area;
        if (TouchScreenKeyboard.visible && area.height > 0f)
        {
            return area.y + area.height;
        }

        // Fallback heuristic when area is not populated by the platform/Unity version.
        if (TouchScreenKeyboard.visible)
        {
            // 40% of the screen height is a decent approximation on many devices.
            return Mathf.Min(Screen.height, Screen.height * 0.4f);
        }
#endif
        // Editor or no keyboard: bottom is just the safe area bottom.
        return Screen.safeArea.yMin;
    }
}
