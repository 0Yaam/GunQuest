using UnityEngine;
using UnityEngine.InputSystem;
using InputActions = GunQuest.Input.PlayerInput;

[RequireComponent(typeof(PlayerMotor))]
public class InputManager : MonoBehaviour
{
    private InputActions playerInput;
    private InputActions.OnFootActions onFoot;
    private PlayerMotor motor;
    private PlayerLook look;
    private PlayerInteract interact;
    private PlayerWeapon weapon;
    private PlayerHealth health;
    public bool IsAiming => onFoot.Aim != null && onFoot.Aim.IsPressed();
    private bool CanAct => Time.timeScale > 0f && (health == null || !health.IsDead);

    public InputActions.OnFootActions OnFoot => onFoot;

    void Awake()
    {
        // Khởi tạo PlayerInput và tham chiếu đến Action Map "OnFoot" [1]
        playerInput = new InputActions();
        onFoot = playerInput.OnFoot;
        
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        interact = GetComponent<PlayerInteract>();
        weapon = GetComponent<PlayerWeapon>();
        health = GetComponent<PlayerHealth>();

        if (look == null)
        {
            look = GetComponentInChildren<PlayerLook>();
        }

        if (interact == null)
        {
            interact = GetComponentInChildren<PlayerInteract>();
        }

        // Đăng ký sự kiện Jump khi hành động được thực hiện [3]
        onFoot.Jump.performed += ctx =>
        {
            if (CanAct && motor != null)
            {
                motor.Jump();
            }
        };

        onFoot.Crouch.performed += ctx =>
        {
            if (CanAct && motor != null)
            {
                motor.Crouch();
            }
        };

        onFoot.Sprint.performed += ctx =>
        {
            if (CanAct && motor != null)
            {
                motor.SetSprinting(true);
            }
        };

        onFoot.Interact.performed += ctx =>
        {
            if (CanAct && interact != null)
            {
                interact.ProcessInteract();
            }
        };
        onFoot.Sprint.canceled += ctx => { if (motor != null) motor.SetSprinting(false); };
        onFoot.Reload.performed += ctx => { if (CanAct && weapon != null) weapon.BeginReload(); };
    }

    void Update()
    {
        if (!CanAct) return;
        if (weapon == null) weapon = GetComponent<PlayerWeapon>();
        if (weapon != null && onFoot.Fire.IsPressed()) weapon.TryFire();
        // Yêu cầu PlayerMotor xử lý di chuyển bằng giá trị từ Movement Action [4]
        if (motor != null)
        {
            motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
        }
    }

    void LateUpdate()
    {
        if (!CanAct) return;
        // Yêu cầu PlayerLook xử lý nhìn xung quanh [2]
        if (look != null)
        {
            look.ProcessLook(onFoot.Look.ReadValue<Vector2>(), onFoot.Look.activeControl?.device is Gamepad, IsAiming ? 0.55f : 1f);
        }
    }

    private void OnEnable()
    {
        onFoot.Enable(); // Kích hoạt Action Map [1]
    }

    private void OnDisable()
    {
        if (motor != null) motor.SetSprinting(false);
        onFoot.Disable(); // Hủy kích hoạt Action Map [5]
    }
    private void OnDestroy() => playerInput?.Dispose();
}
