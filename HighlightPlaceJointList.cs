using Normal.Realtime;
using Oculus.Interaction;
using System.Collections.Generic;
using UnityEngine;

// I feel like using states such as "placed" "hovering" etc would make more sense here? Maybe that would be better practice too?

/// <summary>
/// Attaches this object to one of a list of objects via a fixed joint.
/// </summary>
public class HighlightPlaceJointList : MonoBehaviour
{
    [SerializeField]
    protected Grabbable grabber;
    [SerializeField]
    private List<PlaceJointRef> placements;

    public Grabbable Grabber { get{ return grabber; }}
    
    protected Joint mJoint = null; // the currently attached joint on the object, object is considered "placed" when this is NOT null.

    public Joint MJoint { get{ return mJoint; } }

    /// <summary>
    /// List of currently triggering placements (least recently triggered first) <br/>
    /// Last index is always the currently selected placement
    /// </summary>
    private List<PlaceJointRef> inLst = new List<PlaceJointRef>();

    private bool isGrabbed = false;
    public bool IsGrabbed {
        get { return isGrabbed; }
        set {
            if (isGrabbed == value) return; // do nothing if value has not changed

            isGrabbed = value;
            if (isGrabbed) { // detach when grabbed
                UnPlace();
            } else if (inLst.Count > 0) { // if inside a collider, place in last entered reference
                Place(inLst[inLst.Count - 1]);
            }
        }
    }

    /// <summary>
    /// Returns true if the object has an attached joint (placement).
    /// </summary>
    public bool IsConnected { get { return (mJoint != null); } }

    #region temp_variables
    private PlaceJointRef tempRef;
    #endregion

    protected virtual void Awake() {
        // Joint component must be destroyed, then mJoint set to null;
        // Yet, the variable isn't a copy?!
        // cannot set connectedBody to null; this will essentialy fix the object in place

        // add grab listeners (use normcore sync model if attached)
        if (TryGetComponent<HighlightPlaceJointListSync>(out HighlightPlaceJointListSync sync))
        {
            grabber.onSelect.AddListener(delegate { sync.MakeGrab(true); });
            grabber.onUnselect.AddListener(delegate { sync.MakeGrab(false); });
        }
        else
        { // no normcore component attached (use local grab function)
            grabber.onSelect.AddListener(delegate { IsGrabbed = true; });
            grabber.onUnselect.AddListener(delegate { IsGrabbed = false; });
        }
    }

    // returns PlaceJointRef of collider if it's in the placements list, null if otherwise
    private PlaceJointRef IsInPlacementList(Collider other) {
        foreach (PlaceJointRef p in placements) {
            if (other == p.trigger) return p;
        }

        return null;
    }

    // returns PlaceJointRef of rigidbody if it's in the placements list, null if otherwise
    private PlaceJointRef IsInPlacementList(Rigidbody other) {
        foreach (PlaceJointRef p in placements) {
            if (other == p.connectingBody) return p;
        }

        return null;
    }

    private void OnTriggerEnter(Collider other) {
        tempRef = IsInPlacementList(other);
        if (tempRef != null) { // entering collider of placeable reference
            // disable last mesh before adding (if there is one) and invoke onUnhover
            if (inLst.Count > 0 && isGrabbed) {
                Hover(inLst[inLst.Count - 1], false);
            }

            inLst.Add(tempRef);

            // enable newly entered mesh
            if (isGrabbed) {
                Hover(inLst[inLst.Count - 1], true);
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        tempRef = IsInPlacementList(other);
        if (tempRef != null && inLst.Contains(tempRef)) {
            // disable mesh and invoke onUnhover (if mesh is enabled)
            if (tempRef.highlightMesh.enabled && isGrabbed) {
                Hover(tempRef, false);
            }
            // remove from list
            inLst.Remove(tempRef);

            // highlight previously added mesh (if there is one)
            if (inLst.Count > 0 && isGrabbed) {
                Hover(inLst[inLst.Count - 1], true);
            }
        }
    }

    public virtual void Place(PlaceJointRef place) {
        // can't place if already placed
        if (IsConnected) { 
            Debug.LogError($"Cannot place {this.name} while it's attached to {mJoint.connectedBody.name}! \n(Did you null the joint after destroying it?)");
            return;
        }

        // sleep rigidbody if enabled
        if (place.sleepConnectionOnPlace) { this.GetComponent<Rigidbody>().Sleep(); }

        // if placement exists, move object there before creating the joint
        if (place.placement != null) {
            this.transform.position = place.placement.position;
            this.transform.rotation = place.placement.rotation;
        }

        // since joint is destroyed, create a new one
        mJoint = this.gameObject.AddComponent<FixedJoint>();
        mJoint.connectedBody = place.connectingBody;

        Hover(place, false);
        place.Place(this);
    }

    /// <summary>
    /// Destroys the joint created by this script.
    /// </summary>
    public virtual void UnPlace() {
        if (!IsConnected) { return; } // not currently placed, so ignore call

        tempRef = IsInPlacementList(mJoint.connectedBody);

        Destroy(mJoint); // removes component from object
        mJoint = null; // must be set to null manually; effectively releasing the object from placement

        // enable selected mesh (if there is one) and invoke hover events
        if (inLst.Count > 0 && isGrabbed) {
            Hover(inLst[inLst.Count - 1], true);
            inLst[inLst.Count - 1].onHover.Invoke();
        }

        tempRef.Remove();
    }

    private void DebugPrintInLst() {
        string log = "inLst items:\n";
        for (int i = 0; i < inLst.Count; i++) {
            log += $"[{i}]: {inLst[i].trigger.name}";
        }

        Debug.Log(log);
    }

    /// <summary>
    /// Sets the PlaceJointRef to hover/unhover. Controls mesh and hover event invocation.
    /// </summary>
    /// <param name="place"></param>
    /// <param name="isHover">Set to true to invoke onHover, false to invoke onUnhover</param>

    protected void Hover(PlaceJointRef place, bool isHover) {
        place.highlightMesh.enabled = isHover;
        if (isHover) { place.onHover.Invoke(); }
        else { place.onUnhover.Invoke(); }
    }

    /// <summary>
    /// Gets ownership of the realtime transform of this and any attached PlaceJointReference (if any)
    /// </summary>
    public void RequestOwnershipOfAttached() {
        if (TryGetComponent<RealtimeTransform>(out RealtimeTransform rt))
            rt.RequestOwnership();

        if (TryGetComponent<PlaceJointRef>(out PlaceJointRef p) && p.ParentAttachment != null) {
            p.ParentAttachment.RequestOwnershipOfAttached();
        }
    }
}
