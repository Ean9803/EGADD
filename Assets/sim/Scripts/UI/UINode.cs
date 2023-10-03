using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class UINode : UIComponents
{
    private Node node;
    private GameObject Input, Output;

    private Dictionary<int, Dictionary<int, Dictionary<int, GameObject>>> Pins = new Dictionary<int, Dictionary<int, Dictionary<int, GameObject>>>();

    private UICommunicator Board;
    private MasterControl MasterNode;
    private Vector3 Position;
    private Quaternion Rotation;

    private List<Vector3> Chunks = new List<Vector3>();

    public override void SetUpProperties(UI ui)
    {
        if (node != null)
            if (node.GetLogic() != null)
                node.GetLogic().UpdateProperties(ui);
    }

    public override void Highlight()
    {
        
    }

    public override void UnHighlight()
    {
        
    }

    public override void HighlightSelected()
    {
        if (node != null)
            if (node.GetLogic() != null)
                node.GetLogic().HighlightSelected();
    }

    public override NodeType GetNodeType()
    {
        return NodeType.Gate;
    }

    public override void DeleteComponent()
    {
        node.RemoveAllConnections();
        MasterNode.GetMasterNode().GetLogic().BreakLink(node.Name, -1, -1, (-1, -1), Node.NodeInfo.PinType.Input);
        MasterNode.GetMasterNode().GetLogic().BreakLink(node.Name, -1, -1, (-1, -1), Node.NodeInfo.PinType.Output);
        MasterNode.GetMasterNode().GetLogic().DeleteNode(node.Name.ToUpper());
        MasterNode.Refresh();
    }

    public void SetNode(Node node, GameObject Input, GameObject Output, UICommunicator Board, MasterControl MasterNode, int[] Total)
    {
        this.Board = Board;
        this.MasterNode = MasterNode;
        this.node = node;
        this.Input = Input;
        this.Output = Output;

        node.SetUINode(this);

        int[] Types = { (int)Node.NodeInfo.PinType.Output, (int)Node.NodeInfo.PinType.Input };
        int[] Faces = { (int)Logic.Pin.Face.FORWARD, (int)Logic.Pin.Face.BACK, (int)Logic.Pin.Face.LEFT, (int)Logic.Pin.Face.RIGHT };
        Vector3[] Offset = { new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 1) };
        GameObject[] PinOuts = { Output, Input };

        int Size = 0;
        int SubPins = 0;
        int[] PinNumbers = new int[4];

        Dictionary<int, List<GameObject>> Connections = new Dictionary<int, List<GameObject>>();

        float PinSpace = 1f;

        for (int i = 0; i < Types.Length; i++)
        {
            Size = node.Info.PinSize((Node.NodeInfo.PinType)Types[i]);
            for (int j = 0; j < Size; j++)
            {
                for (int h = 0; h < Faces.Length; h++)
                {
                    if ((int)node.Info.GetFace(j, (Node.NodeInfo.PinType)Types[i]) == Faces[h])
                    {
                        float total = 0;
                        switch ((Logic.Pin.Face)Faces[h])
                        {
                            case Logic.Pin.Face.BACK:
                                total = (float)Total[0];
                                break;
                            case Logic.Pin.Face.FORWARD:
                                total = (float)Total[1];
                                break;
                            case Logic.Pin.Face.LEFT:
                                total = (float)Total[2];
                                break;
                            case Logic.Pin.Face.RIGHT:
                                total = (float)Total[3];
                                break;
                            default:
                                break;
                        }

                        GameObject PinObject = new GameObject(((Node.NodeInfo.PinType)Types[i]).ToString() + ":" + j);
                        
                        SubPins = node.Info.PinSubSize(j, (Node.NodeInfo.PinType)Types[i]);
                        for (int k = 0; k < SubPins; k++)
                        {
                            GameObject Pin = Instantiate(PinOuts[i]);
                            Pin.name = "SubPin: " + k;
                            Pin.transform.parent = PinObject.transform;
                            
                            Pin.transform.localPosition = new Vector3(0, 0, PinNumbers[h] - ((total - 1) * (PinSpace * 0.5f)));
                            UIPin Data = Pin.GetComponent<UIPin>();
                            Data.AssignID(node.Name + "PIN-" + ((Node.NodeInfo.PinType)Types[i]).ToString() + ((Logic.Pin.Face)h).ToString() + j.ToString() + k.ToString());
                            MasterNode.AddToCollection(Data);
                            if ((Node.NodeInfo.PinType)Types[i] == Node.NodeInfo.PinType.Input)
                            {
                                bool Can = true;

                                Logic.ConnectionType[] ConTypes = MasterNode.GetMasterNode().GetLogic().GetConnectionType(node.Name, j, k, Node.NodeInfo.PinType.Input);
                                for (int c = 0; c < ConTypes.Length; c++)
                                {
                                    if (ConTypes[c] != Logic.ConnectionType.Force)
                                    {
                                        Can = false;
                                        break;
                                    }
                                }
                                Data.AssignPinData(node.Info.PinName(j, (Node.NodeInfo.PinType)Types[i]), j, k, node.Name, Can, MasterNode);
                            }
                            else
                            {
                                Data.AssignPinData(node.Info.PinName(j, (Node.NodeInfo.PinType)Types[i]), j, k, node.Name, false, null);
                            }
                            if (!Pins.ContainsKey(Types[i]))
                            {
                                Pins.Add(Types[i], new Dictionary<int, Dictionary<int, GameObject>>());
                            }
                            if (!Pins[Types[i]].ContainsKey(j))
                            {
                                Pins[Types[i]].Add(j, new Dictionary<int, GameObject>());
                            }
                            if (!Pins[Types[i]][j].ContainsKey(k))
                            {
                                Pins[Types[i]][j].Add(k, Pin);
                            }
                            else
                            {
                                Pins[Types[i]][j][k] = Pin;
                            }

                            PinNumbers[h]++;
                        }

                        if (!Connections.ContainsKey(h))
                        {
                            Connections.Add(h, new List<GameObject>());
                        }
                        Connections[h].Add(PinObject);

                        PinObject.transform.Rotate(new Vector3(0, (int)node.Info.GetFace(j, (Node.NodeInfo.PinType)Types[i]), 0));
                    }
                }
            }
        }

        float MaxZ = (float)Mathf.Max(2, Mathf.Max(PinNumbers[0], PinNumbers[1])) * 0.5f;
        float MaxX = (float)Mathf.Max(2, Mathf.Max(PinNumbers[2], PinNumbers[3])) * 0.5f;

        transform.localScale = new Vector3(MaxX, 1, MaxZ);

        float[] MoveBy = new float[] { -(MaxX - 1), MaxX - 1, MaxZ - 1, -(MaxZ - 1) };

        foreach (var item in Connections)
        {
            Vector3 Off = Offset[item.Key] * MoveBy[item.Key];
            foreach (var Con in item.Value)
            {
                Con.transform.position += Off;
                Con.transform.parent = transform;
            }
        }

        UpdateChunks();
    }

    public void RemoveGate()
    {
        Board.RemoveChunks(this, Chunks);
        Destroy(gameObject);
    }

    public Node GetNode()
    {
        return node;
    }

    public GameObject GetPin(int Index, int SubIndex, Node.NodeInfo.PinType Type)
    {
        int T = (int)Type;

        if (Pins.ContainsKey(T))
        {
            if (Pins[T].ContainsKey(Index))
            {
                if (Pins[T][Index].ContainsKey(SubIndex))
                {
                    return Pins[T][Index][SubIndex];
                }
            }
        }
        return null;
    }

    public bool ObjectChange()
    {
        return (Position - transform.position).sqrMagnitude > 0.001f || (Rotation * Quaternion.Inverse(transform.rotation)).eulerAngles.sqrMagnitude > 0.001f;
    }

    public void UpdateChunks()
    {
        Position = transform.position;
        Rotation = transform.rotation;

        List<Vector3> C = OverlapChunks();
        List<Vector3> Add = new List<Vector3>();
        List<Vector3> Remove = new List<Vector3>(Chunks);
        for (int i = 0; i < C.Count; i++)
        {
            if (!Chunks.Contains(C[i]))
            {
                Add.Add(C[i]);
                Chunks.Add(C[i]);
            }
            else
            {
                Remove.Remove(C[i]);
            }
        }
        for (int i = 0; i < Remove.Count; i++)
        {
            Chunks.Remove(Remove[i]);
        }

        Board.AddChunks(this, Add);
        Board.RemoveChunks(this, Remove);
    }

    private List<Vector3> CornerCoords(float Extra)
    {
        List<Vector3> Corners = new List<Vector3>();

        Corners.Add(transform.TransformPoint(new Vector3(1, 1, 1) * Extra));
        Corners.Add(transform.TransformPoint(new Vector3(-1, 1, 1) * Extra));
        Corners.Add(transform.TransformPoint(new Vector3(1, -1, 1) * Extra));
        Corners.Add(transform.TransformPoint(new Vector3(-1, -1, 1) * Extra));
        Corners.Add(transform.TransformPoint(new Vector3(1, 1, -1) * Extra));
        Corners.Add(transform.TransformPoint(new Vector3(-1, 1, -1) * Extra));
        Corners.Add(transform.TransformPoint(new Vector3(1, -1, -1) * Extra));
        Corners.Add(transform.TransformPoint(new Vector3(-1, -1, -1) * Extra));

        return Corners;
    }

    private (Vector3 Max, Vector3 Min) GetBoundingBox()
    {
        List<Vector3> Cs = CornerCoords(1);
        Vector3 Max = Cs[0];
        Vector3 Min = Cs[0];

        for (int i = 0; i < Cs.Count; i++)
        {
            Max.x = Mathf.Max(Max.x, Cs[i].x);
            Max.y = Mathf.Max(Max.y, Cs[i].y);
            Max.z = Mathf.Max(Max.z, Cs[i].z);

            Min.x = Mathf.Min(Min.x, Cs[i].x);
            Min.y = Mathf.Min(Min.y, Cs[i].y);
            Min.z = Mathf.Min(Min.z, Cs[i].z);
        }

        return (Max, Min);
    }

    private List<Vector3> OverlapChunks()
    {
        (Vector3 Max, Vector3 Min) = GetBoundingBox();
        Vector3 Start = Board.GridSnap(Min);
        Vector3 End = Board.GridSnap(Max);
        List<Vector3> ChunksDetected = new List<Vector3>();
        for (float i = Start.x; i <= End.x; i += Board.GridSize * 2)
        {
            for (float j = Start.y; j <= End.y; j += Board.GridSize * 2)
            {
                for (float k = Start.z; k <= End.z; k += Board.GridSize * 2)
                {
                    ChunksDetected.Add(Board.GridSnap(i, j, k));
                }
            }
        }
        return ChunksDetected;
    }

    public float CubeDistance(Vector3 Point)
    {
        return CubeDistance(Point, transform.position, transform.localScale, transform.rotation.eulerAngles);
    }

    private static void sincos(float Angle, out float sin, out float cos)
    {
        sin = Mathf.Sin(Angle);
        cos = Mathf.Cos(Angle);
    }


    private static float3x3 AngleAxis3x3(float angle, Vector3 axis)
    {
        angle = angle * (3.14159265f / 180);
        float c, s;
        sincos(angle, out s, out c);

        float t = 1 - c;
        float x = axis.x;
        float y = axis.y;
        float z = axis.z;

        return new float3x3(
            t * x * x + c, t * x * y - s * z, t * x * z + s * y,
            t * x * y + s * z, t * y * y + c, t * y * z - s * x,
            t * x * z - s * y, t * y * z + s * x, t * z * z + c
            );
    }

    private static float3 mul(float3x3 matrix, float4 vector)
    {
        float3 y = new float3(vector.xyz);
        return new float3(
            (matrix.c0.x * y.x) + (matrix.c1.x * y.y) + (matrix.c2.x * y.z),
            (matrix.c0.y * y.x) + (matrix.c1.y * y.y) + (matrix.c2.y * y.z),
            (matrix.c0.z * y.x) + (matrix.c1.z * y.y) + (matrix.c2.z * y.z)
            );
    }

    private static Vector3 EyeRotation(Vector3 eye, Vector3 center, Vector3 Rotation)
    {
        eye = eye - center;
        Vector3 eyeY = mul((AngleAxis3x3(-Rotation.y, new Vector3(0, 1, 0))), new float4(eye, 1)).xyz;
        Vector3 eyeX = mul((AngleAxis3x3(-Rotation.x, new Vector3(1, 0, 0))), new float4(eyeY, 1)).xyz;
        Vector3 eyeZ = mul((AngleAxis3x3(-Rotation.z, new Vector3(0, 0, 1))), new float4(eyeX, 1)).xyz;
        return eyeZ;
    }

    public static float CubeDistance(Vector3 eye, Vector3 center, Vector3 size, Vector3 Rotation)
    {
        eye = EyeRotation(eye, center, Rotation);
        Vector3 o = new Vector3(Mathf.Abs(eye.x), Mathf.Abs(eye.y), Mathf.Abs(eye.z)) - size;
        float ud = (Vector3.Max(o, Vector3.zero)).magnitude;
        float n = Mathf.Min(Mathf.Max(Mathf.Max(o.x, o.y), o.z), 0);
        return ud + n;
    }

    public override UINode GetUINode()
    {
        return this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (ObjectChange())
        {
            UpdateNodeProfile();
            UpdateChunks();
        }
        if (Board != null)
        {
            transform.position = Board.ClampInBounds(transform.position);
        }
    }

    public void UpdateNodeProfile()
    {
        node.SetPosition(transform.position);
        node.SetRotation(transform.rotation);
    }
}
