using System;
using UnityEngine;

namespace Hypernex.Tools
{
    public class ColliderEvents : MonoBehaviour
    {
        public Action<Collider> TriggerEnter { get; set; } = c => { };
        public Action<Collider> TriggerStay { get; set; } = c => { };
        public Action<Collider> TriggerExit { get; set; } = c => { };
        public Action<Collision> CollisionEnter { get; set; } = c => { };
        public Action<Collision> CollisionStay { get; set; } = c => { };
        public Action<Collision> CollisionExit { get; set; } = c => { };

        public void OnTriggerEnter(Collider c) => TriggerEnter.Invoke(c);
        public void OnTriggerStay(Collider c) => TriggerStay.Invoke(c);
        public void OnTriggerExit(Collider c) => TriggerExit.Invoke(c);
        public void OnCollisionEnter(Collision collision) => CollisionEnter.Invoke(collision);
        public void OnCollisionStay(Collision collision) => CollisionStay.Invoke(collision);
        public void OnCollisionExit(Collision collision) => CollisionExit.Invoke(collision);
    }
}