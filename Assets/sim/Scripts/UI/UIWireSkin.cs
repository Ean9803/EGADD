using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWireSkin : MonoBehaviour
{
    public GameObject StartBone;

    public void Init()
    {
        SetLocal(StartBone);
    }

    private void SetLocal(GameObject Subject)
    {
        Subject.transform.localPosition = Vector3.zero;
        if (Subject.transform.childCount > 0)
            SetLocal(Subject.transform.GetChild(0).gameObject);
    }

    public int GetDepth()
    {
        return Dig(StartBone) - 1;
    }

    public void SetJoint(int I, Vector3 Position, Vector3 Direction)
    {
        GameObject Subject = StartBone;
        for (int i = -1; i < I; i++)
        {
            if (Subject.transform.childCount > 0)
                Subject = Subject.transform.GetChild(0).gameObject;
        }
        Subject.transform.position = Position;
        Subject.transform.LookAt(Direction + Position, Vector3.up);
        Subject.transform.Rotate(new Vector3(-90, 0, 0));
    }

    private int Dig(GameObject Top)
    {
        if (Top.transform.childCount > 0)
            return 1 + Dig(Top.transform.GetChild(0).gameObject);
        return 1;
    }
}
