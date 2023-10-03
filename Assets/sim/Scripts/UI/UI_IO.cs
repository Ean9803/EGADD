using UnityEngine;

public class UI_IO : UIComponents
{
    public enum Type { Input, Output }
    public Type Mode;
    public bool State;
    private Vector2Int Pin;
    private Animator Ani;
    private Node NodeInfo;
    private bool S;

    public void SetPin(Vector2Int Pin, Node Info)
    {
        this.Pin = Pin;
        NodeInfo = Info;
    }

    public override void SetUpProperties(UI ui)
    {
        ui.AddItem("Current State: " + (S ? "1" : "0"), null);
        ui.AddItem("PIN: " + Pin.x.ToString(), null);
        ui.AddItem("SUBPIN: " + Pin.y.ToString(), null);
        ui.AddItem("Mode: " + Mode.ToString(), null);
    }

    public override void Highlight() 
    {
    }

    public override void HighlightSelected()
    {
        if (Mode == Type.Input)
        {
            State = !State;
        }
    }

    public void SetIO(float State)
    {
        this.State = State > 0;
        if (Mode == Type.Input && NodeInfo != null)
        {
            NodeInfo.ForceInput(this.State ? 1 : 0, Pin.x, Pin.y);
        }
    }

    public override void UnHighlight()
    {
    }

    public override UINode GetUINode()
    {
        return null;
    }

    public override void DeleteComponent()
    {
        
    }

    private Vector3 Position;
    private Quaternion Rotation;

    public void Start()
    {
        Position = transform.position;
        Rotation = transform.rotation;
        Ani = GetComponent<Animator>();
    }

    public float GetIO()
    {
        if (Mode == Type.Output)
        {
            if (NodeInfo.GetOutput(out float State, Pin.x, Pin.y))
            {
                return State;
            }
        }
        else
        {
            if (NodeInfo.Info.GetPin(Pin.x, out float State, Pin.y))
            {
                return State;
            }
        }
        return 0;
    }

    public void Update()
    {
        if (NodeInfo != null)
        {
            if (Mode == Type.Input)
            {
                NodeInfo.ForceInput(State ? 1 : 0, Pin.x, Pin.y);
            }

            S = GetIO() > 0;

            if (Ani != null)
            {
                Ani.SetBool("Activated", S);
            }
        }
        else
        {
            if (Ani != null)
                Ani.SetBool("Activated", false);
        }
        transform.position = Position;
        transform.rotation = Rotation;
    }

    public override Vector2Int GetPin()
    {
        return Pin;
    }

    public override NodeType GetNodeType()
    {
        return (Mode == Type.Input ? NodeType.Input : NodeType.Output);
    }
}
