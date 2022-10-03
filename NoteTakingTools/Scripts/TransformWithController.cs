using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class responsible for transforming the object in synch with the users controller
// (used when transforming text guiding the user)
public class TransformWithController : MonoBehaviour
{
    [SerializeField]
    private GameObject controller;
    void Update()
    {
        if (!controller) return;
        gameObject.transform.position = controller.transform.position;
        gameObject.transform.rotation = controller.transform.rotation;
    }
}
