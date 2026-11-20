using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// Mini-game 1. The attacker fires Phishing / Malware / DDoS at three servers; the defender
    /// drives in front of them and switches the shield to the matching defense
    /// (Email filter / Antivirus / Rate limiter). Two halves; roles swap at half-time.
    /// The winner is whoever stole more data while attacking.
    /// </summary>
    public sealed partial class FirewallArena : MonoBehaviour, IMiniGame
    {
        public static readonly Rect AttackerZone = Rect.MinMaxRect(-17.5f, -9.5f, -3f, 9.5f);
        public static readonly Rect DefenderZone = Rect.MinMaxRect(3f, -9.5f, 13.4f, 9.5f);
        public static readonly float[] Lanes = { -6f, 0f, 6f };
        public const float ServerX = 16.5f;
        public const float ShieldRadius = 2.5f;
        public const float GlobalCooldown = 0.3f;
        public const float StunSeconds = 1.2f;
        public const int BlockPoints = 5;
        public const float FortFrontX = 14.6f;
        public const float GateHalfWidth = 1.7f;

        public float HalfSeconds = 60f;
        public bool AiEnabled = true;
        /// <summary>Tests drive the simulation with Step() instead of Update().</summary>
        public bool ManualStep;

        public string Title => "FIREWALL ARENA";
        public MiniGameResult Result { get; private set; }

        // ---- per-match stats (index = player) ----
        public readonly float[] StolenAsAttacker = new float[2];
        public readonly int[] Blocks = new int[2];
        public readonly int[] WrongDefenses = new int[2];
        public readonly int[] HitsByType = new int[Attacks.Count];
        public readonly int[] FiredByType = new int[Attacks.Count];

        // ---- current half ----
        public int Half { get; private set; }
        public int AttackerIndex => Half == 0 ? 0 : 1;
        public int DefenderIndex => 1 - AttackerIndex;
        public float TimeLeft { get; private set; }
        public float StolenThisHalf { get; private set; }
        public bool Playing => _playing;
        public AttackType Shield => _shield;
        public int Lane => _lane;
        public int ActiveShots => _shots.Count;
        public float Stun => _stun;
        public float CooldownLeft(AttackType t) => Mathf.Max(_cd[(int)t], _globalCd);
        public HoverVehicle Vehicle(int player) => _veh[player];

        Session _s;
        GameFlow _flow;
        HoverVehicle[] _veh = new HoverVehicle[2];
        FirewallHud _hud;
        bool _playing, _built;
        AttackType _shield;
        int _lane = 1;
        readonly float[] _cd = new float[Attacks.Count];
        float _globalCd, _stun, _wrongFlash, _blockFlash, _shake;

        sealed class Shot
        {
            public AttackType Type;
            public Vector3 Pos, Vel;
            public int Lane;
            public int Burst;            // DDoS pellets fired together share a burst id (0 = single shot)
            public float Radius, Value, Age;
            public bool Reflected, Passed, Dead;
            public GameObject Go;
            public Material Mat;
            public TextMesh Label;
        }

        readonly List<Shot> _shots = new List<Shot>();
        int _nextBurst;

        public static FirewallArena Create() => new GameObject("FirewallArena").AddComponent<FirewallArena>();

        // ================================================================== lifecycle

        /// <summary>Builds arena, vehicles and HUD for a session. Safe to call once.</summary>
        public void Init(Session s)
        {
            if (_built) return;
            _built = true;
            _s = s;
            if (GameFlow.HalfSecondsOverride > 0) HalfSeconds = GameFlow.HalfSecondsOverride;
            BuildArena();
            _veh[0] = HoverVehicle.Create(transform, Team.Red, "RedVehicle");
            _veh[1] = HoverVehicle.Create(transform, Team.Blue, "BlueVehicle");
            BuildRoleVisuals();
            _hud = new FirewallHud(this, s);
        }

        public void StartHalf(int half, bool play = true)
        {
            Half = half;
            ClearShots();
            TimeLeft = HalfSeconds;
            StolenThisHalf = 0;
            _stun = 0;
            _globalCd = 0;
            for (int i = 0; i < Attacks.Count; i++) _cd[i] = 0;
            _shield = AttackType.Phishing;
            _lane = 1;
            _veh[AttackerIndex].Teleport(new Vector3(-12f, 0, 0));
            _veh[AttackerIndex].Face(Vector3.right);
            _veh[DefenderIndex].Teleport(new Vector3(9f, 0, 0));
            _veh[DefenderIndex].Face(Vector3.left);
            ApplyRoleVisuals();
            _hud.OnHalfStart();
            foreach (var p in _s.Players) Haptics.LightBar(p.Controls, p.Color);
            _playing = play;
        }

        public IEnumerator Run(GameFlow flow, Session session)
        {
            _flow = flow;
            flow.InMatch = true;
            flow.OrbitCamera(false);
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
                ClearShots();
                Sfx.Play("whistle");
                Haptics.StopAll();
                yield return new WaitForSeconds(1.2f);
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

        void OnDestroy()
        {
            ClearShots();
            _hud?.Destroy();
        }

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
            if (_shake > 0) _flow.Cam.transform.position += Random.insideUnitSphere * (_shake * 0.35f);
        }

        public static void PlaceCamera(Camera cam)
        {
            if (cam == null) return;
            cam.transform.position = new Vector3(1.5f, 28f, -26f);
            cam.transform.LookAt(new Vector3(1.5f, 0, 1.4f));
            cam.fieldOfView = 47f;
        }

        // ================================================================== simulation

        public void Step(float dt)
        {
            if (!_playing) return;
            TimeLeft -= dt;
            var atk = _s.Players[AttackerIndex].Controls;
            var def = _s.Players[DefenderIndex].Controls;
            if (AiEnabled)
            {
                if (atk.IsCpu) AiAttacker(atk, dt);
                if (def.IsCpu) AiDefender(def, dt);
            }

            // Attacker
            var av = _veh[AttackerIndex];
            av.Drive(_stun > 0 ? atk.Move * 0.35f : atk.Move, atk.Boost && _stun <= 0, dt, AttackerZone);
            _lane = PickLane(av.transform.position, atk.Aim);
            av.Face(_servers[_lane].Pos - av.transform.position);
            _stun = Mathf.Max(0, _stun - dt);
            _globalCd = Mathf.Max(0, _globalCd - dt);
            for (int i = 0; i < Attacks.Count; i++) _cd[i] = Mathf.Max(0, _cd[i] - dt);
            for (int i = 0; i < Attacks.Count; i++) if (atk.ActionDown(i)) TryFire((AttackType)i);

            // Defender
            var dv = _veh[DefenderIndex];
            dv.Drive(def.Move, def.Boost, dt, DefenderZone);
            dv.Face(Vector3.left);
            for (int i = 0; i < Attacks.Count; i++) if (def.ActionDown(i)) SetShield((AttackType)i);

            UpdateShots(dt);
            UpdateVisuals(dt);
            _hud.Refresh();

            if (TimeLeft <= 0f || StolenThisHalf >= 100f)
            {
                if (StolenThisHalf >= 100f) _hud.Alert("SERVERS DRAINED!", Palette.Bad);
                TimeLeft = Mathf.Max(0, TimeLeft);
                _playing = false;
            }
        }

        static int PickLane(Vector3 attacker, Vector2 aim)
        {
            if (aim.sqrMagnitude > 0.25f)
            {
                int best = 1;
                float bestDot = -2f;
                var dir = new Vector2(aim.x, aim.y).normalized;
                for (int i = 0; i < Lanes.Length; i++)
                {
                    var to = new Vector2(ServerX - attacker.x, Lanes[i] - attacker.z).normalized;
                    float d = Vector2.Dot(dir, to);
                    if (d > bestDot) { bestDot = d; best = i; }
                }
                return best;
            }
            int lane = 0;
            for (int i = 1; i < Lanes.Length; i++)
                if (Mathf.Abs(attacker.z - Lanes[i]) < Mathf.Abs(attacker.z - Lanes[lane])) lane = i;
            return lane;
        }

        public bool TryFire(AttackType t)
        {
            if (!_playing || _stun > 0 || _globalCd > 0 || _cd[(int)t] > 0) return false;
            _cd[(int)t] = Attacks.Cooldown(t);
            _globalCd = GlobalCooldown;
            FiredByType[(int)t]++;

            var av = _veh[AttackerIndex].transform.position;
            var origin = new Vector3(av.x + 1.5f, 1.3f, av.z);
            var target = _servers[_lane].Pos;
            var dir = target - origin;
            dir.y = 0;
            dir.Normalize();
            var defender = _s.Players[DefenderIndex].Controls;
            string hint = Attacks.Name(t); // the matching defense is shown in the HUD alert and legend

            if (t == AttackType.DDoS)
            {
                int burst = ++_nextBurst;
                for (int k = -2; k <= 2; k++)
                {
                    var d = Quaternion.Euler(0, k * 7f, 0) * dir;
                    SpawnShot(t, origin, d * Attacks.Speed(t), 0.32f, Attacks.Value(t), k == 0 ? hint : null, burst);
                }
            }
            else if (t == AttackType.Password)
            {
                // A chain of login attempts in single file: MFA stops the whole chain.
                int burst = ++_nextBurst;
                for (int k = 0; k < 3; k++)
                    SpawnShot(t, origin - dir * (k * 1.3f), dir * Attacks.Speed(t), 0.36f, Attacks.Value(t), k == 0 ? hint : null, burst);
            }
            else
            {
                SpawnShot(t, origin, dir * Attacks.Speed(t), t == AttackType.Phishing ? 0.62f : 0.46f, Attacks.Value(t), hint);
            }

            Sfx.Play("fire" + (int)t, 0.8f);
            Haptics.Pulse(_s.Players[AttackerIndex].Controls, 0.15f, 0.35f, 0.08f);
            _hud.Incoming(t, defender);
            return true;
        }

        void SpawnShot(AttackType t, Vector3 pos, Vector3 vel, float radius, float value, string label, int burst = 0)
        {
            var s = new Shot { Type = t, Pos = pos, Vel = vel, Lane = _lane, Radius = radius, Value = value, Burst = burst };
            s.Mat = Mats.GlowInstance(Attacks.Color(t), 2.6f);
            s.Go = Shapes.Ball(transform, "Shot_" + t, pos, radius * 2f, s.Mat);
            var tr = s.Go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = s.Mat;
            tr.time = t == AttackType.Phishing ? 0.45f : 0.22f;
            tr.widthMultiplier = radius * 1.7f;
            tr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (label != null)
            {
                float inv = 1f / (radius * 2f);
                s.Label = Shapes.Text3D(s.Go.transform, label, new Vector3(0, 1.4f * inv, 0), 0.1f * inv, Attacks.Color(t));
            }
            _shots.Add(s);
        }

        public void SetShield(AttackType t)
        {
            if (_shield == t) return;
            _shield = t;
            Sfx.Play("switch", 0.6f);
            Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.1f, 0.15f, 0.05f);
        }

        void UpdateShots(float dt)
        {
            var av = _veh[AttackerIndex].transform.position;
            var dv = _veh[DefenderIndex].transform.position;
            foreach (var s in _shots)
            {
                if (s.Dead) continue;
                s.Age += dt;
                if (s.Reflected)
                {
                    var want = (new Vector3(av.x, 1.2f, av.z) - s.Pos).normalized * s.Vel.magnitude;
                    s.Vel = Vector3.RotateTowards(s.Vel, want, 4f * dt, 0f);
                }
                else if (s.Type == AttackType.Phishing)
                {
                    var want = (_servers[s.Lane].Pos - s.Pos).normalized * s.Vel.magnitude;
                    s.Vel = Vector3.RotateTowards(s.Vel, want, 2.4f * dt, 0f);
                }
                s.Pos += s.Vel * dt;
                s.Go.transform.position = s.Pos;

                if (!s.Reflected && !s.Passed && Flat(s.Pos - dv).magnitude < ShieldRadius + s.Radius * 0.5f)
                {
                    if (_shield == s.Type) Reflect(s); else Pass(s);
                }

                if (!s.Reflected && s.Pos.x > FortFrontX - 0.4f && s.Pos.x < FortFrontX + 0.4f && !InGate(s.Pos.z))
                {
                    Kill(s); // stray shots hit the fort wall
                    continue;
                }

                if (!s.Reflected)
                {
                    for (int i = 0; i < _servers.Length && !s.Dead; i++)
                        if (Flat(s.Pos - _servers[i].Pos).magnitude < 1.5f + s.Radius) HitServer(s, i);
                }
                else if (Flat(s.Pos - av).magnitude < 1.4f + s.Radius)
                {
                    StunAttacker(s);
                }

                if (Mathf.Abs(s.Pos.x) > 24f || Mathf.Abs(s.Pos.z) > 15f || s.Age > 8f) Kill(s);
            }
            _shots.RemoveAll(s => s.Dead);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        static bool InGate(float z)
        {
            foreach (var lane in Lanes) if (Mathf.Abs(z - lane) < GateHalfWidth) return true;
            return false;
        }

        void Reflect(Shot s)
        {
            s.Reflected = true;
            s.Vel = (Flat(_veh[AttackerIndex].transform.position - s.Pos)).normalized * Mathf.Max(16f, s.Vel.magnitude * 1.2f);
            Mats.SetGlow(s.Mat, _s.Players[DefenderIndex].Color, 3f);
            if (s.Label != null) { s.Label.text = "BLOCKED"; s.Label.color = Palette.Good; }
            Blocks[DefenderIndex]++;
            _blockFlash = 1f;
            bool flood = s.Burst > 0;
            if (flood)
            {
                // Rate limiting / MFA stop the whole burst, not just one packet.
                foreach (var o in _shots)
                    if (o != s && o.Burst == s.Burst && !o.Dead && !o.Reflected)
                    {
                        Popup.Show(o.Pos + Vector3.up * 0.8f, "×", Palette.Good, 0.09f);
                        Kill(o);
                    }
            }
            string blockText = s.Type == AttackType.DDoS ? "FLOOD FILTERED!" : s.Type == AttackType.Password ? "MFA BLOCKED IT!" : "BLOCKED!";
            Popup.Show(s.Pos + Vector3.up * 1.5f, blockText, Palette.Good);
            Sfx.Play("block", 0.9f);
            Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.2f, 0.5f, 0.12f);
        }

        void Pass(Shot s)
        {
            s.Passed = true;
            if (s.Burst > 0)
            {
                bool counted = false;
                foreach (var o in _shots)
                {
                    if (o.Burst != s.Burst || o == s) continue;
                    if (o.Passed) counted = true;
                    o.Passed = true;
                }
                if (counted) return; // one wrong defense per burst
            }
            WrongDefenses[DefenderIndex]++;
            _wrongFlash = 1f;
            Popup.Show(_veh[DefenderIndex].transform.position + Vector3.up * 3.2f, $"WRONG DEFENSE\n<size=44>needs {Attacks.Defense(s.Type)}</size>", Palette.Bad, 0.1f);
            Sfx.Play("wrong", 0.8f);
            Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.5f, 0.2f, 0.15f);
        }

        void HitServer(Shot s, int server)
        {
            float gained = Mathf.Min(s.Value, 100f - StolenThisHalf);
            StolenThisHalf += gained;
            StolenAsAttacker[AttackerIndex] += gained;
            HitsByType[(int)s.Type]++;
            _servers[server].Flash = 1f;
            _shake = Mathf.Max(_shake, s.Type == AttackType.DDoS ? 0.25f : 0.6f);
            Popup.Show(_servers[server].Pos + Vector3.up * 3.6f, $"-{gained:0.#}% DATA", Palette.Bad);
            Sfx.Play("hit", s.Type == AttackType.DDoS ? 0.45f : 0.9f);
            Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.7f, 1f, 0.25f);
            Haptics.Pulse(_s.Players[AttackerIndex].Controls, 0.1f, 0.3f, 0.1f);
            Kill(s);
        }

        void StunAttacker(Shot s)
        {
            _stun = StunSeconds;
            Popup.Show(_veh[AttackerIndex].transform.position + Vector3.up * 3f, "STUNNED", Palette.Gold);
            Sfx.Play("stun");
            Haptics.Pulse(_s.Players[AttackerIndex].Controls, 0.8f, 0.4f, 0.3f);
            Kill(s);
        }

        void Kill(Shot s)
        {
            if (s.Dead) return;
            s.Dead = true;
            if (s.Go != null) Destroy(s.Go);
            if (s.Mat != null) Destroy(s.Mat);
        }

        void ClearShots()
        {
            foreach (var s in _shots) Kill(s);
            _shots.Clear();
        }

        // ================================================================== CPU opponent

        float _aiFireTimer = 1f, _aiLaneTimer, _aiReactAt = -1f;
        int _aiLane = 1;
        Shot _aiThreat;
        AttackType _aiPlannedShield;

        void AiAttacker(PlayerControls c, float dt)
        {
            _aiLaneTimer -= dt;
            if (_aiLaneTimer <= 0f) { _aiLane = Random.Range(0, Lanes.Length); _aiLaneTimer = Random.Range(1.2f, 2.6f); }
            var p = _veh[AttackerIndex].transform.position;
            var target = new Vector2(-11f + Mathf.Sin(Time.time * 0.7f) * 3f, Lanes[_aiLane]);
            var to = target - new Vector2(p.x, p.z);
            c.CpuMove = to.magnitude > 0.4f ? Vector2.ClampMagnitude(to * 0.6f, 1f) : Vector2.zero;
            _aiFireTimer -= dt;
            if (_aiFireTimer <= 0f && Mathf.Abs(p.z - Lanes[_aiLane]) < 1.2f)
            {
                c.CpuAction = Random.Range(0, Attacks.Count);
                _aiFireTimer = Random.Range(0.8f, 1.5f);
            }
        }

        void AiDefender(PlayerControls c, float dt)
        {
            var dv = _veh[DefenderIndex].transform.position;
            Shot threat = null;
            float best = float.MaxValue;
            foreach (var s in _shots)
            {
                if (s.Dead || s.Reflected || s.Passed || s.Pos.x > dv.x + 1f) continue;
                float tReach = (dv.x - s.Pos.x) / Mathf.Max(s.Vel.x, 0.5f);
                if (tReach < best) { best = tReach; threat = s; }
            }

            float wantZ = 0f;
            if (threat != null)
            {
                wantZ = Mathf.Clamp(threat.Pos.z + threat.Vel.z * best, -9f, 9f);
                if (threat != _aiThreat)
                {
                    _aiThreat = threat;
                    _aiReactAt = Time.time + Random.Range(0.3f, 0.55f);
                    _aiPlannedShield = Random.value < 0.82f ? threat.Type : (AttackType)(((int)threat.Type + Random.Range(1, Attacks.Count)) % Attacks.Count);
                }
                if (_aiReactAt > 0 && Time.time >= _aiReactAt)
                {
                    if (_shield != _aiPlannedShield) c.CpuAction = (int)_aiPlannedShield;
                    _aiReactAt = -1f;
                }
            }
            var to = new Vector2(9f - dv.x, wantZ - dv.z);
            c.CpuMove = to.magnitude > 0.3f ? Vector2.ClampMagnitude(to * 0.5f, 1f) : Vector2.zero;
        }

        // ================================================================== cards & result

        string Legend()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in Attacks.All)
                sb.AppendLine(Palette.Paint($"{Attacks.PadLabels[(int)t]}    {Attacks.Name(t)}  →  {Attacks.Defense(t)}", Attacks.Color(t)) +
                              Palette.Paint($"   ({Attacks.Behaviour(t)})", Palette.Muted));
            return sb.ToString();
        }

        Card IntroCard() => new Card
        {
            Kicker = $"MINI-GAME 1 OF {1 + GameFlow.ComingSoon.Length}",
            Title = "FIREWALL ARENA",
            Accent = Palette.Gold,
            Body =
                "Protect OmanLNG's systems inside the SOC fort.\n" +
                "<b>ATTACKER</b> fires cyber-attacks at the three systems.  " +
                "<b>DEFENDER</b> blocks them with the <b>matching defense</b>.\n\n" +
                Legend() + "\n" +
                "Right defense = blocked and bounced back.   Wrong defense = data stolen.\n" +
                Palette.Paint("Left stick: move  ·  RT / R1: boost  ·  Roles swap at half-time", Palette.Muted) + "\n\n" +
                $"First half: {_s.Players[0].Tag} attacks, {_s.Players[1].Tag} defends.",
            Footer = "Both players: press A / ✕ when ready",
        };

        Card SwapCard() => new Card
        {
            Kicker = "HALF-TIME",
            Title = "ROLES SWAP",
            Accent = _s.Players[1].Color,
            Body =
                $"{_s.Players[0].Tag} stole <b>{StolenAsAttacker[0]:0}%</b> of the data as attacker.\n" +
                $"{_s.Players[1].Tag} blocked {Blocks[1]} attacks · {WrongDefenses[1]} wrong defenses.\n\n" +
                $"Now {_s.Players[1].Tag} attacks and {_s.Players[0].Tag} defends.\nCan you steal more?",
            AutoSeconds = 7f,
        };

        public MiniGameResult ComputeResult()
        {
            var r = new MiniGameResult { Game = Title, ScoreLabel = "data stolen %" };
            r.Scores[0] = StolenAsAttacker[0];
            r.Scores[1] = StolenAsAttacker[1];
            for (int i = 0; i < 2; i++) r.Points[i] = Mathf.RoundToInt(StolenAsAttacker[i]) + BlockPoints * Blocks[i];
            if (Mathf.Abs(r.Scores[0] - r.Scores[1]) > 0.01f) r.Winner = r.Scores[0] > r.Scores[1] ? 0 : 1;
            else if (Blocks[0] != Blocks[1]) r.Winner = Blocks[0] > Blocks[1] ? 0 : 1;

            // Lesson: the attack type that got through most often (or the most-used one).
            int worst = 0;
            for (int i = 1; i < Attacks.Count; i++) if (HitsByType[i] > HitsByType[worst]) worst = i;
            if (HitsByType[worst] == 0)
                for (int i = 1; i < Attacks.Count; i++) if (FiredByType[i] > FiredByType[worst]) worst = i;
            var t = (AttackType)worst;
            string lead = HitsByType[worst] > 0 ? $"{Attacks.Name(t)} got through {HitsByType[worst]} times. " : "";
            r.Lesson = lead + Attacks.Lesson(t);
            return r;
        }

        Card ResultCard()
        {
            var r = Result;
            string Row(int i) =>
                $"{_s.Players[i].Tag}   stole <b>{StolenAsAttacker[i]:0}%</b>  ·  blocked {Blocks[i]}  ·  wrong {WrongDefenses[i]}   " +
                Palette.Paint($"= {r.Points[i]} pts", Palette.Gold);
            return new Card
            {
                Kicker = "FIREWALL ARENA · RESULT",
                Title = r.Winner < 0 ? "DRAW" : $"{_s.Players[r.Winner].Name} WINS",
                Accent = r.Winner < 0 ? Palette.Gold : _s.Players[r.Winner].Color,
                Body = Row(0) + "\n" + Row(1) + "\n\n" +
                       Palette.Paint("LESSON", Palette.Gold) + "\n" + r.Lesson + "\n\n" +
                       Palette.Paint("Different attacks need different defenses: match the defense to the threat.", Palette.Muted),
                Footer = "Both players: press A / ✕ to continue",
            };
        }
    }
}
