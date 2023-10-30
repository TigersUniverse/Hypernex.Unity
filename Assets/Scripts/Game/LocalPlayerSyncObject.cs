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
        public bool IsSpecial { get; private set; }

        private const float MAX_VALUE_VECTOR_DISTANCE = 0.1f;
        private const float MAX_ANGLE_QUATERNION = 1f;
        private PathDescriptor pathDescriptor;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastSize;
        private Coroutine c;
        private CancellationTokenSource cts;
        private bool allowCheck;
        
        public void Check(bool force = false)
        {
            if (!force && (!allowCheck || GameInstance.FocusedInstance == null || !GameInstance.FocusedInstance.IsOpen || 
                LocalPlayer.Instance == null))
                return;
            allowCheck = false;
            bool needsUpdate = AlwaysSync || force;
            NetworkedObject networkedObject = null;
            if (CheckLocal)
            {
                if (IsDifferentByRange(lastPosition, transform.localPosition, MAX_VALUE_VECTOR_DISTANCE))
                    needsUpdate = true;
                if (IsDifferentByRange(lastRotation, transform.localRotation, MAX_ANGLE_QUATERNION))
                    needsUpdate = true;
                if (IsDifferentByRange(lastSize, transform.localScale, MAX_VALUE_VECTOR_DISTANCE))
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
                if (IsDifferentByRange(lastPosition, transform.position, MAX_VALUE_VECTOR_DISTANCE))
                    needsUpdate = true;
                if (IsDifferentByRange(lastRotation, transform.rotation, MAX_ANGLE_QUATERNION))
                    needsUpdate = true;
                if (IsDifferentByRange(lastSize, transform.localScale, MAX_VALUE_VECTOR_DISTANCE))
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

        private bool IsDifferentByRange(Vector3 last, Vector3 current, float value)
        {
            if (last == current)
                return false;
            float v = Vector3.Distance(last, current);
            return v > value;
        }

        private bool IsDifferentByRange(Quaternion last, Quaternion current, float value)
        {
            if (last == current)
                return false;
            float angle = Quaternion.Angle(current, last);
            return angle > value;
        }

#if DYNAMIC_BONE
        public void MakeSpecial(DynamicBone d)
        {
            IsSpecial = true;
            foreach (LocalPlayerSyncObject localPlayerSyncObject in transform.GetComponentsInChildren<LocalPlayerSyncObject>(true))
            {
                if(d.m_Exclusions.Contains(localPlayerSyncObject.transform)) continue;
                if(AnimationUtility.IsChildOfExclusion(d.m_Exclusions, localPlayerSyncObject.transform)) continue;
                localPlayerSyncObject.IsSpecial = true;
                localPlayerSyncObject.CheckTime = CheckTime.FixedUpdate;
            }
            CheckTime = CheckTime.FixedUpdate;
        }
#endif
        
#if MAGICACLOTH
        public void MakeSpecial()
        {
            IsSpecial = true;
            foreach (LocalPlayerSyncObject localPlayerSyncObject in transform.GetComponentsInChildren<LocalPlayerSyncObject>(true))
            {
                localPlayerSyncObject.IsSpecial = true;
                localPlayerSyncObject.CheckTime = CheckTime.Update;
            }
        }
#endif

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
            IsSpecial = false;
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