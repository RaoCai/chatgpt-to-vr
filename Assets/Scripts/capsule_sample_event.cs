using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class capsule_sample_event : MonoBehaviour
{
    public Rigidbody targetRigidbody;

    public void SetGravity()
    {

        if (!targetRigidbody.useGravity)
        {
            targetRigidbody.useGravity = true;
        }
    }
}
