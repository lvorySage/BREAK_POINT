using UnityEngine;
using UnityEngine.UI;

namespace BreachPoint
{
    /// <summary>Firewall Arena HUD. Attacker panel is always on the left (attacker zone), defender on the right.</summary>
    public sealed class FirewallHud
    {
        readonly FirewallArena _g;
        readonly Session _s;
        readonly Canvas _canvas;

        Text _atkTitle, _defTitle, _timer, _half, _stolen, _shieldName, _defStats, _alert, _big, _stunText;
        RectTransform _stolenFill;
        Image _atkEdge, _defEdge;
        readonly Image[] _atkChip = new Image[Attacks.Count], _defChip = new Image[Attacks.Count];
        readonly RectTransform[] _atkCd = new RectTransform[Attacks.Count];
        readonly Text[] _atkChipText = new Text[Attacks.Count], _defChipText = new Text[Attacks.Count];
        float _alertUntil, _bigFadeAt = -1f, _bigFadeLen;

        public FirewallHud(FirewallArena g, Session s)
        {
            _g = g;
            _s = s;
            _canvas = Ui.Canvas("FirewallHUD", 5);
            var root = _canvas.transform;

            // Attacker panel (top-left)
            var atk = Ui.Panel(root, Palette.Panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -232), new Vector2(784, -24), "Attacker");
            _atkEdge = Ui.Panel(atk.transform, Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero, "Edge");
            _atkTitle = Ui.Label(atk.transform, "", 34, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -56), new Vector2(-20, -12), Fonts.Title);
            Ui.Label(atk.transform, "DATA STOLEN", 22, Palette.Muted, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -92), new Vector2(-20, -62));
            _stolen = Ui.Label(atk.transform, "0%", 30, Palette.Bad, TextAnchor.MiddleRight, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -94), new Vector2(-20, -60));
            _stolenFill = Ui.Bar(atk.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -120), new Vector2(-20, -98), Palette.Bad);
            for (int i = 0; i < Attacks.Count; i++)
            {
                float x0 = 20 + i * 182;
                _atkChip[i] = Ui.Panel(atk.transform, new Color(0, 0, 0, 0.5f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0, -196), new Vector2(x0 + 174, -134), "Chip");
                _atkCd[i] = Ui.Panel(_atkChip[i].transform, new Color(0, 0, 0, 0.6f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Cooldown").rectTransform;
                _atkChipText[i] = Ui.Fill(_atkChip[i].transform, "", 21, Color.white);
            }
            _stunText = Ui.Label(atk.transform, "", 26, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -40), new Vector2(0, -4));

            // Defender panel (top-right)
            var def = Ui.Panel(root, Palette.Panel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-784, -232), new Vector2(-24, -24), "Defender");
            _defEdge = Ui.Panel(def.transform, Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero, "Edge");
            _defTitle = Ui.Label(def.transform, "", 34, Color.white, TextAnchor.UpperRight, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -56), new Vector2(-20, -12), Fonts.Title);
            Ui.Label(def.transform, "SHIELD", 22, Palette.Muted, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -92), new Vector2(-20, -62));
            _shieldName = Ui.Label(def.transform, "", 34, Color.white, TextAnchor.MiddleRight, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -100), new Vector2(-20, -58));
            _defStats = Ui.Label(def.transform, "", 22, Palette.Muted, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -128), new Vector2(-20, -100));
            for (int i = 0; i < Attacks.Count; i++)
            {
                float x0 = 20 + i * 182;
                _defChip[i] = Ui.Panel(def.transform, new Color(0, 0, 0, 0.5f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0, -196), new Vector2(x0 + 174, -134), "Chip");
                _defChipText[i] = Ui.Fill(_defChip[i].transform, "", 21, Color.white);
            }

            // Timer (top-centre)
            var tbox = Ui.Panel(root, Palette.Panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-150, -150), new Vector2(150, -24), "Timer");
            _timer = Ui.Label(tbox.transform, "1:00", 72, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.3f), new Vector2(1, 1), default, default, Fonts.Title);
            _half = Ui.Label(tbox.transform, "", 22, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0.32f));

            // Legend (bottom-centre): the core lesson, always visible.
            var legend = Ui.Panel(root, Palette.Panel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-900, 20), new Vector2(900, 84), "Legend");
            for (int i = 0; i < Attacks.Count; i++)
            {
                var t = (AttackType)i;
                Ui.Label(legend.transform, $"{Attacks.PadLabels[i]}  {Attacks.Name(t)} → {Attacks.Defense(t)}", 26, Attacks.Color(t), TextAnchor.MiddleCenter,
                    new Vector2(i / (float)Attacks.Count, 0), new Vector2((i + 1) / (float)Attacks.Count, 1));
            }

            _alert = Ui.Label(root, "", 40, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(1f, 0), new Vector2(0, 110), new Vector2(-40, 170));
            _big = Ui.Label(root, "", 220, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.3f), new Vector2(1, 0.75f), default, default, Fonts.Title);
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
            _atkTitle.text = $"{a.Name} · ATTACKER";
            _atkTitle.color = a.Color;
            _atkEdge.color = a.Color;
            _defTitle.text = $"DEFENDER · {d.Name}";
            _defTitle.color = d.Color;
            _defEdge.color = d.Color;
            _half.text = _g.Half == 0 ? "HALF 1 / 2 · ROLES SWAP NEXT" : "HALF 2 / 2 · FINAL";
            for (int i = 0; i < Attacks.Count; i++)
            {
                var t = (AttackType)i;
                _atkChipText[i].text = $"{a.Controls.ButtonName(i)}  {(t == AttackType.Password ? "PASSWORD" : Attacks.Name(t))}";
                _atkChipText[i].color = Attacks.Color(t);
                _defChipText[i].text = $"{d.Controls.ButtonName(i)}  {Attacks.Defense(t)}";
            }
            _alert.text = "";
            Refresh();
        }

        public void Refresh()
        {
            float tl = Mathf.Max(0, _g.TimeLeft);
            _timer.text = $"{(int)tl / 60}:{(int)tl % 60:00}";
            _timer.color = tl <= 10 ? Color.Lerp(Palette.Bad, Color.white, Mathf.PingPong(Time.time * 3f, 1f)) : Color.white;

            _stolen.text = $"{_g.StolenThisHalf:0}%";
            Ui.SetBar(_stolenFill, _g.StolenThisHalf / 100f);
            for (int i = 0; i < Attacks.Count; i++)
            {
                float cd = _g.CooldownLeft((AttackType)i);
                float max = Mathf.Max(Attacks.Cooldown((AttackType)i), FirewallArena.GlobalCooldown);
                _atkCd[i].anchorMax = new Vector2(1, Mathf.Clamp01(cd / max));
                _atkChip[i].color = Palette.A(Attacks.Color((AttackType)i) * 0.25f, cd > 0 ? 0.5f : 0.9f);
            }
            _stunText.text = _g.Stun > 0 ? "STUNNED: your attack bounced back!" : "";

            var sh = _g.Shield;
            _shieldName.text = Attacks.Defense(sh);
            _shieldName.color = Attacks.Color(sh);
            for (int i = 0; i < Attacks.Count; i++)
            {
                bool on = (int)sh == i;
                var c = Attacks.Color((AttackType)i);
                _defChip[i].color = on ? Palette.A(c * 0.55f, 0.95f) : new Color(0, 0, 0, 0.5f);
                _defChipText[i].color = on ? Color.white : Palette.A(c, 0.7f);
            }
            _defStats.text = $"BLOCKED {_g.Blocks[_g.DefenderIndex]}   ·   WRONG {_g.WrongDefenses[_g.DefenderIndex]}";

            if (Time.time > _alertUntil) _alert.text = "";
            else _alert.color = Palette.A(_alert.color, Mathf.Clamp01((_alertUntil - Time.time) * 2f));

            if (_bigFadeAt > 0)
            {
                float k = (Time.time - _bigFadeAt) / _bigFadeLen;
                _big.color = Palette.A(_big.color, 1f - Mathf.Clamp01(k));
                if (k >= 1f) { _big.text = ""; _bigFadeAt = -1f; }
            }
        }

        /// <summary>Defender-side warning naming the attack and the button that stops it.</summary>
        public void Incoming(AttackType t, PlayerControls defender)
        {
            Alert($"⚠ INCOMING {Attacks.Name(t)}  →  {defender.ButtonName((int)t)} {Attacks.Defense(t)}", Attacks.Color(t));
        }

        public void Alert(string text, Color c)
        {
            _alert.text = text;
            _alert.color = c;
            _alertUntil = Time.time + 1.3f;
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
