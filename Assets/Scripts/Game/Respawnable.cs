using System;
using UnityEngine;

namespace Hypernex.Game
{
    public class Respawnable : MonoBehaviour
    {
        public float LowestPointRespawnThreshold = 50f;
        public Action OnRespawn = () => { };

        private Grabbable Grabbable;
        private Vector3 StartPosition;
        private Quaternion StartRotation;

        public void Respawn()
        {
            if (Grabbable != null)
            {
                Grabbable.rb.velocity = Vector3.zero;
                Grabbable.rb.angularVelocity = Vector3.zero;
            }
            transform.position = StartPosition;
            transform.rotation = StartRotation;
            OnRespawn.Invoke();
        }

        private void Check()
        {
            if (LocalPlayer.Instance == null)
                return;
            if (transform.position.y < LocalPlayer.Instance.LowestPoint.y - Mathf.Abs(LowestPointRespawnThreshold))
                Respawn();
        }

        private void OnEnable() => Grabbable = GetComponent<Grabbable>();

        private void Start()
        {
            StartPosition = transform.position;
            StartRotation = transform.rotation;
        }

        private void Update()
        {
            if (Grabbable != null)
                return;
            Check();
        }

        private void FixedUpdate()
        {
            if(Grabbable == null)
                return;
            Check();
        }
    }
}