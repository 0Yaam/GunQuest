using UnityEngine;
using UnityEngine.InputSystem;

namespace GunQuest.Input
{
    // C# wrapper for PlayerInput.inputactions, stored beside that asset as in the tutorial.
    public sealed class PlayerInput : System.IDisposable
    {
        public InputActionAsset Asset { get; }

        private readonly InputActionMap onFoot;
        private readonly InputAction movement;
        private readonly InputAction look;
        private readonly InputAction jump;
        private readonly InputAction crouch;
        private readonly InputAction sprint;
        private readonly InputAction interact;
        private readonly InputAction fire;
        private readonly InputAction reload;
        private readonly InputAction aim;

        public PlayerInput()
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            onFoot = Asset.AddActionMap("OnFoot");

            movement = onFoot.AddAction("Movement", InputActionType.Value, expectedControlLayout: "Vector2");
            movement.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            movement.AddBinding("<Gamepad>/leftStick");

            look = onFoot.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Pointer>/delta");
            look.AddBinding("<Gamepad>/rightStick");

            jump = onFoot.AddAction("Jump", InputActionType.Button, expectedControlLayout: "Button");
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            crouch = onFoot.AddAction("Crouch", InputActionType.Button, expectedControlLayout: "Button");
            crouch.AddBinding("<Keyboard>/leftCtrl");
            crouch.AddBinding("<Gamepad>/buttonEast");

            sprint = onFoot.AddAction("Sprint", InputActionType.Button, expectedControlLayout: "Button");
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");

            interact = onFoot.AddAction("Interact", InputActionType.Button, expectedControlLayout: "Button");
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");
            fire = onFoot.AddAction("Fire", InputActionType.Button);
            fire.AddBinding("<Mouse>/leftButton");
            fire.AddBinding("<Gamepad>/rightTrigger");
            reload = onFoot.AddAction("Reload", InputActionType.Button);
            reload.AddBinding("<Keyboard>/r");
            reload.AddBinding("<Gamepad>/rightShoulder");
            aim = onFoot.AddAction("Aim", InputActionType.Button);
            aim.AddBinding("<Mouse>/rightButton");
            aim.AddBinding("<Gamepad>/leftTrigger");
        }

        public OnFootActions OnFoot => new OnFootActions(this);
        public void Dispose()
        {
            Asset.Disable();
            Object.Destroy(Asset);
        }

        public readonly struct OnFootActions
        {
            private readonly PlayerInput wrapper;
            public OnFootActions(PlayerInput wrapper) => this.wrapper = wrapper;
            public InputAction Movement => wrapper.movement;
            public InputAction Look => wrapper.look;
            public InputAction Jump => wrapper.jump;
            public InputAction Crouch => wrapper.crouch;
            public InputAction Sprint => wrapper.sprint;
            public InputAction Interact => wrapper.interact;
            public InputAction Fire => wrapper.fire;
            public InputAction Reload => wrapper.reload;
            public InputAction Aim => wrapper.aim;
            public void Enable() => wrapper.onFoot.Enable();
            public void Disable() => wrapper.onFoot.Disable();
        }
    }
}
