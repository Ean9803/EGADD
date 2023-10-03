using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISelect : MonoBehaviour
{
    public bool KeepX;
    public bool KeepY;
    public bool KeepZ;

    private Vector3 Keep;
    private void Start()
    {
        Keep = transform.position;
    }

    public void Update()
    {
        Vector3 Pos = transform.position;
        if (KeepX)
        {
            Pos.x = Keep.x;
        }
        if (KeepY)
        {
            Pos.y = Keep.y;
        }
        if (KeepZ)
        {
            Pos.z = Keep.z;
        }
        transform.position = Pos;
    }
}
