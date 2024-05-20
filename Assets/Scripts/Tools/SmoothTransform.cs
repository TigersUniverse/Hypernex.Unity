using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.Tools
{
    /// <summary>
    /// A tool that drives values of a transform smoothly
    /// </summary>
    public class SmoothTransform : IDisposable
    {
        public float InterpolationFramesCount => Init.Instance.SmoothingFrames;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool PullFromTransform { get; set; }
        
        private Transform transform;
        private bool localSpace;
        
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;

        public SmoothTransform(Transform targetTransform, bool localSpace)
        {
            transform = targetTransform;
            this.localSpace = localSpace;
            if (localSpace)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
            }
            else
            {
                Position = transform.position;
                Rotation = transform.localRotation;
            }
            Scale = transform.localScale;
        }

        public bool IsBoneMoving()
        {
            if (localSpace)
                return Position != transform.localPosition;
            return Position != transform.position;
        }

        public void SetLocalSpace(bool isLocalSpace) => localSpace = isLocalSpace;

        internal void Update()
        {
            if (PullFromTransform)
            {
                switch (localSpace)
                {
                    case true:
                        Position = transform.localPosition;
                        Rotation = transform.localRotation;
                        break;
                    case false:
                        Position = transform.position;
                        Rotation = transform.rotation;
                        break;
                }
                Scale = transform.localScale;
                return;
            }
            switch (localSpace)
            {
                case true:
                    transform.localPosition = Vector3.Slerp(lastPosition, Position, InterpolationFramesCount);
                    transform.localRotation = Quaternion.Slerp(lastRotation, Rotation, InterpolationFramesCount);
                    lastPosition = transform.localPosition;
                    lastRotation = transform.localRotation;
                    break;
                case false:
                    transform.position = Vector3.Slerp(lastPosition, Position, InterpolationFramesCount);
                    transform.rotation = Quaternion.Slerp(lastRotation, Rotation, InterpolationFramesCount);
                    lastPosition = transform.position;
                    lastRotation = transform.rotation;
                    break;
            }
            transform.localScale = Vector3.Slerp(lastScale, Scale, InterpolationFramesCount);
            lastScale = Scale;
        }

        public void Dispose() => Object.Destroy(transform.gameObject);
    }
}