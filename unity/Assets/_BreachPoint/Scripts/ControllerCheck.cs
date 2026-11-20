using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace BreachPoint
{
    /// <summary>
    /// Booth hardware check: lists every connected gamepad as Player 1 / Player 2,
    /// shows live sticks, triggers and buttons, rumbles on South (A / Cross), and
    /// colours DualSense / DualShock light bars red (P1) and blue (P2).
    /// Open via the menu: Breach Point → Controller Check Scene, then press Play.
    /// </summary>
    public class ControllerCheck : MonoBehaviour
    {
        static readonly Color Red = new Color(0.98f, 0.23f, 0.36f);
        static readonly Color Blue = new Color(0.22f, 0.74f, 0.97f);

        readonly Dictionary<Gamepad, float> _rumbleUntil = new Dictionary<Gamepad, float>();
        GUIStyle _title, _body, _box;

        void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;

        void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var g in Gamepad.all) g.ResetHaptics();
        }

        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Gamepad && (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected))
                Debug.Log($"[ControllerCheck] Connected: {device.displayName}");
            if (device is Gamepad && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected))
                Debug.Log($"[ControllerCheck] Disconnected: {device.displayName}");
        }

        void Update()
        {
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                var g = pads[i];
                if (g is DualShockGamepad ds) ds.SetLightBarColor(i == 0 ? Red : i == 1 ? Blue : Color.white);

                if (g.buttonSouth.wasPressedThisFrame)
                {
                    g.SetMotorSpeeds(0.35f, 0.8f);
                    _rumbleUntil[g] = Time.unscaledTime + 0.35f;
                }
                if (_rumbleUntil.TryGetValue(g, out var until) && Time.unscaledTime > until)
                {
                    g.SetMotorSpeeds(0f, 0f);
                    _rumbleUntil.Remove(g);
                }
            }
        }

        static string Family(Gamepad g)
        {
            var n = (g.displayName + " " + g.description.product).ToLowerInvariant();
            if (g is DualShockGamepad || n.Contains("dualsense") || n.Contains("dualshock") || n.Contains("wireless controller"))
                return "PlayStation";
            if (n.Contains("xbox") || n.Contains("xinput")) return "Xbox";
            return "Other";
        }

        static string Pressed(Gamepad g)
        {
            var sb = new StringBuilder();
            foreach (var c in g.allControls)
            {
                if (c is UnityEngine.InputSystem.Controls.ButtonControl b && !c.synthetic && b.isPressed)
                    sb.Append(c.displayName).Append("  ");
            }
            return sb.Length > 0 ? sb.ToString() : "—";
        }

        void Styles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
            _box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(18, 18, 14, 14) };
        }

        void OnGUI()
        {
            Styles();
            float s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
            float w = Screen.width / s;

            GUI.Label(new Rect(40, 24, w - 80, 50), "BREACH POINT · CONTROLLER CHECK", _title);
            var pads = Gamepad.all;
            GUI.Label(new Rect(40, 76, w - 80, 34),
                pads.Count == 0
                    ? "No gamepads found. Connect an Xbox or PS5 controller (USB or Bluetooth)."
                    : $"{pads.Count} gamepad(s) connected. Press A / Cross to rumble.", _body);

            float colW = (w - 120) / 2f;
            for (int i = 0; i < pads.Count && i < 4; i++)
            {
                var g = pads[i];
                float x = 40 + (i % 2) * (colW + 40), y = 130 + (i / 2) * 430;
                var team = i == 0 ? Red : i == 1 ? Blue : Color.white;
                var prev = GUI.color;
                GUI.color = team;
                GUI.Box(new Rect(x, y, colW, 400), GUIContent.none, _box);
                GUI.color = prev;

                string role = i == 0 ? "PLAYER 1 · RED TEAM" : i == 1 ? "PLAYER 2 · BLUE TEAM" : $"PLAYER {i + 1} · (spare)";
                var ls = g.leftStick.ReadValue();
                var rs = g.rightStick.ReadValue();
                string text =
                    $"<b>{role}</b>\n" +
                    $"{g.displayName}  ({Family(g)})\n\n" +
                    $"Left stick   {ls.x,6:0.00} {ls.y,6:0.00}\n" +
                    $"Right stick  {rs.x,6:0.00} {rs.y,6:0.00}\n" +
                    $"Triggers     L {g.leftTrigger.ReadValue():0.00}   R {g.rightTrigger.ReadValue():0.00}\n\n" +
                    $"Pressed: {Pressed(g)}";
                _body.richText = true;
                GUI.Label(new Rect(x + 22, y + 18, colW - 44, 370), text, _body);
            }
        }
    }
}
