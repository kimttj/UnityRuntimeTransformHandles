using System;
using System.Collections.Generic;
using TransformHandles.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Linq;

namespace TransformHandles
{
    public class TransformHandleManager : Singleton<TransformHandleManager>
    {
        public Camera mainCamera;

        [Header("Prefabs")]
        [SerializeField] private GameObject transformHandlePrefab;
        [SerializeField] private GameObject ghostPrefab;

        [Header("Settings")]
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private Color highlightColor = Color.white;

        [Header("Shortcuts")]
        [SerializeField] private Key outlineShortcut = Key.S;
        [SerializeField] private Key positionShortcut = Key.W;
        [SerializeField] private Key rotationShortcut = Key.E;
        [SerializeField] private Key scaleShortcut = Key.R;
        [SerializeField] private Key allShortcut = Key.A;
        [SerializeField] private Key spaceShortcut = Key.X;
        [SerializeField] private Key pivotShortcut = Key.Z;

        [Header("Input Settings")]
        [SerializeField] public bool enableKeyboardInput = true;

        private RaycastHit[] _rayHits;

        private Vector3 _previousMousePosition;
        private Vector3 _handleHitPoint;

        private HandleBase _previousAxis;
        private HandleBase _draggingHandle;
        private HandleBase _hoveredHandle;

        private Ghost _interactedGhost;
        private Handle _interactedHandle;

        private HashSet<Transform> _transformHashSet;
        private Dictionary<Handle, TransformGroup> _handleGroupMap;
        private Dictionary<Ghost, TransformGroup> _ghostGroupMap;

        private bool _handleActive;
        private bool _isInitialized;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            InitializeManager();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnDestroy()
        {
            // シーン終了時にアウトラインをクリア
            ClearAllOutlines();
        }

        private void ClearAllOutlines()
        {
            // すべてのターゲットオブジェクトからCustomOutlineコンポーネントを削除
            if (_transformHashSet != null)
            {
                foreach (var target in _transformHashSet)
                {
                    if (target != null)
                    {
                        var customOutline = target.GetComponent<TransformHandles.CustomOutline>();
                        if (customOutline != null)
                        {
                            if (Application.isPlaying)
                            {
                                Destroy(customOutline);
                            }
                            else
                            {
                                DestroyImmediate(customOutline);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void OnActiveSceneChanged(Scene arg0, Scene scene)
        {
            _isInitialized = false;

            InitializeManager();
        }

        private void InitializeManager()
        {
            if (_isInitialized) return;

            InputUtils.EnableEnhancedTouch();

            mainCamera = mainCamera == null ? Camera.main : mainCamera;

            _handleGroupMap = new Dictionary<Handle, TransformGroup>();
            _ghostGroupMap = new Dictionary<Ghost, TransformGroup>();
            _transformHashSet = new HashSet<Transform>();

            _isInitialized = true;
        }

        public Handle CreateHandle(Transform target)
        {
            if (_transformHashSet.Contains(target)) { Debug.LogWarning($"{target} already has a handle."); return null; }

            var ghost = CreateGhost();
            ghost.Initialize();

            var transformHandle = Instantiate(transformHandlePrefab).GetComponent<Handle>();
            transformHandle.Enable(ghost.transform);

            var group = new TransformGroup(ghost, transformHandle);

            _handleGroupMap.Add(transformHandle, group);
            _ghostGroupMap.Add(ghost, group);

            var success = AddTarget(target, transformHandle);
            if (!success) { DestroyHandle(transformHandle); }

            _handleActive = true;

            return transformHandle;
        }

        /// <summary>
        /// 既存のHandleのTransformを外部から設定する
        /// </summary>
        /// <param name="handle">設定対象のHandle</param>
        /// <param name="newTarget">新しいターゲットTransform</param>
        /// <returns>設定が成功したかどうか</returns>
        public bool SetHandleTarget(Handle handle, Transform newTarget)
        {
            if (handle == null)
            {
                Debug.LogError("Handle is null");
                return false;
            }

            if (newTarget == null)
            {
                Debug.LogError("New target is null");
                return false;
            }

            // 既存のターゲットを取得
            var currentTargets = GetTargetsForHandle(handle);
            if (currentTargets == null || currentTargets.Count == 0)
            {
                Debug.LogWarning("Handle has no current targets");
                return false;
            }

            // 新しいターゲットが既に他のHandleで使用されているかチェック
            // ただし、同じHandleで既に使用されている場合は許可
            var existingHandle = GetHandleForTarget(newTarget);
            if (existingHandle != null && existingHandle != handle)
            {
                Debug.LogWarning($"{newTarget} is already used by another handle.");
                return false;
            }

            // ハンドルの位置を直接更新（ターゲットの削除・追加は行わない）
            if (_handleGroupMap.TryGetValue(handle, out var group))
            {
                // 新しいターゲットの位置でハンドルを更新
                var newPosRotScale = new PosRotScale
                {
                    Position = newTarget.position,
                    Rotation = newTarget.rotation,
                    Scale = newTarget.localScale
                };

                group.GroupGhost.UpdateGhostTransform(newPosRotScale);
            }

            return true;
        }

        /// <summary>
        /// 既存のHandleのTransformを外部から設定する（複数ターゲット）
        /// </summary>
        /// <param name="handle">設定対象のHandle</param>
        /// <param name="newTargets">新しいターゲットTransformのリスト</param>
        /// <returns>設定が成功したかどうか</returns>
        public bool SetHandleTargets(Handle handle, List<Transform> newTargets)
        {
            if (handle == null)
            {
                Debug.LogError("Handle is null");
                return false;
            }

            if (newTargets == null || newTargets.Count == 0)
            {
                Debug.LogError("New targets list is null or empty");
                return false;
            }

            // 新しいターゲットが既に他のHandleで使用されているかチェック
            foreach (var newTarget in newTargets)
            {
                if (newTarget == null)
                {
                    Debug.LogError("One of the new targets is null");
                    return false;
                }

                if (_transformHashSet.Contains(newTarget))
                {
                    Debug.LogWarning($"{newTarget} is already used by another handle.");
                    return false;
                }
            }

            // 既存のターゲットを削除
            var currentTargets = GetTargetsForHandle(handle);
            if (currentTargets != null)
            {
                foreach (var currentTarget in currentTargets.ToList())
                {
                    RemoveTarget(currentTarget, handle);
                }
            }

            // 新しいターゲットを追加
            bool allSuccess = true;
            foreach (var newTarget in newTargets)
            {
                var success = AddTarget(newTarget, handle);
                if (!success)
                {
                    Debug.LogError($"Failed to add target {newTarget} to handle");
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        public Handle CreateHandleFromList(List<Transform> targets)
        {
            if (targets.Count == 0) { Debug.LogWarning("List is empty."); return null; }

            var ghost = CreateGhost();
            ghost.Initialize();

            var transformHandle = Instantiate(transformHandlePrefab).GetComponent<Handle>();
            transformHandle.Enable(ghost.transform);

            var group = new TransformGroup(ghost, transformHandle);
            _handleGroupMap.Add(transformHandle, group);
            _ghostGroupMap.Add(ghost, group);

            foreach (var target in targets)
            {
                if (_transformHashSet.Contains(target))
                {
                    Debug.LogWarning($"{target} already has a handle.");
                    DestroyHandle(transformHandle);
                    return null;
                }
                AddTarget(target, transformHandle);
            }

            _handleActive = true;

            return transformHandle;
        }

        private Ghost CreateGhost()
        {
            Ghost ghost;

            if (ghostPrefab == null)
            {
                var ghostObject = new GameObject();
                ghost = ghostObject.AddComponent<Ghost>();
            }
            else
            {
                ghost = Instantiate(ghostPrefab).GetComponent<Ghost>();
            }

            return ghost;
        }

        public void RemoveHandle(Handle handle)
        {
            if (handle == null) { Debug.LogError("Handle is already null"); return; }
            if (_handleGroupMap == null) return;

            var group = _handleGroupMap[handle];
            if (group == null) { Debug.LogError("Group is null"); return; }

            _handleGroupMap.Remove(handle);
            handle.Disable();

            var groupGhost = group.GroupGhost;
            if (groupGhost != null)
            {
                _ghostGroupMap.Remove(groupGhost);
                group.GroupGhost.Terminate();
            }

            if (_handleGroupMap.Count == 0) _handleActive = false;
        }

        public List<Transform> GetTargets()
        {
            var targets = new List<Transform>();

            // 現在アクティブなハンドルグループのターゲットを取得
            foreach (var group in _handleGroupMap.Values)
            {
                targets.AddRange(group.Transforms);
            }

            return targets;
        }

        public List<Transform> GetTargetsForHandle(Handle handle)
        {
            if (handle == null) return new List<Transform>();

            if (_handleGroupMap.TryGetValue(handle, out var group))
            {
                return group.Transforms.ToList();
            }

            return new List<Transform>();
        }

        /// <summary>
        /// 指定されたターゲットに関連するHandleを取得する
        /// </summary>
        /// <param name="target">検索するターゲット</param>
        /// <returns>関連するHandle、見つからない場合はnull</returns>
        public Handle GetHandleForTarget(Transform target)
        {
            if (target == null) return null;

            foreach (var kvp in _handleGroupMap)
            {
                var handle = kvp.Key;
                var group = kvp.Value;

                if (group.Transforms.Contains(target))
                {
                    return handle;
                }
            }

            return null;
        }

        public static void DestroyHandle(Handle handle)
        {
            // ハンドルが破棄される前に、関連するオブジェクトからCustomOutlineコンポーネントを削除
            if (handle != null && handle.type == HandleType.Outline)
            {
                // ハンドルに関連するターゲットからCustomOutlineを削除
                var manager = TransformHandleManager.Instance;
                if (manager != null)
                {
                    var targets = manager.GetTargetsForHandle(handle);
                    if (targets != null)
                    {
                        foreach (var target in targets)
                        {
                            if (target != null)
                            {
                                var customOutline = target.GetComponent<TransformHandles.CustomOutline>();
                                if (customOutline != null)
                                {
                                    if (Application.isPlaying)
                                    {
                                        Destroy(customOutline);
                                    }
                                    else
                                    {
                                        DestroyImmediate(customOutline);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            DestroyImmediate(handle.gameObject);
        }

        /// <summary>
        /// 指定されたHandleを破棄する（インスタンスメソッド版）
        /// </summary>
        /// <param name="handle">破棄するHandle</param>
        public void DestroyHandleInstance(Handle handle)
        {
            DestroyHandle(handle);
        }

        public void DestroyAllHandles()
        {
            foreach (var handle in _handleGroupMap.Keys)
            {
                DestroyHandle(handle);
            }
        }

        public bool AddTarget(Transform target, Handle handle)
        {
            if (_transformHashSet.Contains(target)) { Debug.LogWarning($"{target} already has a handle."); return false; }
            if (handle == null) { Debug.LogError("Handle is null"); return false; }

            var group = _handleGroupMap[handle];
            var targetAdded = group.AddTransform(target);
            if (!targetAdded) { Debug.LogWarning($"{target} is relative to the selected ones."); return false; }

            var averagePosRotScale = group.GetAveragePosRotScale();
            group.GroupGhost.UpdateGhostTransform(averagePosRotScale);

            _transformHashSet.Add(target);

            // HandleType.Outlineの場合、新しいターゲットにアウトラインを適用
            if (handle.type == HandleType.Outline)
            {
                ApplyOutlineToHandleTargets(handle);
            }

            return true;
        }

        private void ApplyOutlineToHandleTargets(Handle handle)
        {
            // 現在のハンドルに関連するすべてのターゲットオブジェクトを取得
            var actualTargets = GetTargetsForHandle(handle);

            if (actualTargets != null && actualTargets.Count > 0)
            {
                // すべての実際のターゲットオブジェクトにCustomOutlineをアタッチ
                foreach (var actualTarget in actualTargets)
                {
                    // Ghostオブジェクトを除外
                    if (actualTarget.GetComponent<Ghost>() != null)
                    {
                        continue;
                    }

                    var customOutline = actualTarget.GetComponent<TransformHandles.CustomOutline>();
                    if (customOutline == null)
                    {
                        // CustomOutlineスクリプトを動的にアタッチ
                        customOutline = actualTarget.gameObject.AddComponent<TransformHandles.CustomOutline>();
                    }
                }
            }
        }

        public void RemoveTarget(Transform target, Handle handle)
        {
            if (!_transformHashSet.Contains(target)) { Debug.LogWarning($"{target} doesn't have a handle."); return; }
            if (handle == null) { Debug.LogError("Handle is null"); return; }

            // CustomOutlineコンポーネントを削除
            var customOutline = target.GetComponent<TransformHandles.CustomOutline>();
            if (customOutline != null)
            {
                ClearOutlineFromTarget(target);
            }

            _transformHashSet.Remove(target);

            var group = _handleGroupMap[handle];
            var groupElementsRemoved = group.RemoveTransform(target);
            if (groupElementsRemoved) { DestroyHandle(handle); return; }

            var averagePosRotScale = group.GetAveragePosRotScale();
            group.GroupGhost.UpdateGhostTransform(averagePosRotScale);
        }

        /// <summary>
        /// Handleを破棄せずにターゲットを削除する
        /// </summary>
        private void RemoveTargetWithoutDestroyingHandle(Transform target, Handle handle)
        {
            if (!_transformHashSet.Contains(target)) { Debug.LogWarning($"{target} doesn't have a handle."); return; }
            if (handle == null) { Debug.LogError("Handle is null"); return; }

            // CustomOutlineコンポーネントを削除
            var customOutline = target.GetComponent<TransformHandles.CustomOutline>();
            if (customOutline != null)
            {
                ClearOutlineFromTarget(target);
            }

            _transformHashSet.Remove(target);

            var group = _handleGroupMap[handle];
            group.RemoveTransform(target);

            // ターゲットが0個になった場合は、Ghostの位置をリセット
            if (group.Transforms.Count == 0)
            {
                group.GroupGhost.ResetGhostTransform();
            }
            else
            {
                var averagePosRotScale = group.GetAveragePosRotScale();
                group.GroupGhost.UpdateGhostTransform(averagePosRotScale);
            }
        }

        private void ClearOutlineFromTarget(Transform target)
        {
            // CustomOutlineコンポーネントを削除
            var customOutline = target.GetComponent<TransformHandles.CustomOutline>();
            if (customOutline != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(customOutline);
                }
                else
                {
                    DestroyImmediate(customOutline);
                }
            }
        }

        protected virtual void Update()
        {
            if (!_handleActive) return;

            _hoveredHandle = null;
            _handleHitPoint = Vector3.zero;

            GetHandle(ref _hoveredHandle, ref _handleHitPoint);

            HandleOverEffect(_hoveredHandle);

            MouseInput();
            KeyboardInput();
        }

        protected virtual void GetHandle(ref HandleBase handle, ref Vector3 hitPoint)
        {
            _rayHits = new RaycastHit[16];
            var size = 0;

            try
            {
                var ray = mainCamera.ScreenPointToRay(InputUtils.GetInputScreenPosition());
                size = Physics.RaycastNonAlloc(ray, _rayHits, 1000, layerMask);
            }
            catch (MissingReferenceException)
            {
                mainCamera = Camera.main;
                Debug.Log("Camera is null, trying to find main camera");
                if (mainCamera == null)
                {
                    Debug.Log("Main camera is null, aborting");
                    Destroy(gameObject);
                    return;
                }
            }

            if (size == 0) return;

            Array.Sort(_rayHits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (var hit in _rayHits)
            {
                if (hit.collider == null) continue;
                handle = hit.collider.GetComponentInParent<HandleBase>();
                if (handle != null)
                {
                    hitPoint = hit.point;
                    return;
                }
            }
        }

        protected virtual void HandleOverEffect(HandleBase handleBase)
        {
            if (_draggingHandle == null && _previousAxis != null && _previousAxis != handleBase)
            {
                _previousAxis.SetDefaultColor();
            }

            if (handleBase != null && _draggingHandle == null)
            {
                handleBase.SetColor(highlightColor);
            }

            _previousAxis = handleBase;
        }

        protected virtual void MouseInput()
        {
            if (InputUtils.IsPrimaryPressed() && _draggingHandle != null)
            {
                _draggingHandle.Interact(_previousMousePosition);
                OnInteraction();
            }

            if (InputUtils.IsPrimaryPressedThisFrame() && _hoveredHandle != null)
            {
                _draggingHandle = _hoveredHandle;
                _draggingHandle.StartInteraction(_handleHitPoint);
                OnInteractionStart();
            }

            if (InputUtils.IsPrimaryReleasedThisFrame() && _draggingHandle != null)
            {
                _draggingHandle.EndInteraction();
                _draggingHandle = null;
                OnInteractionEnd();
            }

            _previousMousePosition = InputUtils.GetInputScreenPosition();
        }

        protected virtual void KeyboardInput()
        {
            if (Keyboard.current == null || !enableKeyboardInput) return;

            if (Keyboard.current[outlineShortcut].wasPressedThisFrame)
            {
                foreach (var handle in _handleGroupMap.Keys)
                {
                    ChangeHandleType(handle, HandleType.Outline);
                }
            }

            if (Keyboard.current[positionShortcut].wasPressedThisFrame)
            {
                foreach (var handle in _handleGroupMap.Keys)
                {
                    ChangeHandleType(handle, HandleType.Position);
                }
            }

            if (Keyboard.current[rotationShortcut].wasPressedThisFrame)
            {
                foreach (var handle in _handleGroupMap.Keys)
                {
                    ChangeHandleType(handle, HandleType.Rotation);
                }
            }

            if (Keyboard.current[scaleShortcut].wasPressedThisFrame)
            {
                foreach (var handle in _handleGroupMap.Keys)
                {
                    ChangeHandleType(handle, HandleType.Scale);
                }
            }

            if (Keyboard.current[allShortcut].wasPressedThisFrame)
            {
                foreach (var handle in _handleGroupMap.Keys)
                {
                    ChangeHandleType(handle, HandleType.All);
                }
            }

            if (Keyboard.current[spaceShortcut].wasPressedThisFrame)
            {
                foreach (var handle in _handleGroupMap.Keys)
                {
                    ChangeHandleSpace(handle, handle.space == Space.World ? Space.Self : Space.World);
                }
            }

            if (Keyboard.current[pivotShortcut].wasPressedThisFrame)
            {
                foreach (var group in _handleGroupMap.Values)
                {
                    ChangeHandlePivot(group, !group.IsOriginOnCenter);
                }
            }
        }

        protected virtual void OnInteractionStart()
        {
            _interactedHandle = _draggingHandle.GetComponentInParent<Handle>();
            _interactedGhost = _handleGroupMap[_interactedHandle].GroupGhost;
            _interactedGhost.OnInteractionStart();

            _interactedHandle.InteractionStart();
        }

        protected virtual void OnInteraction()
        {
            _interactedGhost.OnInteraction(_interactedHandle.type);

            _interactedHandle.InteractionStay();
        }

        protected virtual void OnInteractionEnd()
        {
            var group = _handleGroupMap[_interactedHandle];
            group.UpdateBounds();

            _interactedHandle.InteractionEnd();
        }

        public static void ChangeHandleType(Handle handle, HandleType type)
        {
            if (handle == null) { Debug.LogError("Handle is null"); return; }
            handle.ChangeHandleType(type);
        }

        public void ChangeHandleSpace(Handle handle, Space space)
        {
            if (handle == null) { Debug.LogError("Handle is null"); return; }
            handle.ChangeHandleSpace(space);

            var group = _handleGroupMap[handle];
            group.GroupGhost.UpdateGhostTransform(group.GetAveragePosRotScale());
        }

        public void ChangeHandlePivot(TransformGroup group, bool originToCenter)
        {
            if (group == null) { Debug.LogError("Group is null"); return; }
            group.IsOriginOnCenter = originToCenter;
            group.GroupGhost.UpdateGhostTransform(group.GetAveragePosRotScale());
        }

        public void UpdateGroupPosition(Ghost ghost, Vector3 positionChange)
        {
            var group = _ghostGroupMap[ghost];
            group.UpdatePositions(positionChange);
        }

        public void UpdateGroupRotation(Ghost ghost, Quaternion rotationChange)
        {
            var group = _ghostGroupMap[ghost];
            group.UpdateRotations(rotationChange);
        }

        public void UpdateGroupScaleUpdate(Ghost ghost, Vector3 scaleChange)
        {
            var group = _ghostGroupMap[ghost];
            group.UpdateScales(scaleChange);
        }

        /// <summary>
        /// キーボード入力の有効・無効を設定する
        /// </summary>
        /// <param name="enabled">キーボード入力を有効にする場合はtrue</param>
        public void SetKeyboardInputEnabled(bool enabled)
        {
            enableKeyboardInput = enabled;
        }

        /// <summary>
        /// キーボード入力が有効かどうかを取得する
        /// </summary>
        /// <returns>キーボード入力が有効な場合はtrue</returns>
        public bool IsKeyboardInputEnabled()
        {
            return enableKeyboardInput;
        }

        /// <summary>
        /// キーボード入力を有効にする
        /// </summary>
        public void EnableKeyboardInput()
        {
            enableKeyboardInput = true;
        }

        /// <summary>
        /// キーボード入力を無効にする
        /// </summary>
        public void DisableKeyboardInput()
        {
            enableKeyboardInput = false;
        }
    }

    public class TransformGroup
    {
        public Ghost GroupGhost { get; }
        public Handle GroupHandle { get; }

        public bool IsOriginOnCenter;

        public HashSet<Transform> Transforms { get; }
        public Dictionary<Transform, MeshRenderer> RenderersMap { get; }
        public Dictionary<Transform, Bounds> BoundsMap { get; }

        public TransformGroup(Ghost groupGhost, Handle groupHandle)
        {
            GroupGhost = groupGhost;
            GroupHandle = groupHandle;

            Transforms = new HashSet<Transform>();
            RenderersMap = new Dictionary<Transform, MeshRenderer>();
            BoundsMap = new Dictionary<Transform, Bounds>();
        }

        public bool AddTransform(Transform tElement)
        {
            if (IsTargetRelativeToSelectedOnes(tElement)) return false;

            var meshRenderer = tElement.GetComponent<MeshRenderer>();

            Transforms.Add(tElement);
            RenderersMap.Add(tElement, meshRenderer);
            BoundsMap.Add(tElement, meshRenderer != null ? meshRenderer.bounds : tElement.GetBounds());

            return true;
        }

        public bool RemoveTransform(Transform transform)
        {
            Transforms.Remove(transform);
            RenderersMap.Remove(transform);
            BoundsMap.Remove(transform);

            return Transforms.Count == 0;
        }

        public void UpdateBounds()
        {
            foreach (var (tElement, meshRenderer) in RenderersMap)
            {
                var bounds = meshRenderer ? meshRenderer.bounds : tElement.GetBounds();
                BoundsMap[tElement] = bounds;
            }
        }

        public void UpdatePositions(Vector3 positionChange)
        {
            foreach (var tElement in RenderersMap.Keys)
            {
                tElement.position += positionChange;
            }
        }

        public void UpdateRotations(Quaternion rotationChange)
        {
            var ghostPosition = GroupGhost.transform.position;
            var rotationAxis = rotationChange.normalized.eulerAngles;
            var rotationChangeMagnitude = rotationChange.eulerAngles.magnitude;
            foreach (var tElement in RenderersMap.Keys)
            {
                if (GroupHandle.space == Space.Self)
                {
                    tElement.position = rotationChange * (tElement.position - ghostPosition) + ghostPosition;
                    tElement.rotation = rotationChange * tElement.rotation;
                }
                else
                {
                    tElement.RotateAround(ghostPosition, rotationAxis, rotationChangeMagnitude);
                }
            }
        }

        public void UpdateScales(Vector3 scaleChange)
        {
            foreach (var (tElement, meshRenderer) in RenderersMap)
            {
                if (IsOriginOnCenter)
                {
                    if (meshRenderer != null)
                    {
                        var oldCenter = meshRenderer.bounds.center;

                        tElement.localScale += scaleChange;

                        // ReSharper disable once Unity.InefficientPropertyAccess
                        var newCenter = meshRenderer.bounds.center;

                        var change = newCenter - oldCenter;

                        tElement.position += change * -1;
                    }
                    else
                    {
                        tElement.localScale += scaleChange;
                    }
                }
                else
                {
                    tElement.localScale += scaleChange;
                }
            }
        }

        private Vector3 GetCenterPoint(Transform tElement)
        {
            return IsOriginOnCenter ? BoundsMap[tElement].center : tElement.position;
        }

        public PosRotScale GetAveragePosRotScale()
        {
            var space = GroupHandle.space;

            var averagePosRotScale = new PosRotScale();

            var centerPositions = new List<Vector3>();
            var sumQuaternion = Quaternion.identity;

            var transformsCount = Transforms.Count;

            foreach (var tElement in Transforms)
            {
                var centerPoint = GetCenterPoint(tElement);
                centerPositions.Add(centerPoint);

                if (space == Space.World) continue;
                sumQuaternion *= tElement.rotation;
            }

            var averagePosition = Vector3.zero;
            foreach (var centerPosition in centerPositions)
            {
                averagePosition += centerPosition;
            }
            averagePosition /= transformsCount;

            averagePosRotScale.Position = averagePosition;
            averagePosRotScale.Rotation = sumQuaternion;
            averagePosRotScale.Scale = Vector3.one;

            return averagePosRotScale;
        }

        private bool IsTargetRelativeToSelectedOnes(Transform newTarget)
        {
            foreach (var transformInHash in Transforms)
            {
                if (transformInHash.IsDeepParentOf(newTarget)) return true;

                if (!newTarget.IsDeepParentOf(transformInHash)) continue;
                RemoveTransform(transformInHash);
                return false;
            }

            return false;
        }
    }
}