using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// Mini-game 3. Both ships race laps of a rooftop track. Each straight has a FORK: two lanes, each painted
    /// with a link. One is real (omanlng.com): speed boost. The other is a look-alike: PHISHED, the ship stalls.
    /// Each heat one player is the PHISHER (X drops bait lures behind them) and the other is the TARGET
    /// (X reports a nearby lure). Two heats; roles swap. Winner = more race points.
    /// </summary>
    public sealed partial class PhishRace : MonoBehaviour, IMiniGame
    {
        // Track: a stadium. Centreline = straights of half-length L joined by semicircles of radius R.
        public const float L = 10f, R = 8f, Width = 8f;
        public const float CarRadius = 1.2f;
        public const float ForkHalf = 6f;          // the lane divider runs x ∈ [-ForkHalf, ForkHalf] on both straights
        public const int Laps = 3;
        public static readonly float Perimeter = 2f * Mathf.PI * R + 4f * L;
        /// <summary>Distance along the track where each fork's lanes end (the gate that decides real vs phish).</summary>
        public static readonly float[] ForkExitS = { Mathf.PI * R / 2f + L + ForkHalf, 1.5f * Mathf.PI * R + 3f * L + ForkHalf };
        public const float ForkLength = ForkHalf * 2f;

        public const float SurgeSeconds = 1.3f, SurgeScale = 1.45f;
        public const float StallSeconds = 1.5f;
        public const float LureCooldown = 4.5f, LureLife = 25f, LureRadius = 1.3f;
        public const int MaxLures = 3;
        public const float ReportCooldown = 2.5f, ReportRange = 10f;
        public const int HeatWinPoints = 20, SafePoints = 5, LurePoints = 10, ReportPoints = 5;
        public const float FinishGrace = 8f;       // the other ship gets this long after the leader finishes

        public float HalfSeconds = 60f;
        public bool AiEnabled = true;
        public bool ManualStep;
        public int CpuLevel = 1;

        public string Title => "PHISH RACE";
        public MiniGameResult Result { get; private set; }

        // ---- per-match stats (index = player) ----
        public readonly int[] HeatWins = new int[2];
        public readonly int[] SafeLinks = new int[2];
        public readonly int[] Phished = new int[2];
        public readonly int[] LureHits = new int[2];
        public readonly int[] Reports = new int[2];
        public readonly int[] LuresDropped = new int[2];

        // ---- current heat ----
        public int Half { get; private set; }
        public int AttackerIndex => Half == 0 ? 0 : 1;
        public int DefenderIndex => 1 - AttackerIndex;
        public float TimeLeft { get; private set; }
        public bool Playing => _playing;
        public int HeatWinner { get; private set; } = -1;
        public float Progress(int p) => _prog[p];
        public bool Finished(int p) => _finishedAt[p] >= 0f;
        public int Lap(int p) => Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0, _prog[p]) / Perimeter) + 1, 1, Laps);
        public int Place(int p) => Finished(p) && Finished(1 - p) ? (_finishedAt[p] <= _finishedAt[1 - p] ? 1 : 2)
                                 : Finished(p) ? 1 : Finished(1 - p) ? 2 : (_prog[p] >= _prog[1 - p] ? 1 : 2);
        public float StallLeft(int p) => _stall[p];
        public float SurgeLeft(int p) => _surge[p];
        public float LureCooldownLeft => _lureCd;
        public float ReportCooldownLeft => _reportCd;
        public int LureCount => _lures.Count;
        public int HeatPoints(int p) => _heatPts[p];
        public int MatchPoints(int p) => _matchPts[p];
        public bool ForkRealIsOuter(int fork) => _realOuter[fork];
        public Links.Pair ForkLinks(int fork) => Links.All[_pair[fork]];
        public HoverVehicle Vehicle(int player) => _veh[player];

        Session _s;
        GameFlow _flow;
        readonly HoverVehicle[] _veh = new HoverVehicle[2];
        PhishRaceHud _hud;
        bool _playing, _built;
        float _time, _shake;
        readonly float[] _prog = new float[2], _lastS = new float[2];
        readonly float[] _finishedAt = { -1f, -1f };
        readonly int[] _nextGate = new int[2];
        readonly float[] _stall = new float[2], _surge = new float[2];
        readonly int[] _heatPts = new int[2], _matchPts = new int[2];
        float _lureCd, _reportCd;
        readonly bool[] _realOuter = new bool[2];
        readonly int[] _pair = new int[2];
        readonly bool[] _refreshPending = new bool[2];
        int _lastPair = -1;

        sealed class Lure
        {
            public Vector3 Pos;
            public float Age;
            public GameObject Go;
        }
        readonly List<Lure> _lures = new List<Lure>();

        public static PhishRace Create() => new GameObject("PhishRace").AddComponent<PhishRace>();

        // ================================================================== track geometry

        /// <summary>Distance along the centreline (0 = start line at the right-hand apex, counter-clockwise).</summary>
        public static float S(Vector3 p)
        {
            float x = p.x, z = p.z;
            if (x > L)
            {
                float a = Mathf.Atan2(z, x - L);
                return a >= 0 ? a * R : 1.5f * Mathf.PI * R + 4f * L + (a + Mathf.PI / 2f) * R;
            }
            if (x < -L)
            {
                float b = Mathf.Atan2(z, x + L);
                if (b < 0) b += Mathf.PI * 2f;
                return Mathf.PI * R / 2f + 2f * L + (b - Mathf.PI / 2f) * R;
            }
            return z >= 0 ? Mathf.PI * R / 2f + (L - x) : 1.5f * Mathf.PI * R + 2f * L + (x + L);
        }

        /// <summary>Point on the track at distance s, pushed <paramref name="offset"/> metres outward from the centreline.</summary>
        public static Vector3 At(float s, float offset = 0f)
        {
            s = Mathf.Repeat(s, Perimeter);
            float r = R + offset, q = Mathf.PI * R / 2f;
            if (s < q) { float a = s / R; return new Vector3(L + r * Mathf.Cos(a), 0, r * Mathf.Sin(a)); }
            s -= q;
            if (s < 2f * L) return new Vector3(L - s, 0, r);
            s -= 2f * L;
            if (s < Mathf.PI * R) { float b = Mathf.PI / 2f + s / R; return new Vector3(-L + r * Mathf.Cos(b), 0, r * Mathf.Sin(b)); }
            s -= Mathf.PI * R;
            if (s < 2f * L) return new Vector3(-L + s, 0, -r);
            s -= 2f * L;
            { float a = -Mathf.PI / 2f + s / R; return new Vector3(L + r * Mathf.Cos(a), 0, r * Mathf.Sin(a)); }
        }

        /// <summary>Signed distance outward from the centreline (+ = outer side).</summary>
        public static float Offset(Vector3 p)
        {
            var c = new Vector3(Mathf.Clamp(p.x, -L, L), 0, 0);
            return Flat(p - c).magnitude - R;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        // ================================================================== lifecycle

        public void Init(Session s)
        {
            if (_built) return;
            _built = true;
            _s = s;
            if (GameFlow.HalfSecondsOverride > 0) HalfSeconds = GameFlow.HalfSecondsOverride;
            BuildTrack();
            _veh[0] = HoverVehicle.Create(transform, Team.Red, "RedVehicle");
            _veh[1] = HoverVehicle.Create(transform, Team.Blue, "BlueVehicle");
            _hud = new PhishRaceHud(this, s);
        }

        public void StartHalf(int half, bool play = true)
        {
            Half = half;
            TimeLeft = HalfSeconds;
            _time = 0f;
            HeatWinner = -1;
            _lureCd = _reportCd = 0f;
            ClearLures();
            for (int i = 0; i < 2; i++)
            {
                _finishedAt[i] = -1f;
                _nextGate[i] = 0;
                _stall[i] = _surge[i] = 0f;
                _heatPts[i] = 0;
                // Grid: just behind the start line, side by side (the attacker takes the outside).
                float off = i == AttackerIndex ? 2f : -2f;
                var pos = At(-2.5f, off);
                _veh[i].Teleport(pos);
                _veh[i].Face(Vector3.forward);
                _lastS[i] = S(pos);
                _prog[i] = _lastS[i] - Perimeter;
            }
            for (int f = 0; f < 2; f++) RandomizeFork(f);
            _hud.OnHalfStart();
            foreach (var p in _s.Players) Haptics.LightBar(p.Controls, p.Color);
            _playing = play;
        }

        public IEnumerator Run(GameFlow flow, Session session)
        {
            _flow = flow;
            flow.InMatch = true;
            flow.OrbitCamera(false);
            HalfSeconds = Settings.HalfSeconds;
            CpuLevel = Settings.Cpu;
            Init(session);
            PlaceCamera(flow.Cam);
            _hud.SetVisible(false);

            yield return flow.ShowCard(IntroCard());
            for (int h = 0; h < 2; h++)
            {
                StartHalf(h, play: false);
                _hud.SetVisible(true);
                yield return Countdown();
                _playing = true;
                while (_playing) yield return null;
                Sfx.Play("whistle");
                Haptics.StopAll();
                yield return new WaitForSeconds(1.4f);
                if (h == 0) yield return flow.ShowCard(SwapCard());
            }

            Result = ComputeResult();
            _hud.SetVisible(false);
            Sfx.Play("win");
            if (Result.Winner >= 0) Haptics.Pulse(_s.Players[Result.Winner].Controls, 0.4f, 0.8f, 0.5f);
            yield return flow.ShowCard(ResultCard());
            flow.InMatch = false;
            Destroy(gameObject);
        }

        public void Abort()
        {
            _playing = false;
            if (this != null) Destroy(gameObject);
        }

        void OnDestroy() => _hud?.Destroy();

        IEnumerator Countdown()
        {
            for (int n = 3; n > 0; n--)
            {
                _hud.Big(n.ToString(), Palette.Gold);
                Sfx.Play("count");
                yield return new WaitForSeconds(0.75f);
            }
            _hud.Big("GO!", Palette.Good);
            Sfx.Play("go");
            foreach (var p in _s.Players) Haptics.Pulse(p.Controls, 0.3f, 0.5f, 0.2f);
            _hud.BigFade(0.6f);
        }

        void Update()
        {
            if (!ManualStep && _playing && (_flow == null || !_flow.InputBlocked)) Step(Time.deltaTime);
            else if (_built && !_playing) UpdateVisuals(Time.deltaTime);
        }

        void LateUpdate()
        {
            if (_flow == null || _flow.Cam == null) return;
            _shake = Mathf.Max(0, _shake - Time.deltaTime * 2.5f);
            PlaceCamera(_flow.Cam);
            if (_shake > 0) _flow.Cam.transform.position += Random.insideUnitSphere * (_shake * 0.3f);
        }

        public static void PlaceCamera(Camera cam)
        {
            if (cam == null) return;
            cam.transform.position = new Vector3(0f, 33f, -29f);
            cam.transform.LookAt(new Vector3(0f, 0f, 1.2f));
            cam.fieldOfView = 47f;
        }

        // ================================================================== simulation

        static readonly Rect Bounds = Rect.MinMaxRect(-40f, -30f, 40f, 30f);

        public void Step(float dt)
        {
            if (!_playing) return;
            _time += dt;
            TimeLeft -= dt;
            _lureCd = Mathf.Max(0, _lureCd - dt);
            _reportCd = Mathf.Max(0, _reportCd - dt);

            if (AiEnabled)
                for (int i = 0; i < 2; i++)
                    if (_s.Players[i].Controls.IsCpu && !Finished(i)) Ai(i);

            for (int i = 0; i < 2; i++)
            {
                var c = _s.Players[i].Controls;
                var v = _veh[i];
                _stall[i] = Mathf.Max(0, _stall[i] - dt);
                _surge[i] = Mathf.Max(0, _surge[i] - dt);
                bool fire = c.ActionDown(0);
                bool tool = c.ActionDown(2);
                c.ActionDown(1); c.ActionDown(3); // unused buttons: drain stale presses

                if (Finished(i))
                {
                    // Cruise down the track after the finish.
                    var ahead = At(S(v.transform.position) + 4f, Offset(v.transform.position)) - v.transform.position;
                    v.Drive(new Vector2(ahead.x, ahead.z).normalized * 0.4f, false, dt, Bounds);
                }
                else
                {
                    float scale = c.IsCpu ? CpuScale : 1f;
                    if (_stall[i] > 0f) scale *= 0.12f;
                    else if (_surge[i] > 0f) scale *= SurgeScale;
                    v.Drive(c.Move, c.Boost || fire, dt, Bounds, scale);
                    if (tool)
                    {
                        if (i == AttackerIndex) DropLure();
                        else Report();
                    }
                }
                if (v.Velocity.sqrMagnitude > 1f) v.Face(v.Velocity);
                ConstrainToTrack(v);
            }
            ShipCollision();
            for (int i = 0; i < 2; i++) TrackProgress(i);
            StepLures(dt);
            for (int f = 0; f < 2; f++) if (_refreshPending[f] && !Approaching(f)) RandomizeFork(f);

            UpdateVisuals(dt);
            _hud.Refresh();

            bool bothDone = Finished(0) && Finished(1);
            bool graceOver = (Finished(0) || Finished(1)) && _time - Mathf.Max(_finishedAt[0], _finishedAt[1]) > FinishGrace;
            if (bothDone || graceOver || TimeLeft <= 0f) EndHeat();
        }

        float CpuScale => CpuLevel == 0 ? 0.8f : CpuLevel == 2 ? 0.99f : 0.9f;

        void ConstrainToTrack(HoverVehicle v)
        {
            var p = v.transform.position;
            var c = new Vector3(Mathf.Clamp(p.x, -L, L), 0, 0);
            var d = Flat(p - c);
            float r = d.magnitude;
            if (r < 1e-4f) return;
            var n = d / r;
            float rMin = R - Width / 2f + CarRadius, rMax = R + Width / 2f - CarRadius;
            if (r < rMin) Push(v, n, rMin - r);
            else if (r > rMax) Push(v, -n, r - rMax);

            // Lane dividers inside the forks: a thin wall along each straight's centreline.
            p = v.transform.position;
            float side = Mathf.Sign(p.z);
            var a = new Vector3(-ForkHalf, 0, side * R);
            var b = new Vector3(ForkHalf, 0, side * R);
            var q = new Vector3(Mathf.Clamp(p.x, a.x, b.x), 0, a.z);
            var dd = Flat(p - q);
            float dist = dd.magnitude, min = CarRadius * 0.75f + 0.25f;
            if (dist < min)
            {
                var nn = dist > 1e-4f ? dd / dist : new Vector3(0, 0, Mathf.Abs(p.z) >= R ? side : -side);
                Push(v, nn, min - dist);
            }
        }

        static void Push(HoverVehicle v, Vector3 n, float amount)
        {
            v.Nudge(n * amount);
            float vn = Vector3.Dot(v.Velocity, n);
            if (vn < 0f) v.Bump(-n * vn * 1.2f);
        }

        void ShipCollision()
        {
            var a = _veh[0].transform.position;
            var b = _veh[1].transform.position;
            var d = Flat(b - a);
            float dist = d.magnitude, min = CarRadius * 1.8f;
            if (dist >= min || dist < 1e-4f) return;
            var n = d / dist;
            float overlap = min - dist;
            _veh[0].Nudge(-n * overlap * 0.5f);
            _veh[1].Nudge(n * overlap * 0.5f);
            float rel = Vector3.Dot(_veh[0].Velocity - _veh[1].Velocity, n);
            if (rel > 0)
            {
                _veh[0].Bump(-n * rel * 0.55f);
                _veh[1].Bump(n * rel * 0.55f);
                if (rel > 8f)
                {
                    Sfx.Play("stun", 0.5f);
                    foreach (var p in _s.Players) Haptics.Pulse(p.Controls, 0.5f, 0.3f, 0.15f);
                    _shake = Mathf.Max(_shake, 0.3f);
                }
            }
        }

        float GateS(int n) => Mathf.Floor(n / 2f) * Perimeter + ForkExitS[n % 2];

        void TrackProgress(int i)
        {
            var p = _veh[i].transform.position;
            float s = S(p);
            float d = s - _lastS[i];
            if (d < -Perimeter / 2f) d += Perimeter;
            if (d > Perimeter / 2f) d -= Perimeter;
            _lastS[i] = s;
            if (Finished(i)) return;
            _prog[i] += d;

            while (_nextGate[i] < Laps * 2 && _prog[i] >= GateS(_nextGate[i]))
            {
                PassFork(i, _nextGate[i] % 2, Offset(p) > 0f);
                _nextGate[i]++;
            }
            if (_prog[i] >= Laps * Perimeter) Finish(i);
        }

        /// <summary>Lane choice at a fork's exit: real link = boost, look-alike = phished.</summary>
        void PassFork(int i, int fork, bool outer)
        {
            var at = _veh[i].transform.position + Vector3.up * 2.5f;
            var pair = ForkLinks(fork);
            bool safe = outer == _realOuter[fork];
            FlashLane(fork, outer, safe);
            if (safe)
            {
                SafeLinks[i]++;
                AddPoints(i, SafePoints);
                _surge[i] = SurgeSeconds;
                Popup.Show(at, $"REAL LINK +{SafePoints}", Palette.Good, 0.09f);
                Sfx.Play("confirm", 0.8f);
                Haptics.Pulse(_s.Players[i].Controls, 0.1f, 0.4f, 0.12f);
            }
            else
            {
                Phished[i]++;
                _stall[i] = StallSeconds;
                Popup.Show(at, "PHISHED!", Palette.Bad, 0.12f);
                _hud.Alert($"{_s.Players[i].Name} clicked {pair.Fake}: {pair.Clue}", Palette.Bad, i);
                if (i == DefenderIndex) AddPoints(AttackerIndex, LurePoints / 2);
                Sfx.Play("hit");
                Haptics.Pulse(_s.Players[i].Controls, 0.8f, 1f, 0.35f);
                _shake = Mathf.Max(_shake, 0.5f);
            }
            _refreshPending[fork] = true;
            if (!Approaching(fork)) RandomizeFork(fork);
        }

        /// <summary>True while a racing ship is on the run-up to this fork (its links must not change under them).</summary>
        bool Approaching(int fork)
        {
            for (int i = 0; i < 2; i++)
            {
                if (Finished(i)) continue;
                float ahead = Mathf.Repeat(ForkExitS[fork] - _lastS[i], Perimeter);
                if (ahead < ForkLength + 16f) return true;
            }
            return false;
        }

        void RandomizeFork(int fork)
        {
            int pair;
            do pair = Random.Range(0, Links.All.Length);
            while ((pair == _lastPair || pair == _pair[1 - fork] || pair == _pair[fork]) && Links.All.Length > 3);
            _lastPair = pair;
            _pair[fork] = pair;
            _realOuter[fork] = Random.value < 0.5f;
            _refreshPending[fork] = false;
            ShowForkLinks(fork);
        }

        void Finish(int i)
        {
            _finishedAt[i] = _time;
            _prog[i] = Laps * Perimeter;
            bool first = !Finished(1 - i);
            if (first)
            {
                HeatWinner = i;
                AddPoints(i, HeatWinPoints);
                _hud.Big($"{_s.Players[i].Name} WINS THE HEAT", _s.Players[i].Color);
                _hud.BigFade(2.5f);
                Sfx.Play("win");
            }
            Popup.Show(_veh[i].transform.position + Vector3.up * 3f, first ? "FINISH! 1ST" : "FINISH 2ND", first ? Palette.Gold : Palette.Text, 0.11f);
            Haptics.Pulse(_s.Players[i].Controls, 0.3f, 0.7f, 0.3f);
        }

        void EndHeat()
        {
            TimeLeft = Mathf.Max(0, TimeLeft);
            if (HeatWinner < 0)
            {
                // Time ran out: whoever got further wins the heat.
                if (Mathf.Abs(_prog[0] - _prog[1]) > 0.5f)
                {
                    HeatWinner = _prog[0] > _prog[1] ? 0 : 1;
                    AddPoints(HeatWinner, HeatWinPoints);
                    _hud.Big($"TIME! {_s.Players[HeatWinner].Name} LEADS", _s.Players[HeatWinner].Color);
                }
                else _hud.Big("TIME!", Palette.Gold);
                _hud.BigFade(2.5f);
            }
            if (HeatWinner >= 0) HeatWins[HeatWinner]++;
            ClearLures();
            _playing = false;
            _hud.Refresh();
        }

        void AddPoints(int i, int pts)
        {
            _heatPts[i] += pts;
            _matchPts[i] += pts;
        }

        // ================================================================== lures (phisher) and reports (target)

        public bool DropLure()
        {
            if (!_playing || _lureCd > 0f) return false;
            var v = _veh[AttackerIndex];
            var back = v.Velocity.sqrMagnitude > 1f ? -v.Velocity.normalized : -(At(S(v.transform.position) + 1f) - At(S(v.transform.position))).normalized;
            var pos = Flat(v.transform.position + back * (CarRadius + LureRadius + 0.6f));
            if (_lures.Count >= MaxLures) RemoveLure(_lures[0]);
            _lures.Add(new Lure { Pos = pos, Go = BuildLure(pos, Links.Bait[Random.Range(0, Links.Bait.Length)]) });
            LuresDropped[AttackerIndex]++;
            _lureCd = LureCooldown;
            Sfx.Play("fire2", 0.7f);
            Haptics.Pulse(_s.Players[AttackerIndex].Controls, 0.1f, 0.3f, 0.1f);
            return true;
        }

        public bool Report()
        {
            if (!_playing || _reportCd > 0f) return false;
            var me = Flat(_veh[DefenderIndex].transform.position);
            Lure best = null;
            float bestD = ReportRange;
            foreach (var l in _lures)
            {
                float d = (l.Pos - me).magnitude;
                if (d < bestD) { bestD = d; best = l; }
            }
            if (best == null)
            {
                _hud.Alert("No phishing bait in range to report", Palette.Gold, DefenderIndex);
                return false;
            }
            Reports[DefenderIndex]++;
            AddPoints(DefenderIndex, ReportPoints);
            _reportCd = ReportCooldown;
            Popup.Show(best.Pos + Vector3.up * 2f, $"REPORTED +{ReportPoints}", Palette.Blue, 0.1f);
            Sfx.Play("block");
            Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.2f, 0.4f, 0.12f);
            RemoveLure(best);
            return true;
        }

        void StepLures(float dt)
        {
            var target = Flat(_veh[DefenderIndex].transform.position);
            for (int k = _lures.Count - 1; k >= 0; k--)
            {
                var l = _lures[k];
                l.Age += dt;
                if (l.Age > LureLife) { RemoveLure(l); continue; }
                if (l.Age > 0.4f && !Finished(DefenderIndex) && (l.Pos - target).magnitude < CarRadius + LureRadius)
                {
                    LureHits[AttackerIndex]++;
                    Phished[DefenderIndex]++;
                    AddPoints(AttackerIndex, LurePoints);
                    _stall[DefenderIndex] = StallSeconds;
                    Popup.Show(l.Pos + Vector3.up * 2.5f, "HOOKED BY BAIT!", Palette.Bad, 0.11f);
                    _hud.Alert($"{_s.Players[DefenderIndex].Name} took the bait! Don't click unexpected offers.", Palette.Bad, DefenderIndex);
                    Sfx.Play("hit");
                    Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.8f, 1f, 0.35f);
                    _shake = Mathf.Max(_shake, 0.5f);
                    RemoveLure(l);
                }
            }
        }

        void RemoveLure(Lure l)
        {
            if (l.Go != null) Destroy(l.Go);
            _lures.Remove(l);
        }

        void ClearLures()
        {
            for (int k = _lures.Count - 1; k >= 0; k--) RemoveLure(_lures[k]);
        }

        /// <summary>Test hooks.</summary>
        public void SetProgress(int i, float progress)
        {
            _prog[i] = progress;
            var pos = At(progress, Offset(_veh[i].transform.position));
            _veh[i].Teleport(pos);
            _lastS[i] = S(pos);
            _nextGate[i] = 0;
            while (_nextGate[i] < Laps * 2 && GateS(_nextGate[i]) <= progress) _nextGate[i]++;
        }

        public void SetFork(int fork, bool realOuter) { _realOuter[fork] = realOuter; ShowForkLinks(fork); }

        public void AddLureAt(Vector3 pos)
        {
            _lures.Add(new Lure { Pos = Flat(pos), Go = BuildLure(Flat(pos), Links.Bait[0]) });
        }

        // ================================================================== CPU

        readonly int[] _aiGate = { -1, -1 };
        readonly float[] _aiLane = new float[2];
        readonly float[] _aiToolTimer = new float[2];

        void Ai(int i)
        {
            var c = _s.Players[i].Controls;
            var v = _veh[i];
            var me = Flat(v.transform.position);

            // Decide the lane for the next fork once, with a chance of falling for the fake.
            int gate = _nextGate[i];
            float toGate = gate < Laps * 2 ? GateS(gate) - _prog[i] : 999f;
            if (gate != _aiGate[i] && toGate < ForkLength + 14f)
            {
                _aiGate[i] = gate;
                float accuracy = CpuLevel == 0 ? 0.55f : CpuLevel == 2 ? 0.92f : 0.75f;
                bool real = _realOuter[gate % 2];
                bool pickOuter = Random.value < accuracy ? real : !real;
                _aiLane[i] = pickOuter ? 2f : -2f;
            }
            float lane = toGate < ForkLength + 14f ? _aiLane[i] : Mathf.MoveTowards(Offset(me), 0f, 1.5f);

            var target = At(_lastS[i] + 5.5f, lane);
            var to = target - me;
            c.CpuMove = to.sqrMagnitude > 0.01f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
            c.CpuBoost = Random.value < 0.01f;

            _aiToolTimer[i] -= 0.02f;
            if (_aiToolTimer[i] > 0f) return;
            if (i == AttackerIndex)
            {
                // Drop bait when the target is close behind.
                float gap = _prog[i] - _prog[DefenderIndex];
                if (_lureCd <= 0f && gap > 2f && gap < 16f) { c.CpuAction = 2; _aiToolTimer[i] = Random.Range(0.5f, 1.5f); }
            }
            else
            {
                foreach (var l in _lures)
                    if ((l.Pos - me).magnitude < ReportRange * 0.8f && _reportCd <= 0f && Random.value < (CpuLevel == 0 ? 0.03f : CpuLevel == 2 ? 0.2f : 0.08f))
                    {
                        c.CpuAction = 2;
                        _aiToolTimer[i] = 0.4f;
                        break;
                    }
            }
        }

        // ================================================================== cards & result

        string Legend() =>
            $"Race {Laps} laps. On each straight the track {Palette.Paint("FORKS", Palette.Gold)}: each lane is painted with a link.\n" +
            $"{Palette.Paint("REAL link", Palette.Good)} (really omanlng.com) = speed boost  ·  {Palette.Paint("LOOK-ALIKE", Palette.Bad)} = PHISHED, you stall.\n\n" +
            $"{Palette.Paint("PHISHER", Palette.Red)}: X / □ drops bait behind you. Hook the target for +{LurePoints}.\n" +
            $"{Palette.Paint("TARGET", Palette.Blue)}: X / □ reports nearby bait (+{ReportPoints}), or just steer around it.\n\n" +
            Palette.Paint("Both: steer with the stick  ·  A / ✕ or RT / R1 boost  ·  1st over the line +" + HeatWinPoints, Palette.Muted);

        Card IntroCard() => new Card
        {
            Kicker = GameFlow.Kicker(this),
            Title = Title,
            Accent = Palette.Gold,
            Body = "Phishing links look almost real. Read them before you click!\n\n" + Legend() +
                   $"\n\nHeat 1: {_s.Players[0].Tag} is the phisher, {_s.Players[1].Tag} is the target.",
            Footer = "Both players: press A / ✕ when ready",
        };

        Card SwapCard() => new Card
        {
            Kicker = "HEAT 1 DONE",
            Title = "ROLES SWAP",
            Accent = _s.Players[1].Color,
            Body =
                (HeatWinner >= 0 ? $"{_s.Players[HeatWinner].Tag} won the heat.\n" : "The heat was a dead heat.\n") +
                $"{_s.Players[0].Tag} {_matchPts[0]} pts   ·   {_s.Players[1].Tag} {_matchPts[1]} pts\n\n" +
                $"Now {_s.Players[1].Tag} is the phisher and {_s.Players[0].Tag} is the target.",
            AutoSeconds = 7f,
        };

        public MiniGameResult ComputeResult()
        {
            var r = new MiniGameResult { Game = Title, ScoreLabel = "race points" };
            for (int i = 0; i < 2; i++)
            {
                r.Scores[i] = _matchPts[i];
                r.Points[i] = _matchPts[i];
            }
            if (_matchPts[0] != _matchPts[1]) r.Winner = _matchPts[0] > _matchPts[1] ? 0 : 1;
            else if (HeatWins[0] != HeatWins[1]) r.Winner = HeatWins[0] > HeatWins[1] ? 0 : 1;

            int phished = Phished[0] + Phished[1], reports = Reports[0] + Reports[1];
            r.Lesson = phished > 0
                ? $"Phishing caught you {phished} time(s). Before you click, read the real domain: it's the part just before the first \"/\". " +
                  "Look-alikes swap letters (0 for O), add words (omanlng-login) or use free email. When in doubt, report it to IT."
                : reports > 0
                    ? $"Nobody got phished, and {reports} bait(s) were reported. Reporting suspicious emails warns everyone else too."
                    : "Phishing links look almost real. Check the domain before you click, and report anything suspicious.";
            return r;
        }

        Card ResultCard()
        {
            var r = Result;
            string Row(int i) =>
                $"{_s.Players[i].Tag}   heats {HeatWins[i]}  ·  real links {SafeLinks[i]}  ·  phished {Phished[i]}  ·  bait hits {LureHits[i]}  ·  reports {Reports[i]}   " +
                Palette.Paint($"= {r.Points[i]} pts", Palette.Gold);
            return new Card
            {
                Kicker = "PHISH RACE · RESULT",
                Title = r.Winner < 0 ? "DRAW" : $"{_s.Players[r.Winner].Name} WINS",
                Accent = r.Winner < 0 ? Palette.Gold : _s.Players[r.Winner].Color,
                Body = Row(0) + "\n" + Row(1) + "\n\n" + Palette.Paint("LESSON", Palette.Gold) + "\n" + r.Lesson,
                Footer = "Both players: press A / ✕ to continue",
            };
        }
    }
}
