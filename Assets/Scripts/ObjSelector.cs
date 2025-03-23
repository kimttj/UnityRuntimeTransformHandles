using System.Collections.Generic;
using TransformHandles;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TransformHandles.Utils;

public class ObjSelector : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color unselectedColor;

    private Camera _camera;
    private CameraMovement _cameraMovement;

    private TransformHandleManager _manager;

    private Handle _lastHandle;
    private Dictionary<Transform, Handle> _handleDictionary;

    private void Awake()
    {
        InputUtils.EnableEnhancedTouch();

        _camera = Camera.main;
        if (_camera != null) _cameraMovement = _camera.GetComponent<CameraMovement>();

        _manager = TransformHandleManager.Instance;
        _handleDictionary = new Dictionary<Transform, Handle>();
    }

    private void Update()
    {
        if (InputUtils.IsControlPressed() && InputUtils.IsPrimaryPressedThisFrame())
        {
            HandleSelectAction();
        }
        else if (InputUtils.IsPrimaryPressedThisFrame())
        {
            // Add the object to handle if exists, else create a new handle
            HandleAddAction();
        }

        if (InputUtils.IsSecondaryPressedThisFrame())
        {
            // Remove the object from handle
            HandleRemoveAction();
        }

        if (InputUtils.IsMiddlePressed())
        {
            HandleCreateNewHandle();
        }

#if !UNITY_EDITOR
        CheckTwoFingerDeselect();
#endif
    }

    private void HandleSelectAction()
    {
        var ray = _camera.ScreenPointToRay(InputUtils.GetInputScreenPosition());
        if (Physics.Raycast(ray, out var hit, 1000f, layerMask))
        {
            var hitTransform = hit.transform;
            if (_handleDictionary.ContainsKey(hitTransform)) return;
            CreateHandle(hitTransform);

            foreach (var child in hitTransform.GetComponentsInChildren<Transform>())
            {
                SelectObject(child);
            }
        }
    }

    private void HandleAddAction()
    {
        var ray = _camera.ScreenPointToRay(InputUtils.GetInputScreenPosition());
        if (Physics.Raycast(ray, out var hit, 1000f, layerMask))
        {
            var hitTransform = hit.transform;
            if (_handleDictionary.ContainsKey(hitTransform)) return;
            if (_lastHandle == null)
            {
                CreateHandle(hitTransform);
            }
            else
            {
                AddTarget(hitTransform);
            }

            foreach (var child in hitTransform.GetComponentsInChildren<Transform>())
            {
                SelectObject(child);
            }
        }
    }

    private void HandleRemoveAction()
    {
        var ray = _camera.ScreenPointToRay(InputUtils.GetInputScreenPosition());
        if (Physics.Raycast(ray, out var hit))
        {
            var hitTransform = hit.transform;
            if (!_handleDictionary.ContainsKey(hitTransform)) return;
            RemoveTarget(hitTransform);
            DeselectObject(hitTransform);

            foreach (var child in hitTransform.GetComponentsInChildren<Transform>())
            {
                DeselectObject(child);
            }
        }
    }

    private void HandleCreateNewHandle()
    {
        var ray = _camera.ScreenPointToRay(InputUtils.GetInputScreenPosition());
        if (!Physics.Raycast(ray, out var hit, 1000f, layerMask)) return;
        var hitTransform = hit.transform;
        if (_handleDictionary.ContainsKey(hitTransform)) return;

        CreateHandle(hitTransform);
        SelectObject(hitTransform);
    }

    private void CheckTwoFingerDeselect()
    {
        if (Touchscreen.current == null || Touch.activeTouches.Count < 2) return;

        var t1 = Touch.activeTouches[0];
        var t2 = Touch.activeTouches[1];

        if (t1.phase != UnityEngine.InputSystem.TouchPhase.Began || t2.phase != UnityEngine.InputSystem.TouchPhase.Began) return;

        Vector2 centerPosition = (t1.screenPosition + t2.screenPosition) * 0.5f;
        Ray ray = _camera.ScreenPointToRay(centerPosition);
        if (Physics.Raycast(ray, out var hit, 1000f, layerMask))
        {
            var hitTransform = hit.transform;

            if (_handleDictionary.ContainsKey(hitTransform))
            {
                // Remove the object from handle
                RemoveTarget(hitTransform);
                DeselectObject(hitTransform);

                foreach (var child in hitTransform.GetComponentsInChildren<Transform>())
                    DeselectObject(child);
            }
        }
    }

    // -------------------- Handle Selection Helpers ----------------------

    private void DeselectObject(Transform hitInfoTransform)
    {
        _handleDictionary.Remove(hitInfoTransform);
        hitInfoTransform.tag = "Untagged";

        var renderer = hitInfoTransform.GetComponent<Renderer>() ?? hitInfoTransform.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = unselectedColor;
        }
    }

    private void SelectObject(Transform hitInfoTransform)
    {
        _handleDictionary.Add(hitInfoTransform, _lastHandle);
        hitInfoTransform.tag = "Selected";

        var renderer = hitInfoTransform.GetComponent<Renderer>() ?? hitInfoTransform.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = selectedColor;
        }
    }

    private void CreateHandle(Transform hitTransform)
    {
        var handle = _manager.CreateHandle(hitTransform);
        _lastHandle = handle;

        handle.OnInteractionStartEvent += OnHandleInteractionStart;
        handle.OnInteractionEvent += OnHandleInteraction;
        handle.OnInteractionEndEvent += OnHandleInteractionEnd;
        handle.OnHandleDestroyedEvent += OnHandleDestroyed;
    }

    private void AddTarget(Transform hitTransform)
    {
        _manager.AddTarget(hitTransform, _lastHandle);
    }

    private void RemoveTarget(Transform hitTransform)
    {
        var handle = _handleDictionary[hitTransform];
        if (_lastHandle == handle) _lastHandle = null;

        _manager.RemoveTarget(hitTransform, handle);
    }

    private void OnHandleInteractionStart(Handle handle)
    {
        _cameraMovement.enabled = false;
    }

    private static void OnHandleInteraction(Handle handle)
    {
        Debug.Log($"{handle.name} is being interacted with");
    }

    private void OnHandleInteractionEnd(Handle handle)
    {
        _cameraMovement.enabled = true;
    }

    private void OnHandleDestroyed(Handle handle)
    {
        handle.OnInteractionStartEvent -= OnHandleInteractionStart;
        handle.OnInteractionEvent -= OnHandleInteraction;
        handle.OnInteractionEndEvent -= OnHandleInteractionEnd;
        handle.OnHandleDestroyedEvent -= OnHandleDestroyed;
    }
}