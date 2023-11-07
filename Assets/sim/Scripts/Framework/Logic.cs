using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public abstract class Logic
{
    private Profile profile;

    public class Profile
    {
        public enum DataTypes { String, Int, Float, Bool, Vector3, Quaternion }
        private Dictionary<string, Dictionary<string, List<string>>> Data = new Dictionary<string, Dictionary<string, List<string>>>();

        public string GetJson()
        {
            return JsonConvert.SerializeObject(Data, Formatting.None);
        }

        public Profile FromJson(string Data)
        {
            if (!string.IsNullOrEmpty(Data))
                this.Data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(Data);
            return this;
        }

        public bool Has(string Key, DataTypes DataType)
        {
            string Type = DataType.ToString();
            if (!Data.ContainsKey(Type))
            {
                return false;
            }
            else
            {
                if (!Data[Type].ContainsKey(Key))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private void Store(DataTypes DataType, string Key, string Value, int Index)
        {
            string Type = DataType.ToString();
            if (!Data.ContainsKey(Type))
            {
                Data.Add(Type, new Dictionary<string, List<string>>() { { Key, new List<string>() { (Value != null ? Value : "") } } });
            }
            else
            {
                if (!Data[Type].ContainsKey(Key))
                {
                    Data[Type].Add(Key, new List<string>() { (Value != null ? Value : "") });
                }
                else
                {
                    if (Index < 0)
                    {
                        if (Value != null)
                        {
                            for (int i = 0; i < Data[Type][Key].Count; i++)
                            {
                                Data[Type][Key][i] = Value;
                            }
                        }
                    }
                    else
                    {
                        for (int i = Data[Type][Key].Count; i < Index; i++)
                        {
                            Data[Type][Key].Add("");
                        }
                        if (Value != null)
                        {
                            Data[Type][Key][Index] = Value;
                        }
                    }
                }
            }
        }

        public Profile SetSize(DataTypes Type, string Key, int Size)
        {
            Size = (int)Mathf.Max(0, Size);
            string DataType = Type.ToString();
            int DataSize = GetSize(Type, Key);

            if (DataSize < Size)
                Store(Type, Key, null, Size);
            else
            {
                int Over = Size - DataSize;
                for (int i = 0; i < Over; i++)
                {
                    Data[DataType][Key].RemoveAt(Data[DataType][Key].Count - 1);
                }
            }

            return this;
        }

        public int GetSize(DataTypes Type, string Key)
        {
            if (Retrive(Type, Key, (0, int.MaxValue), out List<string> V))
            {
                return V.Count;
            }
            return -1;
        }

        private bool Retrive(DataTypes DataType, string Key, (int Start, int End) Bounds, out List<string> Value)
        {
            string Type = DataType.ToString();
            if (!Data.ContainsKey(Type))
            {
                Value = null;
                return false;
            }
            else
            {
                if (!Data[Type].ContainsKey(Key))
                {
                    Value = null;
                    return false;
                }
                else
                {
                    if (Bounds.Start < 0)
                        Bounds.Start = 0;
                    if (Bounds.End >= Data[Type][Key].Count)
                        Bounds.End = Data[Type][Key].Count - 1;
                    List<string> Ret = new List<string>();
                    for (int i = Bounds.Start; i <= Bounds.End; i++)
                    {
                        Ret.Add(Data[Type][Key][i]);
                    }
                    Value = Ret;
                    return true;
                }
            }
        }

        public static int ConvertInt(string Value, int Default = 0)
        {
            if (int.TryParse(Value, out int Result))
            {
                return Result;
            }
            return Default;
        }

        public static float ConvertFloat(string Value, float Default = 0)
        {
            if (float.TryParse(Value, out float Result))
            {
                return Result;
            }
            return Default;
        }

        public static bool ConvertBool(string Value, bool Default = false)
        {
            if (Value.Equals("1"))
            {
                return true;
            }
            return Default;
        }

        public static string BoolConvert(bool Value)
        {
            return Value ? "1" : "0";
        }

        public static string Vector3Convert(Vector3 Value)
        {
            return Value.x + "|" + Value.y + "|" + Value.z;
        }

        public static string QuaternionConvert(Quaternion Value)
        {
            return Value.x + "|" + Value.y + "|" + Value.z + "|" + Value.w;
        }

        public static Vector3 ConvertVector3(string Value)
        {
            string[] Data = Value.Split('|');
            return new Vector3(ConvertFloat(Data[0]), ConvertFloat(Data[1]), ConvertFloat(Data[2]));
        }

        public static Quaternion ConvertQuaternion(string Value)
        {
            string[] Data = Value.Split('|');
            return new Quaternion(ConvertFloat(Data[0]), ConvertFloat(Data[1]), ConvertFloat(Data[2]), ConvertFloat(Data[3]));
        }

        public Profile Add(string Key, string Value, int Index = 0)
        {
            Store(DataTypes.String, Key, Value, Index);
            return this;
        }

        public Profile Add(string Key, int Value, int Index = 0)
        {
            Store(DataTypes.Int, Key, Value.ToString(), Index);
            return this;
        }

        public Profile Add(string Key, float Value, int Index = 0)
        {
            Store(DataTypes.Float, Key, Value.ToString(), Index);
            return this;
        }

        public Profile Add(string Key, Vector3 Value, int Index = 0)
        {
            Store(DataTypes.Vector3, Key, Vector3Convert(Value), Index);
            return this;
        }

        public Profile Add(string Key, Quaternion Value, int Index = 0)
        {
            Store(DataTypes.Quaternion, Key, QuaternionConvert(Value), Index);
            return this;
        }

        public Profile Add(string Key, bool Value, int Index = 0)
        {
            Store(DataTypes.Bool, Key, BoolConvert(Value), Index);
            return this;
        }

        public string[] GetString(string Key, (int Start, int End) Bounds, string[] Default = null, bool SetWhenNull = false)
        {
            if (Retrive(DataTypes.String, Key, Bounds, out List<string> Value))
                return Value.ToArray();
            if (Default != null && SetWhenNull)
            {
                for (int i = 0; i < Default.Length; i++)
                {
                    Add(Key, Default[i], i);
                }
            }
            return Default;
        }

        public int[] GetInt(string Key, (int Start, int End) Bounds, int[] Default = null, bool SetWhenNull = false)
        {
            if (Retrive(DataTypes.Int, Key, Bounds, out List<string> Value))
            {
                int[] Ret = new int[Value.Count];
                for (int i = 0; i < Value.Count; i++)
                {
                    Ret[i] = ConvertInt(Value[i]);
                }
                return Ret;
            }
            if (Default != null && SetWhenNull)
            {
                for (int i = 0; i < Default.Length; i++)
                {
                    Add(Key, Default[i], i);
                }
            }
            return Default;
        }

        public float[] GetFloat(string Key, (int Start, int End) Bounds, float[] Default = null, bool SetWhenNull = false)
        {
            if (Retrive(DataTypes.Float, Key, Bounds, out List<string> Value))
            {
                float[] Ret = new float[Value.Count];
                for (int i = 0; i < Value.Count; i++)
                {
                    Ret[i] = ConvertFloat(Value[i]);
                }
                return Ret;
            }
            if (Default != null && SetWhenNull)
            {
                for (int i = 0; i < Default.Length; i++)
                {
                    Add(Key, Default[i], i);
                }
            }
            return Default;
        }

        public Vector3[] GetVector3(string Key, (int Start, int End) Bounds, Vector3[] Default = null, bool SetWhenNull = false)
        {
            if (Retrive(DataTypes.Vector3, Key, Bounds, out List<string> Value))
            {
                Vector3[] Ret = new Vector3[Value.Count];
                for (int i = 0; i < Value.Count; i++)
                {
                    Ret[i] = ConvertVector3(Value[i]);
                }
                return Ret;
            }
            if (Default != null && SetWhenNull)
            {
                for (int i = 0; i < Default.Length; i++)
                {
                    Add(Key, Default[i], i);
                }
            }
            return Default;
        }

        public Quaternion[] GetQuaternion(string Key, (int Start, int End) Bounds, Quaternion[] Default = null, bool SetWhenNull = false)
        {
            if (Retrive(DataTypes.Quaternion, Key, Bounds, out List<string> Value))
            {
                Quaternion[] Ret = new Quaternion[Value.Count];
                for (int i = 0; i < Value.Count; i++)
                {
                    Ret[i] = ConvertQuaternion(Value[i]);
                }
                return Ret;
            }
            if (Default != null && SetWhenNull)
            {
                for (int i = 0; i < Default.Length; i++)
                {
                    Add(Key, Default[i], i);
                }
            }
            return Default;
        }

        public bool[] GetBool(string Key, (int Start, int End) Bounds, bool[] Default = null, bool SetWhenNull = false)
        {
            if (Retrive(DataTypes.Bool, Key, Bounds, out List<string> Value))
            {
                bool[] Ret = new bool[Value.Count];
                for (int i = 0; i < Value.Count; i++)
                {
                    Ret[i] = ConvertBool(Value[i]);
                }
                return Ret;
            }
            if (Default != null && SetWhenNull)
            {
                for (int i = 0; i < Default.Length; i++)
                {
                    Add(Key, Default[i], i);
                }
            }
            return Default;
        }
    }

    public Logic AssignProfile(Profile newProfile)
    {
        profile = newProfile;
        BuildChip(profile);
        return this;
    }

    public Profile GetProfile()
    {
        return profile;
    }

    public abstract void BuildChip(Profile ChipProfileData);

    public abstract void Compute(Node.NodeInfo node);

    public abstract ChipData Init();

    public struct ChipData
    {
        public List<Pin> Pins;
        public string Name;
        public ChipData(string Name)
        {
            this.Name = Name;
            Pins = new List<Pin>();
        }

        public ChipData AddPin(Pin pinout)
        {
            Pins.Add(pinout);
            return this;
        }
    }

    public struct Pin
    {
        public enum Direction { INPUT, OUTPUT }
        public Direction State;
        public enum Type { SINGLE, EXPANDABLE }
        public Type PinType;
        public Vector3Int Size;
        public enum Face { BACK = 0, FORWARD = 180, LEFT = 90, RIGHT = -90 }
        public Face PortFace;
        public int Port;
        public float DefaultState;
        public float[] StartUpState;
        public string PinName;
    }

    private static readonly Dictionary<int, List<char>> RndTable = new Dictionary<int, List<char>>()
    {
        {0, new List<char>()
            {
                '1', '2', '3', '4', '5', '6', '7', '8', '9', '0'
            }
        },
        {1, new List<char>()
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
            }
        },
        {2, new List<char>()
            {
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
            }
        },
    };

    public static string RandomString()
    {
        string Out = "";
        for (int i = 0; i < 10; i++)
        {
            List<char> Ops = RndTable[(UnityEngine.Random.Range(0, RndTable.Count))];
            Out += Ops[(UnityEngine.Random.Range(0, Ops.Count))];
        }
        return Out;
    }

    public abstract Logic AddNode(Node node);

    public abstract bool GetNode(string Name, out Node node);

    public abstract bool DeleteNode(string Name);

    public abstract Logic SetConnection(string From, int FromPort, int FromSubPort, string To, int ToPort, int ToSubPort);

    public class Link
    {
        public Node node;
        public int Port;
        public int SubPort;
    }

    public abstract Dictionary<(int Port, int SubPort), List<Link>> GetLinks(Node.NodeInfo.PinType Direction);

    public class ForceState
    {
        public string Node;
        public float State;
        public int Port;
        public int SubPort;
    }

    public virtual ForceState GetForceOf(string NodeName, int Port, int SubPort) { return null; }

    public abstract Logic SetLink(string NodeName, int Port, int SubPort, (int Port, int SubPort) EdgePort, Node.NodeInfo.PinType Direction);

    public abstract Logic BreakConnection(string From, string To, int ToPort, int ToSubPort);

    public abstract Logic BreakLink(string NodeName, int Port, int SubPort, (int Port, int SubPort) EdgePort, Node.NodeInfo.PinType Direction);

    public abstract Logic SetForceState(string Node, int Port, int SubPort, float State);

    public abstract Logic RemoveForceState(string Node, int Port, int SubPort);

    public enum LogicType { Cluster, Node, Custom }
    public abstract LogicType GetNodes(out string[] Names);

    public abstract UICommunicator.Skin.Type GetSkin();

    private Node Shell = null;
    public void SetNodeContainer(Node shell)
    {
        Shell = shell;
    }

    public Node GetShell()
    {
        return Shell;
    }

    public abstract void AutoSort();

    public virtual void UpdateProperties(UI ui) { }

    public virtual void HighlightSelected() { }

    public class HeaderNode
    {
        public Node.NodePlacement Place;
        public List<int> Inputs;
        public List<int> Outputs;
        public string Content;
    }

    public string GenerateHeaderData(HeaderNode Header)
    {
        return JsonConvert.SerializeObject(Header, Formatting.None, new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
    }

    public static HeaderNode GenerateHeader(string Data)
    {
        return JsonConvert.DeserializeObject<HeaderNode>(Data);
    }

    public string GenerateNodeHeaderComment(bool IncludeInput, bool IncludeGateType)
    {
        int Total = 0;
        List<int> Inputs = new List<int>();
        List<int> Outputs = new List<int>();
        for (int i = 0; i < GetShell().Info.PinSize(Node.NodeInfo.PinType.Input); i++)
        {
            Inputs.Add(GetShell().Info.PinSubSize(i, Node.NodeInfo.PinType.Input));
            Total += GetShell().Info.PinSubSize(i, Node.NodeInfo.PinType.Input);
        }
        for (int i = 0; i < GetShell().Info.PinSize(Node.NodeInfo.PinType.Output); i++)
        {
            Outputs.Add(GetShell().Info.PinSubSize(i, Node.NodeInfo.PinType.Output));
        }

        Node.NodePlacement Place = GetShell().GetPlacement();
        string Out = "\t$INFO:" + GenerateHeaderData(new HeaderNode()
        {
            Content = GetProfile().GetJson(),
            Inputs = Inputs,
            Outputs = Outputs,
            Place = Place
        }) + "\n\t" + (IncludeGateType ? GetProfile().GetString("GateType", (0, 0), new string[] { "ERROR" })[0] : "");
        if (GetProfile().Has("Input", Profile.DataTypes.Int) && IncludeInput)
        {
            Out += "*" + Total;
        }
        return Out;
    }

    public string Summation(List<string> List)
    {
        string Out = "";
        foreach (var item in List)
        {
            Out += item;
        }
        return Out;
    }

    public virtual string[] GenerateRepresentation(bool Full, out bool isSDLCompat, int Layer = 0, List<string> PreFix = null)
    {
        string[] Output = new string[5];
        for (int i = 0; i < Output.Length; i++)
        {
            Output[i] = "";
        }
        if (PreFix == null)
        {
            PreFix = new List<string>();
        }
        List<string> PathDown = new List<string>(PreFix);
        string Depth = Summation(PathDown);
        List<string> Upper = new List<string>(PreFix);
        if (Upper.Count > 0)
        {
            Upper.RemoveAt(Upper.Count - 1);
        }
        string UpperDepth = Summation(Upper);
        if (GetSkin() == UICommunicator.Skin.Type.LIGHT || GetSkin() == UICommunicator.Skin.Type.SWITCH)
            isSDLCompat = false;
        else
            isSDLCompat = true;
        string Out = GenerateNodeHeaderComment(true, true);
        
        string Name = Depth + GetProfile().GetString("Name", (0, 0), new string[] { "ERROR" })[0];

        Out += "\t\t" + Name;
        Output[0] = Out;

        if (GetProfile().Has("Alias", Profile.DataTypes.String))
        {
            Out = "\t" + Depth + GetProfile().GetString("Alias", (0, 0), new string[] { "ERROR" })[0] + "\t=\t" + Depth + GetProfile().GetString("Name", (0, 0), new string[] { "ERROR" })[0];
            Name = Depth + GetProfile().GetString("Alias", (0, 0), new string[] { "ERROR" })[0];
            Output[1] = Out;
        }
        else
        {
            Output[1] = "";
        }

        Out = "";
        for (int i = 0; i < GetShell().Info.PinSize(Node.NodeInfo.PinType.Input); i++)
        {
            for (int j = 0; j < GetShell().Info.PinSubSize(i, Node.NodeInfo.PinType.Input); j++)
            {
                foreach (var item in GetShell().GetConnectedNode(i, j))
                {
                    string GateName = item.Connected.GetLogic().GetProfile().GetString("Alias", (0, 0), item.Connected.GetLogic().GetProfile().GetString("Name", (0, 0), new string[] { "ERROR" }))[0];
                    int OutputPin = item.Connected.Info.GetAbsPin(item.OutputPin, item.OutputSubPin, Node.NodeInfo.PinType.Output);
                    OutputPin += item.Connected.Info.TotalPinSize(Node.NodeInfo.PinType.Input);
                    int InputPin = GetShell().Info.GetAbsPin(i, j, Node.NodeInfo.PinType.Input);
                    GateName = UpperDepth + GateName;
                    Out += "\t" + GateName + "#" + OutputPin + "\t-\t" + Name + "#" + InputPin + "\n";
                }
            }
        }
        Output[2] = Out;

        return Output;
    }

    public enum ConnectionType { IO, Gate, Force }

    public virtual ConnectionType[] GetConnectionType(string NodeName, int Port, int SubPort, Node.NodeInfo.PinType Direction) { return new ConnectionType[0]; }
}
