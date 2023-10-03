using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPin : UIComponents
{
    public enum PinType { Input, Output }
    public PinType Pin;
    
    public GameObject SelectionRing;
    public Vector3 Offset;
    private GameObject Spawned;
    private GameObject ForcedSpawn;

    private Vector2Int PinData;
    private MasterControl Board;
    private bool CanForce;
    private string NodeConnectedTo;
    private string NamePin;

    public void AssignPinData(string Name, int Port, int SubPort, string NodeName, bool CanForceOn, MasterControl Master)
    {
        NamePin = Name;
        PinData = new Vector2Int(Port, SubPort);
        this.Board = Master;
        CanForce = CanForceOn;
        NodeConnectedTo = NodeName;

        if (!CanCreateLink(out Logic.ForceState State))
        {
            SpawnRing(State);
        }
    }

    private void SpawnRing(Logic.ForceState State)
    {
        ForcedSpawn = Instantiate(SelectionRing, transform);
        ForcedSpawn.transform.localPosition = Offset + new Vector3(0, 0.5f * (State.State > 0 ? 1 : -1), 0);
        ForcedSpawn.transform.Rotate(90, 0, 0);
        ForcedSpawn.transform.localScale *= 2;
    }

    public Vector2Int GetPortData()
    {
        return PinData;
    }

    public bool CanCreateLink(out Logic.ForceState State)
    {
        if (CanForce)
        {
            State = Board.GetMasterNode().GetLogic().GetForceOf(NodeConnectedTo, PinData.x, PinData.y);
            if (State != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        State = null;
        return true;
    }

    public override void SetUpProperties(UI ui)
    {
        ui.AddItem("PIN TYPE: " + Pin.ToString(), null);
        if (!string.IsNullOrEmpty(NamePin))
            ui.AddItem("PIN NAME: " + NamePin, null);

        ui.AddItem("PIN: " + PinData.x.ToString(), null);
        ui.AddItem("SUBPIN: " + PinData.y.ToString(), null);
        if (CanForce)
        {
            Logic.ForceState State = Board.GetMasterNode().GetLogic().GetForceOf(NodeConnectedTo, PinData.x, PinData.y);
            if (State != null)
            {
                ui.AddInputItem("Force State [0, 1]", (GateItem Item, UI ui) => { Item.Input.text = State.State.ToString(); }, (GateItem Item, UI ui) => 
                { 
                    State.State = Item.Input.text.Equals("1") ? 1 : 0;
                    if (ForcedSpawn)
                    {
                        ForcedSpawn.transform.localPosition = Offset + new Vector3(0, 0.5f * (State.State > 0 ? 1 : -1), 0);
                    }
                    else
                    {
                        SpawnRing(State);
                    }
                });
                ui.AddItem("Remove Force State", (GateItem Item, UI ui) => { Board.Stop(() => { Board.GetMasterNode().GetLogic().RemoveForceState(NodeConnectedTo, PinData.x, PinData.y);
                    ui.Elect.Refresh();
                }); });
            }
            else
            {
                ui.AddItem("Add Force State", (GateItem Item, UI ui) => { Board.Stop(() => { Board.GetMasterNode().GetLogic().SetForceState(NodeConnectedTo, PinData.x, PinData.y, 0);
                    ui.Elect.Refresh();
                });});
            }
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.InverseTransformPoint(Offset), 0.1f);
    }

    public override void Highlight()
    {
        if (!Spawned)
        {
            Spawned = Instantiate(SelectionRing, transform);
            Spawned.transform.localPosition = Offset;
            Spawned.transform.Rotate(0, 90, 0);
            Spawned.transform.localScale *= 2;
        }
    }

    public override void UnHighlight()
    {
        if (Spawned)
            Destroy(Spawned);
    }

    public override void DeleteComponent()
    {
        
    }

    public override NodeType GetNodeType()
    {
        return Pin == PinType.Input ? NodeType.InputPin : NodeType.OutputPin;
    }

    public override UINode GetUINode()
    {
        return transform.root.GetComponent<UINode>();
    }
}
