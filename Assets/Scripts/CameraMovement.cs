using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using TransformHandles.Utils;

[RequireComponent(typeof(Camera))]
public class CameraMovement : MonoBehaviour
{
    [Range(0f, 9f)][SerializeField] private float sensitivity = 2f;
    [Range(0f, 90f)][SerializeField] private float yRotationLimit = 60f;
    [Range(0f, 90f)][SerializeField] private float xRotationLimit = 60f;
    [SerializeField] private float zoomSpeed = 10f;

    private Camera _camera;
    private Vector2 _rotation = Vector2.zero;
    private float _fieldOfView;

    private float _prevPinchDistance;

    private void Awake()
    {
        InputUtils.EnableEnhancedTouch();

        _camera = GetComponent<Camera>();
        _rotation = transform.localRotation.eulerAngles;
        _fieldOfView = _camera.fieldOfView;
    }

    private void Update()
    {
#if UNITY_EDITOR
        UpdateRotationMouse();
        UpdateCameraZoomMouse();
#else
        UpdateRotationTouch();
        UpdateCameraZoomTouch();
#endif
    }

    // ---------------- PC（Mouse） ----------------
    private void UpdateRotationMouse()
    {
        if (Mouse.current == null || sensitivity == 0f) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        _rotation.x += mouseDelta.x * sensitivity * Time.deltaTime;
        _rotation.x = Mathf.Clamp(_rotation.x, -xRotationLimit, xRotationLimit);

        _rotation.y += mouseDelta.y * sensitivity * Time.deltaTime;
        _rotation.y = Mathf.Clamp(_rotation.y, -yRotationLimit, yRotationLimit);

        ApplyRotation();
    }

    private void UpdateCameraZoomMouse()
    {
        if (Mouse.current == null || zoomSpeed == 0f) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        _fieldOfView -= scroll * zoomSpeed * Time.deltaTime;
        _fieldOfView = Mathf.Clamp(_fieldOfView, 35f, 100f);
        _camera.fieldOfView = _fieldOfView;
    }
    // ---------------- Mobile（Touch） ----------------
    private void UpdateRotationTouch()
    {
        if (Touch.activeTouches.Count == 1)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.delta;
                _rotation.x += delta.x * sensitivity * 0.01f;
                _rotation.x = Mathf.Clamp(_rotation.x, -xRotationLimit, xRotationLimit);

                _rotation.y += delta.y * sensitivity * 0.01f;
                _rotation.y = Mathf.Clamp(_rotation.y, -yRotationLimit, yRotationLimit);

                ApplyRotation();
            }
        }
    }

    private void UpdateCameraZoomTouch()
    {
        if (Touch.activeTouches.Count >= 2)
        {
            var t1 = Touch.activeTouches[0];
            var t2 = Touch.activeTouches[1];

            float currentDistance = Vector2.Distance(t1.screenPosition, t2.screenPosition);

            if (_prevPinchDistance > 0f)
            {
                float pinchDelta = currentDistance - _prevPinchDistance;
                _fieldOfView -= pinchDelta * zoomSpeed * 0.02f;
                _fieldOfView = Mathf.Clamp(_fieldOfView, 35f, 100f);
                _camera.fieldOfView = _fieldOfView;
            }

            _prevPinchDistance = currentDistance;
        }
        else
        {
            _prevPinchDistance = 0f;
        }
    }
    // --------------------------------

    private void ApplyRotation()
    {
        var xQuaternion = Quaternion.AngleAxis(_rotation.x, Vector3.up);
        var yQuaternion = Quaternion.AngleAxis(_rotation.y, Vector3.left);
        transform.localRotation = xQuaternion * yQuaternion;
    }
}