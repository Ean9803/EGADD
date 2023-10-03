using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class Translator : MonoBehaviour
{
    public MasterControl Master;
    public GameObject BlockInputWindow;
    public GameObject InitStartScreen;
    public Transform LastList;
    public Transform sampleList;
    public GameObject Item;

    private string[] LastFileItems = new string[10];
    private string ActiveCollectionFolder = "temp";
    private static string FileExtenstion = ".egadd";
    public string CurrentOpenFile = "MAIN";
    private static string[] AcceptedFileExtenstion = new string[] { ".egadd", ".sdl" };

    public class CustomNode
    {
        public string Name;
        public string Extension;
        public string Contents;
        public Translator Lator;

        enum Section { NONE, INIT, COMPONENTs, ALIASEs, CONNECTIONs }
        enum Creation { GATE, INPUTS, NAME }

        public Node GenerateNode(out List<string> Ers, Logic.HeaderNode Header = null, int Layer = 0)
        {
            Ers = new List<string>();
            string[] Lines = Contents.Split('\n');
            Section Group = Section.NONE;
            
            Cluster Collection = new Cluster();
            Collection.AssignProfile(new Logic.Profile().Add("Name", Name + Logic.RandomString()).Add("GateType", Name));
            Logic.HeaderNode TempProfile = null;
            Node Main = new Node(Collection);

            if (Header != null)
            {
                Main = AssignData(new Cluster(), Header);
            }
            Main.GetLogic().GetProfile().Add("GateType", Name);

            Dictionary<string, string> Aliase = new Dictionary<string, string>();

            for (int i = 0; i < Lines.Length; i++)
            {
                if (string.IsNullOrEmpty(Lines[i]))
                    continue;
                Lines[i] = Lines[i].Trim();
                if (Lines[i].ToUpper().Equals("$$NODEDATA"))
                {
                    Group = Section.INIT;
                }
                else if (Lines[i].ToUpper().Equals("COMPONENTS"))
                {
                    Group = Section.COMPONENTs;
                }
                else if (Lines[i].ToUpper().Equals("ALIASES"))
                {
                    Group = Section.ALIASEs;
                }
                else if (Lines[i].ToUpper().Equals("CONNECTIONS"))
                {
                    Group = Section.CONNECTIONs;
                }
                else if (Lines[i].ToUpper().Equals("END"))
                {
                    break;
                }
                else
                {
                    switch (Group)
                    {
                        case Section.INIT:
                            if(CreateProfile(Lines[i], out Logic.HeaderNode Prof) && Header == null)
                            {
                                Main = AssignData(Collection, Prof);
                            }
                            break;
                        case Section.COMPONENTs:
                            if (CreateProfile(Lines[i], out Logic.HeaderNode GateProf))
                            {
                                TempProfile = GateProf;
                            }
                            else
                            {
                                if (!Lines[i].StartsWith('$') && Lines[i].Length > 0)
                                {
                                    string LogicName = "";
                                    string GateName = "";
                                    string InputNum = "";
                                    Creation Stage = Creation.GATE;

                                    for (int c = 0; c < Lines[i].Length; c++)
                                    {
                                        if (Lines[i][c] == '*')
                                        {
                                            Stage = Creation.INPUTS;
                                        }
                                        else if (char.IsWhiteSpace(Lines[i][c]))
                                        {
                                            Stage = Creation.NAME;
                                        }
                                        else
                                        {
                                            switch (Stage)
                                            {
                                                case Creation.GATE:
                                                    LogicName += Lines[i][c];
                                                    break;
                                                case Creation.INPUTS:
                                                    InputNum += Lines[i][c];
                                                    break;
                                                case Creation.NAME:
                                                    GateName += char.ToUpper(Lines[i][c]);
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }

                                    int InputCount = Logic.Profile.ConvertInt(InputNum, -1);

                                    if (TempProfile == null)
                                    {
                                        TempProfile = new Logic.HeaderNode()
                                        {
                                            Outputs = new List<int>(),
                                            Inputs = new List<int>(),
                                            Place = new Node.NodePlacement()
                                            {
                                                Pos = Vector3.zero,
                                                Rot = Quaternion.identity
                                            },
                                            Content = new Logic.Profile().Add("Name", GateName).Add("Input", InputCount).GetJson()
                                        };
                                    }
                                    else
                                    {
                                        TempProfile.Content = new Logic.Profile().FromJson(TempProfile.Content).Add("Name", GateName).Add("Input", InputCount).GetJson();
                                    }

                                    Node NewNode;
                                    switch (LogicName)
                                    {
                                        case "NOT":
                                            NewNode = AssignData(new Not(), TempProfile);
                                            break;
                                        case "AND":
                                            NewNode = AssignData(new And(), TempProfile);
                                            break;
                                        case "OR":
                                            NewNode = AssignData(new Or(), TempProfile);
                                            break;
                                        case "XOR":
                                            NewNode = AssignData(new XOr(), TempProfile);
                                            break;
                                        case "LIGHT":
                                            NewNode = AssignData(new Light(), TempProfile);
                                            break;
                                        case "SWITCH":
                                            NewNode = AssignData(new Switch(), TempProfile);
                                            break;
                                        default:
                                            if (Layer < 100)
                                            {
                                                NewNode = Lator.GenerateRootNode(LogicName, out List<string> NestErs, TempProfile, Layer + 1);
                                                if (NewNode == null)
                                                {
                                                    Ers.Add("Unable to make: " + LogicName + " gate");
                                                }
                                                Ers.AddRange(NestErs);
                                            }
                                            else
                                            {
                                                NewNode = AssignData(new Light(), TempProfile);
                                                Ers.Add("Unable to make: " + LogicName + " gate: Recursive depth over 100");
                                            }
                                            break;
                                    }
                                    
                                    Main.GetLogic().AddNode(NewNode);

                                    TempProfile = null;
                                }
                            }
                            break;
                        case Section.ALIASEs:
                            string[] Names = Lines[i].Split('=');
                            Aliase.Add(Names[0].ToUpper().Trim(), Names[1].ToUpper().Trim());
                            break;
                        case Section.CONNECTIONs:
                            string Connection = Lines[i];
                            foreach (var item in Aliase)
                            {
                                Connection = Connection.ToUpper().Replace(item.Key, item.Value);
                            }
                            string[] Gates = Connection.Split('-');
                            for (int g = 0; g < Gates.Length; g++)
                            {
                                Gates[g] = Gates[g].Trim();
                            }
                            string[] Gate1 = Gates[0].Split('#');
                            string[] Gate2 = Gates[1].Split('#');
                            if (!((Gate1[0].Equals("OUT") || Gate1[0].Equals("IN")) || (Gate2[0].Equals("OUT") || Gate2[0].Equals("IN"))))
                            {
                                if (Main.GetLogic().GetNode(Gate1[0], out Node G1))
                                {
                                    if (Main.GetLogic().GetNode(Gate2[0], out Node G2))
                                    {
                                        int G1Port = Logic.Profile.ConvertInt(Gate1[1]) - G1.Info.TotalPinSize(Node.NodeInfo.PinType.Input);
                                        int G2Port = Logic.Profile.ConvertInt(Gate2[1]) - G2.Info.TotalPinSize(Node.NodeInfo.PinType.Input);
                                        if (Mathf.Sign(G1Port) != Mathf.Sign(G2Port))
                                        {
                                            (int Port, int SubPort) InputPins, OutputPins;
                                            string InputGate, OutputGate;
                                            if (G1Port < 0)
                                            {
                                                G1Port = Logic.Profile.ConvertInt(Gate1[1]);
                                                InputPins = G1.Info.GetPinAbs(G1Port, Node.NodeInfo.PinType.Input);
                                                OutputPins = G2.Info.GetPinAbs(G2Port, Node.NodeInfo.PinType.Output);
                                                InputGate = Gate1[0];
                                                OutputGate = Gate2[0];
                                            }
                                            else
                                            {
                                                G2Port = Logic.Profile.ConvertInt(Gate2[1]);
                                                InputPins = G2.Info.GetPinAbs(G2Port, Node.NodeInfo.PinType.Input);
                                                OutputPins = G1.Info.GetPinAbs(G1Port, Node.NodeInfo.PinType.Output);
                                                InputGate = Gate2[0];
                                                OutputGate = Gate1[0];
                                            }

                                            Main.GetLogic().SetConnection(OutputGate, OutputPins.Port, OutputPins.SubPort, InputGate, InputPins.Port, InputPins.SubPort);
                                        }
                                        else
                                        {
                                            Ers.Add("Connection invalid: Port incorrect");
                                        }
                                    }
                                    else
                                    {
                                        Ers.Add("Connection invalid: No gate 2: " + Gate2[0]);
                                    }
                                }
                                else
                                {
                                    Ers.Add("Connection invalid: No gate 1: " + Gate1[0]);
                                }
                            }
                            else if ((Gate1[0].Equals("OUT") || Gate1[0].Equals("IN")) || (Gate2[0].Equals("OUT") || Gate2[0].Equals("IN")))
                            {
                                string GateSubj = (Gate1[0].Equals("OUT") || Gate1[0].Equals("IN")) ? Gate2[0] : Gate1[0];
                                string PinGate = (Gate1[0].Equals("OUT") || Gate1[0].Equals("IN")) ? Gate2[1] : Gate1[1];
                                string GateIO = (Gate1[0].Equals("OUT") || Gate1[0].Equals("IN")) ? Gate1[0] : Gate2[0];
                                string PinIO = (Gate1[0].Equals("OUT") || Gate1[0].Equals("IN")) ? Gate1[1] : Gate2[1];
                                (int Port, int SubPort) GatePins, IOPins;
                                if (Main.GetLogic().GetNode(GateSubj, out Node G1))
                                {
                                    int G1Port = Logic.Profile.ConvertInt(PinGate) - G1.Info.TotalPinSize(Node.NodeInfo.PinType.Input);
                                    if (G1Port < 0)
                                    {
                                        G1Port = Logic.Profile.ConvertInt(PinGate);
                                        GatePins = G1.Info.GetPinAbs(G1Port, Node.NodeInfo.PinType.Input);
                                        IOPins = Main.Info.GetPinAbs(Logic.Profile.ConvertInt(PinIO), Node.NodeInfo.PinType.Input);
                                    }
                                    else
                                    {
                                        GatePins = G1.Info.GetPinAbs(G1Port, Node.NodeInfo.PinType.Output);
                                        IOPins = Main.Info.GetPinAbs(Logic.Profile.ConvertInt(PinIO), Node.NodeInfo.PinType.Output);
                                    }

                                    Main.GetLogic().SetLink(GateSubj, GatePins.Port, GatePins.SubPort, IOPins, GateIO.Equals("IN") ? Node.NodeInfo.PinType.Input : Node.NodeInfo.PinType.Output);
                                }
                                else
                                {
                                    Ers.Add("IO Gate not found: " + GateSubj);
                                }
                            }
                            else if ((Gate1[0].Equals("POWER") || Gate1[0].Equals("GROUND")) || (Gate2[0].Equals("POWER") || Gate2[0].Equals("GROUND")))
                            {
                                string GateSubj = (Gate1[0].Equals("POWER") || Gate1[0].Equals("GROUND")) ? Gate2[0] : Gate1[0];
                                string PinGate = (Gate1[0].Equals("POWER") || Gate1[0].Equals("GROUND")) ? Gate2[1] : Gate1[1];
                                string state = (Gate1[0].Equals("POWER") || Gate1[0].Equals("GROUND")) ? Gate1[0] : Gate2[0];
                                if (Main.GetLogic().GetNode(GateSubj, out Node G1))
                                {
                                    (int Port, int subPort) In = G1.Info.GetPinAbs(Logic.Profile.ConvertInt(PinGate), Node.NodeInfo.PinType.Input);
                                    Main.GetLogic().SetForceState(GateSubj, In.Port, In.subPort, state.Equals("POWER") ? 1 : 0);
                                }
                                else
                                {
                                    Ers.Add("Force Gate not found: " + GateSubj);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            Ers.Add("Built Node");
            return Main;
        }


        private Node AssignData(Logic Collection, Logic.HeaderNode Prof)
        {
            Collection.AssignProfile(new Logic.Profile().FromJson(Prof.Content));
            Node ret = new Node(Collection).SetPosition(Prof.Place.Pos).SetRotation(Prof.Place.Rot);
            for (int p = 0; p < Prof.Inputs.Count; p++)
            {
                if (ret.Info.GetPinObject(p, Node.NodeInfo.PinType.Input, out Node.Pin Pobj))
                {
                    Pobj.SetSubPinSize(Prof.Inputs[p]);
                }
            }
            for (int p = 0; p < Prof.Outputs.Count; p++)
            {
                if (ret.Info.GetPinObject(p, Node.NodeInfo.PinType.Output, out Node.Pin Pobj))
                {
                    Pobj.SetSubPinSize(Prof.Outputs[p]);
                }
            }
            return ret;
        }

        private bool CreateProfile(string Data, out Logic.HeaderNode Info)
        {
            string Header = "$INFO:";
            Data = Data.Trim();
            if (Data.StartsWith(Header))
            {
                Data = Data.Substring(Header.Length);
                try
                {
                    Info = Logic.GenerateHeader(Data);
                    return true;
                }
                catch (System.Exception e)
                {
                    Info = null;
                    return false;
                }
            }
            Info = null;
            return false;
        }
    }

    private CustomNode GetCustom(string NameNode, out string Name, out string Ext)
    {
        string ext = FileExtenstion;
        if (NameNode.LastIndexOf('.') != -1)
        {
            ext = NameNode.Substring(NameNode.LastIndexOf('.'));
            NameNode = NameNode.Substring(0, NameNode.LastIndexOf('.'));
        }
        string Key = ActiveCollectionFolder + NameNode + ext;
        Name = NameNode;
        Ext = ext;
        if (Nodes.ContainsKey(Key))
        {
            return Nodes[Key];
        }
        return null;
    }

    public Node GenerateRootNode(string NameNode, out List<string> Ers, Logic.HeaderNode Header = null, int Layer = 0)
    {
        CustomNode n = GetCustom(NameNode, out NameNode, out string ext);
        if (n != null)
        {
            Node root = n.GenerateNode(out Ers, Header, Layer);
            return root;
        }
        CreateFile(new Node(new Cluster().AssignProfile(new Logic.Profile().Add("Name", Logic.RandomString()))), NameNode, ext);
        return GenerateRootNode(NameNode + ext, out Ers, Header, Layer);
    }

    public Node CompileCode(string NodeName, string Code, out string[] Errors)
    {
        CustomNode n = GetCustom(NodeName, out NodeName, out string ext);
        List<string> Error = new List<string>();
        if (n != null)
        {
            n.Contents = Code;
            Node root = n.GenerateNode(out Error);
            Errors = Error.ToArray();
            return root;
        }
        else
        {
            CreateFile(new Node(new Cluster().AssignProfile(new Logic.Profile().Add("Name", Logic.RandomString()))), NodeName, ext);
            return CompileCode(NodeName + ext, Code, out Errors);
        }
    }

    public string GetNodeContent(string NameNode)
    {
        CustomNode n = GetCustom(NameNode, out NameNode, out string ext);
        if (n != null)
        {
            return n.Contents;
        }
        return "$Empty";
    }

    private Dictionary<string, CustomNode> Nodes = new Dictionary<string, CustomNode>();

    public CustomNode[] GetCustomNodes()
    {
        return Nodes.Values.ToArray();
    }

    private void AddToList(string Path)
    {
        int AddNew = -1;
        for (int i = 0; i < LastFileItems.Length; i++)
        {
            if (string.IsNullOrEmpty(LastFileItems[i]))
            {
                LastFileItems[i] = "";
            }
            else if (LastFileItems[i].Equals(Path))
            {
                AddNew = i;
            }
        }
        if (AddNew == -1)
        {
            for (int i = LastFileItems.Length - 1; i > 0; i--)
            {
                LastFileItems[i] = LastFileItems[i - 1];
            }
        }
        else
        {
            for (int i = AddNew; i > 0; i--)
            {
                LastFileItems[i] = LastFileItems[i - 1];
            }
        }
        LastFileItems[0] = Path;
        SaveList();
    }

    private void SaveList()
    {
        PlayerPrefs.SetString("PreviousList", JsonConvert.SerializeObject(LastFileItems, Formatting.None));
        PlayerPrefs.Save();
    }

    private void LoadList()
    {
        if (PlayerPrefs.HasKey("PreviousList"))
        {
            LastFileItems = JsonConvert.DeserializeObject<string[]>(PlayerPrefs.GetString("PreviousList"));
            if (LastFileItems == null)
            {
                LastFileItems = new string[10];
                for (int i = 0; i < LastFileItems.Length; i++)
                {
                    LastFileItems[i] = "";
                }
            }
        }
        else
        {
            for (int i = 0; i < LastFileItems.Length; i++)
            {
                LastFileItems[i] = "";
            }
            SaveList();
        }
    }

    public void Start()
    {
        LoadList();
        if (!Directory.Exists(Application.dataPath + "/EGADD_Files/temp/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/EGADD_Files/temp/");
        }
        foreach (var item in Directory.GetFiles(Application.dataPath + "/EGADD_Files/temp/"))
        {
            File.Delete(item);
        }
        ActiveCollectionFolder = Application.dataPath + "/EGADD_Files/temp/";
        CreateFile(new Node(new Cluster().AssignProfile(new Logic.Profile().Add("Name", "MAIN"))), "MAIN");
        BlockInput(false);
        SetInitScreen(true);
        FillLists();
    }

    public void SetInitScreen(bool Init)
    {
        InitStartScreen.SetActive(Init);
    }

    public void FillLists()
    {
        if (!Directory.Exists(Application.dataPath + "/EGADD_Files/Samples/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/EGADD_Files/Samples/");
        }
        for (int i = sampleList.childCount - 1; i >= 0; i--)
        {
            Destroy(sampleList.GetChild(i).gameObject);
        }
        for (int i = LastList.childCount - 1; i >= 0; i--)
        {
            Destroy(LastList.GetChild(i).gameObject);
        }
        foreach (var item in Directory.GetDirectories(Application.dataPath + "/EGADD_Files/Samples/"))
        {
            SpawnInitItem(item, sampleList);
        }
        foreach (var item in LastFileItems)
        {
            if (!string.IsNullOrEmpty(item))
            {
                SpawnInitItem(item, LastList);
            }
        }
    }

    private void SpawnInitItem(string PathFile, Transform List)
    {
        GameObject NewItem = Instantiate(Item, List);
        InitItem i = NewItem.GetComponent<InitItem>();
        i.FilePath = PathFile;
        i.Lator = this;
        if (PathFile.EndsWith('/'))
        {
            PathFile = PathFile.Substring(0, PathFile.Length - 1);
        }
        int Last = PathFile.LastIndexOf('/');
        if (Last == -1)
        {
            Last = PathFile.LastIndexOf('\\');
        }
        if (Last != -1)
        {
            PathFile = PathFile.Substring(Last + 1);
        }
        i.Title.text = PathFile;
    }

    public string GenerateLogicScript(Node node, bool Full, out bool isCompat)
    {
        string Header = "$AUTO GENERATED BY E.G.A.D.D:\n$Electronic\r\n$Graphical\r\n$Application\r\n$\tfor\r\n$Digital\r\n$Diagrams\r\n$\tBy: Ian R. Poll\r\n$~~~~~~~~~~~~\n";
        string[] Output = node.GetLogic().GenerateRepresentation(Full, out isCompat);
        string Out = Header + "$$NODEDATA\n" + Output[3] + "\nCOMPONENTS\n" + Output[0] + "\nALIASES\n" + Output[1] + "\nCONNECTIONS\n" + Output[2] + "\nEND";
        return Out;
    }

    public void BlockInput(bool Block)
    {
        BlockInputWindow.SetActive(Block);
    }

    public void ExportSDL()
    {
        BlockInput(true);
        FileBrowser.ShowSaveDialog(ExportSuccess, Cancel, FileBrowser.PickMode.Files, false, title: "Export SDL File", initialPath: ActiveCollectionFolder);
    }

    public void ImportSDL()
    {
        BlockInput(true);
        FileBrowser.ShowLoadDialog(ImportSuccess, Cancel, FileBrowser.PickMode.Files, false, title: "Import SDL File", initialPath: ActiveCollectionFolder);
    }

    public void SaveCollection()
    {
        SaveSuccess(new string[] { ActiveCollectionFolder });
    }

    public void SaveAsCollection()
    {
        BlockInput(true);
        FileBrowser.ShowSaveDialog(SaveSuccess, Cancel, FileBrowser.PickMode.Files, false, title: "Save Collection Folder", initialPath: ActiveCollectionFolder);
    }

    public void LoadCollection()
    {
        BlockInput(true);
        FileBrowser.ShowLoadDialog((string[] Paths) => 
        { 
            LoadSuccess(Paths);
            Master.ResetPath();
        }, Cancel, FileBrowser.PickMode.Folders, false, title: "Load Collection Folder", initialPath: ActiveCollectionFolder);
    }

    public void ReloadFiles()
    {
        LoadSuccess(new string[] { ActiveCollectionFolder });
    }

    public void CreateFile(Node Item, string Name, string FileExt = "")
    {
        string FilePath = ActiveCollectionFolder + Name + (FileExt.Length == 0 ? FileExtenstion : FileExt);
        SaveFileInfo(FilePath, GenerateLogicScript(Item, false, out bool Compat));
        ReloadFiles();
    }

    private void SaveFileInfo(string FilePath, string Contents)
    {
        string[] NewContent = Contents.Split('\n');
        Dictionary<string, List<int>> OldComments = new Dictionary<string, List<int>>();
        string SendConent = "";
        if (File.Exists(FilePath))
        {
            string[] OldContent = File.ReadAllText(FilePath).Split('\n');
            for (int i = 0; i < OldContent.Length; i++)
            {
                if (OldContent[i].StartsWith('$') && !OldContent[i].StartsWith("$INFO:") && !OldContent[i].StartsWith("$$NODEDATA"))
                {
                    if (OldComments.ContainsKey(OldContent[i]))
                    {
                        OldComments[OldContent[i]].Add(i);
                    }
                    else
                    {
                        OldComments.Add(OldContent[i], new List<int>() { i });
                    }
                }
            }
        }

        List<string> SendContentList = new List<string>();

        for (int i = 0; i < NewContent.Length; i++)
        {
            if (OldComments.ContainsKey(NewContent[i]))
            {
                if (OldComments[NewContent[i]].Count > 0)
                {
                    OldComments[NewContent[i]].RemoveAt(0);
                }
                if (OldComments[NewContent[i]].Count == 0)
                {
                    OldComments.Remove(NewContent[i]);
                }
            }
            SendContentList.Add(NewContent[i]);
        }
        foreach (var item in OldComments)
        {
            for (int i = 0; i < item.Value.Count; i++)
            {
                SendContentList.Insert(item.Value[i], item.Key);
            }
        }

        foreach (var item in SendContentList)
        {
            SendConent += item + "\n";
        }

        File.WriteAllText(FilePath, SendConent);
    }

    private void ExportSuccess(string[] Paths)
    {
        string Path = Paths[0];
        if (Path.LastIndexOf('.') != -1)
        {
            Path = Path.Substring(0, Path.LastIndexOf('.'));
        }
        string Data = GenerateLogicScript(Master.GetMasterNode(), true, out bool Compat);
        Path += Compat ? ".sdl" : FileExtenstion;
        SaveFileInfo(Path, Data);
        BlockInput(false);
    }

    private void ImportSuccess(string[] Paths)
    {
        string FilePath = "";
        for (int i = 0; i < Paths.Length; i++)
        {
            if (IsAccepted(Paths[i]))
            {
                FilePath = Paths[i];
                File.WriteAllText(ActiveCollectionFolder + Path.GetFileNameWithoutExtension(FilePath) + Path.GetExtension(FilePath), File.ReadAllText(FilePath));
            }
        }
        ReloadFiles();
        BlockInput(false);
    }

    public void SaveCurrentNodeAs(string Name)
    {
        string OldExt = FileExtenstion;
        if (Name.LastIndexOf('.') != -1)
        {
            Name = Name.Substring(0, Name.LastIndexOf('.'));
        }
        foreach (var item in Nodes)
        {
            if (Name.Equals(item.Value.Name))
            {
                OldExt = item.Value.Extension;
                break;
            }
        }
        string Data = GenerateLogicScript(Master.GetMasterNode(), false, out bool Compat);
        string Path = ActiveCollectionFolder + Name + (Compat ? OldExt : FileExtenstion);
        SaveFileInfo(Path, Data);
        ReloadFiles();
        BlockInput(false);
    }

    private void SaveSuccess(string[] Paths)
    {
        string PathSave = Paths[0];
        if (PathSave.LastIndexOf('.') != -1)
        {
            PathSave = PathSave.Substring(0, PathSave.LastIndexOf('.'));
        }
        if (!PathSave.EndsWith("/"))
        {
            PathSave += ".sdl2/";
        }
        if (!Directory.Exists(PathSave))
        {
            Directory.CreateDirectory(PathSave);
        }
        foreach (var item in Nodes)
        {
            SaveFileInfo(PathSave + item.Value.Name + FileExtenstion, item.Value.Contents);
        }
        SaveFileInfo(PathSave + "MAIN" + FileExtenstion, GenerateLogicScript(Master.GetMasterNode(), false, out bool compat));
        ActiveCollectionFolder = PathSave;
        BlockInput(false);
        AddToList(ActiveCollectionFolder);
    }

    private bool IsAccepted(string Path)
    {
        for (int i = 0; i < AcceptedFileExtenstion.Length; i++)
        {
            if (Path.EndsWith(AcceptedFileExtenstion[i]))
                return true;
        }
        return false;
    }

    public void LoadSuccess(string[] Paths)
    {
        Nodes.Clear();
        ActiveCollectionFolder = Paths[0];
        if (!ActiveCollectionFolder.EndsWith("/"))
        {
            ActiveCollectionFolder += "/";
        }
        string FilePath = "";
        foreach (var item in Directory.GetFiles(ActiveCollectionFolder))
        {
            if (IsAccepted(item))
            {
                FilePath = item;
                if (!Nodes.ContainsKey(FilePath))
                {
                    Nodes.Add(FilePath, null);
                }
                Nodes[FilePath] = new CustomNode()
                {
                    Name = Path.GetFileNameWithoutExtension(FilePath),
                    Contents = File.ReadAllText(FilePath),
                    Extension = Path.GetExtension(FilePath),
                    Lator = this
                };
            }
        }
        BlockInput(false);
        SetInitScreen(false);
        AddToList(ActiveCollectionFolder);
    }

    private void Cancel()
    {
        BlockInput(false);
    }
}
