using PageNS;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InitPage : BaseUIPage
{
    private InputAction input;

    public override void OnAwake() // 顺序一定是先设名字再处理基类
    {
        SetName(nameof(InitPage));

        PageOpenAnimeDuration = 0.001f;

        PageCloseAnimeDuration = 0.5f;

        base.OnAwake();

        input = new InputAction(binding: "/*/<button>");
    }

    public override void OnOpenPage()
    {
        base.OnOpenPage();

        input.Enable();

        input.performed += OnToMainPage;
    }

    public override void OnClosePage()
    {
        base.OnClosePage();

        input.Disable();

        input.performed -= OnToMainPage;
    }

    private void OnToMainPage(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && ctx.control.device is Keyboard)
        {
            Manager.SwitchToPage(nameof(MainPage));
        }
    }

    public override void OnDestroyPage()
    {
        base.OnDestroyPage();
    }
}
