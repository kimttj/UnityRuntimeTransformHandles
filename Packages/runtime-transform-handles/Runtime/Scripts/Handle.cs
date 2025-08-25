using System;
using UnityEngine;
using System.Collections.Generic;

namespace TransformHandles
{
    public class Handle : MonoBehaviour
    {
        [SerializeField] private float autoScaleSizeInPixels = 192;
        [SerializeField] public bool autoScale;

        public virtual event Action<Handle> OnInteractionStartEvent;
        public virtual event Action<Handle> OnInteractionEvent;
        public virtual event Action<Handle> OnInteractionEndEvent;
        public virtual event Action<Handle> OnHandleDestroyedEvent;

        public Transform target;
        public HandleAxes axes = HandleAxes.XYZ;
        public Space space = Space.Self;
        public HandleType type = HandleType.Outline;
        public SnappingType snappingType = SnappingType.Relative;

        public Vector3 positionSnap = Vector3.zero;
        public float rotationSnap;
        public Vector3 scaleSnap = Vector3.zero;

        public Camera handleCamera;

        private PositionHandle PositionHandle { get; set; }
        private RotationHandle RotationHandle { get; set; }
        private ScaleHandle ScaleHandle { get; set; }

        private static TransformHandleManager Manager => TransformHandleManager.Instance;

        protected virtual void Awake()
        {
            PositionHandle = GetComponentInChildren<PositionHandle>();
            RotationHandle = GetComponentInChildren<RotationHandle>();
            ScaleHandle = GetComponentInChildren<ScaleHandle>();

            Clear();
        }

        protected virtual void OnEnable()
        {
            handleCamera = Manager.mainCamera;
        }

        protected virtual void OnDisable()
        {
            Disable();
        }

        protected void OnDestroy()
        {
            if (Manager == null) return;

            Manager.RemoveHandle(this);
            OnHandleDestroyedEvent?.Invoke(this);
        }

        protected virtual void LateUpdate()
        {
            UpdateHandleTransformation();

            if (!autoScale || handleCamera == null) return;
            transform.PreserveScaleOnScreen(handleCamera.fieldOfView, autoScaleSizeInPixels, handleCamera);
        }

        public virtual void Enable(Transform targetTransform)
        {
            target = targetTransform;
            transform.position = targetTransform.position;

            CreateHandles();
        }

        public virtual void Disable()
        {
            target = null;

            Clear();
        }

        public virtual void InteractionStart()
        {
            OnInteractionStartEvent?.Invoke(this);
        }

        public virtual void InteractionStay()
        {
            OnInteractionEvent?.Invoke(this);
        }

        public virtual void InteractionEnd()
        {
            OnInteractionEndEvent?.Invoke(this);
        }

        public virtual void ChangeHandleType(HandleType handleType)
        {
            type = handleType;

            Clear();
            CreateHandles();
        }

        public virtual void ChangeHandleSpace(Space newSpace)
        {
            if (type == HandleType.Scale)
                space = Space.Self;
            else
                space = newSpace == Space.Self ? Space.Self : Space.World;
        }

        public virtual void ChangeAxes(HandleAxes handleAxes)
        {
            axes = handleAxes;

            Clear();
            CreateHandles();
        }

        protected virtual void UpdateHandleTransformation()
        {
            if (!target) return;

            transform.position = target.transform.position;
            if (space == Space.Self || type == HandleType.Scale)
            {
                transform.rotation = target.transform.rotation;
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }
        }

        protected virtual void CreateHandles()
        {
            switch (type)
            {
                case HandleType.Outline:
                    ActivateOutlineHandle();
                    break;
                case HandleType.Position:
                    ActivatePositionHandle();
                    break;
                case HandleType.Rotation:
                    ActivateRotationHandle();
                    break;
                case HandleType.Scale:
                    ActivateScaleHandle();
                    break;
                case HandleType.PositionRotation:
                    ActivatePositionHandle();
                    ActivateRotationHandle();
                    break;
                case HandleType.PositionScale:
                    ActivatePositionHandle();
                    ActivateScaleHandle();
                    break;
                case HandleType.RotationScale:
                    ActivateRotationHandle();
                    ActivateScaleHandle();
                    break;
                case HandleType.All:
                    ActivatePositionHandle();
                    ActivateRotationHandle();
                    ActivateScaleHandle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ActivateOutlineHandle()
        {
            // HandleType.Outlineの場合、ターゲットが追加された後にアウトラインを適用
            StartCoroutine(ApplyOutlineAfterDelay());
        }

        private System.Collections.IEnumerator ApplyOutlineAfterDelay()
        {
            // 1フレーム待機してターゲットが追加されるのを待つ
            yield return null;

            ApplyOutlineToTargets();
        }

        private void ApplyOutlineToTargets()
        {
            // 現在のハンドルに関連するターゲットオブジェクトを取得
            var actualTargets = Manager.GetTargetsForHandle(this);

            // GetTargetsForHandle()が空の場合、一般的なGetTargets()を試す
            if (actualTargets == null || actualTargets.Count == 0)
            {
                actualTargets = Manager.GetTargets();
            }

            if (actualTargets != null && actualTargets.Count > 0)
            {
                // すべての実際のターゲットオブジェクトにCustomOutlineをアタッチ
                for (int i = 0; i < actualTargets.Count; i++)
                {
                    var actualTarget = actualTargets[i];

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

        private void ActivatePositionHandle()
        {
            PositionHandle.Initialize(this);
            PositionHandle.gameObject.SetActive(true);
        }

        private void ActivateRotationHandle()
        {
            RotationHandle.Initialize(this);
            RotationHandle.gameObject.SetActive(true);
        }

        private void ActivateScaleHandle()
        {
            ScaleHandle.Initialize(this);
            ScaleHandle.gameObject.SetActive(true);
        }

        protected virtual void Clear()
        {
            if (PositionHandle != null && PositionHandle.gameObject.activeSelf) PositionHandle.gameObject.SetActive(false);
            if (RotationHandle != null && RotationHandle.gameObject.activeSelf) RotationHandle.gameObject.SetActive(false);
            if (ScaleHandle != null && ScaleHandle.gameObject.activeSelf) ScaleHandle.gameObject.SetActive(false);

            // HandleType.Outlineの場合、アウトラインをクリア
            if (type == HandleType.Outline)
            {
                ClearOutlineFromTargets();
            }
        }

        private void ClearOutlineFromTargets()
        {
            // すべてのターゲットオブジェクトからCustomOutlineコンポーネントを削除
            if (Manager != null)
            {
                var actualTargets = Manager.GetTargets();
                if (actualTargets != null)
                {
                    foreach (var actualTarget in actualTargets)
                    {
                        if (actualTarget != null)
                        {
                            var customOutline = actualTarget.GetComponent<TransformHandles.CustomOutline>();
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

        /// <summary>
        /// このHandleのターゲットTransformを設定する
        /// </summary>
        /// <param name="newTarget">新しいターゲットTransform</param>
        /// <returns>設定が成功したかどうか</returns>
        public bool SetTarget(Transform newTarget)
        {
            if (Manager == null)
            {
                Debug.LogError("Manager is null");
                return false;
            }

            return Manager.SetHandleTarget(this, newTarget);
        }

        /// <summary>
        /// このHandleのターゲットTransformを設定する（複数ターゲット）
        /// </summary>
        /// <param name="newTargets">新しいターゲットTransformのリスト</param>
        /// <returns>設定が成功したかどうか</returns>
        public bool SetTargets(List<Transform> newTargets)
        {
            if (Manager == null)
            {
                Debug.LogError("Manager is null");
                return false;
            }

            return Manager.SetHandleTargets(this, newTargets);
        }

        /// <summary>
        /// このHandleの現在のターゲットTransformを取得する
        /// </summary>
        /// <returns>現在のターゲットTransformのリスト</returns>
        public List<Transform> GetCurrentTargets()
        {
            if (Manager == null)
            {
                Debug.LogError("Manager is null");
                return new List<Transform>();
            }

            return Manager.GetTargetsForHandle(this);
        }
    }
}