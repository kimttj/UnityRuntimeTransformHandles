﻿using UnityEngine;
using TransformHandles.Utils;

namespace TransformHandles
{
    public class ScaleGlobal : HandleBase
    {
        [SerializeField] private Color defaultColor;
        [SerializeField] private MeshRenderer cubeMeshRenderer;

        private Vector3 _axis;
        private Vector3 _startScale;

        public void Initialize(Handle handle, Vector3 pAxis)
        {
            InputUtils.EnableEnhancedTouch();

            ParentHandle = handle;
            _axis = pAxis;
            DefaultColor = defaultColor;
        }

        public override void Interact(Vector3 pPreviousPosition)
        {
            Vector3 currentInputPosition = InputUtils.GetInputScreenPosition();
            Vector3 deltaPos = currentInputPosition - pPreviousPosition
            ;
            var d = (deltaPos.x + deltaPos.y) * Time.deltaTime * 2;
            delta += d;
            ParentHandle.target.localScale = _startScale + Vector3.Scale(_startScale, _axis) * delta;

            base.Interact(pPreviousPosition);
        }

        public override void StartInteraction(Vector3 pHitPoint)
        {
            base.StartInteraction(pHitPoint);
            _startScale = ParentHandle.target.localScale;
        }

        public override void SetColor(Color color)
        {
            cubeMeshRenderer.material.color = color;
        }

        public override void SetDefaultColor()
        {
            cubeMeshRenderer.material.color = DefaultColor;
        }
    }
}