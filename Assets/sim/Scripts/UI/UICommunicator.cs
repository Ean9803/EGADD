using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MasterControl;

public class UICommunicator : MonoBehaviour
{
    public GameObject Input;
    public GameObject Output;
    public GameObject Wire;

    public List<Skin> Skins;

    public Dictionary<Vector3, List<UINode>> ActiveComponents = new Dictionary<Vector3, List<UINode>>();
    public Dictionary<Vector3, bool> ActiveWires = new Dictionary<Vector3, bool>();
    public List<UINode> ActiveGates = new List<UINode>();

    public float GridSize = 0.25f;
    public MasterControl Master;
    //

    [System.Serializable]
    public class Skin
    {
        public enum Type { AND, OR, NOT, IC, XOR, INPUT, OUTPUT, LIGHT, SWITCH }
        public Type SkinType;
        public GameObject SkinObject;
    }

    public Vector3 ClampInBounds(Vector3 Position)
    {
        float X = Position.x;
        if (Mathf.Abs(Position.x) > Mathf.Abs(UI.HB.x))
        {
            X = Mathf.Sign(Position.x) * Mathf.Abs(UI.HB.x);
        }
        float Z = Position.z;
        if (Mathf.Abs(Position.z) > Mathf.Abs(UI.HB.y))
        {
            Z = Mathf.Sign(Position.z) * Mathf.Abs(UI.HB.y);
        }
        float Y = Position.y;
        if (Position.y < UI.VB.x)
        {
            Y = UI.VB.x;
        }
        else if (Position.y > UI.VB.y)
        {
            Y = UI.VB.y;
        }

        return new Vector3(X, Y, Z);
    }

    public UINode SpawnItem(Node node, MasterControl Master)
    {
        Skin.Type S = node.GetLogic().GetSkin();
        for (int i = 0; i < Skins.Count; i++)
        {
            if (Skins[i].SkinType == S && Skins[i].SkinObject != null)
            {
                Node.NodePlacement Place = node.GetPlacement();
                GameObject NodeSpawn = Instantiate(Skins[i].SkinObject);

                UINode UI = NodeSpawn.GetComponent<UINode>();
                if (UI == null)
                    UI = NodeSpawn.AddComponent<UINode>();

                UI.SetNode(node, Input, Output, this, Master, node.Info.TotalPins());

                UI.AssignID("GATE:" + S.ToString() + node.Name);
                Master.AddToCollection(UI);

                NodeSpawn.transform.position = Place.Pos;
                NodeSpawn.transform.rotation = Place.Rot;

                Vector3 Point = GridSnap(Place.Pos);
                if (!ActiveComponents.ContainsKey(Point))
                    ActiveComponents.Add(Point, new List<UINode>());
                UI.UpdateChunks();
                ActiveGates.Add(UI);
                return UI;
            }
        }
        return null;
    }

    private Dictionary<int, Dictionary<int, List<GameObject>>> IO = new Dictionary<int, Dictionary<int, List<GameObject>>>();
    private Dictionary<int, int> FacePins = new Dictionary<int, int>();

    public void AddIO(Node.NodeInfo.PinType Type, int Port, Node Info, int[] Total, MasterControl Master, PinCollection StartState)
    {
        Logic.Pin.Face F = Info.Info.GetFace(Port, Type);
        int Face = (int)F;
        int total = 0;
        switch (F)
        {
            case Logic.Pin.Face.BACK:
                total = Total[0];
                break;
            case Logic.Pin.Face.FORWARD:
                total = Total[1];
                break;
            case Logic.Pin.Face.LEFT:
                total = Total[2];
                break;
            case Logic.Pin.Face.RIGHT:
                total = Total[3];
                break;
            default:
                break;
        }
        if (!FacePins.ContainsKey(Face))
            FacePins.Add(Face, 0);
        if (!IO.ContainsKey((int)Type))
            IO.Add((int)Type, new Dictionary<int, List<GameObject>>());
        if (!IO[(int)Type].ContainsKey(Port))
        {
            List<GameObject> SubPins = new List<GameObject>();
            for (int i = 0; i < Info.Info.PinSubSize(Port, Type); i++)
            {
                for (int s = 0; s < Skins.Count; s++)
                {
                    if (Skins[s].SkinType == (Type == Node.NodeInfo.PinType.Input ? Skin.Type.INPUT : Skin.Type.OUTPUT) && Skins[s].SkinObject != null)
                    {
                        GameObject NodeSpawn = Instantiate(Skins[s].SkinObject);
                        
                        float Card = (F == Logic.Pin.Face.BACK || F == Logic.Pin.Face.LEFT ? 1 : -1);
                        Card *= Mathf.Abs((F == Logic.Pin.Face.LEFT || F == Logic.Pin.Face.RIGHT ? UI.HB.y : UI.HB.x));

                        float Offset = Mathf.Abs((F == Logic.Pin.Face.LEFT || F == Logic.Pin.Face.RIGHT ? UI.HB.x : UI.HB.y));
                        Offset /= (float)total;
                        //Pin.transform.localPosition = new Vector3(0, 0, PinNumbers[h] - ((total - 1) * (PinSpace * 0.5f)));
                        float StartPoint = (Offset + 0.5f) * -((float)(total - 1) / 2);
                        StartPoint += (FacePins[Face] * (Offset + 0.5f));

                        Vector3 Pos = new Vector3((F == Logic.Pin.Face.FORWARD || F == Logic.Pin.Face.BACK ? Card : StartPoint), UI.VB.x, (F == Logic.Pin.Face.LEFT || F == Logic.Pin.Face.RIGHT ? Card : StartPoint));
                        NodeSpawn.transform.position = Pos;

                        UI_IO IOPin = NodeSpawn.AddComponent<UI_IO>();
                        IOPin.AssignID("IO:" + Type.ToString() + F.ToString() + Port + i);
                        Master.AddToCollection(IOPin);
                        if (StartState != null)
                        {
                            if (i < StartState.Values.Count)
                            {
                                IOPin.SetIO(StartState.Values[i] ? 1 : 0);
                            }
                        }

                        IOPin.Mode = (Type == Node.NodeInfo.PinType.Input ? UI_IO.Type.Input : UI_IO.Type.Output);
                        IOPin.SetPin(new Vector2Int(Port, i), Info);

                        SubPins.Add(NodeSpawn);
                        FacePins[Face]++;
                    }
                }
            }
            IO[(int)Type].Add(Port, SubPins);
        }
    }

    private UI_IO GetIOPin(int Port, int SubPort, Node.NodeInfo.PinType Type)
    {
        if (IO[(int)Type].ContainsKey(Port))
        {
            if (SubPort >= 0 && SubPort < IO[(int)Type][Port].Count)
            {
                return IO[(int)Type][Port][SubPort].GetComponent<UI_IO>();
            }
        }
        return null;
    }

    public int[] IOSize(Node.NodeInfo.PinType Type)
    {
        return IO[(int)Type].Keys.ToArray();
    }

    public int IOSize(int Port, Node.NodeInfo.PinType Type)
    {
        return IO[(int)Type][Port].Count;
    }

    public bool SetIO(int Port, int SubPort, float State)
    {
        UI_IO IU = GetIOPin(Port, SubPort, Node.NodeInfo.PinType.Input);
        if (IU != null)
        {
            IU.SetIO(State);
            return true;
        }
        return false;
    }

    public bool GetIO(int Port, int SubPort, out float State)
    {
        UI_IO IU = GetIOPin(Port, SubPort, Node.NodeInfo.PinType.Output);
        if (IU != null)
        {
            State = IU.GetIO();
            return true;
        }
        State = 0;
        return false;
    }

    public void ConnectIO(MasterControl MasterNode)
    {
        Dictionary<(int Port, int SubPort), List<Logic.Link>> Ls;
        GameObject PinWire;
        GameObject PinOBJ;
        UIWire wire;
        Vector3 WireOffset = new Vector3(1.5f, 0, 0);
        for (int i = (int)Node.NodeInfo.PinType.Input; i <= (int)Node.NodeInfo.PinType.Output; i++)
        {
            Ls = MasterNode.GetMasterNode().GetLogic().GetLinks((Node.NodeInfo.PinType)i);
            if (Ls != null)
            {
                foreach (var item in Ls)
                {
                    foreach (var node in item.Value)
                    {
                        PinOBJ = node.node.Info.Skin.GetPin(node.Port, node.SubPort, (Node.NodeInfo.PinType)i);
                        //if (PinOBJ)
                        {
                            PinWire = Instantiate(Wire, PinOBJ.transform);
                            PinWire.transform.localPosition += WireOffset;

                            wire = PinWire.GetComponent<UIWire>();
                            wire.AssignID("IO:" + node.node.Name + "TO" + MasterNode.GetMasterNode().Name + node.Port + node.SubPort + item.Key.Port + item.Key.SubPort + ((Node.NodeInfo.PinType)i).ToString());
                            MasterNode.AddToCollection(wire);
                            wire.TargetPoint.transform.localRotation = Quaternion.identity;

                            wire.Board = this;

                            wire.TargetPoint.transform.parent = IO[i][item.Key.Port][item.Key.SubPort].transform;
                            wire.TargetPoint.transform.localPosition = Vector3.zero;

                            wire.AssignLink(node.node.Name, node.Port, node.SubPort, item.Key.Port, item.Key.SubPort, (Node.NodeInfo.PinType)i);
                        }
                    }
                }
            }
        }
    }

    public void DrawConnections(MasterControl Master)
    {
        int PinSize;
        int SubPinSize;
        Node Connected;
        GameObject PinOBJ;
        GameObject PinFromOBJ;
        GameObject PinWire;
        UIWire wire;
        Vector3 WireOffset = new Vector3(1.5f, 0, 0);
        foreach (var item in ActiveGates)
        {
            PinSize = item.GetNode().Info.PinSize(Node.NodeInfo.PinType.Input);
            for (int i = 0; i < PinSize; i++)
            {
                SubPinSize = item.GetNode().Info.PinSubSize(i, Node.NodeInfo.PinType.Input);
                for (int j = 0; j < SubPinSize; j++)
                {
                    PinOBJ = item.GetPin(i, j, Node.NodeInfo.PinType.Input);
                    if (PinOBJ != null)
                    {
                        Node.Connection[] Cons = item.GetNode().GetConnectedNode(i, j);
                        for (int k = 0; k < Cons.Length; k++)
                        {
                            Connected = Cons[k].Connected;
                            foreach (var SubjectNode in ActiveGates)
                            {
                                if (SubjectNode.GetNode() == Connected)
                                {
                                    PinFromOBJ = SubjectNode.GetPin(Cons[k].OutputPin, Cons[k].OutputSubPin, Node.NodeInfo.PinType.Output);
                                    if (PinFromOBJ != null)
                                    {
                                        PinWire = Instantiate(Wire, PinOBJ.transform);
                                        PinWire.transform.localPosition += WireOffset;

                                        wire = PinWire.GetComponent<UIWire>();
                                        wire.AssignID("WIRE:" + item.GetNode().Name + "TO" + SubjectNode.GetNode().Name + i + j + Cons[k].OutputPin + Cons[k].OutputSubPin);
                                        Master.AddToCollection(wire);
                                        wire.TargetPoint.transform.localRotation = Quaternion.identity;

                                        wire.TargetPoint.transform.LookAt(PinFromOBJ.transform);
                                        wire.TargetPoint.transform.Rotate(new Vector3(90, 0, 0));

                                        wire.Board = this;

                                        wire.TargetPoint.transform.parent = PinFromOBJ.transform;
                                        wire.TargetPoint.transform.localPosition = WireOffset;

                                        wire.AssignNode(Cons[k].Connected.Name, item.GetNode().Name, Cons[k].OutputPin, Cons[k].OutputSubPin);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public struct WaterFlowNode
    {
        public bool Obstructed;
        public Vector3 Position;
        public List<Vector3> Ring;
        public List<Vector3> SurfaceRing;
    }

    public WaterFlowNode[] GenerateClearPath(int Size, Vector3 StartPos, Vector3 EndPos)
    {
        WaterFlowNode[] Path = new WaterFlowNode[Size];
        Path[0].Position = StartPos;
        Path[Size - 1].Position = EndPos;

        Vector3 ChunkPos;

        Vector3 Direction = EndPos - StartPos;
        float Dist = Direction.magnitude / (Size - 1);
        Direction = Direction.normalized;
        List<UINode> Checked = new List<UINode>();

        int Points = 10;
        Vector3 RingPoint;
        Vector3 TestPoint;
        Vector3 TestPointDir;
        Vector3 MovePos;
        float SurfaceDist;
        float SwimDist;

        for (int i = 1; i < Size - 1; i++)
        {
            Direction = (EndPos - Path[i - 1].Position).normalized;
            Path[i].Position = Path[i - 1].Position + (Direction * Dist);

            ChunkPos = GridSnap(Path[i].Position);
            Checked.Clear();
            if (ActiveComponents.ContainsKey(ChunkPos))
            {
                float MinDist = float.MaxValue;
                foreach (var item in ActiveComponents[ChunkPos])
                {
                    if (!Checked.Contains(item))
                    {
                        Checked.Add(item);
                        MinDist = Mathf.Min(MinDist, item.CubeDistance(Path[i].Position));
                    }
                }
                if (MinDist != float.MaxValue)
                {
                    Path[i].Position = Path[i - 1].Position + (Direction * Dist);
                    if (MinDist > 0)
                    {
                        Path[i].Obstructed = false;
                    }
                    else
                    {
                        Path[i].Obstructed = true;
                        Path[i].Ring = new List<Vector3>();
                        Path[i].SurfaceRing = new List<Vector3>();
                        RingPoint = (Vector3.Cross(Direction, Direction == Vector3.up ? Vector3.right : Vector3.up).normalized * 0.2f) + Path[i].Position;
                        SurfaceDist = float.MinValue;

                        MovePos = Path[i].Position;

                        for (int r = 0; r < Points; r++)
                        {
                            TestPoint = RoatatePoint(RingPoint, Path[i].Position, Direction, r * (360.0f / Points));
                            Path[i].Ring.Add(TestPoint);
                            TestPointDir = (TestPoint - Path[i].Position).normalized;
                            SwimDist = SwimUp(TestPoint, TestPointDir);
                            if (SwimDist > SurfaceDist)
                            {
                                SurfaceDist = SwimDist;
                                MovePos = TestPoint + (TestPointDir * Mathf.Abs(SwimDist));
                            }
                            Path[i].SurfaceRing.Add(TestPoint + (TestPointDir * Mathf.Abs(SwimDist)));
                        }

                        Path[i].Position = MovePos;
                    }
                }
            }
        }

        return Path;
    }

    private float SwimUp(Vector3 Point, Vector3 SwimDirection)
    {
        Vector3 ChunkPos = GridSnap(Point);
        List<UINode> Checked = new List<UINode>();
        float MinDist = 0;
        if (ActiveComponents.ContainsKey(ChunkPos))
        {
            foreach (var item in ActiveComponents[ChunkPos])
            {
                if (!Checked.Contains(item))
                {
                    Checked.Add(item);
                    MinDist = Mathf.Min(MinDist, item.CubeDistance(Point));
                }
            }
            if (MinDist < 0 && (Mathf.Abs(MinDist) - 0.01f > 0))
            {
                MinDist += SwimUp(Point + (SwimDirection * Mathf.Abs(MinDist)), SwimDirection);
            }
        }
        return MinDist;
    }

    private Vector3 FindUnblocked(Vector3 Point)
    {
        Vector3 Chunk = GridSnap(Point);
        List<Vector3> Can = new List<Vector3>();
        Vector3[] Shell;
        float Dist = float.MaxValue;
        Vector3 Ret = Point;
        if (ActiveWires.ContainsKey(Chunk))
        {
            if (!ActiveWires[Chunk])
            {
                return Chunk;
            }
            else
            {
                for (int i = 1; i < 10; i++)
                {
                    Shell = GetShell(Chunk, i);
                    Can.Clear();
                    for (int S = 0; S < Shell.Length; S++)
                    {
                        if (!Blocked(Shell[S]) && !Underwater(Shell[S]))
                        {
                            if (!Can.Contains(Shell[S]))
                            {
                                Can.Add(Shell[S]);
                                Ret = Shell[S];
                            }
                        }
                    }

                    for (int C = 0; C < Can.Count; C++)
                    {
                        if ((Point - Can[C]).sqrMagnitude < Dist)
                        {
                            Dist = (Point - Can[C]).sqrMagnitude;
                            Ret = Can[C];
                        }
                        if (C == Can.Count - 1)
                        {
                            return Ret;
                        }
                    }
                }
            }
        }
        return Chunk;
    }

    private Vector3 RoatatePoint(Vector3 Point, Vector3 Center, Vector3 Axis, float Angle)
    {
        return Quaternion.AngleAxis(Angle, Axis) * (Point - Center) + Center;
    }

    public class BrickSegment
    {
        public Vector3 StartPoint;
        public Vector3 EndPoint;
    }

    private Vector3[] GetShell(Vector3 Center, int Layer)
    {
        List<Vector3> Surface = new List<Vector3>();

        Vector3[] Faces = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        Vector3 Core = GridSnap(Center);
        Vector3 CenterFace;
        Vector3 SurfaceChunk;
        for (int i = 0; i < Faces.Length; i++)
        {
            CenterFace = GridSnap(Faces[i] * ((GridSize * 2) * Layer));
            SurfaceChunk = CenterFace;
            for (int x = -Layer; x <= Layer; x++)
            {
                for (int y = -Layer; y <= Layer; y++)
                {
                    int[] Offset = { x, y };
                    int Val = 0;
                    for (int a = 0; a < 3; a++)
                    {
                        if (Faces[i][a] == 0)
                        {
                            SurfaceChunk[a] = Offset[Val++] * (GridSize * 2);
                        }
                    }
                    
                    Surface.Add(GridSnap(SurfaceChunk) + Core);
                }
            }
        }

        return Surface.ToArray();
    }


    private Vector3 GetChunk(Vector3 Point, Vector3 Direction)
    {
        return GridSnap(Direction.normalized * (GridSize * 2)) + GridSnap(Point);
    }

    private bool Underwater(Vector3 Point)
    {
        List<UINode> Checked = new List<UINode>();
        Vector3 ChunkPos = GridSnap(Point);
        if (ActiveComponents.ContainsKey(ChunkPos))
        {
            float MinDist = float.MaxValue;
            foreach (var item in ActiveComponents[ChunkPos])
            {
                if (!Checked.Contains(item))
                {
                    Checked.Add(item);
                    MinDist = Mathf.Min(MinDist, item.CubeDistance(Point));
                }
            }
            if (MinDist != float.MaxValue)
            {
                if (MinDist > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool Blocked(Vector3 Point)
    {
        Vector3 ChunkPos = GridSnap(Point);
        if (ActiveWires.ContainsKey(ChunkPos))
            return ActiveWires[ChunkPos];
        return false;
    }

    public Vector3[] GeneratePointsOnSphere(int n)
    {
        List<Vector3> upts = new List<Vector3>();
        float inc = Mathf.PI * (3 - Mathf.Sqrt(5));
        float off = 2.0f / n;
        float x = 0;
        float y = 0;
        float z = 0;
        float r = 0;
        float phi = 0;

        for (var k = 0; k < n; k++)
        {
            y = k * off - 1 + (off / 2);
            r = Mathf.Sqrt(1 - y * y);
            phi = k * inc;
            x = Mathf.Cos(phi) * r;
            z = Mathf.Sin(phi) * r;

            upts.Add(new Vector3(x, y, z));
        }
        Vector3[] pts = upts.ToArray();
        return pts;
    }

    private Vector3 EscapeSwim(Vector3 Point)
    {
        Vector3 MovePos = Point;
        Vector3 TestPoint;
        Vector3 TestPointDir;
        float SwimDist;
        float SurfaceDist = float.MinValue;
        Vector3[] Points = GeneratePointsOnSphere(10);
        for (int r = 0; r < Points.Length; r++)
        {
            TestPoint = Points[r] + Point;
            TestPointDir = (TestPoint - Point).normalized;
            SwimDist = SwimUp(TestPoint, TestPointDir);
            if (SwimDist > SurfaceDist)
            {
                SurfaceDist = SwimDist;
                MovePos = TestPoint + (TestPointDir * Mathf.Abs(SwimDist));
            }
        }

        return MovePos;
    }

    public class LineProfile
    {
        public List<Vector3> LinePoints = new List<Vector3>();
        public List<Vector3> Directions = new List<Vector3>();
        public List<Vector3> Chunks = new List<Vector3>();
        public Vector3 StartPoint;
        public Vector3 EndPoint;
        public bool Updated;
    }

    public LineProfile GenerateCircitPath(LineProfile PastLine, Vector3 StartPoint, Vector3 EndPoint)
    {
        if (PastLine == null)
        {
            PastLine = new LineProfile();
            PastLine.Updated = true;
        }

        foreach (var item in PastLine.Chunks)
        {
            if (ActiveWires.ContainsKey(item))
                ActiveWires[item] = false;
        }

        (List<Vector3> PathOut, List<Vector3> ChunksOccupy) = GenerateBrickPath(StartPoint, EndPoint);
        if (PastLine.LinePoints.Count != PathOut.Count)
        {
            PastLine.Updated = true;
            PastLine.LinePoints = PathOut;
        }
        else
        {
            for (int i = 0; i < PathOut.Count; i++)
            {
                if (PastLine.LinePoints[i] != PathOut[i])
                {
                    PastLine.Updated = true;
                }
                PastLine.LinePoints[i] = PathOut[i];
            }
        }

        PastLine.Directions.Clear();
        for (int i = 1; i < PastLine.LinePoints.Count - 1; i++)
        {
            PastLine.Directions.Add((PastLine.LinePoints[i - 1] - PastLine.LinePoints[i + 1]).normalized);
        }
        PastLine.Directions.Insert(0, (PastLine.LinePoints[0] - PastLine.LinePoints[1]).normalized);
        PastLine.Directions.Add((PastLine.LinePoints[PastLine.LinePoints.Count - 2] - PastLine.LinePoints[PastLine.LinePoints.Count - 1]).normalized);

        PastLine.Chunks = ChunksOccupy;

        PastLine.StartPoint = StartPoint;
        PastLine.EndPoint = EndPoint;

        return PastLine;
    }

    public (List<Vector3> PathOut, List<Vector3> ChunksOccupy) GenerateBrickPath(Vector3 StartPoint, Vector3 EndPoint)
    {
        List<BrickSegment> Ret = new List<BrickSegment>();
        Vector3 StartChunk = GridSnap(StartPoint);
        Vector3 EndChunk = GridSnap(EndPoint);

        while (Underwater(EndChunk))
        {
            EndChunk = GridSnap(EscapeSwim(EndChunk));
        }
        EndChunk = FindUnblocked(EndChunk);

        while (Underwater(StartChunk))
        {
            StartChunk = GridSnap(EscapeSwim(StartChunk));
        }
        StartChunk = FindUnblocked(StartChunk);

        Vector3 CurrentChunk = StartChunk;
        Vector3 PossiblePick = CurrentChunk;

        float Dist;
        float ClosestDist;
        Vector3 Pick = CurrentChunk;
        Vector3 ClosestPick = CurrentChunk;
        Vector3 PickDirection = Vector3.zero;
        Vector3 ClosestPickDirection = Vector3.zero;
        Vector3 LastDirection = Vector3.zero;

        List<Vector3> ChunksTaken = new List<Vector3>();

        Vector3[] Picks = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        for (int l = 0; l < 100 && CurrentChunk != EndChunk; l++)
        {
            ChunksTaken.Add(CurrentChunk);
            if (!ActiveWires.ContainsKey(CurrentChunk))
                ActiveWires.Add(CurrentChunk, true);
            else
                ActiveWires[CurrentChunk] = true;

            Dist = float.MaxValue;
            ClosestDist = float.MaxValue;
            Pick = CurrentChunk;
            for (int i = 0; i < Picks.Length; i++)
            {
                PossiblePick = GetChunk(CurrentChunk, Picks[i]);
                if (Blocked(PossiblePick))
                    continue;

                if ((PossiblePick - EndChunk).sqrMagnitude < Dist)
                {
                    if (!Underwater(PossiblePick) || PossiblePick == EndChunk)
                    {
                        Dist = (PossiblePick - EndChunk).sqrMagnitude;
                        Pick = PossiblePick;
                        PickDirection = Picks[i];
                    }
                }

                if ((ClosestPick - EndChunk).sqrMagnitude < ClosestDist)
                {
                    ClosestDist = (PossiblePick - EndChunk).sqrMagnitude;
                    ClosestPick = PossiblePick;
                    ClosestPickDirection = Picks[i];
                }

            }
            if (Pick != CurrentChunk)
            {
                if (LastDirection != PickDirection)
                {
                    LastDirection = PickDirection;
                    Ret.Add(new BrickSegment() { StartPoint = CurrentChunk, EndPoint = Pick });
                }
                else
                {
                    Ret[Ret.Count - 1].EndPoint = Pick;
                }
                CurrentChunk = Pick;
            }
            else
            {
                Vector3 RingPoint = (Vector3.Cross(ClosestPickDirection, ClosestPickDirection == Vector3.up ? Vector3.right : Vector3.up).normalized * 0.2f) + ClosestPick;
                float SurfaceDist = float.MinValue;

                Vector3 MovePos = ClosestPick;
                int Points = 10;

                for (int r = 0; r < Points; r++)
                {
                    Vector3 TestPoint = RoatatePoint(RingPoint, ClosestPick, ClosestPickDirection, r * (360.0f / Points));
                    Vector3 TestPointDir = (TestPoint - ClosestPick).normalized;
                    float SwimDist = SwimUp(TestPoint, TestPointDir);
                    if (SwimDist > SurfaceDist)
                    {
                        SurfaceDist = SwimDist;
                        MovePos = TestPoint + (TestPointDir * Mathf.Abs(SwimDist));
                    }
                }
                LastDirection = Vector3.zero;
                CurrentChunk = MovePos;
            }
        }

        Ret.Add(new BrickSegment() { StartPoint = CurrentChunk, EndPoint = EndPoint });
        Ret.Insert(0, new BrickSegment() { StartPoint = StartPoint, EndPoint = StartChunk });

        float Dist1;
        float Dist2;

        float PercentageMin;
        float PercentageMax;

        float Taken;

        List<Vector3> PointsOut = new List<Vector3>();
        PointsOut.Add(StartPoint);

        for (int i = 1; i < Ret.Count; i++)
        {
            Dist1 = (Ret[i - 1].StartPoint - Ret[i - 1].EndPoint).sqrMagnitude;
            Dist2 = (Ret[i].StartPoint - Ret[i].EndPoint).sqrMagnitude;
            
            PercentageMin = Mathf.Min(Dist1, Dist2);
            PercentageMax = Mathf.Max(Dist1, Dist2);
            
            Taken = 0;

            if (PercentageMin == 0)
            {
                Taken = PercentageMax;
            }
            else
            {
                Taken = PercentageMin;
            }

            Taken = Mathf.Sqrt(Taken) / 2;
            Ret[i - 1].EndPoint += (Ret[i - 1].StartPoint - Ret[i - 1].EndPoint).normalized * Taken;
            Ret[i].StartPoint += (Ret[i].EndPoint - Ret[i].StartPoint).normalized * Taken;

            if ((PointsOut[PointsOut.Count - 1] - Ret[i - 1].StartPoint).magnitude > 0)
            {
                PointsOut.Add(Ret[i - 1].StartPoint);
            }
            if ((PointsOut[PointsOut.Count - 1] - Ret[i - 1].EndPoint).magnitude > 0)
            {
                PointsOut.Add(Ret[i - 1].EndPoint);
            }
        }
        PointsOut.Add(EndPoint);

        return (PointsOut, ChunksTaken);
    }

    public void ClearScreen()
    {
        foreach (var item in ActiveGates)
        {
            if (item.gameObject)
                Destroy(item.gameObject);
        }
        foreach (var Type in IO)
        {
            foreach (var Pin in Type.Value)
            {
                for (int i = 0; i < Pin.Value.Count; i++)
                {
                    if (Pin.Value[i])
                        Destroy(Pin.Value[i]);
                }
            }
        }
        IO.Clear();
        FacePins.Clear();
        ActiveComponents.Clear();
        ActiveWires.Clear();
        ActiveGates.Clear();
    }

    public float GetGridSize()
    {
        return GridSize;
    }

    public void SetGridSize(float Size)
    {
        GridSize = Mathf.Max(0.01f, Size);
    }

    public Vector3 GridSnap(Vector3 Coord)
    {
        return GridSnap(Coord.x, Coord.y, Coord.z);
    }

    public Vector3 GridSnap(float CoordX, float CoordY, float CoordZ)
    {
        return new Vector3(
            Mathf.Floor((CoordX + GridSize) / (GridSize * 2)) * (GridSize * 2),
            Mathf.Floor((CoordY + GridSize) / (GridSize * 2)) * (GridSize * 2),
            Mathf.Floor((CoordZ + GridSize) / (GridSize * 2)) * (GridSize * 2)
            );
    }

    public void AddChunks(UINode node, List<Vector3> Chunks)
    {
        foreach (var item in Chunks)
        {
            if (ActiveComponents.ContainsKey(item))
            {
                ActiveComponents[item].Add(node);
            }
            else
            {
                ActiveComponents.Add(item, new List<UINode>() { node });
            }
        }
    }

    public void RemoveChunks(UINode node, List<Vector3> Chunks)
    {
        foreach (var item in Chunks)
        {
            if (ActiveComponents.ContainsKey(item))
            {
                ActiveComponents[item].Remove(node);
                if (ActiveComponents[item].Count == 0)
                {
                    ActiveComponents.Remove(item);
                }
            }
        }
    }


    public void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        foreach (var item in ActiveComponents)
        {
            Gizmos.DrawWireCube(item.Key, Vector3.one * GridSize * 2);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
