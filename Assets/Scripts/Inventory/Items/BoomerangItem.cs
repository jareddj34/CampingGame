using UnityEngine;

public class BoomerangItem : Item
{
    public MeshRenderer meshRenderer;

    public override void UseItem()
    {
        base.UseItem();
    }

    public void HideMesh()
    {
        meshRenderer.enabled = false;
    }

    public void ShowMesh()
    {
        meshRenderer.enabled = true;
    }


}
