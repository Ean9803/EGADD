using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class StaticInfo : MonoBehaviour
{
    public static StaticInfo Info { get; internal set; }

    public List<ObjectInfo> Objects = new List<ObjectInfo>();

    [System.Serializable]
    public class ObjectInfo
    {
        public string ObjectName;
        public GameObject Object;
    }

    public GameObject GetObject(string Name)
    {
        foreach (var item in Objects)
        {
            if (item.ObjectName.Equals(Name))
            {
                return item.Object;
            }
        }
        return null;
    }

    public void Awake()
    {
        Info = this;
    }
}
