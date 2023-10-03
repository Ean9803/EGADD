using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class GateItem : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public TMP_InputField Input;
    private UI ui;

    public delegate void RunAction(GateItem Item, UI ui);
    public RunAction BtnRunAction;

    public void Run()
    {
        if (BtnRunAction != null)
        {
            BtnRunAction(this, ui);
        }
    }

    public void SetInput(string Data)
    {
        if (Input != null)
            Input.text = Data;
    }

    public string GetInput()
    {
        if (Input != null)
            return Input.text;
        return "";
    }

    public void SetUI(UI ui)
    {
        this.ui = ui;
    }
}
