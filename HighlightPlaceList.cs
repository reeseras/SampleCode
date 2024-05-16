using Oculus.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attached object must have a collider and rigidbody. Either attached collider 
/// or all placement colliders need 'isTrigger' checked. <br/>
/// Only places upon releasing a grab. <br/>
/// Disables placement's triggering collider to prevent any other objects from being placed there.
/// </summary>
public class HighlightPlaceList : MonoBehaviour
{
    public List<PlaceRef> placements = new List<PlaceRef>();
    public Grabbable grabbable;
    
    [Header("Optional")]
    [Tooltip("Should be a blank anchor (Preferably leave blank)")]
    public AnchorObject anchor;
    [Tooltip("Transform of anchoring object. Will use attached transform if not set")]
    public Transform cObj;

    /// <summary>
    /// List of currently triggering placements (least recently triggered first) <br/>
    /// Last index is always the currently selected placement
    /// </summary>
    private List<PlaceRef> inLst = new List<PlaceRef>(); // 
    private PlaceRef placedIn = null; // where object is currently placed

    private bool isGrabbed = false;
    public bool IsGrabbed {
        get { return isGrabbed; }
        set {
            if (isGrabbed == value)
                return; // do nothing if nothing changes

            if (value) { // grabbed
                //Debug.Log($"Grab {this.name}");
                UnPlace();
            } else { // released
                //Debug.Log($"Release {this.name}");
                TryPlace();
            }
            isGrabbed = value;
        }
    }

    private void Awake() {
        if (anchor == null) {
            anchor = this.gameObject.AddComponent<AnchorObject>();
            anchor.rot = true;
            anchor.enabled = false;
        }

        if (cObj == null) // get default transform if not set
            cObj = this.transform;

        if (placements.Count <= 0)
            Debug.LogWarning($"{this.name} has no placements", this.gameObject);

        // add grab listeners (use normcore sync model if attached)
        if (TryGetComponent<HighlightPlaceListSync>(out HighlightPlaceListSync sync)) {
            grabbable.onSelect.AddListener(delegate { sync.MakeGrab(true); });
            grabbable.onUnselect.AddListener(delegate { sync.MakeGrab(false); });
        } else { // no normcore component attached (use local grab function)
            grabbable.onSelect.AddListener(delegate { IsGrabbed = true; });
            grabbable.onUnselect.AddListener(delegate { IsGrabbed = false; });
        }
    }

    /// <summary>
    /// Returns index if collider is in placement list, -1 if not
    /// </summary>
    public int IsInList(Collider c) {
        for (int i = 0; i < placements.Count; i++){
            if (c == placements[i].trigger)
                return i;
        }
        return -1;
    }

    private void TryPlace() {
        if (inLst.Count <= 0) // not within any colliders
            return;
        
        placedIn = inLst[inLst.Count - 1]; // place in last entered collider (if still in collider)
        Place();
    }

    // !! REMOVED because I don't want to make this sync through normcore !!
    ///// <summary>
    ///// Forces placement on object based on the index of the PlaceRef in the place list
    ///// </summary>
    //public void TryPlace(int pIndex) {
    //    if (pIndex < 0 || pIndex > placements.Count - 1) {
    //        Debug.LogWarning($"Index {pIndex} outside of place list for {this.name}", this);
    //        return;
    //    }

    //    placedIn = placements[pIndex];
    //    Place();
    //}

    private void Place() {
        anchor.anchor = placedIn.placement;
        anchor.enabled = true;
        placedIn.trigger.enabled = false; // disable trigger to prevent multiple objects from being placed in the same spot
        placedIn.highlightMesh.enabled = false; // disable mesh
        placedIn.onPlace.Invoke();

        // clear data on placement
        inLst.Clear();
    }

    private void UnPlace() {
        if (placedIn == null) // not placed, do nothing
            return;

        anchor.enabled = false;
        placedIn.trigger.enabled = true;
        placedIn.onRemove.Invoke();
        placedIn = null;
    }

    /// <summary>
    /// returns the index of where the object is currently placed, -1 if unplaced
    /// </summary>
    public int GetPlacedIndex() {
        if (placedIn == null) // not placed
            return -1;

        for (int i = 0; i < placements.Count; i++) {
            if (placements[i] == placedIn)
                return i;
        }

        // Shouldn't get here. I guess the if statement above could be removed and we *could* just use this return statement instead (without the error)
        Debug.LogError($"placedIn trigger object \"{placedIn.trigger.name}\" not found in placements (size {placements.Count})", this);
        return -1;
    }

    private void OnTriggerEnter(Collider other) {
        int index = IsInList(other);
        if (index >= 0 && placedIn == null && !inLst.Contains(placements[index])) { // might not need to check if it's in the triggering list
            inLst.Add(placements[index]);
        }

        // highlight currently selected mesh
        if (inLst.Count > 0)
            inLst[inLst.Count - 1].highlightMesh.enabled = true;
    }

    private void OnTriggerExit(Collider other) {
        int index = IsInList(other);
        if (index >= 0 &&  placedIn == null && inLst.Contains(placements[index])) {
            int lIndex = IndexInInLst(other);

            // disable mesh (whether or not it's highlighted)
            inLst[lIndex].highlightMesh.enabled = false;
            inLst.Remove(placements[index]);
        }

        // highlight currently selected mesh
        if (inLst.Count > 0)
        inLst[inLst.Count - 1].highlightMesh.enabled = true;
    }

    private int IndexInInLst(Collider c) {
        for (int i = 0; i < inLst.Count; i++) {
            if (inLst[i].trigger == c) return i;
        }

        return -1;
    }
}

[Serializable]
public class PlaceRef {
    [Tooltip("Where the object is going to be anchored")]
    public Transform placement;
    [Tooltip("This mesh will be enabled when the object is held over it")]
    public MeshRenderer highlightMesh;
    [Tooltip("The collider that triggers highlight and placement")]
    public Collider trigger;
    public UnityEvent onPlace = new UnityEvent();
    public UnityEvent onRemove = new UnityEvent();

    public bool Equals(PlaceRef other) {
        if (other.placement == placement && other.trigger == trigger) return true;
        else return false;
    }
}
