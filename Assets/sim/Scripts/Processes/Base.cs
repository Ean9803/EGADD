using System.Collections.Generic;
using UnityEngine;

public abstract class BaseLogic : Logic
{
    public override sealed Logic AddNode(Node node)
    {
        return this;
    }

    public override sealed Logic BreakConnection(string From, string To, int ToPort, int ToSubPort)
    {
        return this;
    }

    public override sealed Logic BreakLink(string NodeName, int Port, int SubPort, (int Port, int SubPort) EdgePort, Node.NodeInfo.PinType Direction)
    {
        return this;
    }

    public override sealed bool DeleteNode(string Name)
    {
        return false;
    }

    public override sealed bool GetNode(string Name, out Node node)
    {
        node = null;
        return false;
    }

    public override sealed Logic RemoveForceState(string Node, int Port, int SubPort)
    {
        return this;
    }

    public override sealed Logic SetConnection(string From, int FromPort, int FromSubPort, string To, int ToPort, int ToSubPort)
    {
        return this;
    }

    public override sealed Logic SetForceState(string Node, int Port, int SubPort, float State)
    {
        return this;
    }

    public override sealed Logic SetLink(string NodeName, int Port, int SubPort, (int Port, int SubPort) EdgePort, Node.NodeInfo.PinType Direction)
    {
        return this;
    }

    public override sealed LogicType GetNodes(out string[] Names)
    {
        Names = null;
        return LogicType.Node;
    }

    public override sealed Dictionary<(int Port, int SubPort), List<Link>> GetLinks(Node.NodeInfo.PinType Direction)
    {
        return null;
    }

    public override sealed void AutoSort(){}
}

public class Not : BaseLogic
{
    private string Name;

    public override void BuildChip(Profile ChipProfileData)
    {
        ChipProfileData.Add("GateType", "NOT", 0);
        this.Name = ChipProfileData.GetString("Name", (0, 0), new string[] { "Not" + Logic.RandomString() }, true)[0];
    }

    public override void Compute(Node.NodeInfo node)
    {
        node.GetPin(0, out float State);
        node.SetPin(0, State == 0 ? 1 : 0);
    }

    public override ChipData Init()
    {
        return new ChipData(Name).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.BACK, Size = Vector3Int.one, State = Pin.Direction.INPUT, PinName = "Input" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.FORWARD, Size = Vector3Int.one, State = Pin.Direction.OUTPUT, PinName = "Output" });
    }

    public override UICommunicator.Skin.Type GetSkin()
    {
        return UICommunicator.Skin.Type.NOT;
    }

    public override void UpdateProperties(UI ui)
    {
        ui.AddInputItem("Alias",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetProfile().GetString("Alias", (0, 0), new string[] { "Not:" + Logic.RandomString() }, true)[0];
        },
        (GateItem Item, UI ui) =>
        {
            GetProfile().Add("Alias", Item.Input.text);
        });
    }
}

public class And : BaseLogic
{
    private string Name;
    private int Inputs;

    public override void BuildChip(Profile ChipProfileData)
    {
        ChipProfileData.Add("GateType", "AND", 0);
        this.Name = ChipProfileData.GetString("Name", (0, 0), new string[] { "And" + Logic.RandomString() }, true)[0];
        this.Inputs = ChipProfileData.GetInt("Input", (0, 0), new int[] { 2 }, true)[0];
    }

    public override void Compute(Node.NodeInfo node)
    {
        node.GetPin(0, out float InitState, 0);
        bool Result = InitState > 0;
        for (int i = 1; i < node.PinSubSize(0, Node.NodeInfo.PinType.Input); i++)
        {
            node.GetPin(0, out float State, i);
            Result = Result && (State > 0);
        }
        node.SetPin(0, Result ? 1 : 0);
    }

    public override ChipData Init()
    {
        return new ChipData(Name).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.EXPANDABLE, Port = 0, PortFace = Pin.Face.BACK, Size = new Vector3Int(2, Inputs, 1000), State = Pin.Direction.INPUT, PinName = "AND Input" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.FORWARD, Size = Vector3Int.one, State = Pin.Direction.OUTPUT, PinName = "Output" });
    }

    public override UICommunicator.Skin.Type GetSkin()
    {
        return UICommunicator.Skin.Type.AND;
    }

    public override void UpdateProperties(UI ui)
    {
        ui.AddInputItem("Alias",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetProfile().GetString("Alias", (0, 0), new string[] { "And:" + Logic.RandomString() }, true)[0];
        },
        (GateItem Item, UI ui) =>
        {
            GetProfile().Add("Alias", Item.Input.text);
        });

        ui.AddInputItem("Input Count",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetShell().Info.PinSubSize(0, Node.NodeInfo.PinType.Input).ToString();
        },
        (GateItem Item, UI ui) =>
        {
            int Inputs = Logic.Profile.ConvertInt(Item.Input.text, 2);
            GetProfile().Add("Input", (int)Mathf.Max(2, Inputs));
            this.Inputs = GetProfile().GetInt("Input", (0, 0), new int[] { 2 })[0];
            if (GetShell().Info.GetPinObject(0, Node.NodeInfo.PinType.Input, out Node.Pin P))
            {
                P.SetSubPinSize(Inputs);
            }
            ui.Elect.Refresh();
        });
    }
}

public class Or : BaseLogic
{
    private string Name;
    private int Inputs;
    
    public override void BuildChip(Profile ChipProfileData)
    {
        ChipProfileData.Add("GateType", "OR", 0);
        this.Name = ChipProfileData.GetString("Name", (0, 0), new string[] { "Or" + Logic.RandomString() }, true)[0];
        this.Inputs = ChipProfileData.GetInt("Input", (0, 0), new int[] { 2 }, true)[0];
    }

    public override void Compute(Node.NodeInfo node)
    {
        node.GetPin(0, out float InitState, 0);
        bool Result = InitState > 0;
        for (int i = 1; i < node.PinSubSize(0, Node.NodeInfo.PinType.Input); i++)
        {
            node.GetPin(0, out float State, i);
            Result = Result || (State > 0);
        }
        node.SetPin(0, Result ? 1 : 0);
    }

    public override ChipData Init()
    {
        return new ChipData(Name).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.EXPANDABLE, Port = 0, PortFace = Pin.Face.BACK, Size = new Vector3Int(2, Inputs, 1000), State = Pin.Direction.INPUT, PinName = "OR Input" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.FORWARD, Size = Vector3Int.one, State = Pin.Direction.OUTPUT, PinName = "Output" });
    }

    public override UICommunicator.Skin.Type GetSkin()
    {
        return UICommunicator.Skin.Type.OR;
    }

    public override void UpdateProperties(UI ui)
    {
        ui.AddInputItem("Alias",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetProfile().GetString("Alias", (0, 0), new string[] { "Or:" + Logic.RandomString() }, true)[0];
        },
        (GateItem Item, UI ui) =>
        {
            GetProfile().Add("Alias", Item.Input.text);
        });

        ui.AddInputItem("Input Count",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetShell().Info.PinSubSize(0, Node.NodeInfo.PinType.Input).ToString();
        },
        (GateItem Item, UI ui) =>
        {
            int Inputs = Logic.Profile.ConvertInt(Item.Input.text, 2);
            GetProfile().Add("Input", (int)Mathf.Max(2, Inputs));
            this.Inputs = GetProfile().GetInt("Input", (0, 0), new int[] { 2 })[0];
            if (GetShell().Info.GetPinObject(0, Node.NodeInfo.PinType.Input, out Node.Pin P))
            {
                P.SetSubPinSize(Inputs);
            }
            ui.Elect.Refresh();
        });
    }
}

public class XOr : BaseLogic
{
    private string Name;
    private int Inputs;

    public override void BuildChip(Profile ChipProfileData)
    {
        ChipProfileData.Add("GateType", "XOR", 0);
        this.Name = ChipProfileData.GetString("Name", (0, 0), new string[] { "XOr" + Logic.RandomString() }, true)[0];
        this.Inputs = ChipProfileData.GetInt("Input", (0, 0), new int[] { 2 }, true)[0];
    }

    public override void Compute(Node.NodeInfo node)
    {
        node.GetPin(0, out float InitState, 0);
        bool Result = InitState > 0;
        for (int i = 1; i < node.PinSubSize(0, Node.NodeInfo.PinType.Input); i++)
        {
            node.GetPin(0, out float State, i);
            Result = (Result != (State > 0));
        }
        node.SetPin(0, Result ? 1 : 0);
    }

    public override ChipData Init()
    {
        return new ChipData(Name).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.EXPANDABLE, Port = 0, PortFace = Pin.Face.BACK, Size = new Vector3Int(2, Inputs, 1000), State = Pin.Direction.INPUT, PinName = "XOR Input" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.FORWARD, Size = Vector3Int.one, State = Pin.Direction.OUTPUT, PinName = "Output" });
    }

    public override UICommunicator.Skin.Type GetSkin()
    {
        return UICommunicator.Skin.Type.XOR;
    }

    public override void UpdateProperties(UI ui)
    {
        ui.AddInputItem("Alias",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetProfile().GetString("Alias", (0, 0), new string[] { "XOr:" + Logic.RandomString() }, true)[0];
        },
        (GateItem Item, UI ui) =>
        {
            GetProfile().Add("Alias", Item.Input.text);
        });

        ui.AddInputItem("Input Count",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetShell().Info.PinSubSize(0, Node.NodeInfo.PinType.Input).ToString();
        },
        (GateItem Item, UI ui) =>
        {
            int Inputs = Logic.Profile.ConvertInt(Item.Input.text, 2);
            GetProfile().Add("Input", (int)Mathf.Max(2, Inputs));
            this.Inputs = GetProfile().GetInt("Input", (0, 0), new int[] { 2 })[0];
            if (GetShell().Info.GetPinObject(0, Node.NodeInfo.PinType.Input, out Node.Pin P))
            {
                P.SetSubPinSize(Inputs);
            }
            ui.Elect.Refresh();
        });
    }
}

public class Light : BaseLogic
{
    private string Name;

    public override void BuildChip(Profile ChipProfileData)
    {
        ChipProfileData.Add("GateType", "LIGHT", 0);
        this.Name = ChipProfileData.GetString("Name", (0, 0), new string[] { "LIGHT:" + Logic.RandomString() }, true)[0];
    }

    public override void Compute(Node.NodeInfo node)
    {
        node.GetPin(0, out float State0, 0);
        node.GetPin(1, out float State1, 0);
        float Result = Mathf.Clamp01(State0 + State1);

        if (GetShell() != null)
        {
            if (GetShell().Info.Skin != null)
            {
                Transform NodeTransform = GetShell().Info.Skin.transform;
                for (int i = 0; i < NodeTransform.childCount; i++)
                {
                    if (NodeTransform.GetChild(i).gameObject.name.Equals("Source"))
                    {
                        NodeTransform.GetChild(i).gameObject.SetActive(Result > 0);
                    }
                }
            }
        }

        GetProfile().Add("LightState", Result);

        node.SetPin(0, Result);
        node.SetPin(1, Result);
    }

    public override ChipData Init()
    {
        return new ChipData(Name).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.LEFT, Size = Vector3Int.one, State = Pin.Direction.INPUT, PinName = "INPUT1" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 1, PortFace = Pin.Face.BACK, Size = Vector3Int.one, State = Pin.Direction.INPUT, PinName = "INPUT2" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.RIGHT, Size = Vector3Int.one, State = Pin.Direction.OUTPUT, PinName = "OUTPUT1" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 1, PortFace = Pin.Face.FORWARD, Size = Vector3Int.one, State = Pin.Direction.OUTPUT, PinName = "OUTPUT2" });
    }

    public override UICommunicator.Skin.Type GetSkin()
    {
        return UICommunicator.Skin.Type.LIGHT;
    }

    public override void UpdateProperties(UI ui)
    {
        ui.AddInputItem("Alias",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetProfile().GetString("Alias", (0, 0), new string[] { "LIGHT:" + Logic.RandomString() }, true)[0];
        },
        (GateItem Item, UI ui) =>
        {
            GetProfile().Add("Alias", Item.Input.text);
        });
    }
}

public class Switch : BaseLogic
{
    private string Name;

    public override void BuildChip(Profile ChipProfileData)
    {
        ChipProfileData.Add("GateType", "SWITCH", 0);
        this.Name = ChipProfileData.GetString("Name", (0, 0), new string[] { "SWITCH:" + Logic.RandomString() }, true)[0];
    }

    public override void Compute(Node.NodeInfo node)
    {

        if (node.GetPin(0, out float State0, 0))
        {
            if (State0 > 0)
            {
                GetProfile().Add("Value", false, 0);
            }
        }
        if (node.GetPin(1, out float State1, 0))
        {
            if (State1 > 0)
            {
                GetProfile().Add("Value", true, 0);
            }
        }

        bool[] Val = GetProfile().GetBool("Value", (0, 0), null);
        bool Output = false;
        if (Val == null)
        {
            GetProfile().Add("Value", false, 0);
        }
        else
        {
            Output = Val[0];
        }

        node.SetPin(0, Output ? 1 : 0);

        Transform NodeTransform = GetShell().Info.Skin.transform;
        for (int i = 0; i < NodeTransform.childCount; i++)
        {
            if (NodeTransform.GetChild(i).gameObject.name.Equals("ON"))
            {
                NodeTransform.GetChild(i).gameObject.SetActive(Output);
            }
        }
    }

    public override void HighlightSelected()
    {
        bool[] Val = GetProfile().GetBool("Value", (0, 0), null);
        if (Val == null)
        {
            GetProfile().Add("Value", false, 0);
        }
        else
        {
            GetProfile().Add("Value", Val[0] ? false : true, 0);
        }
    }

    public override ChipData Init()
    {
        return new ChipData(Name).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.RIGHT, Size = Vector3Int.one, State = Pin.Direction.OUTPUT, PinName = "Output" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 0, PortFace = Pin.Face.LEFT, Size = Vector3Int.one, State = Pin.Direction.INPUT, PinName = "OFF" }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.SINGLE, Port = 1, PortFace = Pin.Face.LEFT, Size = Vector3Int.one, State = Pin.Direction.INPUT, PinName = "ON" });
    }

    public override UICommunicator.Skin.Type GetSkin()
    {
        return UICommunicator.Skin.Type.SWITCH;
    }

    public override void UpdateProperties(UI ui)
    {
        ui.AddInputItem("Alias",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetProfile().GetString("Alias", (0, 0), new string[] { "SWITCH:" + Logic.RandomString() }, true)[0];
        },
        (GateItem Item, UI ui) =>
        {
            GetProfile().Add("Alias", Item.Input.text);
        });
    }
}