using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class MasterControl : MonoBehaviour
{
    Node Master;
    public bool OverrideUI;
    private List<(int Port, int SubPort, float Value)> PortListIn = new List<(int Port, int SubPort, float Value)>();
    private bool TestCon = false;
    public List<PinCollection> Outputs = new List<PinCollection>();

    private List<UIComponents> ActiveCollection = new List<UIComponents>();
    public UICommunicator UIScreen;
    public UI ui;

    public GameObject GameBoard;
    public TMP_InputField TechnoPanel;
    public TMP_InputField OutputTechnoPanel;
    public TMP_InputField AnaylisTechnoPanel;

    public Node GetMasterNode()
    {
        return Master;
    }

    [System.Serializable]
    public class PinCollection
    {
        public List<bool> Values = new List<bool>();
    }

    private List<string> NodeDepth = new List<string>();

    public void ResetPath()
    {
        NodeDepth.Clear();
        NodeDepth.Add("MAIN");
        BuildComputer();
    }

    public bool PreviousNodes(out string[] Nodenames)
    {
        Nodenames = new string[NodeDepth.Count];
        for (int i = 0; i < NodeDepth.Count; i++)
        {
            Nodenames[i] = NodeDepth[i];
        }
        if (NodeDepth.Count > 1)
        {
            return true;
        }
        return false;
    }

    public void SaveCurrentNode()
    {
        if (NodeDepth.Count >= 1)
        {
            ui.Lator.SaveCurrentNodeAs(NodeDepth[NodeDepth.Count - 1]);
        }
    }

    public void OpenNode(string NodeFile)
    {
        SaveCurrentNode();
        NodeDepth.Add(NodeFile);
        BuildComputer();
    }

    public void ExitNode()
    {
        if (NodeDepth.Count > 1)
        {
            SaveCurrentNode();
            NodeDepth.RemoveAt(NodeDepth.Count - 1);
            BuildComputer();
        }
    }

    
    void Start()
    {
        OpenNode("MAIN");
        //Cluster ActiveCluster = new Cluster();
        /*
        ActiveCluster.AddNode(new Node(new Not().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "wire1")))).

                                    AddNode(new Node(new Not().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "wire2")))).

                                    AddNode(new Node(new Not().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "wire3")))).

                                    AddNode(new Node(new Not().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "wire4")))).

                                    AddNode(new Node(new And().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "And1")))).

                                    SetConnection("wire4", 0, 0, "wire1", 0, 0).
                                    SetConnection("wire1", 0, 0, "And1", 0, 0).
                                    SetConnection("wire2", 0, 0, "And1", 0, 0).
                                    SetConnection("wire3", 0, 0, "And1", 0, 1).

                                    SetLink("wire4", 0, 0, (0, 0), Node.NodeInfo.PinType.Input).
                                    SetLink("wire2", 0, 0, (0, 1), Node.NodeInfo.PinType.Input).
                                    SetLink("wire3", 0, 0, (0, 2), Node.NodeInfo.PinType.Input).
                                    SetLink("And1", 0, 0, (0, 0), Node.NodeInfo.PinType.Output).

                                    AssignProfile(new Logic.Profile().Add("Name", "INTERNAL"));

        ActiveCluster.AutoSort();
        */
        //Master = new Node(ActiveCluster.AssignProfile(new Logic.Profile().Add("Name", "INTERNAL")));
        /*
        Master = new Node(new Cluster().
                                AddNode(new Node(new Cluster().

                                    AddNode(new Node(new Wire().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "wire1")))).

                                    AddNode(new Node(new Wire().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "wire2")))).

                                    AddNode(new Node(new Wire().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "wire3")))).

                                    AddNode(new Node(new And().
                                        AssignProfile(new Logic.Profile().
                                            Add("Name", "And1")))).

                                    SetConnection("wire1", 0, 0, "And1", 0, 0).
                                    SetConnection("wire2", 0, 0, "And1", 0, 0).
                                    SetConnection("wire3", 0, 0, "And1", 0, 1).

                                    SetLink("wire1", 0, 0, 0, Node.NodeInfo.PinType.Input).
                                    SetLink("wire2", 0, 0, 1, Node.NodeInfo.PinType.Input).
                                    SetLink("wire3", 0, 0, 2, Node.NodeInfo.PinType.Input).
                                    SetLink("And1", 0, 0, 0, Node.NodeInfo.PinType.Output).

                                    AssignProfile(new Logic.Profile().Add("Name", "INTERNAL")))).

                                SetLink("INTERNAL", 0, 0, 0, Node.NodeInfo.PinType.Input).
                                SetLink("INTERNAL", 0, 1, 1, Node.NodeInfo.PinType.Input).
                                SetLink("INTERNAL", 0, 2, 2, Node.NodeInfo.PinType.Input).
                                SetLink("INTERNAL", 0, 0, 0, Node.NodeInfo.PinType.Output).

                                AssignProfile(new Logic.Profile().Add("Name", "MASTER"))
                                );
        */
        //GenerateUI(Master);
    }

    public void OutputHelp()
    {
        ui.SetTargetGroup("Controls");
        ui.SetTabInGroup(2);
        ui.SetTargetGroup("code");
        ui.SetTabInGroup(1);
        
        OutputTechnoPanel.text = "E.G.A.D.D Help\nFiles found in [" + Application.dataPath + "/HELP_FILES/]\n";
        if (!Directory.Exists(Application.dataPath + "/HELP_FILES/"))
        {
            Directory.CreateDirectory(Application.dataPath + "/HELP_FILES/");
        }
        if (!File.Exists(Application.dataPath + "/HELP_FILES/DefaultHelp.txt"))
        {
            File.WriteAllText(Application.dataPath + "/HELP_FILES/DefaultHelp.txt", "E.G.A.D.D made by Ian R. Poll stands for \r\nElectronic\r\nGraphical\r\nApplication\r\n\t" +
                "for\r\nDigital\r\nDiagrams\r\n\r\nFall 2023\r\n\r\nGITHUB Code: https://github.com/Ean9803/Egadd\r\nIt is intended to be used as an educational tool for simulating logic diagrams in a 3D environment.\r\n\r\n" +
                "On start up, a start menu will pop up, you can ignore it and close the popup by slecting the upper right 'X' in the popup. Within the popup are two lists, list" +
                " one labled \"Samples\" are folders contained in the EGADD_FILES/Samples folder, this will always show folders located in that directory. The second list contains" +
                " the last 10 folders the user previously saved or loaded.\r\n\r\nThe folder labled \"temp\" is the default folder that opens up and has its contents erased when" +
                " the EGADD application is started.\r\n\r\nLocated on the upper left side of the screen is the select menu list, here the user is able to add gates, create gates," +
                " save, load, export, analysis, ... on the current board.\r\n\r\nThe options are listed from top to bottom:\r\n-File\r\n-Handle Control\r\n-Gates\r\n-Properties\r\n" +
                "-Camera\r\n-Technologic\r\n\r\n-File\r\nIn the file menu, the user is able to load in new files or save the current file in edit as well as export to the .sdl format" +
                " (when possible)\r\nEGADD is capable to import .egadd files and .sdl files\r\nThe difference between .egadd and .sdl files are to components that are able to be" +
                " stated in each file.\r\nThe .sdl file has the basic logic components and connections so that the file can be ran by sdl.h to run logic, .egadd files have additional" +
                " capabilites by being able to reference other files as logic gates, the other files can also be .sdl files as well as .egadd files. When a file is refrenced in a .egadd" +
                " file EGADD will \"pack up\" the refrenced file into an IC gate and will contain the logic of said file inside. The maximum depth that files can be referenced is capped" +
                " at 100 meaning that if someone were to have a .egadd file reference itself, EGADD will only build the logic 100 times before it terminates by creating a lightbulb" +
                " instead.\r\n\r\nNew EGADD components:\r\n-Lightbulb\r\n-Switch\r\n-IC\r\n\r\nLightbulb - a lightbulb turns on/off when a positive signal is recived and spits out the" +
                " same input to its ouptut pins. When a lightbulb is contained in an IC, and the IC is double selected, the IC will create a unit square and place all surface lightbulbs" +
                " in their 3D percentage position. The percentage position is determined by how long and wide the IC is and how far up the lightbulb is relative to the max height. IC with" +
                " IC inside which also have bulbs inside will not be displayed, so if an IC has 2 bulbs and an IC with 10 bulbs inside of it, only the 2 bulbs will be shown, and if you" +
                " open the IC with the 2 bulbs, the IC with 10 bulbs can be displayed.\r\n\r\nSwitch - a switch is able to be interacted by the user for manual control by double selecting" +
                " it to toggle it on/off. The two input pins are used to turn on/off the switch, once a switch is on via the on pin, it will say on until the off pin is high. Switches that" +
                " have no input connected can be manually controlled.\r\n\r\n-IC - ICs as stated before contain additional logic which was created by using an additional .sdl or .egadd" +
                " file. An IC can have up to 1000 inputs and outputs and a minimum of 1 input and 1 output. An IC size does not affect how the IC is scaled when inside a logic circuit," +
                " the scale is determined by the maximum number of pins.\r\n\r\nSaving a node - (Nodes and Gates will be used interchangeably) This saves the current node you are editing" +
                "\r\nExit to [] - backs out to the previously edited node\r\nImport SDL - Opens a file explorer to select a .sdl file, you can also import a .egadd file as well\r\n\r\n" +
                "Export SDL - Opens a file explorer to save a file, an exported file will be a .sdl if no new EGADD components are used, if they are used then a .egadd file will be created" +
                " in its place. If ICs are used then they are expaneded in the exported file, so if your node you are exporting has ICs, as long as the IC don't have EGADD components, an" +
                " .sdl file will be made that has all the connections made, turing a multiple file .egadd collection to a ONE file .sdl script. You can import the newly made .sdl or .egadd" +
                " file, but remember the nodes will be displayed in the same position they were when in the IC, so expect some or a lot of overlapped nodes. Auto sort logic in the properties" +
                " menu will do its best to place the gates in a sensible place\r\n\r\nSave Collection - Saves all nodes listed in Gates in a folder with a .sdl2 postname\r\nSave As" +
                " Collection - Opens a file explorer to save the collection - A collection is saved as a folder that contains all gates used in the current collection\r\nLoad Collection" +
                " - Opens a file explorer to open a folder, NOT a file, the folder does not hae to have a \".sdl2\" ending, I just put that so the user would be ablt to tell if the files" +
                " contained were created by EGADD\r\n\r\nHelp - This is how you got here, there is the quick help and the long help, the long help can be modified by editing or adding" +
                " files in the EGADD_HELP folder, all file's contents are displayed here\r\n\r\nExit - Quits EGADD\r\n\r\nHandle Control - Toggles between controling node position or node" +
                " rotation\r\n\r\nProperties - List the properties of the currenly selected node and the logic board properites, when a pin is selected, the user can force a pin on or off" +
                " and this is part of the .sdl system so it won't affect the export file extension. The Auto sort logic button located at the bottom of the properites menu will place" +
                " logic gates in order by how many gates are connected in series to the input pins and by how many gates have the same depth. The board properties controls how big the" +
                " board is in the X and Y (technically Z) direction between 15 and 70 units and how many IO pins.\r\n\r\nCamera - Displays the controls for navigating in the current camera" +
                " mode, the default is perspective, but the user can toggle ortho on and the camera will be forced to look down at the board like a 2D display, the Arrow on the logic board" +
                " signals where the forward direction is and what side the output is on.\r\n\r\nTechnologic - The user is able to modify the current file opened and compile it using the" +
                " lightning button at the top, the output is displayed here if there is any errors. In the analysis tab, the current gate opened is tested against every binary combination" +
                " on the input and the output result is displayed. You can start/pause or restart the analysis using the control buttons below the tab select buttons\r\n\r\nHow to interact" +
                " - \r\nWhen in the Gate menu, selecting on a gate will spawn the gate at the center of the board, here the user is able to change properites of the selected gate in the" +
                " properites menu. A gate can be seen in three parts, the body, the input pins, and the output pins. The body, when selected will show the properties of the gate, here you" +
                " can change the input amount (if the gate allows) and the nickname given to the gate, this attribute will be saved in the .sdl/.egadd file. The input pins, when selected" +
                " will show if the pin can be forced on or off and the stats of the pin, if a pin is force on/off, the pin will not accept any connections from any output pins. The output" +
                " pin, when selected, doesn't have any modifyable data, so if you want to negate a pin just place a NOT gate in sequence.\r\n\r\nMaking connections - \r\nWhen a pin, that" +
                " is not forced to a state, is double selected, a connection is able to be made to an opposite pin type. Input pins are represented as ball joints and output pins as socket" +
                " joints. A connection to the same type of pin is not allowed and will cancel the connection. When a connection is ablt to be made, a wire will be made connecting the pin" +
                " selected to the mouse cursor. When the user selects a pin of opposite type with their mouse cursor a connection will be formed and a wire will be shown connecting the" +
                " previously selected pin and the selected pin. Pins can have more than one connection on the same pin. Pins can also be connected to their own gate, but don't think you" +
                " can make the gate work like that, I made it so no infinite loops can be made sucker!\r\n\r\nIO Pins -\r\nLocated on opposite ends of the board are the IO pins. The input" +
                " pins have a downward arrow and the outputs have an uppward arrow. Output pins cannot be manualy controlled, but inputs pins can be toggled, similar to the switch, when" +
                " double selected. When an IO pin is on, the arrow will be animated, spinning and occilating up and down. Making a connection is the same as connecting gates to each other," +
                " select the pin for connection by double selecting it and select the IO pin to connect to, input pins can only connect to input IO and same with output pins and output" +
                " IO. IO pins cannot initiate a connection so connections between Input IO and Output IO cannot be made by the user, if you want something like that, first \"why?\", and" +
                " second, you can put two NOT gates in series connecting the IO pins.\r\n\r\nDelete -\r\nWhen a gate or wire is selected, the selected item can be deleted by pressing the" +
                " [delete] button, you cannot delete pins this way, the properties menu controls the pin count.\r\n\r\nCreating ICs -\r\nLocated in the Gates menu is an input field to" +
                " add a new gate, when a user enters a gate name and presses enter, a new file will be made in the collection and will show up in the Gates list. To edit the IC select the" +
                " \"-Edit [GateName]\" button to the gate you want to edit. You can edit gates when not in the main gate and EGADD keeps track of your dive, so in the files menu is an" +
                " option to exit to the previous node you were in. Its best to save the current node first before surfacing.\r\n");
        }
        foreach (var item in Directory.GetFiles(Application.dataPath + "/HELP_FILES/"))
        {
            OutputTechnoPanel.text += "\n[Help file conents of: " + item + "]:\n\n" + File.ReadAllText(item) + "\n[End of " + item + "]\n";
        }
    }

    public void CompileCode()
    {
        Node n = ui.Lator.CompileCode(NodeDepth[NodeDepth.Count - 1], TechnoPanel.text, out string[] Ers);
        OutputTechnoPanel.text = "";
        foreach (var item in Ers)
        {
            OutputTechnoPanel.text += item + "\n";
        }
        if (n != null)
        {
            BuildComputer();
        }
        ui.SetTargetGroup("code");
        ui.SetTabInGroup(1);
    }

    public void PauseAnaylsis()
    {
        TestCon = false;
    }

    public void ResumeAnaylsis()
    {
        if (PortListIn.Count > 0)
            TestCon = true;
        else
            StartAnalysis();
    }

    public void StartAnalysis()
    {

        PortListIn.Clear();

        int[] Ports = UIScreen.IOSize(Node.NodeInfo.PinType.Input);
        for (int i = 0; i < Ports.Length; i++)
        {
            for (int j = 0; j < UIScreen.IOSize(Ports[i], Node.NodeInfo.PinType.Input); j++)
            {
                PortListIn.Add((Ports[i], j, 0));
            }
        }
        TestCon = true;
        AnaylisTechnoPanel.text = "\t[IN]\t=>\t[OUT]\n\n";
    }

    public void BuildComputer()
    {
        Master = ui.Lator.GenerateRootNode(NodeDepth[NodeDepth.Count - 1], out List<string> Ers);
        TechnoPanel.text = ui.Lator.GetNodeContent(NodeDepth[NodeDepth.Count - 1]);
        Refresh();
    }

    public void Refresh()
    {
        ActiveCollection.Clear();
        string LastSelected = ui.GetSelectedItem();
        ui.ResetDef();
        GenerateUI(Master);
        if (LastSelected != null)
        {
            ui.UpdateProperty(GetComponentID(LastSelected));
        }
        Halt = false;
        StopAction = null;
    }

    public void AddToCollection(UIComponents Comp)
    {
        ActiveCollection.Add(Comp);
    }

    public UIComponents GetComponentID(string ID)
    {
        foreach (var item in ActiveCollection)
        {
            if (item.GetID().Equals(ID))
                return item;
        }
        return null;
    }

    public void GenerateUI(Node VisibleNode)
    {
        GameBoard.transform.localScale = new Vector3((GetMasterNode().GetLogic().GetProfile().GetFloat("SizeX", (0, 0), new float[] { 59 }, true)[0] / 59.0f),
                                                     1,
                                                     (GetMasterNode().GetLogic().GetProfile().GetFloat("SizeY", (0, 0), new float[] { 59 }, true)[0] / 59.0f));
        Vector2 Bounds = new Vector2(GetMasterNode().GetLogic().GetProfile().GetFloat("SizeX", (0, 0), new float[] { 59 }, true)[0],
                            GetMasterNode().GetLogic().GetProfile().GetFloat("SizeY", (0, 0), new float[] { 59 }, true)[0]);
        UI.HB = Bounds;
        ui.HBounds = Bounds;
        UIScreen.ClearScreen();
        if (VisibleNode.GetLogic().GetNodes(out string[] Nodes) == Logic.LogicType.Cluster)
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (VisibleNode.GetLogic().GetNode(Nodes[i], out Node child))
                {
                    UIScreen.SpawnItem(child, this);
                }
            }
            int[] Total = VisibleNode.Info.TotalPins();
            for (int t = (int)Node.NodeInfo.PinType.Input; t <= (int)Node.NodeInfo.PinType.Output; t++)
            {
                for (int i = 0; i < VisibleNode.Info.PinSize((Node.NodeInfo.PinType)t); i++)
                {
                    UIScreen.AddIO((Node.NodeInfo.PinType)t, i, VisibleNode, Total, this, null);
                }
            }
            UIScreen.ConnectIO(this);
            UIScreen.DrawConnections(this);
        }
    }

    private void CopySide(Node.NodeInfo.PinType Side, List<PinCollection> List)
    {
        KeyValuePair<string, Dictionary<int, Dictionary<int, string>>> Output = Master.PrintSide(Side);
        List.Clear();
        foreach (var item in Output.Value)
        {
            for (int i = List.Count; i <= item.Key; i++)
            {
                List.Add(new PinCollection());
            }
            foreach (var SubPin in item.Value)
            {
                for (int i = List[item.Key].Values.Count; i <= SubPin.Key; i++)
                {
                    List[item.Key].Values.Add(false);
                }
                List[item.Key].Values[SubPin.Key] = Output.Value[item.Key][SubPin.Key].Equals("1");
            }
        }
    }

    private void RunMasterNode()
    {
        Master.Process();
        CopySide(Node.NodeInfo.PinType.Output, Outputs);
        //KeyValuePair<string, Dictionary<int, Dictionary<int, string>>> Input = Master.PrintSide(Node.NodeInfo.PinType.Input);
        //string Out = "GATE:\t" + Input.Key + "\n[IN]\t" + Node.ConvertToString(Input) + "\n[OUT]\t" + Node.ConvertToString(Output);
        //Debug.Log(Out);
    }

    private bool Halt = false;
    private Action StopAction = null;

    public void Stop(Action OnStop)
    {
        Halt = true;
        StopAction = OnStop;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Halt)
        {
            if (TestCon)
            {
                foreach (var item in PortListIn)
                {
                    UIScreen.SetIO(item.Port, item.SubPort, item.Value);
                }
                KeyValuePair<string, Dictionary<int, Dictionary<int, string>>> Input = Master.PrintSide(Node.NodeInfo.PinType.Input);
                RunMasterNode();
                KeyValuePair<string, Dictionary<int, Dictionary<int, string>>> Output = Master.PrintSide(Node.NodeInfo.PinType.Output);
                AnaylisTechnoPanel.text += Node.ConvertToString(Input) + "\t=>\t" + Node.ConvertToString(Output) + "\n";
                TestCon = Inc(PortListIn, PortListIn.Count - 1);

                if (!TestCon)
                {
                    AnaylisTechnoPanel.text += "[Completed Analysis]\n";
                    ui.SetTargetGroup("Control");
                    ui.SetTabInGroup(0);
                    PortListIn.Clear();
                }
            }
            else
            {
                RunMasterNode();
            }
        }
        else if (StopAction != null)
        {
            StopAction();
            StopAction = null;
        }
    }

    private bool Inc(List<(int Port, int SubPort, float Value)> PortListIn, int Index)
    {
        float NewVal = PortListIn[Index].Value + 1;
        if (NewVal > 1)
        {
            NewVal = 0;
            PortListIn[Index] = (PortListIn[Index].Port, PortListIn[Index].SubPort, NewVal);
            if (Index - 1 >= 0)
            {
                return Inc(PortListIn, Index - 1);
            }
            else
            {
                return false;
            }
        }
        PortListIn[Index] = (PortListIn[Index].Port, PortListIn[Index].SubPort, NewVal);
        return true;
    }
}
