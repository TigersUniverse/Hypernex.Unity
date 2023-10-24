using System.Collections;
using System.Threading;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Game
{
    public class LocalPlayerSyncObject : MonoBehaviour
    {
        public bool CheckLocal = true;
        public string FallbackPath;
        public bool IgnoreObjectLocation;
        public CheckTime CheckTime = CheckTime.LateUpdate;
        public float RefreshTime { get; set; } = 0.05f;
        public bool AlwaysSync;
        
        private PathDescriptor pathDescriptor;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastSize;
        private Coroutine c;
        private CancellationTokenSource cts;
        private bool allowCheck;
        
        public void Check()
        {
            if (!allowCheck || GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen || 
                LocalPlayer.Instance == null)
                return;
            allowCheck = false;
            bool needsUpdate = AlwaysSync;
            NetworkedObject networkedObject = null;
            if (CheckLocal)
            {
                if (lastPosition != transform.localPosition)
                    needsUpdate = true;
                if (lastRotation != transform.localRotation)
                    needsUpdate = true;
                if (lastSize != transform.localScale)
                    needsUpdate = true;
                lastPosition = transform.localPosition;
                lastRotation = transform.localRotation;
                lastSize = transform.localScale;
                if(needsUpdate)
                {
                    networkedObject = new NetworkedObject
                    {
                        Position = NetworkConversionTools.Vector3Tofloat3(transform.localPosition),
                        Rotation = new float4(transform.localEulerAngles.x, transform.localEulerAngles.y,
                            transform.localEulerAngles.z, 0),
                        Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale)
                    };
                }
            }
            else
            {
                if (lastPosition != transform.position)
                    needsUpdate = true;
                if (lastRotation != transform.rotation)
                    needsUpdate = true;
                if (lastSize != transform.localScale)
                    needsUpdate = true;
                lastPosition = transform.position;
                lastRotation = transform.rotation;
                lastSize = transform.localScale;
                if (needsUpdate)
                {
                    networkedObject = new NetworkedObject
                    {
                        Position = NetworkConversionTools.Vector3Tofloat3(transform.position),
                        Rotation = NetworkConversionTools.QuaternionTofloat4(new Quaternion(transform.eulerAngles.x,
                            transform.eulerAngles.y, transform.eulerAngles.z, 0)),
                        Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale)
                    };
                }
            }
            if (networkedObject != null)
            {
                networkedObject.IgnoreObjectLocation = IgnoreObjectLocation;
                if (IgnoreObjectLocation)
                    networkedObject.ObjectLocation = FallbackPath;
                else
                    networkedObject.ObjectLocation = pathDescriptor == null
                        ? networkedObject.ObjectLocation = FallbackPath
                        : networkedObject.ObjectLocation = pathDescriptor.path;
                LocalPlayer.Instance.UpdateObject(networkedObject);
            }
        }

        private IEnumerator _c()
        {
            while (!cts.IsCancellationRequested)
            {
                allowCheck = true;
                yield return new WaitForSeconds(RefreshTime);
            }
        }

        private void OnEnable()
        {
            pathDescriptor = GetComponent<PathDescriptor>();
            cts = new CancellationTokenSource();
            c = StartCoroutine(_c());
        }

        private void FixedUpdate()
        {
            if (CheckTime != CheckTime.FixedUpdate) return;
            Check();
        }

        private void Update()
        {
            if (CheckTime != CheckTime.Update) return;
            Check();
        }

        private void LateUpdate()
        {
            if (CheckTime != CheckTime.LateUpdate) return;
            Check();
        }

        private void OnDisable()
        {
            pathDescriptor = null;
            cts.Cancel();
            if(c != null)
                StopCoroutine(c);
        }
    }

    public enum CheckTime
    {
        FixedUpdate,
        Update,
        LateUpdate,
        InvokeManually
    }
}