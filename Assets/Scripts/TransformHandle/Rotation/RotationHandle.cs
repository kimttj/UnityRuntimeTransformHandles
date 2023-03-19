﻿using UnityEngine;

// ReSharper disable once CheckNamespace
namespace TransformHandle
{
    public class RotationHandle : MonoBehaviour
    {
        public RotationAxis xAxis;
        public RotationAxis yAxis;
        public RotationAxis zAxis;

        private TransformHandle _parentHandle;

        private bool _handleInitialized;

        public void Initialize(TransformHandle transformHandle)
        {
            if (_handleInitialized) return;
            
            _parentHandle = transformHandle;
            transform.SetParent(_parentHandle.transform, false);

            if (_parentHandle.axes is HandleAxes.X or HandleAxes.XY or HandleAxes.XZ or HandleAxes.XYZ)
            {
                xAxis.gameObject.SetActive(true);
                xAxis.Initialize(_parentHandle, Vector3.right);
            }

            if (_parentHandle.axes is HandleAxes.Y or HandleAxes.XY or HandleAxes.YZ or HandleAxes.XYZ)
            {
                yAxis.gameObject.SetActive(true);
                yAxis.Initialize(_parentHandle, Vector3.up);
            }

            if (_parentHandle.axes is HandleAxes.Z or HandleAxes.YZ or HandleAxes.XZ or HandleAxes.XYZ)
            {
                zAxis.gameObject.SetActive(true);
                zAxis.Initialize(_parentHandle, Vector3.forward);
            }
            _handleInitialized = true;
        }
    }
}