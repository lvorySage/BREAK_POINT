using UnityEngine;
using UnityEngine.UI;

namespace BreachPoint
{
    /// <summary>Data Heist Ball HUD: attacker panel left (exfil side), defender panel right, tools with cooldowns.</summary>
    public sealed class DataHeistHud
    {
        readonly DataHeistBall _g;
        readonly Session _s;
        readonly Canvas _canvas;
        Text _atkTitle, _defTitle, _timer, _half, _stolen, _defStats, _alert, _big;
        Text _encText, _lockText, _atkKick, _defKick;
        RectTransform _stolenFill, _encCd, _lockCd, _atkKickCd, _defKickCd;
        Image _atkEdge, _defEdge, _encChip, _lockChip;
        float _alertUntil, _bigFadeAt = -1f, _bigFadeLen;

        public DataHeistHud(DataHeistBall g, Session s)
        {
            _g = g;
            _s = s;
            _canvas = Ui.Canvas("DataHeistHUD", 5);
            var root = _canvas.transform;

            // Attacker (top-left)
            var atk = Ui.Panel(root, Palette.Panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -232), new Vector2(664, -24), "Attacker");
            _atkEdge = Ui.Panel(atk.transform, Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero, "Edge");
            _atkTitle = Ui.Label(atk.transform, "", 34, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -56), new Vector2(-20, -12), Fonts.Title);
            Ui.Label(atk.transform, "DATA STOLEN", 22, Palette.Muted, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -92), new Vector2(-20, -62));
            _stolen = Ui.Label(atk.transform, "0%", 30, Palette.Bad, TextAnchor.MiddleRight, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -94), new Vector2(-20, -60));
            _stolenFill = Ui.Bar(atk.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -120), new Vector2(-20, -98), Palette.Bad);
            _atkKick = Chip(atk.transform, 0, "", Palette.Text, out _atkKickCd, out _);
            Chip(atk.transform, 1, "RT  BOOST", Palette.Text, out _, out _);
            Ui.Label(atk.transform, "Push the DATA into EXFIL ←", 22, Palette.Muted, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -40), new Vector2(0, -4));

            // Defender (top-right)
            var def = Ui.Panel(root, Palette.Panel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-664, -232), new Vector2(-24, -24), "Defender");
            _defEdge = Ui.Panel(def.transform, Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero, "Edge");
            _defTitle = Ui.Label(def.transform, "", 34, Color.white, TextAnchor.UpperRight, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -56), new Vector2(-20, -12), Fonts.Title);
            _defStats = Ui.Label(def.transform, "", 24, Palette.Text, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -120), new Vector2(-20, -62));
            _defKick = Chip(def.transform, 0, "", Palette.Text, out _defKickCd, out _);
            _encText = Chip(def.transform, 1, "", Palette.Gold, out _encCd, out _encChip);
            _lockText = Chip(def.transform, 2, "", Palette.Good, out _lockCd, out _lockChip);
            Ui.Label(def.transform, "Return the DATA to the VAULT →", 22, Palette.Muted, TextAnchor.MiddleRight, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -40), new Vector2(0, -4));

            // Timer
            var tbox = Ui.Panel(root, Palette.Panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-150, -150), new Vector2(150, -24), "Timer");
            _timer = Ui.Label(tbox.transform, "1:00", 72, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.3f), new Vector2(1, 1), default, default, Fonts.Title);
            _half = Ui.Label(tbox.transform, "", 22, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0.32f));

            // Legend: the lesson, always visible.
            var legend = Ui.Panel(root, Palette.Panel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-900, 20), new Vector2(900, 84), "Legend");
            Ui.Label(legend.transform, Palette.Paint("X/□ ENCRYPT", Palette.Gold) + " = stolen data is useless", 26, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(0.5f, 1));
            Ui.Label(legend.transform, Palette.Paint("Y/△ DLP LOCK", Palette.Good) + " = data can't leave the network", 26, Palette.Text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(1, 1));

            _alert = Ui.Label(root, "", 38, Color.white, TextAnchor.MiddleCenter, new Vector2(0.25f, 0), new Vector2(0.75f, 0), new Vector2(0, 100), new Vector2(0, 160));
            _big = Ui.Label(root, "", 220, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.3f), new Vector2(1, 0.75f), default, default, Fonts.Title);
        }

        static Text Chip(Transform parent, int slot, string text, Color c, out RectTransform cooldown, out Image chip)
        {
            float x0 = 20 + slot * 205;
            chip = Ui.Panel(parent, new Color(0, 0, 0, 0.5f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0, -196), new Vector2(x0 + 195, -134), "Chip");
            cooldown = Ui.Panel(chip.transform, new Color(0, 0, 0, 0.6f), Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero, "Cooldown").rectTransform;
            return Ui.Fill(chip.transform, text, 22, c);
        }

        public void SetVisible(bool on) => _canvas.enabled = on;

        public void Destroy()
        {
            if (_canvas != null) Object.Destroy(_canvas.gameObject);
        }

        public void OnHalfStart()
        {
            var a = _s.Players[_g.AttackerIndex];
            var d = _s.Players[_g.DefenderIndex];
            _atkTitle.text = $"{a.Display} · ATTACKER";
            _atkTitle.color = a.Color;
            _atkEdge.color = a.Color;
            _defTitle.text = $"DEFENDER · {d.Display}";
            _defTitle.color = d.Color;
            _defEdge.color = d.Color;
            _half.text = _g.Half == 0 ? "HALF 1 / 2 · ROLES SWAP NEXT" : "HALF 2 / 2 · FINAL";
            _atkKick.text = $"{a.Controls.ButtonName(0)}  KICK";
            _defKick.text = $"{d.Controls.ButtonName(0)}  KICK";
            _encText.text = $"{d.Controls.ButtonName(2)}  ENCRYPT";
            _lockText.text = $"{d.Controls.ButtonName(3)}  DLP LOCK";
            _alert.text = "";
            Refresh();
        }

        static void Cd(RectTransform r, float left, float total) => r.anchorMax = new Vector2(1, Mathf.Clamp01(left / total));

        public void Refresh()
        {
            float tl = Mathf.Max(0, _g.TimeLeft);
            _timer.text = $"{(int)tl / 60}:{(int)tl % 60:00}";
            _timer.color = tl <= 10 ? Color.Lerp(Palette.Bad, Color.white, Mathf.PingPong(Time.time * 3f, 1f)) : Color.white;
            _stolen.text = $"{_g.StolenThisHalf:0}%";
            Ui.SetBar(_stolenFill, _g.StolenThisHalf / 100f);
            int d = _g.DefenderIndex;
            _defStats.text = $"SECURED {_g.Recoveries[d]}   ·   SAVES {_g.EncryptedSaves[d] + _g.LockSaves[d]}";
            Cd(_atkKickCd, _g.KickCooldownLeft(_g.AttackerIndex), DataHeistBall.KickCooldown);
            Cd(_defKickCd, _g.KickCooldownLeft(d), DataHeistBall.KickCooldown);
            Cd(_encCd, _g.EncryptCooldownLeft, DataHeistBall.EncryptCooldown);
            Cd(_lockCd, _g.LockCooldownLeft, DataHeistBall.LockCooldown);
            _encChip.color = _g.EncryptedLeft > 0 ? Palette.A(Palette.Gold * 0.6f, 0.95f) : new Color(0, 0, 0, 0.5f);
            _lockChip.color = _g.LockLeft > 0 ? Palette.A(Palette.Good * 0.5f, 0.95f) : new Color(0, 0, 0, 0.5f);

            if (Time.time > _alertUntil) _alert.text = "";
            if (_bigFadeAt > 0)
            {
                float k = (Time.time - _bigFadeAt) / _bigFadeLen;
                _big.color = Palette.A(_big.color, 1f - Mathf.Clamp01(k));
                if (k >= 1f) { _big.text = ""; _bigFadeAt = -1f; }
            }
        }

        public void Alert(string text, Color c)
        {
            _alert.text = text;
            _alert.color = c;
            _alertUntil = Time.time + 1.6f;
        }

        public void Big(string text, Color c)
        {
            _bigFadeAt = -1f;
            _big.text = text;
            _big.color = c;
        }

        public void BigFade(float seconds)
        {
            _bigFadeAt = Time.time;
            _bigFadeLen = seconds;
        }
    }
}
