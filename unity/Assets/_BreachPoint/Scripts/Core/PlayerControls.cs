using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace BreachPoint
{
    public enum ControlKind { Gamepad, KeyboardLeft, KeyboardRight, Cpu }

    /// <summary>
    /// One player's input, whatever the device. Actions 0-3 are the four face buttons
    /// (A·Cross / B·Circle / X·Square / Y·Triangle on pads; F G H T or 1 2 3 4 on keyboards).
    /// CPU players are driven by writing the Cpu* fields.
    /// </summary>
    public sealed class PlayerControls
    {
        public readonly ControlKind Kind;
        public readonly Gamepad Pad;

        public Vector2 CpuMove;
        public int CpuAction = -1;
        public bool CpuBoost;

        public PlayerControls(ControlKind kind, Gamepad pad = null)
        {
            Kind = kind;
            Pad = pad;
        }

        public bool IsCpu => Kind == ControlKind.Cpu;
        public bool Connected => Kind != ControlKind.Gamepad || (Pad != null && Pad.added);

        public bool IsPlayStation
        {
            get
            {
                if (Pad == null) return false;
                if (Pad is DualShockGamepad) return true;
                var n = (Pad.displayName + " " + Pad.description.product).ToLowerInvariant();
                return n.Contains("dualsense") || n.Contains("dualshock") || n.Contains("wireless controller");
            }
        }

        public string DeviceLabel => Kind switch
        {
            ControlKind.Gamepad => Pad != null ? Pad.displayName : "Gamepad",
            ControlKind.KeyboardLeft => "Keyboard · WASD + F G H T",
            ControlKind.KeyboardRight => "Keyboard · Arrows + 1 2 3 4",
            _ => "CPU opponent",
        };

        static readonly string[] Xbox = { "A", "B", "X", "Y" };
        static readonly string[] PlayStation = { "✕", "○", "□", "△" };
        static readonly string[] KeysLeft = { "F", "G", "H", "T" };
        static readonly string[] KeysRight = { "1", "2", "3", "4" };

        public string ButtonName(int action) => Kind switch
        {
            ControlKind.Gamepad => (IsPlayStation ? PlayStation : Xbox)[action],
            ControlKind.KeyboardLeft => KeysLeft[action],
            ControlKind.KeyboardRight => KeysRight[action],
            _ => Xbox[action],
        };

        public string ConfirmName => Kind switch
        {
            ControlKind.Gamepad => IsPlayStation ? "✕" : "A",
            ControlKind.KeyboardLeft => "F",
            ControlKind.KeyboardRight => "1",
            _ => "",
        };

        static Keyboard Kb => Keyboard.current;
        static bool Held(Key k) => Kb != null && Kb[k].isPressed;
        static bool Down(Key k) => Kb != null && Kb[k].wasPressedThisFrame;

        static Vector2 KeyAxes(Key up, Key down, Key left, Key right)
        {
            var v = new Vector2((Held(right) ? 1 : 0) - (Held(left) ? 1 : 0), (Held(up) ? 1 : 0) - (Held(down) ? 1 : 0));
            return v.sqrMagnitude > 1 ? v.normalized : v;
        }

        public Vector2 Move
        {
            get
            {
                switch (Kind)
                {
                    case ControlKind.Gamepad:
                        if (Pad == null) return Vector2.zero;
                        var s = Pad.leftStick.ReadValue();
                        if (s.sqrMagnitude < 0.04f) s = Pad.dpad.ReadValue();
                        return Vector2.ClampMagnitude(s, 1f);
                    case ControlKind.KeyboardLeft: return KeyAxes(Key.W, Key.S, Key.A, Key.D);
                    case ControlKind.KeyboardRight: return KeyAxes(Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow);
                    default: return Vector2.ClampMagnitude(CpuMove, 1f);
                }
            }
        }

        public Vector2 Aim => Kind == ControlKind.Gamepad && Pad != null ? Pad.rightStick.ReadValue() : Vector2.zero;

        public bool ActionDown(int i)
        {
            switch (Kind)
            {
                case ControlKind.Gamepad:
                    if (Pad == null) return false;
                    return i switch
                    {
                        0 => Pad.buttonSouth.wasPressedThisFrame,
                        1 => Pad.buttonEast.wasPressedThisFrame,
                        2 => Pad.buttonWest.wasPressedThisFrame,
                        _ => Pad.buttonNorth.wasPressedThisFrame,
                    };
                case ControlKind.KeyboardLeft:
                    return Down(i == 0 ? Key.F : i == 1 ? Key.G : i == 2 ? Key.H : Key.T);
                case ControlKind.KeyboardRight:
                    return i == 0 ? Down(Key.Numpad1) || Down(Key.Digit1)
                         : i == 1 ? Down(Key.Numpad2) || Down(Key.Digit2)
                         : i == 2 ? Down(Key.Numpad3) || Down(Key.Digit3)
                         : Down(Key.Numpad4) || Down(Key.Digit4);
                default:
                    if (CpuAction != i) return false;
                    CpuAction = -1;
                    return true;
            }
        }

        public bool ConfirmDown => Kind switch
        {
            ControlKind.Gamepad => Pad != null && Pad.buttonSouth.wasPressedThisFrame, // Start is reserved for pause
            ControlKind.KeyboardLeft => Down(Key.F) || Down(Key.Space),
            ControlKind.KeyboardRight => Down(Key.Numpad1) || Down(Key.Digit1) || Down(Key.Enter) || Down(Key.NumpadEnter),
            _ => false,
        };

        public bool BackDown => Kind switch
        {
            ControlKind.Gamepad => Pad != null && Pad.buttonEast.wasPressedThisFrame,
            ControlKind.KeyboardLeft => Down(Key.G),
            ControlKind.KeyboardRight => Down(Key.Numpad2) || Down(Key.Digit2),
            _ => false,
        };

        public bool PauseDown => Kind switch
        {
            ControlKind.Gamepad => Pad != null && Pad.startButton.wasPressedThisFrame,
            ControlKind.KeyboardLeft or ControlKind.KeyboardRight => Down(Key.Escape) || Down(Key.P),
            _ => false,
        };

        public bool Boost => Kind switch
        {
            ControlKind.Gamepad => Pad != null && (Pad.rightTrigger.ReadValue() > 0.5f || Pad.rightShoulder.isPressed || Pad.leftShoulder.isPressed),
            ControlKind.KeyboardLeft => Held(Key.LeftShift),
            ControlKind.KeyboardRight => Held(Key.RightShift),
            _ => CpuBoost,
        };
    }
}
