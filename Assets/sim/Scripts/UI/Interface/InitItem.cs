using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class InitItem : MonoBehaviour
{
    public TextMeshProUGUI Title;

    public string FilePath;
    public Translator Lator;

    public void RunItem()
    {
        if (Directory.Exists(FilePath))
        {
            Lator.LoadSuccess(new string[] { FilePath });
            Lator.Master.ResetPath();
        }
    }
}
