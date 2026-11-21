using UnityEngine;
using UnityEngine.UI;

namespace BreachPoint
{
    /// <summary>Phish Race HUD: one panel per player (Red left, Blue right) with lap, place, points and their tool.</summary>
    public sealed class PhishRaceHud
    {
        readonly PhishRace _g;
        readonly Session _s;
        readonly Canvas _canvas;
        readonly Text[] _title = new Text[2], _lap = new Text[2], _place = new Text[2], _pts = new Text[2], _tool = new Text[2], _boost = new Text[2], _alert = new Text[2];
        readonly RectTransform[] _toolCd = new RectTransform[2], _progFill = new RectTransform[2];
        readonly Image[] _edge = new Image[2];
        readonly float[] _alertUntil = new float[2];
        Text _timer, _half, _big;
        float _bigFadeAt = -1f, _bigFadeLen;

        public PhishRaceHud(PhishRace g, Session s)
        {
            _g = g;
            _s = s;
            _canvas = Ui.Canvas("PhishRaceHUD", 5);
            var root = _canvas.transform;

            for (int i = 0; i < 2; i++)
            {
                bool left = i == 0;
                var aMin = new Vector2(left ? 0 : 1, 1);
                var panel = Ui.Panel(root, Palette.Panel, aMin, aMin,
                    left ? new Vector2(24, -232) : new Vector2(-624, -232), left ? new Vector2(624, -24) : new Vector2(-24, -24), "Player" + i);
                var t = panel.transform;
                var align = left ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
                _edge[i] = Ui.Panel(t, Color.white, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero, "Edge");
                _title[i] = Ui.Label(t, "", 34, Color.white, align, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -56), new Vector2(-20, -12), Fonts.Title);
                _lap[i] = Ui.Label(t, "", 28, Palette.Text, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -96), new Vector2(-20, -62), Fonts.Mono);
                _place[i] = Ui.Label(t, "", 30, Palette.Gold, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -96), new Vector2(-20, -62), Fonts.Title);
                _pts[i] = Ui.Label(t, "", 28, Palette.Gold, TextAnchor.MiddleRight, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -96), new Vector2(-20, -62), Fonts.Mono);
                _progFill[i] = Ui.Bar(t, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -122), new Vector2(-20, -106), Palette.Of(i == 0 ? Team.Red : Team.Blue));
                _boost[i] = Chip(t, 0, "", Palette.Text, out _);
                _tool[i] = Chip(t, 1, "", Palette.Gold, out _toolCd[i]);
                _alert[i] = Ui.Label(t, "", 26, Palette.Bad, left ? TextAnchor.UpperLeft : TextAnchor.UpperRight, new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, -80), new Vector2(-4, -6));
            }

            var tbox = Ui.Panel(root, Palette.Panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-150, -150), new Vector2(150, -24), "Timer");
            _timer = Ui.Label(tbox.transform, "1:00", 72, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.3f), new Vector2(1, 1), default, default, Fonts.Title);
            _half = Ui.Label(tbox.transform, "", 22, Palette.Muted, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(1, 0.32f));

            var legend = Ui.Panel(root, Palette.Panel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-900, 20), new Vector2(900, 84), "Legend");
            Ui.Label(legend.transform,
                $"Pick the lane with the {Palette.Paint("REAL link", Palette.Good)}: the domain must really be {Palette.Paint("omanlng.com", Palette.Good)}.  " +
                $"Look-alikes = {Palette.Paint("PHISHED", Palette.Bad)}", 26, Palette.Text, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

            _big = Ui.Label(root, "", 150, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.3f), new Vector2(1, 0.75f), default, default, Fonts.Title);
        }

        static Text Chip(Transform parent, int slot, string text, Color c, out RectTransform cooldown)
        {
            float x0 = 20 + slot * 285;
            var chip = Ui.Panel(parent, new Color(0, 0, 0, 0.5f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0, -196), new Vector2(x0 + 275, -134), "Chip");
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
            for (int i = 0; i < 2; i++)
            {
                var p = _s.Players[i];
                bool atk = i == _g.AttackerIndex;
                _title[i].text = i == 0 ? $"{p.Name} · {(atk ? "PHISHER" : "TARGET")}" : $"{(atk ? "PHISHER" : "TARGET")} · {p.Name}";
                _title[i].color = p.Color;
                _edge[i].color = p.Color;
                _boost[i].text = $"{p.Controls.ButtonName(0)} / RT  BOOST";
                _tool[i].text = $"{p.Controls.ButtonName(2)}  {(atk ? "DROP BAIT" : "REPORT BAIT")}";
                _tool[i].color = atk ? new Color(1f, 0.45f, 0.8f) : Palette.Blue;
                _alert[i].text = "";
            }
            _half.text = _g.Half == 0 ? "HEAT 1 / 2 · ROLES SWAP NEXT" : "HEAT 2 / 2 · FINAL";
            Refresh();
        }

        static void Cd(RectTransform r, float left, float total) => r.anchorMax = new Vector2(1, Mathf.Clamp01(left / total));

        public void Refresh()
        {
            float tl = Mathf.Max(0, _g.TimeLeft);
            _timer.text = $"{(int)tl / 60}:{(int)tl % 60:00}";
            _timer.color = tl <= 10 ? Color.Lerp(Palette.Bad, Color.white, Mathf.PingPong(Time.time * 3f, 1f)) : Color.white;
            for (int i = 0; i < 2; i++)
            {
                bool fin = _g.Finished(i);
                _lap[i].text = fin ? "FINISHED" : $"LAP {_g.Lap(i)}/{PhishRace.Laps}";
                int place = _g.Place(i);
                _place[i].text = place == 1 ? "1ST" : "2ND";
                _place[i].color = place == 1 ? Palette.Gold : Palette.Muted;
                _pts[i].text = $"{_g.MatchPoints(i)} PTS";
                Ui.SetBar(_progFill[i], Mathf.Clamp01(_g.Progress(i) / (PhishRace.Laps * PhishRace.Perimeter)));
                bool atk = i == _g.AttackerIndex;
                Cd(_toolCd[i], atk ? _g.LureCooldownLeft : _g.ReportCooldownLeft, atk ? PhishRace.LureCooldown : PhishRace.ReportCooldown);
                if (_g.StallLeft(i) > 0) { _alert[i].color = Palette.Bad; if (Time.time > _alertUntil[i]) _alert[i].text = "PHISHED! STALLED…"; }
                else if (Time.time > _alertUntil[i]) _alert[i].text = "";
            }
            if (_bigFadeAt > 0)
            {
                float k = (Time.time - _bigFadeAt) / _bigFadeLen;
                _big.color = Palette.A(_big.color, 1f - Mathf.Clamp01(k));
                if (k >= 1f) { _big.text = ""; _bigFadeAt = -1f; }
            }
        }

        public void Alert(string text, Color c, int player)
        {
            _alert[player].text = text;
            _alert[player].color = c;
            _alertUntil[player] = Time.time + 2.6f;
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
