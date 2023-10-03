using UnityEngine;

public abstract class UIComponents : MonoBehaviour
{
    public bool CanSelect = true;

    public abstract void Highlight();

    public abstract void UnHighlight();

    public virtual void HighlightSelected() { }

    public enum NodeType { Wire, Gate, InputPin, OutputPin, Input, Output }

    public abstract NodeType GetNodeType();

    public abstract void DeleteComponent();

    public virtual UINode GetUINode() { return null; }

    public virtual Vector2Int GetPin() { return new Vector2Int(-1, -1); }

    public virtual void SetUpProperties(UI ui) { }

    private string ID = "";

    public void AssignID(string ID)
    {
        this.ID = ID;
    }

    public string GetID()
    {
        return ID;
    }
}
