using System.Collections.Generic;
using UnityEngine;

public class Node
{
    private Pin[] Inputs;
    private Pin[] Outputs;
    private Logic Processor;
    public NodeInfo Info { get; internal set; }
    public string Name;

    private Vector3 Position = Vector3.zero;
    private Quaternion Rotation = Quaternion.identity;

    public Node(Logic Processor)
    {
        if (Processor != null)
        {
            this.Processor = Processor;
            Processor.SetNodeContainer(this);
            ReInit();
        }
        else
        {
            Name = "GATE:" + Logic.RandomString();
            Inputs = new Pin[0];
            Outputs = new Pin[0];
        }
        Info = new NodeInfo(this);
    }

    public void ReInit()
    {
        Logic.ChipData Data = Processor.Init();
        Name = Data.Name;
        List<Pin> INs = new List<Pin>();
        List<Pin> OUTs = new List<Pin>();
        for (int i = 0; i < Data.Pins.Count; i++)
        {
            List<Pin> Subject;
            if (Data.Pins[i].State == Logic.Pin.Direction.INPUT)
            {
                Subject = INs;
            }
            else
            {
                Subject = OUTs;
            }

            for (int j = Subject.Count; j <= Data.Pins[i].Port; j++)
            {
                Subject.Add(new Pin(Data.Pins[i].State == Logic.Pin.Direction.INPUT ? Logic.Pin.Direction.INPUT : Logic.Pin.Direction.OUTPUT, Data.Pins[i].PortFace, Logic.Pin.Type.SINGLE, Vector3Int.one, i, this, 0, null, ""));
            }
            Subject[Data.Pins[i].Port] = new Pin(Data.Pins[i].State, Data.Pins[i].PortFace, Data.Pins[i].PinType, Data.Pins[i].Size, Data.Pins[i].Port, this, Data.Pins[i].DefaultState, Data.Pins[i].StartUpState, Data.Pins[i].PinName);
        }
        Inputs = INs.ToArray();
        Outputs = OUTs.ToArray();
    }

    public Logic GetLogic()
    {
        return Processor;
    }

    private Node Parent = null;

    public Node GetParentNode()
    {
        return Parent;
    }

    public Node SetParentNode(Node Parent)
    {
        this.Parent = Parent;
        return this;
    }

    public Node SetPosition(Vector3 pos)
    {
        Position = pos;
        return this;
    }

    public Node SetRotation(Quaternion rot)
    {
        Rotation = rot;
        return this;
    }

    public Vector3 CalculateSize()
    {
        int[] Types = { (int)Node.NodeInfo.PinType.Output, (int)Node.NodeInfo.PinType.Input };
        int[] Faces = { (int)Logic.Pin.Face.FORWARD, (int)Logic.Pin.Face.BACK, (int)Logic.Pin.Face.LEFT, (int)Logic.Pin.Face.RIGHT };

        int Size = 0;
        int SubPins = 0;
        int[] PinNumbers = new int[4];

        for (int i = 0; i < Types.Length; i++)
        {
            Size = Info.PinSize((Node.NodeInfo.PinType)Types[i]);
            for (int j = 0; j < Size; j++)
            {
                for (int h = 0; h < Faces.Length; h++)
                {
                    if ((int)Info.GetFace(j, (Node.NodeInfo.PinType)Types[i]) == Faces[h])
                    {
                        SubPins = Info.PinSubSize(j, (Node.NodeInfo.PinType)Types[i]);
                        for (int k = 0; k < SubPins; k++)
                        {
                            PinNumbers[h]++;
                        }
                    }
                }
            }
        }

        float MaxZ = (float)Mathf.Max(2, Mathf.Max(PinNumbers[0], PinNumbers[1])) * 0.5f;
        float MaxX = (float)Mathf.Max(2, Mathf.Max(PinNumbers[2], PinNumbers[3])) * 0.5f;

        return new Vector3(MaxX, 1, MaxZ);
    }

    public struct NodePlacement
    {
        public Vector3 Pos;
        public Quaternion Rot;
    }

    public NodePlacement GetPlacement()
    {
        return new NodePlacement()
        {
            Pos = Position,
            Rot = Rotation
        };
    }

    public bool Process()
    {
        if (Processor != null && Info != null && CompletedHandshake())
        {
            ResetOut();
            Processor.Compute(Info);
            SendHandshake();
            ResetIn();
            return true;
        }
        else
        {
            ResetHandshake();
            return false;
        }
    }

    private void ResetOut()
    {
        for (int i = 0; i < Outputs.Length; i++)
        {
            Outputs[i].Set(0, -1);
        }
    }

    private void ResetIn()
    {
        for (int i = 0; i < Inputs.Length; i++)
        {
            Inputs[i].Set(0, -1);
        }
    }

    private void SendHandshake()
    {
        for (int i = 0; i < Outputs.Length; i++)
        {
            Outputs[i].SendHandshake();
        }
    }

    private bool CompletedHandshake()
    {
        for (int i = 0; i < Inputs.Length; i++)
        {
            if (!Inputs[i].HasCompletedHandShake())
                return false;
        }
        return true;
    }

    private void ResetHandshake()
    {
        for (int i = 0; i < Outputs.Length; i++)
        {
            Outputs[i].ResetHandShake();
        }
    }

    private void ResetInputHandshake(int Index, int SubIndex)
    {
        if (Index >= 0 && Index <= Inputs.Length)
        {
            Inputs[Index].ResetInputHandshake(SubIndex);
        }
    }

    private void SetInput(float State, int Index, int SubIndex)
    {
        if (Index >= 0 && Index <= Inputs.Length)
        {
            Inputs[Index].Set(State + Inputs[Index].Get(SubIndex), SubIndex);
        }
    }

    private void AcceptHandshake(int Index, int SubIndex)
    {
        if (Index >= 0 && Index <= Inputs.Length)
        {
            Inputs[Index].AcceptHandshake(SubIndex);
        }
    }

    public bool ForceInput(float State, int Index, int SubIndex)
    {
        if (GetConnectedNode(Index, SubIndex).Length == 0 && Index >= 0 && Index < Inputs.Length)
        {
            Inputs[Index].Set(State, SubIndex);
            return true;
        }
        return false;
    }

    public bool GetOutput(out float State, int Index, int SubIndex)
    {
        if (ActiveConnections(Index, SubIndex).Length == 0 && Index >= 0 && Index < Outputs.Length)
        {
            if (SubIndex >= 0 && SubIndex < Outputs[Index].MaxSubPin())
            {
                State = Outputs[Index].Get(SubIndex);
                return true;
            }
        }
        State = 0;
        return false;
    }

    public struct Connection
    {
        public Node Connected;
        public int OutputPin;
        public int OutputSubPin;
    }

    public Connection[] GetConnectedNode(int ToPin, int ToSubPin)
    {
        if (ToPin >= 0 && ToPin < Inputs.Length)
        {
            return Inputs[ToPin].GetConnection(ToSubPin);
        }
        return new Connection[0];
    }

    public bool SetConnection(int FromPin, int FromSubPin, Node To, int ToPin, int ToSubPin)
    {
        if (FromPin >= 0 && FromPin < Outputs.Length)
        {
            return Outputs[FromPin].SetConnection(FromSubPin, To, ToPin, ToSubPin);
        }
        return false;
    }

    public string[] ActiveConnections(int FromPin, int FromSubPin)
    {
        if (FromPin >= 0 && FromPin < Outputs.Length)
        {
            return Outputs[FromPin].ActiveConnections(FromSubPin);
        }
        return new string[0];
    }

    public bool RemoveConnection(int FromPin, int FromSubPin, string ToNode, int ToPin, int ToSubPin)
    {
        if (FromPin >= 0 && FromPin < Outputs.Length)
        {
            return Outputs[FromPin].RemoveConnection(FromSubPin, ToNode, ToPin, ToSubPin);
        }
        return false;
    }

    public void RemoveAllOutputConnections()
    {
        for (int i = 0; i < Outputs.Length; i++)
        {
            for (int j = 0; j < Outputs[i].MaxSubPin(); j++)
            {
                string[] nodeNames = ActiveConnections(i, j);
                for (int k = 0; k < nodeNames.Length; k++)
                {
                    Outputs[i].RemoveConnection(j, nodeNames[k], -1, -1);
                }
            }
        }
    }

    public void RemoveAllInputConnections()
    {
        for (int i = 0; i < Inputs.Length; i++)
        {
            for (int j = 0; j < Inputs[i].MaxSubPin(); j++)
            {
                Connection[] nodeConnections = GetConnectedNode(i, j);
                for (int k = 0; k < nodeConnections.Length; k++)
                {
                    nodeConnections[k].Connected.RemoveConnection(nodeConnections[k].OutputPin, nodeConnections[k].OutputSubPin, this.Name, -1, -1);
                }
            }
        }
    }

    public void RemoveAllConnections()
    {
        RemoveAllOutputConnections();
        RemoveAllInputConnections();
    }

    public KeyValuePair<string, Dictionary<int, Dictionary<int, string>>> PrintSide(NodeInfo.PinType Type)
    {
        Dictionary<int, Dictionary<int, string>> Ret = new Dictionary<int, Dictionary<int, string>>();
        Pin[] Collection = (Type == NodeInfo.PinType.Input ? Inputs : Outputs);
        for (int i = 0; i < Collection.Length; i++)
        {
            if (!Ret.ContainsKey(i))
            {
                Ret.Add(i, new Dictionary<int, string>());
            }
            for (int j = 0; j < Collection[i].MaxSubPin(); j++)
            {
                if (!Ret[i].ContainsKey(j))
                {
                    Ret[i].Add(j, "0");   
                }
                Ret[i][j] = Collection[i].Get(j).ToString();
            }
        }
        return new KeyValuePair<string, Dictionary<int, Dictionary<int, string>>>(Name, Ret);
    }

    public static string ConvertToString(KeyValuePair<string, Dictionary<int, Dictionary<int, string>>> Input)
    {
        string Out = "";
        foreach (var Pin in Input.Value)
        {
            foreach (var Sub in Pin.Value)
            {
                Out += Sub.Value + "\t";
            }
        }
        return Out;
    }


    public void SetUINode(UINode nodeSkin)
    {
        Info.Skin = nodeSkin;
    }

    public class NodeInfo
    {
        private Node node;
        public UINode Skin;

        public NodeInfo(Node node)
        {
            this.node = node;
        }

        public bool GetPin(int Index, out float State, int SubIndex = -1)
        {
            if (Index >= 0 && Index <= node.Inputs.Length)
            {
                State = node.Inputs[Index].Get(SubIndex);
                return true;
            }
            State = 0;
            return false;
        }

        public bool SetPin(int Index, float State, int SubIndex = -1)
        {
            if (Index >= 0 && Index <= node.Outputs.Length)
            {
                node.Outputs[Index].Set(State, SubIndex);
                return true;
            }
            return false;
        }

        public enum PinType { Input, Output }
        public bool HasPin(int Index, int SubIndex, PinType Type, out float State)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            if (Index >= 0 && Index < Compare.Length)
            {
                if (SubIndex >= 0 && SubIndex < Compare[Index].MaxSubPin())
                {
                    State = Compare[Index].Get(SubIndex);
                    return true;
                }
            }
            State = 0;
            return false;
        }

        public int PinSize(PinType Type)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            return Compare.Length;
        }

        public int PinSubSize(int Index, PinType Type)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            if (Index >= 0 && Index < Compare.Length)
            {
                return Compare[Index].MaxSubPin();
            }
            return 0;
        }

        public int TotalPinSize(PinType Type)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            int Ret = 0;
            for (int i = 0; i < Compare.Length; i++)
            {
                Ret += Compare[i].MaxSubPin();
            }
            return Ret;
        }

        public string PinName(int Index, PinType Type)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            if (Index >= 0 && Index < Compare.Length)
            {
                return Compare[Index].GetName();
            }
            return "";
        }

        public Logic.Pin.Face GetFace(int Index, PinType Type)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            if (Index >= 0 && Index < Compare.Length)
                return Compare[Index].GetFace();
            return Logic.Pin.Face.FORWARD;
        }

        public bool GetPinObject(int Index, PinType Type, out Pin Object)
        {
            Object = null;
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            if (Index >= 0 && Index < Compare.Length)
            {
                Object = Compare[Index];
                return true;
            }
            return false;
        }

        public int GetAbsPin(int Index, int SubIndex, PinType Type)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            if (Index >= 0 && Index < Compare.Length)
            {
                int Num = 0;
                for (int i = 0; i < Index; i++)
                {
                    Num += Compare[Index].MaxSubPin();
                }
                if (SubIndex >= 0 && SubIndex < Compare[Index].MaxSubPin())
                {
                    Num += SubIndex;
                    return Num;
                }
            }
            return -1;
        }

        public (int Port, int SubPort) GetPinAbs(int Index, PinType Type)
        {
            Pin[] Compare = (Type == PinType.Input ? node.Inputs : node.Outputs);
            (int Port, int SubPort) Ret = (0, 0);
            for (int i = 0; i < Compare.Length; i++)
            {
                if (Index - Compare[i].MaxSubPin() < 0)
                {
                    return (i, Index);
                }
                else if (Index - Compare[i].MaxSubPin() == 0)
                {
                    return (i, 0);
                }
                Index -= Compare[i].MaxSubPin();
            }
            return Ret;
        }

        public int[] TotalPins()
        {
            int[] FaceCount = new int[4];
            for (int F = 0; F < 2; F++)
            {
                Pin[] Compare = (F == 0 ? node.Inputs : node.Outputs);
                for (int i = 0; i < Compare.Length; i++)
                {
                    int Subs = Compare[i].MaxSubPin();
                    switch (Compare[i].GetFace())
                    {
                        case Logic.Pin.Face.BACK:
                            FaceCount[0] += Subs;
                            break;
                        case Logic.Pin.Face.FORWARD:
                            FaceCount[1] += Subs;
                            break;
                        case Logic.Pin.Face.LEFT:
                            FaceCount[2] += Subs;
                            break;
                        case Logic.Pin.Face.RIGHT:
                            FaceCount[3] += Subs;
                            break;
                        default:
                            break;
                    }
                }
            }
            return FaceCount;
        }
    }

    public class Pin
    {
        private Logic.Pin.Direction Direction;
        private Logic.Pin.Face Face;
        private Logic.Pin.Type Type;
        private Con[] Connections;
        private Vector3Int MinMax;
        private Node ParentNode;
        private int PortPin;
        private float Default;
        private string Name;

        public class Con
        {
            private Node ParentNode;
            private int SubPortPin;
            private int PortPin;

            public Con(Node ParentNode, int SubPortPin, int PortPin, float State)
            {
                this.ParentNode = ParentNode;
                this.SubPortPin = SubPortPin;
                this.PortPin = PortPin;
                this.CurrentState = State;
            }

            private class Wire
            {
                private Node ConnectedNode = null;
                public Node node { get { return ConnectedNode; } }
                private int PinPort;
                public int Port { get { return PinPort; } }
                private int PinSubPort;
                public int SubPort { get { return PinSubPort; } }

                public Wire(Node Conn, int port, int sub)
                {
                    ConnectedNode = Conn;
                    PinPort = port;
                    PinSubPort = sub;
                }

                public void SetInput(float Value)
                {
                    ConnectedNode.SetInput(Value, Port, SubPort);
                    ConnectedNode.AcceptHandshake(Port, SubPort);
                }

                public void Reset()
                {
                    ConnectedNode.ResetInputHandshake(Port, SubPort);
                }

                public bool Equals(Wire wire)
                {
                    return ConnectedNode == wire.ConnectedNode && Port == wire.Port && SubPort == wire.SubPort;
                }
            }

            private List<Wire> Wires = new List<Wire>();

            public string[] GetActiveConnections()
            {
                for (int i = Wires.Count - 1; i >= 0; i--)
                {
                    if (Wires[i].node == null)
                    {
                        Wires.RemoveAt(i);
                    }
                }
                string[] Ret = new string[Wires.Count];
                for (int i = 0; i < Ret.Length; i++)
                {
                    Ret[i] = Wires[i].node.Name;
                }
                return Ret;
            }

            public bool HasConnections()
            {
                bool Ret = false;
                for (int i = Wires.Count - 1; i >= 0; i--)
                {
                    if (Wires[i].node != null)
                    {
                        Ret = true;
                    }
                    else
                    {
                        Wires.RemoveAt(i);
                    }
                }
                return Ret;
            }

            public bool SetHandshake(Node NewNode, int Port, int SubPort)
            {
                if (NewNode.Info.GetPin(Port, out float State, SubPort))
                {
                    Wire newWire = new Wire(NewNode, Port, SubPort);
                    bool Add = true;
                    for (int i = Wires.Count - 1; i >= 0; i--)
                    {
                        if (Wires[i].node != null)
                        {
                            if (Wires[i].Equals(newWire))
                            {
                                Add = false;
                            }
                        }
                        else
                        {
                            Wires.RemoveAt(i);
                        }
                    }
                    if (Add)
                    {
                        Wires.Add(newWire);
                        NewNode.Inputs[Port].Connections[SubPort].Wires.Add(new Wire(ParentNode, PortPin, SubPortPin));
                        return true;
                    }
                    else
                    {
                        Debug.LogError("Handshake Failed: " + NewNode.Name + " to " + ParentNode.Name);
                    }
                }
                return false;
            }

            public bool ReleaseHandshake(string NodeName, int ToPin, int ToSubPin)
            {
                for (int i = Wires.Count - 1; i >= 0; i--)
                {
                    if (Wires[i].node.Name.Equals(NodeName))
                    {
                        Pin[] Ps = ParentNode.Outputs;
                        Pin[] Os = Wires[i].node.Inputs;
                        int Port = Wires[i].Port;
                        int SubPort = Wires[i].SubPort;

                        if (ToPin >= 0 && ToPin < Ps.Length)
                        {
                            if (ToSubPin >= 0 && ToSubPin < Ps[ToPin].Connections.Length)
                            {
                                for (int p = 0; p < Os.Length; p++)
                                {
                                    for (int s = 0; s < Os[p].Connections.Length; s++)
                                    {
                                        for (int w = Os[p].Connections[s].Wires.Count - 1; w >= 0; w--)
                                        {
                                            if (Os[p].Connections[s].Wires[w].node == ParentNode
                                                && (Port >= 0 ? p == Port : true) && (SubPort >= 0 ? s == SubPort : true))
                                            {
                                                Os[p].Connections[s].Wires.RemoveAt(w);
                                                Wires.RemoveAt(i);
                                            }
                                        }
                                    }
                                }

                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            public Node.Connection[] GetInputs()
            {
                List<Node.Connection> Cons = new List<Connection>();
                for (int i = Wires.Count - 1; i >= 0; i--)
                {
                    if (Wires[i].node != null)
                    {
                        Cons.Add(new Connection()
                        {
                            Connected = Wires[i].node,
                            OutputPin = Wires[i].Port,
                            OutputSubPin = Wires[i].SubPort
                        });
                    }
                    else
                    {
                        Wires.RemoveAt(i);
                    }
                }
                return Cons.ToArray();
            }

            private float CurrentState = 0;

            private bool Handshake = false;
            public float State
            {
                get { return CurrentState; }
                set
                {
                    CurrentState = value;
                    CurrentState = Mathf.Clamp01(CurrentState);
                }
            }

            public void SendResults()
            {
                Handshake = true;
                for (int i = Wires.Count - 1; i >= 0; i--)
                {
                    if (Wires[i].node != null)
                    {
                        Wires[i].SetInput(CurrentState);
                    }
                    else
                    {
                        Wires.RemoveAt(i);
                    }
                }
            }

            public bool HasCompletedHandshake()
            {
                if (!HasConnections())
                {
                    return true;
                }
                return Handshake;
            }

            public void ResetHandshake()
            {
                Handshake = false;

                for (int i = Wires.Count - 1; i >= 0; i--)
                {
                    if (Wires[i].node != null)
                    {
                        Wires[i].Reset();
                    }
                    else
                    {
                        Wires.RemoveAt(i);
                    }
                }
            }

            public void ResetHandshakeInternal()
            {
                Handshake = false;
            }

            public void AcceptHandshake()
            {
                Handshake = true;
            }
        }

        public Pin(Logic.Pin.Direction direction, Logic.Pin.Face face, Logic.Pin.Type type, Vector3Int Size, int Port, Node node, float DefaultState, float[] StartUp, string Name)
        {
            Direction = direction;
            Face = face;
            Type = type;
            if (!string.IsNullOrEmpty(Name))
                this.Name = Name;
            else
                this.Name = "";
            
            int Min = (int)Mathf.Max(1, Size.x);
            int Max = (int)Mathf.Max(Size.x, Size.z + 1);
            int Current = (int)Mathf.Clamp(Size.y, Min, Max);
            MinMax = new Vector3Int(Min, Current, Max);
            ParentNode = node;
            PortPin = Port;
            Connections = new Con[Current];
            Default = DefaultState;

            for (int i = 0; i < Connections.Length; i++)
            {
                Connections[i] = new Con(node, i, Port, (StartUp == null ? Default : (i < StartUp.Length ? StartUp[i] : Default)));
            }
        }

        public string GetName() { return Name; }

        public Logic.Pin.Direction PinType() { return Direction; }

        public bool IsOutput() { return PinType() != Logic.Pin.Direction.INPUT; }
        public bool IsInput() { return PinType() == Logic.Pin.Direction.INPUT; }

        public Logic.Pin.Face GetFace() { return Face; }

        public int MaxSubPin() { return Connections.Length; }

        public bool IsExpandable() { return Type == Logic.Pin.Type.EXPANDABLE; }

        public bool SetConnection(int FromSubPin, Node To, int ToPin, int ToSubPin)
        {
            if (FromSubPin >= 0 && FromSubPin < Connections.Length)
            {
                return Connections[FromSubPin].SetHandshake(To, ToPin, ToSubPin);
            }
            return Connections[0].SetHandshake(To, ToPin, ToSubPin);
        }

        public bool RemoveConnection(int FromSubPin, string NodeName, int ToPin, int ToSubPin)
        {
            if (FromSubPin >= 0 && FromSubPin < Connections.Length)
            {
                return Connections[FromSubPin].ReleaseHandshake(NodeName, ToPin, ToSubPin);
            }
            return Connections[0].ReleaseHandshake(NodeName, ToPin, ToSubPin);
        }

        public string[] ActiveConnections(int FromSubPin)
        {
            if (FromSubPin >= 0 && FromSubPin < Connections.Length)
            {
                return Connections[FromSubPin].GetActiveConnections();
            }
            return Connections[0].GetActiveConnections();
        }

        public Node.Connection[] GetConnection(int FromSubPin)
        {
            if (FromSubPin >= 0 && FromSubPin < Connections.Length)
            {
                return Connections[FromSubPin].GetInputs();
            }
            return Connections[0].GetInputs();
        }

        public bool HasCompletedHandShake()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                if (!Connections[i].HasCompletedHandshake())
                    return false;
            }
            return true;
        }

        public void ResetHandShake()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                Connections[i].ResetHandshake();
            }
        }

        public void ResetInputHandshake(int SubPort)
        {
            if (SubPort >= 0 && SubPort < Connections.Length)
            {
                Connections[SubPort].ResetHandshakeInternal();
            }
            else
                Connections[0].ResetHandshakeInternal();
        }

        public void AcceptHandshake(int SubPort)
        {
            if (SubPort >= 0 && SubPort < Connections.Length)
            {
                Connections[SubPort].AcceptHandshake();
            }
            else
                Connections[0].AcceptHandshake();
        }

        public void SendHandshake()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                Connections[i].SendResults();
            }
        }

        public bool SetSubPinSize(int NewSize)
        {
            if (Type == Logic.Pin.Type.SINGLE)
                return false;
            NewSize = Mathf.Clamp(NewSize, MinMax.x, MinMax.z);
            bool Able = true;
            for (int i = NewSize; i < Connections.Length; i++)
            {
                if (Connections[i].HasConnections())
                {
                    Able = false;
                    break;
                }
            }
            if (Able)
            {
                Con[] OldNodes = new Con[Connections.Length];
                for (int i = 0; i < Connections.Length; i++)
                {
                    OldNodes[i] = Connections[i];
                }
                Connections = new Con[NewSize];
                for (int i = 0; i < Mathf.Min(Connections.Length, OldNodes.Length); i++)
                {
                    Connections[i] = OldNodes[i];
                }
                for (int i = OldNodes.Length; i < Connections.Length; i++)
                {
                    Connections[i] = new Con(ParentNode, i, PortPin, Default);
                }
                return true;
            }
            return false;
        }

        public bool IncrementSize(int Increment)
        {
            return SetSubPinSize(MaxSubPin() + Increment);
        }

        public bool DecrementSize(int Decrement)
        {
            return SetSubPinSize(MaxSubPin() - Decrement);
        }

        public float Get(int SubPin = -1)
        {
            if (SubPin >= 0 && SubPin < Connections.Length)
                return Connections[SubPin].State;
            return Connections[0].State;
        }

        public void Set(float State, int SubPin = -1)
        {
            if (SubPin < 0)
            {
                for (int i = 0; i < Connections.Length; i++)
                {
                    Connections[i].State = State;
                }
            }
            else
            {
                if (SubPin >= 0 && SubPin < Connections.Length)
                {
                    Connections[SubPin].State = State;
                }
            }
        }
    }
}
