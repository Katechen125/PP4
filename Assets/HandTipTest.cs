using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class HandTipTest : MonoBehaviour
{
    public Transform leftTip;
    public Transform rightTip;
    XRHandSubsystem hands;

    void Update()
    {
        if (hands == null)
        {
            var list = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(list);
            if (list.Count == 0) return;
            hands = list[0];
        }
        Place(hands.leftHand, leftTip);
        Place(hands.rightHand, rightTip);
    }

    void Place(XRHand hand, Transform t)
    {
        if (t == null) return;
        var joint = hand.GetJoint(XRHandJointID.IndexTip);
        bool ok = hand.isTracked && joint.TryGetPose(out Pose pose);
        t.gameObject.SetActive(ok);
        if (ok) { joint.TryGetPose(out pose); t.position = pose.position; }
    }
}