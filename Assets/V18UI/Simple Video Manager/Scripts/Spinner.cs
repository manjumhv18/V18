using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spinner : MonoBehaviour
{
    private Transform m_Transform;

    private void Start()
    {
        m_Transform = gameObject.transform;
    }

    void Update()
    {
        m_Transform.Rotate(new Vector3(0, 0, 25));
    }
}
