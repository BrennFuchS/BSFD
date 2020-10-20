using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;

namespace BSFDTestbed
{
    public class AttachPivot : MonoBehaviour
    {
        public int ID; // ID of this Pivot
        public GameObject attachmentPoint; // GameObject, parent of Part upon attachment.
        public Collider attachmentTrigger; // Collider, Trigger, used for collision test between partTrigger.
    }

    public class Part : MonoBehaviour
    {
        //Bolt related variables
        public GameObject boltParent; // GameObject, Child of Part, Parent of ALL BOLTS.
        public Bolt[] bolts;          // Array of bolts, define in Unity inspector
        public int tightness = 0;    // Current part tightness, calculated by UpdatePartTightness()
        public int MaxTightness;    // Part will not fall off if tightness = MaxTightness

        //part(self) related variables
        public bool isFitted; // Self explanatory
        public int FittmentID; // ID of the current AttachPivot
        public bool disableColliders = false;
        public bool destroyRigidbody = false;
        public Collider partTrigger; // Trigger of part, used for collision test between attachmentTrigger.

        //part (things you are attaching to) related variables
        public List<AttachPivot> Pivots;
        private AttachPivot CurrentPivot;

        //events
        public delegate void AttachDelegate();
        public event AttachDelegate OnAttach;
        public delegate void DetachDelegate();
        public event DetachDelegate OnDetach;

        Rigidbody rb;
        float mass;
        CollisionDetectionMode collmode;
        RigidbodyInterpolation interpolationmode;

        // Use this for initialization
        void Start()
        {
            rb = gameObject.GetComponent<Rigidbody>();
            mass = rb.mass;
            collmode = rb.collisionDetectionMode;
            interpolationmode = rb.interpolation;

            if (bolts.Length != 0 && boltParent != null)
            {
                UpdatePartTightness();
            }

            UpdateIDs();
        }

        public void UpdateIDs() // Only Call in Void Start
        {
            int i = 0;

            foreach (AttachPivot pivots in Pivots)
            {
                pivots.ID = i;
                i++;
            }
        }

        void FixedUpdate()
        {
            if (bolts.Length != 0 && boltParent != null)
            {
                UpdatePartTightness();
            }

            if (isFitted) PartAttached();
        }

        void UpdatePartTightness()
        {
            int _tightness = 0;
            foreach (var b in bolts) _tightness += b.currentBoltStep;
            tightness = _tightness;
        }

        IEnumerator FixParent(Transform parent)
        {
            yield return new WaitForEndOfFrame();
            while (transform.parent != parent)
            {
                transform.parent = parent;
                transform.localPosition = Vector3.zero;
                transform.localEulerAngles = Vector3.zero;
                yield return new WaitForEndOfFrame();
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (other.GetComponent<AttachPivot>() != null && other.GetComponent<AttachPivot>().attachmentTrigger == other && canAttach(other.GetComponent<AttachPivot>()))
            {
                BSFDinteraction.GUIAssemble.Value = true;
                if (Input.GetMouseButtonDown(0))
                {
                    Attach(true, other.GetComponent<AttachPivot>().ID);
                    BSFDinteraction.GUIAssemble.Value = false;
                }
            }
        }

        bool canAttach(AttachPivot pivot) { return transform.IsChildOf(BSFDinteraction.ItemPivot) && pivot.attachmentTrigger.transform.childCount == 0 && !isFitted; }

        public void Attach(bool playAudio, int ID = 0)
        {
            if (isFitted) return;

            if (CurrentPivot == null) { CurrentPivot = Pivots[ID]; }

            transform.parent = Pivots[ID].attachmentPoint.transform;
            FittmentID = ID;
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            StartCoroutine(FixParent(CurrentPivot.attachmentPoint.transform));
            StartCoroutine(LateAttach(playAudio));
            if (boltParent != null)
            {
                boltParent.SetActive(true);
            }
            OnAttach?.Invoke();
        }

        IEnumerator LateAttach(bool playAudio)
        {
            if (!destroyRigidbody)
            {
                while (!rb.isKinematic || rb.useGravity)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;

                    if (disableColliders) { rb.detectCollisions = false; }
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                Component.Destroy(rb);
            }

            if (playAudio) MasterAudio.PlaySound3DAtTransform("CarBuilding", partTrigger.transform, 1f, 1f, 0f, "assemble");
            partTrigger.enabled = false;
            CurrentPivot.attachmentTrigger.enabled = false;
            gameObject.tag = "Untagged";
            isFitted = true;
        }

        void PartAttached()
        {
            if (boltParent == null)
            {
                partTrigger.enabled = true;
            }
            else
            {
                if (tightness >= MaxTightness)
                {
                    partTrigger.enabled = false;
                }
                else if (tightness <= 0)
                {
                    partTrigger.enabled = true;
                }
            }
        }

        public void Detach()
        {
            if (!isFitted) return;

            MasterAudio.PlaySound3DAtTransform("CarBuilding", partTrigger.transform, 1f, 1f, 0f, "disassemble");
            gameObject.tag = "PART";
            transform.parent = null;
            if (!destroyRigidbody)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true;
            }
            CurrentPivot.attachmentTrigger.enabled = true;
            isFitted = false;
            StartCoroutine(FixParent(null));
            if (disableColliders && !destroyRigidbody) { rb.detectCollisions = true; }
            StartCoroutine(LateDetach());
            if (boltParent != null)
            {
                boltParent.SetActive(false);
            }
            OnDetach?.Invoke();
            if (boltParent != null && bolts.Length != 0)
            {
                UntightenAllBolts();
            }
        }

        IEnumerator LateDetach()
        {
            if (!destroyRigidbody)
            {
                while (rb.isKinematic || !rb.useGravity)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    if (disableColliders) { rb.detectCollisions = true; }
                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = mass;
                rb.collisionDetectionMode = collmode;
                rb.interpolation = interpolationmode;
            }
            CurrentPivot.attachmentTrigger.enabled = true;
            CurrentPivot = null;
            FittmentID = 0;
            isFitted = false;
        }

        void UntightenAllBolts()
        {
            foreach (var b in bolts) b.SetBoltStep(0);
        }
    }
}