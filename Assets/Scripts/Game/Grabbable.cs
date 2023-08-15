using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Hypernex.Game
{
    public class Grabbable : MonoBehaviour
    {
        public static List<Grabbable> AllGrabbables => new(allGrabbables);
        public static List<Grabbable> HighlightedGrabbables => new(highlightedGrabbables);
        public bool ApplyVelocity = true;
        public float VelocityAmount = 10f;
        public float VelocityThreshold = 0.05f;
        public bool GrabByLaser = true;
        public float LaserGrabDistance = 5f;
        public bool IgnoreLaserColoring;
        public bool GrabByDistance = true;
        public float GrabDistance = 3f;
        [HideInInspector] public Transform CurrentGrabbed;
        public IBinding CurrentGrabbedFromBinding;
        public Action<IBinding> OnBindingGrab = binding => { };
        public Action<IBinding> OnBindingRelease = binding => { };

        private static List<Grabbable> allGrabbables = new();
        private static List<Grabbable> highlightedGrabbables = new();
        private Transform ObjectGrabbing;
        private NetworkSync NetworkSync;
        private List<Renderer> meshRenderers = new();
        internal Rigidbody rb;
        private Collider cd;
        private Vector3 previousPosition;
        private bool highlighted;

        private List<IBinding> foundBindings = new();

        private bool IsFacingObject(Transform t, Transform target)
        {
            Ray r = new Ray(t.position, t.TransformDirection(Vector3.forward));
            RaycastHit[] raycastHits = Physics.RaycastAll(r, Mathf.Infinity);
            bool c = false;
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.transform == target)
                    c = (GrabByLaser || !LocalPlayer.IsVR) && Vector3.Distance(t.position, target.position) <= LaserGrabDistance;
            }
            bool f = Vector3.Distance(t.position, target.position) < GrabDistance && GrabByDistance && LocalPlayer.IsVR && !c;
            return c || f;
        }
        
        private (bool, bool) IsFacingObjectSpecific(Transform t, Transform target)
        {
            Ray r = new Ray(t.position, t.TransformDirection(Vector3.forward));
            RaycastHit[] raycastHits = Physics.RaycastAll(r, Mathf.Infinity);
            bool c = false;
            foreach (RaycastHit hit in raycastHits)
            {
                if (hit.collider.transform == target)
                    c = (GrabByLaser || !LocalPlayer.IsVR) && Vector3.Distance(t.position, target.position) <= LaserGrabDistance;
            }
            bool f = Vector3.Distance(t.position, target.position) < GrabDistance && GrabByDistance && LocalPlayer.IsVR && !c;
            return (c, f);
        }

        private void Register(Transform from, IBinding instanceBinding)
        {
            GameObject o = new GameObject(gameObject.name + "_" + Guid.NewGuid());
            o.transform.SetParent(from);
            o.transform.SetPositionAndRotation(o.transform.position + transform.position, transform.rotation);
            CurrentGrabbed = o.transform;
            o.SetActive(false);
            if(cd != null)
                cd.enabled = false;
            if (rb != null)
            {
                rb.isKinematic = true;
                //rb.detectCollisions = false;
            }
            CurrentGrabbedFromBinding = instanceBinding;
            OnBindingGrab.Invoke(CurrentGrabbedFromBinding);
            ObjectGrabbing = from;
            if (NetworkSync != null && (!NetworkSync.IsOwned() || NetworkSync.NetworkSteal))
                NetworkSync.Claim();
            //else if(NetworkSync != null && !NetworkSync.IsOwnedByLocalPlayer())
            //Deregister(true);
        }

        private void Deregister(bool ignoreVelocity = false)
        {
            Vector3? direction = null;
            if (ApplyVelocity && rb != null && !ignoreVelocity &&
                Vector3.Distance(CurrentGrabbed.position, previousPosition) > VelocityThreshold)
            {
                //Vector3 velocity = CurrentGrabbed.forward * VelocityAmount;
                //rb.velocity = velocity;
                if (rb != null)
                {
                    rb.isKinematic = false;
                    //rb.detectCollisions = true;
                }
                direction = CurrentGrabbed.position - previousPosition;
                rb.AddForce(direction.Value * (VelocityAmount * 250f));
            }
            if(CurrentGrabbed != null)
                Destroy(CurrentGrabbed.gameObject);
            if(cd != null)
                cd.enabled = true;
            if (!ApplyVelocity && rb != null)
            {
                rb.isKinematic = false;
                //rb.detectCollisions = true;
            }
            OnBindingRelease.Invoke(CurrentGrabbedFromBinding);
            CurrentGrabbedFromBinding = null;
            ObjectGrabbing = null;
            if (NetworkSync != null && NetworkSync.IsOwnedByLocalPlayer())
                NetworkSync.Unclaim(direction, VelocityAmount);
        }

        private void Highlight(GameObject t, (bool, bool) specificResult)
        {
            if (highlighted) return;
            highlighted = true;
            if(!HighlightedGrabbables.Contains(this))
                highlightedGrabbables.Add(this);
            meshRenderers.ForEach(meshRenderer =>
            {
                List<Material> materials = meshRenderer.materials.ToList();
                materials.Add(Init.Instance.OutlineMaterial);
                meshRenderer.materials = materials.ToArray();
            });
            if (!IgnoreLaserColoring)
            {
                if (t != null && specificResult.Item1)
                {
                    XRInteractorLineVisual lineVisual = t.GetComponent<XRInteractorLineVisual>();
                    LineRenderer lineRenderer = t.GetComponent<LineRenderer>();
                    if (lineVisual != null && lineRenderer != null)
                        lineRenderer.colorGradient = lineVisual.validColorGradient;
                }
                else if (t != null && !specificResult.Item1)
                {
                    XRInteractorLineVisual lineVisual = t.GetComponent<XRInteractorLineVisual>();
                    LineRenderer lineRenderer = t.GetComponent<LineRenderer>();
                    if (lineVisual != null && lineRenderer != null)
                        lineRenderer.colorGradient = lineVisual.invalidColorGradient;
                }
            }
        }

        private void NoHighlight()
        {
            if (!highlighted) return;
            highlighted = false;
            if(HighlightedGrabbables.Contains(this))
                highlightedGrabbables.Remove(this);
            if (meshRenderers == null) return;
            meshRenderers.ForEach(meshRenderer =>
            {
                List<Material> materials = meshRenderer.materials.ToList();
                Material matToRemove = materials.ElementAt(materials.Count - 1);
                if (matToRemove.shader != Init.Instance.OutlineMaterial.shader)
                    return;
                materials.Remove(matToRemove);
                meshRenderer.materials = materials.ToArray();
            });
            highlighted = false;
            if (!IgnoreLaserColoring)
            {
                try
                {
                    GameObject[] gs = {
                        LocalPlayer.Instance.Camera.gameObject, 
                        LocalPlayer.Instance.LeftHandReference.gameObject,
                        LocalPlayer.Instance.RightHandReference.gameObject
                    };
                    foreach (GameObject t in gs)
                    {
                        XRInteractorLineVisual lineVisual = t.GetComponent<XRInteractorLineVisual>();
                        LineRenderer lineRenderer = t.GetComponent<LineRenderer>();
                        if (lineVisual != null && lineRenderer != null)
                            lineRenderer.colorGradient = lineVisual.invalidColorGradient;
                    }
                }
                // Happening when the game is closing (probably)
                catch(MissingReferenceException){}
            }
        }

        private bool IsGrabbingSomething(IBinding binding) =>
            AllGrabbables.Any(allGrabbable =>
                allGrabbable.CurrentGrabbedFromBinding?.AttachedObject == binding.AttachedObject);

        private Grabbable GetClosestGrabbable(Transform t)
        {
            (Grabbable, float)? g = null;
            foreach (Grabbable grabbable in AllGrabbables)
            {
                if (grabbable.ObjectGrabbing == t)
                    return null;
                if(grabbable.CurrentGrabbed != null) continue;
                float d = Vector3.Distance(grabbable.transform.position, t.transform.position);
                if (g == null)
                    g = (grabbable, d);
                else if (d < g.Value.Item2)
                    g = (grabbable, d);
            }
            return g?.Item1;
        }

        private void Start()
        {
            NetworkSync = GetComponent<NetworkSync>();
            if (NetworkSync != null)
            {
                NetworkSync.OnForce += force =>
                {
                    rb.AddForce(force);
                    cd.enabled = true;
                };
                NetworkSync.OnSteal += () =>
                {
                    NoHighlight();
                    if (CurrentGrabbed != null)
                        Deregister(true);
                };
            }
            meshRenderers.Clear();
            foreach (Renderer r in transform.GetComponentsInChildren<Renderer>())
                if(!meshRenderers.Contains(r) && r.GetComponent<CanvasRenderer>() == null)
                    meshRenderers.Add(r);
        }

        private void OnEnable()
        {
            rb = GetComponent<Rigidbody>();
            cd = GetComponent<Collider>();
            allGrabbables.Add(this);
        }

        private void Update()
        {
            if (NetworkSync != null && NetworkSync.IsOwned() && !NetworkSync.IsOwnedByLocalPlayer())
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
                if (!NetworkSync.NetworkSteal)
                {
                    NoHighlight();
                    if (CurrentGrabbed != null)
                        Deregister(true);
                }
            }
            else if (CurrentGrabbed == null)
            {
                //if (cd != null)
                    //cd.enabled = true;
                if (rb != null)
                {
                    rb.isKinematic = false;
                    //rb.detectCollisions = true;
                }
            }
            bool vr = LocalPlayer.IsVR;
            foreach (IBinding instanceBinding in new List<IBinding>(LocalPlayer.Instance.Bindings))
            {
                bool facing = IsFacingObject(instanceBinding.AttachedObject, transform);
                if(facing && !instanceBinding.Grab && !foundBindings.Contains(instanceBinding))
                    foundBindings.Add(instanceBinding);
                else if (!facing && foundBindings.Contains(instanceBinding))
                    foundBindings.Remove(instanceBinding);
                if (instanceBinding.Grab && CurrentGrabbedFromBinding != instanceBinding && foundBindings.Contains(instanceBinding) && !IsGrabbingSomething(instanceBinding) && (!vr || GetClosestGrabbable(instanceBinding.AttachedObject) == this))
                {
                    if (CurrentGrabbed != null)
                        Destroy(CurrentGrabbed.gameObject);
                    if (!vr && IsFacingObject(LocalPlayer.Instance.Camera.transform, transform))
                        Register(LocalPlayer.Instance.Camera.transform, instanceBinding);
                    else
                    {
                        if(instanceBinding.IsLook && IsFacingObject(LocalPlayer.Instance.LeftHandReference, transform))
                            Register(LocalPlayer.Instance.LeftHandReference, instanceBinding);
                        else if(!instanceBinding.IsLook && IsFacingObject(LocalPlayer.Instance.RightHandReference, transform))
                            Register(LocalPlayer.Instance.RightHandReference, instanceBinding);
                    }
                    NoHighlight();
                }
                else if (!instanceBinding.Grab && CurrentGrabbedFromBinding == instanceBinding)
                    Deregister();
            }

            (bool, bool) desktopFacingSpecific = IsFacingObjectSpecific(LocalPlayer.Instance.Camera.transform, transform);
            (bool, bool) leftControllerFacingSpecific =
                IsFacingObjectSpecific(LocalPlayer.Instance.LeftHandReference, transform);
            (bool, bool) rightControllerFacingSpecific =
                IsFacingObjectSpecific(LocalPlayer.Instance.RightHandReference, transform);
            bool desktopFacing = !vr && ObjectGrabbing != LocalPlayer.Instance.Camera.transform &&
                                 (desktopFacingSpecific.Item1 || desktopFacingSpecific.Item2);
            bool leftControllerFacing = vr && ObjectGrabbing != LocalPlayer.Instance.LeftHandReference &&
                                        (leftControllerFacingSpecific.Item1 || leftControllerFacingSpecific.Item2);
            bool rightControllerFacing = vr && ObjectGrabbing != LocalPlayer.Instance.RightHandReference &&
                                         (rightControllerFacingSpecific.Item1 || rightControllerFacingSpecific.Item2);
            if(desktopFacing)
                Highlight(LocalPlayer.Instance.Camera.gameObject, desktopFacingSpecific);
            else if(leftControllerFacing && GetClosestGrabbable(LocalPlayer.Instance.LeftHandReference) == this)
                Highlight(LocalPlayer.Instance.LeftHandReference.gameObject, leftControllerFacingSpecific);
            else if(rightControllerFacing && GetClosestGrabbable(LocalPlayer.Instance.RightHandReference) == this)
                Highlight(LocalPlayer.Instance.RightHandReference.gameObject, rightControllerFacingSpecific);
            else
            {
                NoHighlight();
                /*if(CurrentGrabbed == null)
                    foundBindings.Clear();*/
            }
            if (CurrentGrabbed != null)
            {
                transform.position = CurrentGrabbed.position;
                transform.rotation = CurrentGrabbed.rotation;
            }
        }

        private void LateUpdate()
        {
            if(CurrentGrabbed != null)
                previousPosition = CurrentGrabbed.position;
        }

        private void OnDisable()
        {
            allGrabbables.Remove(this);
            NoHighlight();
            Deregister(true);
        }
    }
}