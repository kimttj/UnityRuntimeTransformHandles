using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace TransformHandles
{
    public class Ghost : MonoBehaviour
    {
        private Transform GhostTransform => transform;
        private TransformHandleManager _handleManager;
        private PosRotScale _initialProperties;

        public virtual void Initialize()
        {
            _handleManager = TransformHandleManager.Instance;
        }

        public virtual void Terminate()
        {
            DestroyImmediate(gameObject);
        }

        public void UpdateGhostTransform(PosRotScale average)
        {
            GhostTransform.position = average.Position;
            GhostTransform.rotation = average.Rotation;
            GhostTransform.localScale = average.Scale;
        }

        public void ResetGhostTransform()
        {
            GhostTransform.position = Vector3.zero;
            GhostTransform.rotation = Quaternion.identity;
            GhostTransform.localScale = Vector3.one;
        }

        public virtual void OnInteractionStart()
        {
            _initialProperties = new PosRotScale()
            {
                Position = GhostTransform.position,
                Rotation = GhostTransform.rotation,
                Scale = GhostTransform.lossyScale
            };
        }

        public virtual void OnInteraction(HandleType handleType)
        {
            switch (handleType)
            {
                case HandleType.Outline:
                    UpdateOutline();
                    break;
                case HandleType.Position:
                    UpdatePosition();
                    break;
                case HandleType.Rotation:
                    UpdateRotation();

                    break;
                case HandleType.Scale:
                    UpdateScale();

                    break;
                case HandleType.PositionRotation:
                    UpdatePosition();
                    UpdateRotation();

                    break;
                case HandleType.PositionScale:
                    UpdatePosition();
                    UpdateScale();

                    break;
                case HandleType.RotationScale:
                    UpdateRotation();
                    UpdateScale();

                    break;
                case HandleType.All:
                    UpdatePosition();
                    UpdateRotation();
                    UpdateScale();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ResetInitialGhostTransformProperties();
        }

        private void UpdateOutline()
        {
            // TransformHandleManagerから実際のターゲットオブジェクトを取得
            var actualTargets = TransformHandleManager.Instance.GetTargets();

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
        private void UpdatePosition()
        {
            var positionChange = GhostTransform.position - _initialProperties.Position;
            _handleManager.UpdateGroupPosition(this, positionChange);
        }

        private void UpdateRotation()
        {
            var rotationChange = GhostTransform.rotation * Quaternion.Inverse(_initialProperties.Rotation);
            _handleManager.UpdateGroupRotation(this, rotationChange);
        }

        private void UpdateScale()
        {
            var scaleChange = GhostTransform.localScale - _initialProperties.Scale;
            _handleManager.UpdateGroupScaleUpdate(this, scaleChange);
        }

        private void ResetInitialGhostTransformProperties()
        {
            _initialProperties.Position = GhostTransform.position;
            _initialProperties.Rotation = GhostTransform.rotation;
            _initialProperties.Scale = GhostTransform.lossyScale;
        }
    }
}