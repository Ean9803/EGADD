using System.Collections.Generic;
using UnityEngine;
using RuntimeHandle;
using UnityEngine.InputSystem;
using TMPro;

public class UI : MonoBehaviour
{
    public UICommunicator Board;
    public MasterControl Elect;
    public Translator Lator;
    public GameObject PlayerX;
    public GameObject PlayerY;
    public Camera PlayerCam;
    public Camera PlayerGUICam;
    public Rigidbody Player;
    private bool CanMode = false;
    public float Mult = 10;

    public Transform ContentList;
    public GameObject Item;
    public GameObject InputItem;

    private float LastRot = 0;
    private float Rot = 0;

    public Vector2 HBounds;
    public Vector2 VBounds;

    public static Vector2 HB;
    public static Vector3 VB;

    public static Vector2 DefaultBoardSize = new Vector2(20, 20);
    public static Vector2 MinBoardSize = new Vector2(15, 15);
    public static Vector2 MaxBoardSize = new Vector2(70, 70);

    public List<TabGroupCollection> Groups = new List<TabGroupCollection>();

    [System.Serializable]
    public class TabGroupCollection
    {
        public string Name;
        public List<GameObject> Tabs = new List<GameObject>();
    }

    public RuntimeTransformHandle Handle;
    public TextMeshProUGUI Title;

    private string TargetTabGroup = "";
    private UIComponents SelectedComponent;
    private UIComponents SelectedWireComponent;

    private UIWire DummyWire;
    private Transform DummyTarget;

    private void SetTabGroup(string Group, int Index)
    {
        foreach (var item in Groups)
        {
            if (item.Name.Equals(Group))
            {
                foreach (var OBJs in item.Tabs)
                {
                    if (OBJs)
                        OBJs.SetActive(false);
                }
                if (Index >= 0 && Index < item.Tabs.Count)
                {
                    if (item.Tabs[Index])
                        item.Tabs[Index].SetActive(true);
                }
            }
        }
    }

    public void SetTargetGroup(string GroupName)
    {
        TargetTabGroup = GroupName;
    }

    public void SetTabInGroup(int Index)
    {
        SetTabGroup(TargetTabGroup, Index);
    }

    public void SetTitle(string Title)
    {
        this.Title.text = Title;
    }

    // Start is called before the first frame update
    void Start()
    {
        LastRot = Rot = PlayerY.transform.eulerAngles.x;
        HB = HBounds;
        VB = VBounds;
        SetTabGroup("Handle", 1);
        SetTabGroup("Controls", 0);
    }

    public void SetMoveMode()
    {
        Handle.type = HandleType.POSITION;
    }

    public void SetRotateMode()
    {
        Handle.type = HandleType.ROTATION;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        if (CanMode)
        {
            if (!PlayerCam.orthographic)
            {
                PlayerCam.fieldOfView = 90;
                PlayerGUICam.fieldOfView = 90;
                PlayerGUICam.orthographic = false;

                if (Input.GetKey(KeyCode.Mouse1))
                {
                    PlayerX.transform.Rotate(0, Input.GetAxis("Mouse X") * Mult, 0);

                    Rot -= Input.GetAxis("Mouse Y") * Mult;
                    Rot = Mathf.Clamp(Rot, -90, 90);
                    PlayerY.transform.Rotate(-(LastRot - Rot), 0, 0);
                    LastRot = Rot;

                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = false;
                }
                else if (Input.GetKey(KeyCode.Mouse2))
                {
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                Vector3 MoveDir = new Vector3((Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.Mouse2) ? -Input.GetAxis("Mouse X") : 0),
                                              (Input.GetKey(KeyCode.LeftControl) ? -1 : 0) + (Input.GetKey(KeyCode.Space) ? 1 : 0) + (Input.GetKey(KeyCode.Mouse2) ? -Input.GetAxis("Mouse Y") : 0),
                                              (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0) + (Input.GetAxis("Mouse ScrollWheel") * Mult));

                //Player.MovePosition(Player.transform.position + PlayerY.transform.TransformDirection(MoveDir) * Time.deltaTime * Mult);
                Player.velocity = PlayerY.transform.TransformDirection(MoveDir) * Mult;

                Handle.autoScaleFactor = 2f;
            }
            else
            {
                Vector3 MoveDir = new Vector3((Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.Mouse1) ? -Input.GetAxis("Mouse X") : 0),
                                              0,
                                              (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0) + (Input.GetKey(KeyCode.Mouse2) || Input.GetKey(KeyCode.Mouse1) ? -Input.GetAxis("Mouse Y") : 0));
                Player.velocity = -MoveDir * Mult;
                Player.transform.position = new Vector3(Player.transform.position.x, VB.y - 2, Player.transform.position.z);
                PlayerX.transform.rotation = Quaternion.AngleAxis(180, Vector3.up);

                Rot += 10;
                Rot = Mathf.Clamp(Rot, -90, 90);
                PlayerY.transform.Rotate(-(LastRot - Rot), 0, 0);
                LastRot = Rot;

                PlayerGUICam.orthographic = true;
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    PlayerCam.orthographicSize -= Time.deltaTime * Mult;
                }
                else if (Input.GetKey(KeyCode.Space))
                {
                    PlayerCam.orthographicSize += Time.deltaTime * Mult;
                }
                else
                {
                    PlayerCam.orthographicSize += Input.GetAxis("Mouse ScrollWheel") * Mult;
                }
                PlayerCam.orthographicSize = Mathf.Clamp(PlayerCam.orthographicSize, 1, 70);
                PlayerGUICam.orthographicSize = PlayerCam.orthographicSize;

                Handle.autoScaleFactor = 0.2f;

                if (Input.GetKey(KeyCode.Mouse1))
                {
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = false;
                }
                else if (Input.GetKey(KeyCode.Mouse2))
                {
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                bool HighSelect = false;
                Vector3 mousePos = Mouse.current.position.ReadValue();
                mousePos.z = PlayerCam.nearClipPlane;
                Ray ray = PlayerCam.ScreenPointToRay(mousePos);
                RaycastHit[] Hits = Physics.RaycastAll(ray);
                if (Hits.Length > 0)
                {
                    Transform SelectItem = null;
                    Vector3 Point = PlayerCam.transform.position + PlayerCam.transform.forward * 100;
                    UIComponents NewSelectedComponent = null;

                    foreach (RaycastHit item in Hits)
                    {
                        if (item.transform.root == PlayerCam.transform.root)
                        {
                            continue;
                        }

                        if (GetUIComp(item.transform) == null)
                        {
                            continue;
                        }

                        UIComponents NewTestSelectedComponent = GetUIComp(item.transform);
                        Transform Item = NewTestSelectedComponent.transform;

                        if (SelectItem == null)
                        {
                            SelectItem = Item;
                            Point = item.point;
                            NewSelectedComponent = NewTestSelectedComponent;
                        }
                        else
                        {
                            if ((Point - PlayerCam.transform.position).sqrMagnitude > (item.point - PlayerCam.transform.position).sqrMagnitude)
                            {
                                SelectItem = Item;
                                NewSelectedComponent = NewTestSelectedComponent;
                            }
                        }
                    }

                    if (SelectItem != null)
                    {
                        if (SelectItem.transform.root != Handle.transform.root)
                        {
                            Handle.target = SelectItem.transform.root;
                        }
                    }

                    if (SelectedComponent != NewSelectedComponent)
                    {
                        if (SelectedComponent != null)
                        {
                            SelectedComponent.UnHighlight();
                        }
                        if (NewSelectedComponent != null)
                        {
                            SelectedComponent = NewSelectedComponent;
                            SelectedComponent.Highlight();
                        }
                        else
                            SelectedComponent = null;
                    }
                    else
                    {
                        if (SelectedComponent != null)
                        {
                            SelectedComponent.HighlightSelected();
                            HighSelect = true;
                        }
                    }
                }
                else
                {
                    Handle.target = transform;
                }

                if (SelectedComponent != null)
                {
                    if (SelectedComponent.GetNodeType() == UIComponents.NodeType.OutputPin || SelectedComponent.GetNodeType() == UIComponents.NodeType.InputPin)
                    {
                        if (DummyWire == null && HighSelect && SelectedComponent.GetComponent<UIPin>().CanCreateLink(out Logic.ForceState State))
                        {
                            DummyTarget = SpawnWire(SelectedComponent.gameObject, SelectedComponent);
                        }
                        else if (DummyWire != null && SelectedComponent.GetComponent<UIPin>().CanCreateLink(out Logic.ForceState StateTO))
                        {
                            if (SelectedComponent.GetNodeType() != SelectedWireComponent.GetNodeType())
                            {
                                UINode Input;
                                UINode Output;
                                Vector2 InputPin;
                                Vector2 OutputPin;
                                if (SelectedComponent.GetNodeType() == UIComponents.NodeType.InputPin)
                                {
                                    Input = SelectedComponent.GetUINode();
                                    InputPin = SelectedComponent.GetComponent<UIPin>().GetPortData();
                                    Output = SelectedWireComponent.GetUINode();
                                    OutputPin = SelectedWireComponent.GetComponent<UIPin>().GetPortData();
                                }
                                else
                                {
                                    Output = SelectedComponent.GetUINode();
                                    OutputPin = SelectedComponent.GetComponent<UIPin>().GetPortData();
                                    Input = SelectedWireComponent.GetUINode();
                                    InputPin = SelectedWireComponent.GetComponent<UIPin>().GetPortData();
                                }

                                Elect.GetMasterNode().GetLogic().SetConnection(Output.GetNode().Name, (int)OutputPin.x, (int)OutputPin.y, Input.GetNode().Name, (int)InputPin.x, (int)InputPin.y);
                                Elect.Refresh();
                            }
                            DeleteDummyWire();
                        }
                        else if (!SelectedComponent.GetComponent<UIPin>().CanCreateLink(out Logic.ForceState StateFROM))
                        {
                            DeleteDummyWire();
                        }
                    }
                    else if (SelectedComponent.GetNodeType() == UIComponents.NodeType.Output || SelectedComponent.GetNodeType() == UIComponents.NodeType.Input)
                    {
                        if (DummyWire != null)
                        {
                            UINode LinkNode = SelectedWireComponent.GetUINode();
                            Vector2Int LinkPin = SelectedWireComponent.GetComponent<UIPin>().GetPortData();

                            Vector2Int PinOut = SelectedComponent.GetComponent<UI_IO>().GetPin();
                            
                            if (SelectedWireComponent.GetNodeType() == UIComponents.NodeType.InputPin && SelectedComponent.GetNodeType() == UIComponents.NodeType.Input)
                            {
                                Elect.GetMasterNode().GetLogic().SetLink(LinkNode.GetNode().Name, LinkPin.x, LinkPin.y, (PinOut.x, PinOut.y), Node.NodeInfo.PinType.Input);
                                Elect.Refresh();
                            }
                            else if (SelectedWireComponent.GetNodeType() == UIComponents.NodeType.OutputPin && SelectedComponent.GetNodeType() == UIComponents.NodeType.Output)
                            {
                                Elect.GetMasterNode().GetLogic().SetLink(LinkNode.GetNode().Name, LinkPin.x, LinkPin.y, (PinOut.x, PinOut.y), Node.NodeInfo.PinType.Output);
                                Elect.Refresh();
                            }
                            else
                            {
                                Debug.Log("Fail");
                            }
                            DeleteDummyWire();
                        }
                    }
                    else
                    {
                        DeleteDummyWire();
                    }
                }
                else
                {
                    DeleteDummyWire();
                }
                //
                UpdateProperty();
            }

            if (Handle.target == null)
            {
                Handle.target = transform;
            }
            //
            if (DummyTarget != null)
            {
                Vector3 mousePos = Mouse.current.position.ReadValue();
                mousePos.z = PlayerCam.nearClipPlane;
                Ray ray = PlayerCam.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit Hit, 25))
                {
                    DummyTarget.transform.position = Hit.point;
                }
                else
                {
                    DummyTarget.transform.position = ray.origin + ray.direction * 25;
                }
            }

            if (Input.GetKeyDown(KeyCode.Delete) && SelectedComponent != null)
            {
                if (SelectedComponent.GetNodeType() == UIComponents.NodeType.Wire || SelectedComponent.GetNodeType() == UIComponents.NodeType.Gate)
                {
                    SelectedComponent.DeleteComponent();
                    SelectedComponent = null;
                    Elect.Refresh();
                }
            }
        }
    }

    private Transform SpawnWire(GameObject PinOBJ, UIComponents WireSelect)
    {
        SelectedWireComponent = WireSelect;
        GameObject PinWire = Instantiate(Board.Wire, PinOBJ.transform);
        PinWire.transform.localPosition += new Vector3(1.5f, 0, 0);

        DummyWire = PinWire.GetComponent<UIWire>();
        DummyWire.CanSelect = false;

        DummyWire.TargetPoint.transform.localRotation = Quaternion.identity;

        
        DummyWire.Board = Board;

        return DummyWire.TargetPoint.transform;
    }

    public void ResetDef()
    {
        Handle.target = transform;
        SelectedComponent = null;
        DeleteDummyWire();
        UpdateProperty();
    }

    private void DeleteDummyWire()
    {
        if (DummyWire != null)
            Destroy(DummyWire.gameObject);
    }

    private UIComponents GetUIComp(Transform transform)
    {
        if (transform.TryGetComponent<UIComponents>(out UIComponents Comp))
        {
            if (Comp.CanSelect)
                return Comp;
            else
                return null;
        }
        if (transform.parent != null)
            return GetUIComp(transform.parent);
        return null;
    }

    public void MouseEnter()
    {
        CanMode= true;
    }

    public void MouseExit()
    {
        CanMode = false;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(Mathf.Abs(HBounds.x), VBounds.x, Mathf.Abs(HBounds.y)), new Vector3(-Mathf.Abs(HBounds.x), VBounds.x, Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(Mathf.Abs(HBounds.x), VBounds.x, Mathf.Abs(HBounds.y)), new Vector3(Mathf.Abs(HBounds.x), VBounds.x, -Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(-Mathf.Abs(HBounds.x), VBounds.x, -Mathf.Abs(HBounds.y)), new Vector3(-Mathf.Abs(HBounds.x), VBounds.x, Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(-Mathf.Abs(HBounds.x), VBounds.x, -Mathf.Abs(HBounds.y)), new Vector3(Mathf.Abs(HBounds.x), VBounds.x, -Mathf.Abs(HBounds.y)));

        Gizmos.DrawLine(new Vector3(Mathf.Abs(HBounds.x), VBounds.y, Mathf.Abs(HBounds.y)), new Vector3(-Mathf.Abs(HBounds.x), VBounds.y, Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(Mathf.Abs(HBounds.x), VBounds.y, Mathf.Abs(HBounds.y)), new Vector3(Mathf.Abs(HBounds.x), VBounds.y, -Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(-Mathf.Abs(HBounds.x), VBounds.y, -Mathf.Abs(HBounds.y)), new Vector3(-Mathf.Abs(HBounds.x), VBounds.y, Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(-Mathf.Abs(HBounds.x), VBounds.y, -Mathf.Abs(HBounds.y)), new Vector3(Mathf.Abs(HBounds.x), VBounds.y, -Mathf.Abs(HBounds.y)));

        Gizmos.DrawLine(new Vector3(Mathf.Abs(HBounds.x), VBounds.x, Mathf.Abs(HBounds.y)), new Vector3(Mathf.Abs(HBounds.x), VBounds.y, Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(-Mathf.Abs(HBounds.x), VBounds.x, -Mathf.Abs(HBounds.y)), new Vector3(-Mathf.Abs(HBounds.x), VBounds.y, -Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(Mathf.Abs(HBounds.x), VBounds.x, -Mathf.Abs(HBounds.y)), new Vector3(Mathf.Abs(HBounds.x), VBounds.y, -Mathf.Abs(HBounds.y)));
        Gizmos.DrawLine(new Vector3(-Mathf.Abs(HBounds.x), VBounds.x, Mathf.Abs(HBounds.y)), new Vector3(-Mathf.Abs(HBounds.x), VBounds.y, Mathf.Abs(HBounds.y)));
    }

    public MasterControl GetMasterControl()
    {
        return Elect;
    }

    private void ClearList()
    {
        for (int i = ContentList.childCount - 1; i >= 0; i--)
        {
            Destroy(ContentList.GetChild(i).gameObject);
        }
    }

    public void AddItem(string Title, GateItem.RunAction Action)
    {
        GameObject Btn = Instantiate(Item, ContentList);
        GateItem G = Btn.GetComponent<GateItem>();
        G.SetUI(this);
        G.Title.text = Title;
        G.BtnRunAction = Action;
    }

    public void AddInputItem(string Title, GateItem.RunAction InitAction, GateItem.RunAction Action)
    {
        GameObject Btn = Instantiate(InputItem, ContentList);
        GateItem G = Btn.GetComponent<GateItem>();
        G.SetUI(this);
        G.Title.text = Title;
        G.BtnRunAction = Action;
        if (InitAction != null)
            InitAction(G, this);
    }

    public void UpdateGateList()
    {
        SetTitle("Gates");
        ClearList();
        AddItem("AND", (GateItem Item, UI ui) => 
        {
            string Name = "AND:" + Logic.RandomString();
            ui.GetMasterControl().GetMasterNode().GetLogic().AddNode(new Node(
                new And().
                AssignProfile(new Logic.Profile().
                    Add("Name", Name)
                    )
                ));
            Handle.target = transform;
            ui.GetMasterControl().Refresh();
            if (ui.GetMasterControl().GetMasterNode().GetLogic().GetNode(Name, out Node N))
            {
                UpdateProperty(N.Info.Skin);
            }
        });
        AddItem("OR", (GateItem Item, UI ui) => 
        {
            string Name = "OR:" + Logic.RandomString();
            ui.GetMasterControl().GetMasterNode().GetLogic().AddNode(new Node(
                new Or().
                AssignProfile(new Logic.Profile().
                    Add("Name", Name)
                    )
                ));
            Handle.target = transform;
            ui.GetMasterControl().Refresh();
            if (ui.GetMasterControl().GetMasterNode().GetLogic().GetNode(Name, out Node N))
            {
                UpdateProperty(N.Info.Skin);
            }
        });
        AddItem("XOR", (GateItem Item, UI ui) => 
        {
            string Name = "XOR:" + Logic.RandomString();
            ui.GetMasterControl().GetMasterNode().GetLogic().AddNode(new Node(
                new XOr().
                AssignProfile(new Logic.Profile().
                    Add("Name", Name)
                    )
                ));
            Handle.target = transform;
            ui.GetMasterControl().Refresh();
            if (ui.GetMasterControl().GetMasterNode().GetLogic().GetNode(Name, out Node N))
            {
                UpdateProperty(N.Info.Skin);
            }
        });
        AddItem("NOT", (GateItem Item, UI ui) => 
        {
            string Name = "NOT:" + Logic.RandomString();
            ui.GetMasterControl().GetMasterNode().GetLogic().AddNode(new Node(
                new Not().
                AssignProfile(new Logic.Profile().
                    Add("Name", Name)
                    )
                ));
            Handle.target = transform;
            ui.GetMasterControl().Refresh();
            if (ui.GetMasterControl().GetMasterNode().GetLogic().GetNode(Name, out Node N))
            {
                UpdateProperty(N.Info.Skin);
            }
        });
        AddItem("LIGHT BULB", (GateItem Item, UI ui) =>
        {
            string Name = "LIGHT:" + Logic.RandomString();
            ui.GetMasterControl().GetMasterNode().GetLogic().AddNode(new Node(
                new Light().
                AssignProfile(new Logic.Profile().
                    Add("Name", Name)
                    )
                ));
            Handle.target = transform;
            ui.GetMasterControl().Refresh();
            if (ui.GetMasterControl().GetMasterNode().GetLogic().GetNode(Name, out Node N))
            {
                UpdateProperty(N.Info.Skin);
            }
        });
        AddItem("SWITCH", (GateItem Item, UI ui) =>
        {
            string Name = "SWITCH:" + Logic.RandomString();
            ui.GetMasterControl().GetMasterNode().GetLogic().AddNode(new Node(
                new Switch().
                AssignProfile(new Logic.Profile().
                    Add("Name", Name)
                    )
                ));
            Handle.target = transform;
            ui.GetMasterControl().Refresh();
            if (ui.GetMasterControl().GetMasterNode().GetLogic().GetNode(Name, out Node N))
            {
                UpdateProperty(N.Info.Skin);
            }
        });

        AddItem("--Custom--", (GateItem Item, UI ui) => { });

        AddInputItem("Create new node:", null, (GateItem Item, UI ui) => 
        {
            if (!string.IsNullOrEmpty(Item.Input.text))
            {
                Lator.CreateFile(new Node(new Cluster().AssignProfile(new Logic.Profile().Add("Name", Item.Input.text))), Item.Input.text);
                ui.Elect.Refresh();
                UpdateGateList();
            }
        });

        foreach (var item in Lator.GetCustomNodes())
        {
            AddItem(item.Name + item.Extension, (GateItem Item, UI ui) =>
            {
                Node newNode = item.GenerateNode(out List<string> Errs);
                ui.GetMasterControl().GetMasterNode().GetLogic().AddNode(newNode);
                Handle.target = transform;
                ui.GetMasterControl().Refresh();
                if (ui.GetMasterControl().GetMasterNode().GetLogic().GetNode(newNode.Name, out Node N))
                {
                    UpdateProperty(N.Info.Skin);
                }
                Elect.OutputTechnoPanel.text = "";
                foreach (var item in Errs)
                {
                    Elect.OutputTechnoPanel.text += item;
                }
            });
            AddItem("-EDIT: " + item.Name + item.Extension, (GateItem Item, UI ui) =>
            {
                Elect.OpenNode(item.Name + item.Extension);
                UpdateGateList();
            });
        }
    }

    public void UpdateFileList()
    {
        SetTitle("File");
        ClearList();
        bool L = Elect.PreviousNodes(out string[] Nodenames);
        AddItem("-CURRENT NODE: " + Nodenames[Nodenames.Length - 1], null);
        AddItem("Save Current Node", (GateItem Item, UI ui) => { Elect.SaveCurrentNode(); });

        if (L)
        {
            AddItem("-EXIT TO: " + Nodenames[Nodenames.Length - 2], (GateItem Item, UI ui) =>
            {
                Elect.ExitNode();
                UpdateGateList();
            });
        }

        AddItem("Import SDL", (GateItem Item, UI ui) => { Lator.ImportSDL(); });
        AddItem("Export SDL", (GateItem Item, UI ui) => { Lator.ExportSDL(); });
        AddItem("Save Collection", (GateItem Item, UI ui) => { Lator.SaveCollection(); });
        AddItem("Save As Collection", (GateItem Item, UI ui) => { Lator.SaveAsCollection(); });
        AddItem("Load Collection", (GateItem Item, UI ui) => { Lator.LoadCollection(); });
        AddItem("HELP", (GateItem Item, UI ui) => { HelpScreen(); });
        AddItem("EXIT", (GateItem Item, UI ui) => { ClearList(); AddItem("Are you sure?", null); AddItem("YES", (GateItem Item, UI ui) => { Application.Quit(); }); AddItem("NO", (GateItem item, UI ui) => { UpdateFileList(); }); });
    }

    public void HelpScreen()
    {
        SetTitle("Help");
        ClearList();
        AddItem("[SHOW FULL HELP]", (GateItem Item, UI ui) => { GetMasterControl().OutputHelp(); });
        AddItem("Quick Help:", null);
        AddItem("", null);
        AddItem("SIDE PANEL OPTIONS:", null);
        AddItem("FILE", null);
        AddItem("POSITION/ROTATION", null);
        AddItem("GATES", null);
        AddItem("PROPERTIES", null);
        AddItem("CAMERA", null);
        AddItem("TECHNOLOGIC", null);
        AddItem("ANALYSIS", null);
        AddItem("", null);
        AddItem("Select a gate option to create.", null);
        AddItem("A gate can be moved or rotated.", null);
        AddItem("Press delete when the gate is selected", null);
        AddItem("to remove the gate.", null);
        AddItem("", null);
        AddItem("Double click a gate pin", null);
        AddItem("to start a connection.", null);
        AddItem("", null);
        AddItem("A connection can only be made", null);
        AddItem("to an opposite pin type:", null);
        AddItem("-Ball Pins are INPUT", null);
        AddItem("-Socket Pins are OUTPUT", null);
        AddItem("Connections can also be made to IO Pins", null);
        AddItem("-Down arrow Pins are INPUT", null);
        AddItem("-Up arrow Pins are OUTPUT", null);
        AddItem("", null);
        AddItem("The Arrow on the logic board", null);
        AddItem("specifies the forward direction", null);
        AddItem("", null);
        AddItem("IO INPUT pins can be toggled", null);
    }

    public string GetSelectedItem()
    {
        if (SelectedComponent != null)
        {
            return SelectedComponent.GetID();
        }
        return null;
    }

    public void UpdatePropertyBtn()
    {
        UpdateProperty();
    }

    public void UpdateProperty(UIComponents Select = null)
    {
        if (Select != null)
        {
            if (SelectedComponent != null)
                SelectedComponent.UnHighlight();
            SelectedComponent = Select;
            SelectedComponent.Highlight();
            Handle.target = Select.transform.root;
        }
        SetTitle("Properties");
        ClearList();
        if (SelectedComponent != null)
        {
            SelectedComponent.SetUpProperties(this);
        }
        UpdateBoardProperties();
    }

    public void UpdateCameraList()
    {
        SetTitle("Camera");
        ClearList();

        AddItem("", null);
        if (PlayerCam.orthographic)
        {
            AddItem("Controls:", null);
            AddItem("[WASD] :", null);
            AddItem("[MIDDLE MOUSE] :", null);
            AddItem("[RIGHT MOUSE BTN] :", null);
            AddItem("-Pan", null);
            AddItem("[MOUSE SCROLL] :", null);
            AddItem("[Lft CTL, SPACE] :", null);
            AddItem("-Zoom", null);
            AddItem("Set to Perspective", (GateItem item, UI ui) => { PlayerCam.orthographic = false; UpdateCameraList(); });
        }
        else
        {
            AddItem("Controls:", null);
            AddItem("[WASD] :", null);
            AddItem("-Move", null);
            AddItem("[Lft CTL, SPACE] :", null);
            AddItem("-Up/Down", null);
            AddItem("[MOUSE SCROLL] :", null);
            AddItem("-Move forward/back", null);
            AddItem("[MIDDLE MOUSE] :", null);
            AddItem("-Pan relative to camera", null);
            AddItem("[RIGHT MOUSE BTN] :", null);
            AddItem("-Rotate camera", null);
            AddItem("Set to Orthographic", (GateItem item, UI ui) => { PlayerCam.orthographic = true; UpdateCameraList(); });
        }
    }

    public void UpdateBoardProperties()
    {
        AddItem("Board Properties:", null);
        AddInputItem("Size X",
            (GateItem Item, UI ui) =>
            {
                Item.Input.text = Elect.GetMasterNode().GetLogic().GetProfile().GetFloat("SizeX", (0, 0), new float[] { DefaultBoardSize.x }, true)[0].ToString();
            },
            (GateItem Item, UI ui) =>
            {
                Elect.GetMasterNode().GetLogic().GetProfile().Add("SizeX", Mathf.Clamp(Logic.Profile.ConvertFloat(Item.Input.text, DefaultBoardSize.x), MinBoardSize.x, MaxBoardSize.x), 0);
                Elect.Refresh();
            });
       AddInputItem("Size Y",
            (GateItem Item, UI ui) =>
            {
                Item.Input.text = Elect.GetMasterNode().GetLogic().GetProfile().GetFloat("SizeY", (0, 0), new float[] { DefaultBoardSize.y }, true)[0].ToString();
            },
            (GateItem Item, UI ui) =>
            {
                Elect.GetMasterNode().GetLogic().GetProfile().Add("SizeY", Mathf.Clamp(Logic.Profile.ConvertFloat(Item.Input.text, DefaultBoardSize.y), MinBoardSize.y, MaxBoardSize.y), 0);
                Elect.Refresh();
            });
        AddInputItem("Input Count",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = Elect.GetMasterNode().Info.PinSubSize(0, Node.NodeInfo.PinType.Input).ToString();
        },
        (GateItem Item, UI ui) =>
        {
            int Inputs = Logic.Profile.ConvertInt(Item.Input.text, 1);
            Elect.GetMasterNode().GetLogic().GetProfile().Add("Input", Inputs);
            if (Elect.GetMasterNode().Info.GetPinObject(0, Node.NodeInfo.PinType.Input, out Node.Pin P))
            {
                P.SetSubPinSize(Inputs);
            }
            Elect.Refresh();
        });

        AddInputItem("Output Count",
        (GateItem Item, UI ui) =>
        {
            Item.Input.text = Elect.GetMasterNode().Info.PinSubSize(0, Node.NodeInfo.PinType.Output).ToString();
        },
        (GateItem Item, UI ui) =>
        {
            int Outputs = Logic.Profile.ConvertInt(Item.Input.text, 1);
            Elect.GetMasterNode().GetLogic().GetProfile().Add("Output", Outputs);
            if (Elect.GetMasterNode().Info.GetPinObject(0, Node.NodeInfo.PinType.Output, out Node.Pin P))
            {
                P.SetSubPinSize(Outputs);
            }
            Elect.Refresh();
        });

        AddItem("Auto sort logic", (GateItem item, UI ui) => { Elect.GetMasterNode().GetLogic().AutoSort(); Elect.Refresh(); });
    }
}
