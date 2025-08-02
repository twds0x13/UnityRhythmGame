using Unity.VisualScripting;
using UnityEngine;
using Page = UserInterfaceNS.UserInterfaceManager;

public class AnotherPage : BaseUIPage
{
    public override void OnUpdatePage()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Page.Inst.SwitchToPage(Page.Inst.GamePage);
        }
    }

    public override void OnClosePage()
    {
        base.OnClosePage();
    }
}
