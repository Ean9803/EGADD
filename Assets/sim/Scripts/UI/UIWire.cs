using System.Collections.Generic;
using UnityEngine;

public class UIWire : UIComponents
{
    public GameObject TargetPoint;
    public GameObject StartPoint;
    public GameObject WireSkin;

    public UICommunicator Board;
    private UICommunicator.LineProfile LineProfile;

    private List<UIWireSkin> Wires = new List<UIWireSkin>();
    private List<GameObject> WireCol = new List<GameObject>();

    public GameObject SelectionRing;

    private bool SpawnHighlight = false;
    private bool IsLink = false;

    public override void Highlight()
    {
        SpawnHighlight = true;
        UpdateLine(true);
    }

    public override void UnHighlight()
    {
        SpawnHighlight = false;
        UpdateLine(true);
    }

    public override void SetUpProperties(UI ui)
    {
        if (!IsLink)
        {
            ui.AddItem("GATE From: " + From, null);
            ui.AddItem("GATE To: " + To, null);
        }
        else
        {
            ui.AddItem("GATE To: " + From, null);
            ui.AddItem("PIN connection: " + ToEdgePin.ToString(), null);
            ui.AddItem("SUBPIN connection: " + ToEdgeSubPin.ToString(), null);
            ui.AddItem("DIRECTION: " + Dir.ToString(), null);
        }
    }

    public override UINode GetUINode()
    {
        return transform.root.GetComponent<UINode>();
    }

    public override NodeType GetNodeType()
    {
        return NodeType.Wire;
    }

    public override void DeleteComponent()
    {
        if (!IsLink)
        {
            Board.Master.GetMasterNode().GetLogic().BreakConnection(From, To, ToPin, ToSubPin);
        }
        else
        {
            Board.Master.GetMasterNode().GetLogic().BreakLink(From, ToPin, ToSubPin, (ToEdgePin, ToEdgeSubPin), Dir);
        }
        Board.Master.Refresh();
    }

    private string From, To;
    private int ToPin, ToSubPin;
    private int ToEdgePin, ToEdgeSubPin;
    private Node.NodeInfo.PinType Dir;

    public void AssignNode(string From, string To, int ToPin, int ToSubPin)
    {
        IsLink = false;
        this.From = From;
        this.To = To;
        this.ToPin = ToPin;
        this.ToSubPin = ToSubPin;
    }

    public void AssignLink(string From, int FromPin, int FromSubPin, int EdgePin, int EdgeSubPin, Node.NodeInfo.PinType Dir)
    {
        IsLink = true;
        this.From = From;
        this.ToPin = FromPin;
        this.ToSubPin = FromSubPin;
        ToEdgePin = EdgePin;
        ToEdgeSubPin = EdgeSubPin;
        this.Dir = Dir;
    }

    public void Start()
    {
        UpdateLine(false);
    }

    public void Update()
    {
        UpdateLine(false);
    }

    private void UpdateLine(bool Override)
    {
        LineProfile = Board.GenerateCircitPath(LineProfile, StartPoint.transform.position, TargetPoint.transform.position);

        if (LineProfile.Updated || Override)
        {
            LineProfile.Updated = false;
            foreach (var item in Wires)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            Wires.Clear();

            foreach (var item in WireCol)
            {
                if (item != null)
                    Destroy(item);
            }
            WireCol.Clear();

            UIWireSkin StartWire = Instantiate(WireSkin, StartPoint.transform).GetComponent<UIWireSkin>();
            StartWire.Init();
            Wires.Add(StartWire);
            int MaxDepth = StartWire.GetDepth();
            int j = 0;
            for (int i = 0; i < LineProfile.LinePoints.Count; i++)
            {
                j = i % MaxDepth;
                if (j < MaxDepth - 1)
                {
                    Wires[Wires.Count - 1].SetJoint(j, LineProfile.LinePoints[i], LineProfile.Directions[i]);
                }
                else
                {
                    Wires[Wires.Count - 1].SetJoint(j, LineProfile.LinePoints[i], LineProfile.Directions[i]);
                    Wires.Add(Instantiate(WireSkin, StartPoint.transform).GetComponent<UIWireSkin>());
                    Wires[Wires.Count - 1].Init();
                    Wires[Wires.Count - 1].SetJoint(-1, LineProfile.LinePoints[i], LineProfile.Directions[i]);
                }

                if (i < LineProfile.LinePoints.Count - 1)
                {
                    GameObject WireHitbox = new GameObject("HitBox");
                    WireHitbox.transform.parent = transform;
                    WireCol.Add(WireHitbox);
                    WireHitbox.transform.position = LineProfile.LinePoints[i];
                    WireHitbox.transform.LookAt(LineProfile.LinePoints[i + 1], Vector3.up);
                    CapsuleCollider Col = WireHitbox.AddComponent<CapsuleCollider>();
                    Col.direction = 2;
                    Col.radius = 0.15f;
                    Col.height = (LineProfile.LinePoints[i + 1] - LineProfile.LinePoints[i]).magnitude;
                    Col.center = new Vector3(0, 0, Col.height / 2);
                    
                    if (SpawnHighlight)
                    {
                        GameObject Ring = Instantiate(SelectionRing, WireHitbox.transform);
                        Ring.transform.localPosition = new Vector3(0, 0, Col.height / 2);
                    }
                }
            }
        }
    }
}
