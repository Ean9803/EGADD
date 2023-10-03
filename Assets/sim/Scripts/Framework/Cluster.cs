using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cluster : Logic
{
    private Dictionary<string, Node> Nodes = new Dictionary<string, Node>();
    private List<ForceState> Force = new List<ForceState>();
    private Dictionary<Node.NodeInfo.PinType, Dictionary<(int Port, int SubPort), List<Link>>> Links = new Dictionary<Node.NodeInfo.PinType, Dictionary<(int Port, int SubPort), List<Link>>>();
    private string Name;
    private int Repeat;
    private float[] Ins;
    private Vector2Int EdgeCounts;
    private Vector2 Size;

    public override string[] GenerateRepresentation(bool Full, out bool isSDLCompat, int Layer = 0, List<string> PostFix = null)
    {
        string[] Output = new string[5];
        for (int i = 0; i < Output.Length; i++)
        {
            Output[i] = "";
        }
        string[] NodeData;
        if (PostFix == null)
        {
            PostFix = new List<string>();
        }

        List<string> PathDown = new List<string>(PostFix);
        Output[3] = GenerateNodeHeaderComment(false);
        isSDLCompat = true;
        if (Layer < 100)
        {
            List<(string ICName, string GateName)> Connects = new List<(string ICName, string GateName)>();
            if (Layer == 0 || Full)
            {
                if (Layer != 0)
                {
                    PathDown.Add(Name);
                }
                else
                {
                    PathDown.Add("");
                }
                foreach (var item in Nodes)
                {
                    NodeData = item.Value.GetLogic().GenerateRepresentation(Full, out bool CompatNode, Layer + 1, PathDown);
                    isSDLCompat = CompatNode && isSDLCompat;
                    Output[0] += NodeData[0] + "\n";
                    Output[1] += NodeData[1] + "\n";
                    Output[2] += NodeData[2] + "\n";
                    Output[4] += NodeData[4];
                }

                if (Layer > 0)
                {
                    NodeData = base.GenerateRepresentation(false, out bool Compat, Layer + 1, PathDown);
                    Output[2] += NodeData[2] + "\n";
                    Output[4] += NodeData[4];
                }
                string Prefix = Summation(PathDown);
                foreach (var item in Force)
                {
                    if (GetNode(item.Node, out Node SetNode))
                    {
                        Output[2] += (item.State > 0 ? "POWER" : "GROUND") + "\t-\t" + Prefix + SetNode.Name + "#" + SetNode.Info.GetAbsPin(item.Port, item.SubPort, Node.NodeInfo.PinType.Input);
                    }
                }
                foreach (var item in Links)
                {
                    foreach (var Side in item.Value)
                    {
                        foreach (var Gate in Side.Value)
                        {
                            string GateName = Gate.node.GetLogic().GetProfile().GetString("Alias", (0, 0), Gate.node.GetLogic().GetProfile().GetString("Name", (0, 0), new string[] { "ERROR" }))[0];
                            int InnerPin = Gate.node.Info.GetAbsPin(Gate.Port, Gate.SubPort, item.Key) + (item.Key == Node.NodeInfo.PinType.Output ? Gate.node.Info.TotalPinSize(Node.NodeInfo.PinType.Input) : 0);
                            int IOPin = GetShell().Info.GetAbsPin(Side.Key.Port, Side.Key.SubPort, item.Key);
                            if (Layer == 0)
                            {
                                Output[2] += "\t" + Prefix + GateName + "#" + InnerPin + "\t-\t" + (item.Key == Node.NodeInfo.PinType.Input ? "IN" : "OUT") + "#" + IOPin + "\n";
                            }
                            else
                            {
                                string ICName = GetShell().GetLogic().GetProfile().GetString("Alias", (0, 0), GetShell().GetLogic().GetProfile().GetString("Name", (0, 0), new string[] { "ERROR" }))[0];
                                string Temp = ICName + (item.Key == Node.NodeInfo.PinType.Input ? "#" + IOPin : "#" + (IOPin + GetShell().Info.TotalPinSize(Node.NodeInfo.PinType.Input)).ToString());
                                Connects.Add((Prefix + Temp, Prefix + GateName + "#" + InnerPin));
                                Output[4] += Temp + "\n" + Prefix + GateName + "#" + InnerPin + "\n";
                            }
                        }
                    }
                }

                if (Layer == 0)
                {
                    string[] Replace = Output[4].Split('\n');
                    for (int i = 1; i < Replace.Length; i += 2)
                    {
                        if (Replace[i - 1].Length > 0 && Replace[i].Length > 0)
                        {
                            Output[2] = Output[2].Replace(Replace[i - 1], Replace[i]);
                        }
                    }
                }
            }
            else
            {
                isSDLCompat = false;
                NodeData = base.GenerateRepresentation(false, out bool Compat, Layer + 1);
                Output[0] = NodeData[0];
                Output[1] = NodeData[1];
                Output[2] = NodeData[2];
            }

            foreach (var item in Connects)
            {
                Output[2] = Output[2].Replace(item.ICName, item.GateName);
            }
        }
        return Output;
    }


    public override void BuildChip(Profile ChipProfileData)
    {
        this.Name = ChipProfileData.GetString("Name", (0,0), new string[]{ "Cluster" + Logic.RandomString() }, true)[0];
        Repeat = Mathf.Max(1, ChipProfileData.GetInt("Repeat", (0, 0), new int[] { 1 })[0]);
        Ins = ChipProfileData.GetFloat("Start", (0, int.MaxValue), null);
        EdgeCounts = new Vector2Int(
            ChipProfileData.GetInt("Input", (0, 0), new int[] { 10 }, true)[0],
            ChipProfileData.GetInt("Output", (0, 0), new int[] { 10 }, true)[0]);
        Size = new Vector2(
            ChipProfileData.GetFloat("SizeX", (0, 0), new float[] { UI.DefaultBoardSize.x }, true)[0],
            ChipProfileData.GetFloat("SizeY", (0, 0), new float[] { UI.DefaultBoardSize.y }, true)[0]);
    }

    private void ProcessDirection(Node.NodeInfo node, Node.NodeInfo.PinType Side)
    {
        if (Links.ContainsKey(Side))
        {
            foreach (var item in Links[Side])
            {
                for (int i = item.Value.Count - 1; i >= 0; i--)
                {
                    if (item.Value[i].node == null)
                    {
                        item.Value.RemoveAt(i);
                    }
                    else
                    {
                        Node LinkedNode = item.Value[i].node;
                        if (node.HasPin(item.Key.Port, item.Key.SubPort, Side, out float CurrentState))
                        {
                            if (Side == Node.NodeInfo.PinType.Input)
                            {
                                node.GetPin(item.Key.Port, out float State, item.Key.SubPort);
                                if (!LinkedNode.ForceInput(State + CurrentState, item.Value[i].Port, item.Value[i].SubPort))
                                {
                                    item.Value.RemoveAt(i);
                                }
                            }
                            else
                            {
                                if (LinkedNode.GetOutput(out float State, item.Value[i].Port, item.Value[i].SubPort))
                                {
                                    node.SetPin(item.Key.Port, State + CurrentState, item.Key.SubPort);
                                }
                                else
                                {
                                    item.Value.RemoveAt(i);
                                }
                            }
                        }
                        else
                        {
                            Links[Side].Remove(item.Key);
                        }
                    }
                }
            }
        }
    }

    public override void Compute(Node.NodeInfo node)
    {
        ProcessDirection(node, Node.NodeInfo.PinType.Input);
        for (int R = 0; R < Repeat; R++)
        {
            CheckForces(true);

            foreach (var item in Nodes)
            {
                item.Value.Process();
            }
        }
        if (DataPlane)
        {
            for (int i = DataPlane.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(DataPlane.transform.GetChild(i).gameObject);
            }
            foreach (var item in Nodes)
            {
                if (item.Value.GetLogic().GetSkin() == UICommunicator.Skin.Type.LIGHT)
                {
                    GameObject Bulb = GameObject.Instantiate(StaticInfo.Info.GetObject(item.Value.GetLogic().GetProfile().GetFloat("LightState", (0, 0), new float[] { 0 }, true)[0] > 0 ? "On" : "Off"));
                    Bulb.transform.parent = DataPlane.transform;
                    Vector3 BoardPos = item.Value.GetPlacement().Pos;
                    BoardPos.x /= GetProfile().GetFloat("SizeX", (0, 0), new float[] { UI.DefaultBoardSize.x }, true)[0] * 2;
                    BoardPos.z /= GetProfile().GetFloat("SizeY", (0, 0), new float[] { UI.DefaultBoardSize.y }, true)[0] * 2;
                    BoardPos.y = (BoardPos.y + UI.VB.x) / (UI.VB.y + UI.VB.x);

                    Bulb.transform.localPosition = BoardPos;
                }
            }
        }
        ProcessDirection(node, Node.NodeInfo.PinType.Output);
    }

    private void CheckForces(bool Run)
    {
        for (int i = Force.Count - 1; i >= 0; i--)
        {
            ForceState item = Force[i];
            if (GetNode(item.Node, out Node SetNode))
            {
                if (SetNode.ActiveConnections(item.Port, item.SubPort).Length == 0)
                {
                    if (SetNode.Info.HasPin(item.Port, item.SubPort, Node.NodeInfo.PinType.Input, out float State))
                    {
                        if (Run)
                            SetNode.ForceInput(item.State, item.Port, item.SubPort);
                    }
                    else
                    {
                        Force.Remove(item);
                    }
                }
                else
                {
                    Force.Remove(item);
                }
            }
            else
            {
                Force.Remove(item);
            }
        }
    }

    public override Logic AddNode(Node node)
    {
        string Name = node.Name.ToUpper();
        if (!GetNode(Name, out Node Null))
        {
            Nodes.Add(Name, node);
            if (GetShell() != null)
            {
                node.SetParentNode(GetShell());
            }
        }
        return this;
    }

    public override bool GetNode(string Name, out Node node)
    {
        Name = Name.ToUpper();
        if (Nodes.ContainsKey(Name))
        {
            node = Nodes[Name];
            return true;
        }
        node = null;
        return false;
    }

    public override bool DeleteNode(string Name)
    {
        if (Nodes.ContainsKey(Name))
        {
            Nodes[Name].RemoveAllConnections();
            Nodes.Remove(Name);
            CheckForces(false);
            return true;
        }
        return false;
    }

    public override LogicType GetNodes(out string[] Names)
    {
        Names = Nodes.Keys.ToArray();
        return LogicType.Cluster;
    }

    Dictionary<int, List<Node>> Grid = new Dictionary<int, List<Node>>();

    public override void AutoSort()
    {
        Grid.Clear();
        int MaxDepth = 0;
        foreach (var item in Nodes)
        {
            int Depth = GetConnectionLength(item.Value);
            if (!Grid.ContainsKey(Depth))
            {
                Grid.Add(Depth, new List<Node>());
            }
            Grid[Depth].Add(item.Value);
            MaxDepth = Mathf.Max(MaxDepth, Depth);
        }

        Vector3 StartPos = Vector3.zero;
        Vector3 NodeSize;
        float MaxSize = 0;
        for (int i = 0; i <= MaxDepth; i++)
        {
            if (Grid.TryGetValue(i, out List<Node> Value))
            {
                MaxSize = 0;
                StartPos.z = 0;
                StartPos.y = 2f;
                foreach (var node in Value)
                {
                    node.SetPosition(StartPos);
                    NodeSize = node.CalculateSize();
                    StartPos.z += NodeSize.z + 3;
                    MaxSize = Mathf.Max(MaxSize, NodeSize.x);
                }

                Vector3 CenterOffset = new Vector3(0, 0, StartPos.z / 2.0f);

                foreach (var node in Value)
                {
                    node.SetPosition(node.GetPlacement().Pos - CenterOffset);
                }

                StartPos.x -= MaxSize + 6;
            }
        }
        foreach (var item in Nodes)
        {
            item.Value.SetPosition(item.Value.GetPlacement().Pos - new Vector3(StartPos.x / 2, 0, 0));
        }
    }

    private int GetConnectionLength(Node node)
    {
        int Count = 0;
        int MaxCount = 0;
        Node.Connection[] Cons;

        int PinSize = node.Info.PinSize(Node.NodeInfo.PinType.Input);


        for (int i = 0; i < PinSize; i++)
        {
            int Subs = node.Info.PinSubSize(i, Node.NodeInfo.PinType.Input);

            for (int j = 0; j < Subs; j++)
            {
                Cons = node.GetConnectedNode(i, j);
                for (int k = 0; k < Cons.Length; k++)
                {
                    Count = GetConnectionLength(Cons[k].Connected);

                    MaxCount = Mathf.Max(Count, MaxCount);
                }
            }
        }
        return MaxCount + 1;
    }

    public override UICommunicator.Skin.Type GetSkin()
    {
        return UICommunicator.Skin.Type.IC;
    }

    public override void UpdateProperties(UI ui)
    {
        ui.AddInputItem("Alias",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = GetProfile().GetString("Alias", (0, 0), new string[] { "IC:" + Logic.RandomString() }, true)[0];
        },
        (GateItem Item, UI ui) =>
        {
            GetProfile().Add("Alias", Item.Input.text);
        });
    }

    private GameObject DataPlane = null;

    public override void HighlightSelected()
    {
        if (DataPlane)
        {
            GameObject.Destroy(DataPlane);
        }
        else
        {
            DataPlane = GameObject.Instantiate(StaticInfo.Info.GetObject("Plane"));
            DataPlane.transform.parent = GetShell().Info.Skin.transform;
            DataPlane.transform.localPosition = Vector3.up;
        }
    }

    public override ConnectionType[] GetConnectionType(string NodeName, int Port, int SubPort, Node.NodeInfo.PinType Direction)
    {
        List<ConnectionType> Types = new List<ConnectionType>();
        if (GetNode(NodeName, out Node N))
        {
            switch (Direction)
            {
                case Node.NodeInfo.PinType.Input:
                    if (N.GetConnectedNode(Port, SubPort).Length > 0)
                    {
                        Types.Add(ConnectionType.Gate);
                    }
                    break;
                case Node.NodeInfo.PinType.Output:
                    if (N.ActiveConnections(Port, SubPort).Length > 0)
                    {
                        Types.Add(ConnectionType.Gate);
                    }
                    break;
                default:
                    break;
            }
            if (Direction == Node.NodeInfo.PinType.Input)
            {
                foreach (var item in Force)
                {
                    if (item.Node.Equals(NodeName))
                    {
                        if (item.SubPort == SubPort && item.Port == Port)
                        {
                            Types.Add(ConnectionType.Force);
                            break;
                        }
                    }
                }
            }
            foreach (var item in Links)
            {
                if (item.Key == Direction)
                {
                    foreach (var Dir in item.Value)
                    {
                        foreach (var IO in Dir.Value)
                        {
                            if (IO.node.Equals(N) && IO.Port == Port && IO.SubPort == SubPort)
                            {
                                Types.Add(ConnectionType.IO);
                                break;
                            }
                        }
                        if (Types.Contains(ConnectionType.IO))
                            break;
                    }
                    if (Types.Contains(ConnectionType.IO))
                        break;
                }
            }
        }
        return Types.ToArray();
    }

    public override Logic SetConnection(string From, int FromPort, int FromSubPort, string To, int ToPort, int ToSubPort)
    {
        if (GetNode(From, out Node FromNode))
        {
            if (GetNode(To, out Node ToNode))
            {
                FromNode.SetConnection(FromPort, FromSubPort, ToNode, ToPort, ToSubPort);
            }
        }
        return this;
    }

    public override Dictionary<(int Port, int SubPort), List<Link>> GetLinks(Node.NodeInfo.PinType Direction)
    {
        if (Links.TryGetValue(Direction, out Dictionary<(int Port, int SubPort), List<Link>> Connections))
        {
            return Connections;
        }
        return null;
    }

    public override Logic SetLink(string NodeName, int Port, int SubPort, (int Port, int SubPort) EdgePort, Node.NodeInfo.PinType Direction)
    {
        if (GetNode(NodeName, out Node FromNode))
        {
            if (Links.TryGetValue(Direction, out Dictionary<(int Port, int SubPort), List<Link>> Connections))
            {
                if (Connections.TryGetValue(EdgePort, out List<Link> Nodes))
                {
                    bool Add = true;
                    for (int i = 0; i < Nodes.Count; i++)
                    {
                        if (Nodes[i].node == FromNode)
                        {
                            Add = false;
                            break;
                        }
                    }
                    if (Add)
                    {
                        Links[Direction][EdgePort].Add(new Link()
                        {
                            node = FromNode,
                            Port = Port,
                            SubPort = SubPort
                        });
                    }
                }
                else
                {
                    Links[Direction].Add(EdgePort, new List<Link>()
                    {
                        new Link()
                        {
                            node = FromNode,
                            Port = Port,
                            SubPort = SubPort
                        }
                    });
                }
            }
            else
            {
                Dictionary<(int Port, int SubPort), List<Link>> Connection = new Dictionary<(int Port, int SubPort), List<Link>>()
                {
                    { 
                        EdgePort, new List<Link>() 
                        { new Link()
                            {
                                node = FromNode,
                                Port = Port,
                                SubPort = SubPort
                            }
                        } 
                    }
                };
                Links.Add(Direction, Connection);
            }
        }
        return this;
    }

    public override Logic BreakConnection(string From, string To, int ToPort, int ToSubPort)
    {
        if (GetNode(From, out Node FromNode))
        {
            if (GetNode(To, out Node ToNode))
            {
                FromNode.RemoveConnection(ToPort, ToSubPort, To, ToPort, ToSubPort);
            }
        }
        return this;
    }

    public override Logic BreakLink(string NodeName, int Port, int SubPort, (int Port, int SubPort) EdgePort, Node.NodeInfo.PinType Direction)
    {
        if (GetNode(NodeName, out Node FromNode))
        {
            if (Links.TryGetValue(Direction, out Dictionary<(int Port, int SubPort), List<Link>> Ports))
            {
                if (Ports.TryGetValue(EdgePort, out List<Link> Nodes))
                {
                    for (int i = Nodes.Count - 1; i >= 0; i--)
                    {
                        if ((Port >= 0 ? Nodes[i].Port == Port : true) && (SubPort >= 0 ? Nodes[i].SubPort == SubPort : true) && Nodes[i].node == FromNode)
                        {
                            Links[Direction][EdgePort].Remove(Nodes[i]);
                        }
                    }
                }
                else if (EdgePort.Port == -1 && EdgePort.SubPort == -1)
                {
                    Dictionary<(int Port, int SubPort), Link> RemoveLinks = new Dictionary<(int Port, int SubPort), Link>();
                    foreach (var item in Ports)
                    {
                        foreach (var Link in item.Value)
                        {
                            if (Link.node == FromNode)
                            {
                                RemoveLinks.Add(item.Key, Link);
                            }
                        }
                    }
                    foreach (var item in RemoveLinks)
                    {
                        Links[Direction][item.Key].Remove(item.Value);
                    }
                }
            }
        }
        return this;
    }

    public override Logic SetForceState(string Node, int Port, int SubPort, float State)
    {
        if (GetNode(Node, out Node node))
        {
            bool Changed = false;
            for (int i = 0; i < Force.Count; i++)
            {
                if (Force[i].Node.Equals(Node) && Force[i].Port == Port && Force[i].SubPort == SubPort)
                {
                    Force[i].State = State;
                    Changed = true;
                }
            }
            if (!Changed)
            {
                ForceState NewState = new ForceState();
                NewState.State = State;
                NewState.Node = Node;
                NewState.Port = Port;
                NewState.SubPort = SubPort;
                Force.Add(NewState);
            }
        }
        return this;
    }

    public override Logic RemoveForceState(string Node, int Port, int SubPort)
    {
        for (int i = Force.Count - 1; i >= 0; i--)
        {
            if (Force[i].Node.Equals(Node) && Force[i].Port == Port && Force[i].SubPort == SubPort)
            {
                Force.RemoveAt(i);
            }
        }
        return this;
    }

    public override ForceState GetForceOf(string NodeName, int Port, int SubPort)
    {
        for (int i = Force.Count - 1; i >= 0; i--)
        {
            if (Force[i].Node.Equals(NodeName) && Force[i].Port == Port && Force[i].SubPort == SubPort)
            {
                return Force[i];
            }
        }
        return null;
    }

    public override ChipData Init()
    {
        foreach (var item in Nodes)
        {
            item.Value.SetParentNode(GetShell());
        }
        
        return new ChipData(Name).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.EXPANDABLE, Port = 0, PortFace = Pin.Face.BACK, Size = new Vector3Int(0, EdgeCounts.x, 1000), State = Pin.Direction.INPUT, StartUpState = Ins }).
            AddPin(new Pin() { DefaultState = 0, PinType = Pin.Type.EXPANDABLE, Port = 0, PortFace = Pin.Face.FORWARD, Size = new Vector3Int(0, EdgeCounts.y, 1000), State = Pin.Direction.OUTPUT });
    }
}
