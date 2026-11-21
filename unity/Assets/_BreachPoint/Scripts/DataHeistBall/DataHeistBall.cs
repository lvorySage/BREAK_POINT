using System.Collections;
using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// Mini-game 2. A DATA packet sits in the middle of the rooftop. The attacker pushes it into the
    /// EXFIL portal (left); the defender returns it to the OmanLNG DATA VAULT (right). The defender has
    /// two cyber tools: X ENCRYPT (stolen data is useless for a few seconds) and Y DLP LOCK (seals the exit).
    /// Two halves; roles swap. Winner = whoever stole more data while attacking.
    /// </summary>
    public sealed partial class DataHeistBall : MonoBehaviour, IMiniGame
    {
        // Field (vehicle bounds) and walls.
        public static readonly Rect Field = Rect.MinMaxRect(-16.8f, -8.8f, 16.8f, 8.8f);
        public const float WallX = 18.2f, WallZ = 10.2f, GoalHalf = 3.4f, GoalDepth = 1.4f;
        public const float BallRadius = 0.85f, CarRadius = 1.45f;
        public const float CornerCut = 3.5f;   // 45° deflectors in the corners so the ball can't get stuck
        public const float KickRange = 3.1f, KickSpeed = 17f, KickCooldown = 0.45f;
        public const float EncryptSeconds = 5f, EncryptCooldown = 9f, EncryptRange = 4.5f;
        public const float LockSeconds = 3f, LockCooldown = 12f;
        public const float ExfilValue = 20f;
        public const int RecoveryPoints = 10, SavePoints = 5;
        /// <summary>CPU ships are a little slower so a solo visitor can beat the computer.</summary>
        public const float CpuSpeed = 0.85f;

        public float HalfSeconds = 60f;
        public bool AiEnabled = true;
        /// <summary>CPU difficulty: 0 easy, 1 normal, 2 hard (from the main-menu options).</summary>
        public int CpuLevel = 1;
        float CpuScale => CpuLevel == 0 ? 0.72f : CpuLevel == 2 ? 0.97f : CpuSpeed;
        public bool ManualStep;

        public string Title => "DATA HEIST BALL";
        public MiniGameResult Result { get; private set; }

        // ---- per-match stats (index = player) ----
        public readonly float[] StolenAsAttacker = new float[2];
        public readonly int[] Recoveries = new int[2];      // returned to the vault as defender
        public readonly int[] EncryptedSaves = new int[2];  // exfils made useless by encryption
        public readonly int[] LockSaves = new int[2];       // exfils stopped by the DLP lock
        public readonly int[] Kicks = new int[2];

        // ---- current half ----
        public int Half { get; private set; }
        public int AttackerIndex => Half == 0 ? 0 : 1;
        public int DefenderIndex => 1 - AttackerIndex;
        public float TimeLeft { get; private set; }
        public float StolenThisHalf { get; private set; }
        public bool Playing => _playing;
        public Vector3 BallPos => _ballPos;
        public Vector3 BallVel => _ballVel;
        public float EncryptedLeft { get; private set; }
        public float LockLeft { get; private set; }
        public float EncryptCooldownLeft { get; private set; }
        public float LockCooldownLeft { get; private set; }
        public float KickCooldownLeft(int player) => _kickCd[player];
        public HoverVehicle Vehicle(int player) => _veh[player];

        Session _s;
        GameFlow _flow;
        readonly HoverVehicle[] _veh = new HoverVehicle[2];
        DataHeistHud _hud;
        bool _playing, _built;
        Vector3 _ballPos, _ballVel;
        readonly float[] _kickCd = new float[2];
        float _kickoffPause, _shake;

        public static DataHeistBall Create() => new GameObject("DataHeistBall").AddComponent<DataHeistBall>();

        // ================================================================== lifecycle

        public void Init(Session s)
        {
            if (_built) return;
            _built = true;
            _s = s;
            if (GameFlow.HalfSecondsOverride > 0) HalfSeconds = GameFlow.HalfSecondsOverride;
            BuildField();
            _veh[0] = HoverVehicle.Create(transform, Team.Red, "RedVehicle");
            _veh[1] = HoverVehicle.Create(transform, Team.Blue, "BlueVehicle");
            _hud = new DataHeistHud(this, s);
        }

        public void StartHalf(int half, bool play = true)
        {
            Half = half;
            TimeLeft = HalfSeconds;
            StolenThisHalf = 0;
            EncryptedLeft = LockLeft = EncryptCooldownLeft = LockCooldownLeft = 0;
            Kickoff(pause: 0f);
            for (int i = 0; i < 2; i++) _veh[i].SetTag($"{_s.Players[i].Name} · {(i == AttackerIndex ? "ATTACKER" : "DEFENDER")}", _s.Players[i].Color);
            _hud.OnHalfStart();
            foreach (var p in _s.Players) Haptics.LightBar(p.Controls, p.Color);
            _playing = play;
        }

        /// <summary>Ball to the centre, ships to their own ends (the defender guards the exfil side).</summary>
        void Kickoff(float pause)
        {
            _ballPos = new Vector3(0, BallRadius, 0);
            _ballVel = Vector3.zero;
            // Slightly off-centre starts so play isn't a head-on shoving match from the whistle.
            _veh[AttackerIndex].Teleport(new Vector3(7f, 0, 2.5f));
            _veh[AttackerIndex].Face(Vector3.left);
            _veh[DefenderIndex].Teleport(new Vector3(-7f, 0, -2.5f));
            _veh[DefenderIndex].Face(Vector3.right);
            _kickCd[0] = _kickCd[1] = 0;
            _kickoffPause = pause;
            UpdateBallVisual(0f);
        }

        public IEnumerator Run(GameFlow flow, Session session)
        {
            _flow = flow;
            flow.InMatch = true;
            flow.OrbitCamera(false);
            HalfSeconds = Settings.HalfSeconds;
            CpuLevel = Settings.Cpu;
            Init(session);
            FirewallArena.PlaceCamera(flow.Cam);
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
            FirewallArena.PlaceCamera(_flow.Cam);
            if (_shake > 0) _flow.Cam.transform.position += Random.insideUnitSphere * (_shake * 0.35f);
        }

        // ================================================================== simulation

        public void Step(float dt)
        {
            if (!_playing) return;
            TimeLeft -= dt;
            EncryptedLeft = Mathf.Max(0, EncryptedLeft - dt);
            LockLeft = Mathf.Max(0, LockLeft - dt);
            EncryptCooldownLeft = Mathf.Max(0, EncryptCooldownLeft - dt);
            LockCooldownLeft = Mathf.Max(0, LockCooldownLeft - dt);
            _kickCd[0] = Mathf.Max(0, _kickCd[0] - dt);
            _kickCd[1] = Mathf.Max(0, _kickCd[1] - dt);

            var atk = _s.Players[AttackerIndex].Controls;
            var def = _s.Players[DefenderIndex].Controls;
            if (AiEnabled)
            {
                if (atk.IsCpu) AiAttacker(atk);
                if (def.IsCpu) AiDefender(def);
            }

            if (_kickoffPause > 0)
            {
                _kickoffPause -= dt;
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    var c = _s.Players[i].Controls;
                    var v = _veh[i];
                    v.Drive(c.Move, c.Boost, dt, Field, c.IsCpu ? CpuScale : 1f);
                    if (v.Velocity.sqrMagnitude > 1f) v.Face(v.Velocity);
                    if (c.ActionDown(0)) TryKick(i);
                }
                if (def.ActionDown(2)) Encrypt();
                if (def.ActionDown(3)) LockExit();
                // Drain the attacker's unused tool buttons so stale presses don't fire later.
                atk.ActionDown(2);
                atk.ActionDown(3);

                ShipCollision();
                StepBall(dt);
            }

            UpdateVisuals(dt);
            _hud.Refresh();

            if (TimeLeft <= 0f || StolenThisHalf >= 100f)
            {
                if (StolenThisHalf >= 100f) _hud.Alert("ALL DATA STOLEN!", Palette.Bad);
                TimeLeft = Mathf.Max(0, TimeLeft);
                _playing = false;
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void ShipCollision()
        {
            var a = _veh[0].transform.position;
            var b = _veh[1].transform.position;
            var d = Flat(b - a);
            float dist = d.magnitude, min = CarRadius * 2f;
            if (dist >= min || dist < 1e-4f) return;
            var n = d / dist;
            float overlap = min - dist;
            _veh[0].Nudge(-n * overlap * 0.5f);
            _veh[1].Nudge(n * overlap * 0.5f);
            float rel = Vector3.Dot(_veh[0].Velocity - _veh[1].Velocity, n);
            if (rel > 0)
            {
                _veh[0].Bump(-n * rel * 0.6f);
                _veh[1].Bump(n * rel * 0.6f);
                if (rel > 8f)
                {
                    Sfx.Play("stun", 0.5f);
                    foreach (var p in _s.Players) Haptics.Pulse(p.Controls, 0.5f, 0.3f, 0.15f);
                    _shake = Mathf.Max(_shake, 0.3f);
                }
            }
        }

        void StepBall(float dt)
        {
            // Ships push the ball (dribbling).
            int touching = 0;
            for (int i = 0; i < 2; i++)
            {
                var v = _veh[i];
                var vp = Flat(v.transform.position);
                var d = Flat(_ballPos) - vp;
                float dist = d.magnitude, min = CarRadius + BallRadius;
                if (dist >= min || dist < 1e-4f) continue;
                var n = d / dist;
                _ballPos = new Vector3(vp.x + n.x * min, BallRadius, vp.z + n.z * min);
                float rel = Vector3.Dot(v.Velocity - _ballVel, n);
                if (rel > 0) _ballVel += n * rel * 1.15f;
                touching++;
            }

            // Pinned between both ships: the data squirts out sideways (a fumble, not a deadlock).
            if (touching == 2)
            {
                var across = Flat(_veh[1].transform.position - _veh[0].transform.position);
                var side = new Vector3(-across.z, 0, across.x).normalized;
                if (Vector3.Dot(side, Flat(_ballPos)) < 0) side = -side; // squirt toward open space (away from the centre line)
                if (Mathf.Abs(_ballPos.z) > WallZ - 3f) side = -side;
                _ballVel = side * 11f + _ballVel * 0.2f;
                _ballPos += side * 0.3f;
            }

            _ballVel *= Mathf.Exp(-0.85f * dt);
            _ballVel = Vector3.ClampMagnitude(Flat(_ballVel), 28f);
            _ballPos += _ballVel * dt;
            _ballPos.y = BallRadius;

            // Side walls.
            float zLim = WallZ - BallRadius;
            if (Mathf.Abs(_ballPos.z) > zLim)
            {
                _ballPos.z = Mathf.Sign(_ballPos.z) * zLim;
                _ballVel.z = -_ballVel.z * 0.8f;
            }

            // Corner deflectors (45° walls): bounce the ball back toward open play.
            float cx = Mathf.Sign(_ballPos.x), cz = Mathf.Sign(_ballPos.z);
            float over = (cx * _ballPos.x + cz * _ballPos.z) - (WallX + WallZ - CornerCut - BallRadius * 1.41f);
            if (over > 0f)
            {
                var inward = new Vector3(-cx, 0, -cz).normalized;
                _ballPos += inward * over / 1.41f;
                float vn = Vector3.Dot(_ballVel, inward);
                if (vn < 0f) _ballVel -= inward * vn * 1.8f;
                _ballVel += inward * 2f;
            }

            // Ends: goals in the middle, walls elsewhere.
            float xLim = WallX - BallRadius;
            bool inMouth = Mathf.Abs(_ballPos.z) < GoalHalf - BallRadius * 0.5f;
            if (_ballPos.x < -xLim)
            {
                if (inMouth && LockLeft > 0f)
                {
                    _ballPos.x = -xLim;
                    _ballVel.x = Mathf.Abs(_ballVel.x) * 0.9f + 4f;
                    LockSaves[DefenderIndex]++;
                    Popup.Show(new Vector3(-WallX + 1.5f, 3f, _ballPos.z), "DLP BLOCKED IT!", Palette.Good);
                    Sfx.Play("block");
                    Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.2f, 0.5f, 0.12f);
                }
                else if (!inMouth)
                {
                    _ballPos.x = -xLim;
                    _ballVel.x = -_ballVel.x * 0.8f;
                }
                else if (_ballPos.x < -(WallX + GoalDepth * 0.5f))
                {
                    Exfil();
                }
            }
            else if (_ballPos.x > xLim)
            {
                if (!inMouth)
                {
                    _ballPos.x = xLim;
                    _ballVel.x = -_ballVel.x * 0.8f;
                }
                else if (_ballPos.x > WallX + GoalDepth * 0.5f)
                {
                    Recover();
                }
            }
        }

        public bool TryKick(int player)
        {
            if (!_playing || _kickoffPause > 0 || _kickCd[player] > 0) return false;
            var v = _veh[player];
            var d = Flat(_ballPos - v.transform.position);
            if (d.magnitude > KickRange) return false;
            var dir = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.right;
            _ballVel = dir * KickSpeed + Flat(v.Velocity) * 0.3f;
            _kickCd[player] = KickCooldown;
            Kicks[player]++;
            Sfx.Play("fire1", 0.8f);
            Haptics.Pulse(_s.Players[player].Controls, 0.3f, 0.5f, 0.1f);
            return true;
        }

        public bool Encrypt()
        {
            if (!_playing || EncryptCooldownLeft > 0) return false;
            var d = Flat(_ballPos - _veh[DefenderIndex].transform.position);
            if (d.magnitude > EncryptRange)
            {
                _hud.Alert("Get closer to the data to encrypt it", Palette.Gold);
                return false;
            }
            EncryptedLeft = EncryptSeconds;
            EncryptCooldownLeft = EncryptCooldown;
            Popup.Show(_ballPos + Vector3.up * 2f, "ENCRYPTED!", Palette.Gold);
            Sfx.Play("block", 0.7f);
            Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.2f, 0.4f, 0.12f);
            return true;
        }

        public bool LockExit()
        {
            if (!_playing || LockCooldownLeft > 0) return false;
            LockLeft = LockSeconds;
            LockCooldownLeft = LockCooldown;
            Popup.Show(new Vector3(-WallX + 2f, 4f, 0), "DLP: EXIT LOCKED", Palette.Good);
            Sfx.Play("switch");
            return true;
        }

        void Exfil()
        {
            var at = new Vector3(-WallX + 2.5f, 3.5f, 0);
            if (EncryptedLeft > 0f)
            {
                EncryptedSaves[DefenderIndex]++;
                Popup.Show(at, "ENCRYPTED: USELESS TO THEM!", Palette.Gold, 0.11f);
                _hud.Alert("Stolen, but encrypted: no data lost", Palette.Gold);
                Sfx.Play("block");
            }
            else
            {
                float gained = Mathf.Min(ExfilValue, 100f - StolenThisHalf);
                StolenThisHalf += gained;
                StolenAsAttacker[AttackerIndex] += gained;
                Popup.Show(at, $"-{gained:0}% DATA EXFILTRATED", Palette.Bad, 0.12f);
                Sfx.Play("hit");
                Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.8f, 1f, 0.35f);
                Haptics.Pulse(_s.Players[AttackerIndex].Controls, 0.2f, 0.6f, 0.25f);
                _shake = 0.8f;
            }
            EncryptedLeft = 0;
            Kickoff(pause: 1.0f);
        }

        void Recover()
        {
            Recoveries[DefenderIndex]++;
            Popup.Show(new Vector3(WallX - 2.5f, 3.5f, 0), "DATA SECURED!", Palette.Good, 0.12f);
            Sfx.Play("block");
            Haptics.Pulse(_s.Players[DefenderIndex].Controls, 0.3f, 0.6f, 0.25f);
            _shake = 0.4f;
            Kickoff(pause: 1.0f);
        }

        /// <summary>Test hook: place the ball.</summary>
        public void SetBall(Vector3 pos, Vector3 vel)
        {
            _ballPos = new Vector3(pos.x, BallRadius, pos.z);
            _ballVel = Flat(vel);
            _kickoffPause = 0f;
        }

        // ================================================================== CPU opponent

        static readonly Vector3 ExfilGoal = new Vector3(-WallX - 1f, 0, 0);
        static readonly Vector3 VaultGoal = new Vector3(WallX + 1f, 0, 0);

        float _aiSideTimer;
        float _aiSide = 1f;

        /// <summary>Drive towards the spot behind the ball (relative to a target goal), then shove it in.
        /// If the opponent is blocking the line, swing around them.</summary>
        void ChaseAndShoot(PlayerControls c, HoverVehicle v, Vector3 goal)
        {
            var ball = Flat(_ballPos);
            var toGoal = Flat(goal - ball).normalized;
            var behind = ball - toGoal * 2.4f;
            var me = Flat(v.transform.position);
            bool lined = Vector3.Dot((ball - me).normalized, toGoal) > 0.6f;
            var target = lined ? ball + toGoal : behind;

            var other = _veh[v == _veh[0] ? 1 : 0];
            var them = Flat(other.transform.position);
            bool blocked = (them - ball).magnitude < CarRadius + BallRadius + 0.8f && Vector3.Dot((them - ball).normalized, toGoal) > 0.5f;
            _aiSideTimer -= Time.deltaTime > 0 ? Time.deltaTime : 0.02f;
            if (_aiSideTimer <= 0f) { _aiSide = Random.value < 0.5f ? -1f : 1f; _aiSideTimer = Random.Range(1f, 2f); }
            if (blocked)
            {
                var perp = new Vector3(-toGoal.z, 0, toGoal.x) * _aiSide;
                target = ball + perp * 2.6f - toGoal * 0.5f;
            }
            var to = target - me;
            c.CpuMove = to.magnitude > 0.3f ? Vector2.ClampMagnitude(new Vector2(to.x, to.z) * 0.6f, 1f) : Vector2.zero;
            c.CpuBoost = to.magnitude > 9f;
            if (lined && (ball - me).magnitude < KickRange - 0.3f && Random.value < 0.08f) c.CpuAction = 0;
        }

        void AiAttacker(PlayerControls c) => ChaseAndShoot(c, _veh[AttackerIndex], ExfilGoal);

        void AiDefender(PlayerControls c)
        {
            var v = _veh[DefenderIndex];
            var ball = Flat(_ballPos);
            var me = Flat(v.transform.position);
            if (ball.x < -6f && EncryptCooldownLeft <= 0 && (ball - me).magnitude < EncryptRange && Random.value < 0.05f) { c.CpuAction = 2; return; }
            if (ball.x < -12f && Mathf.Abs(ball.z) < GoalHalf + 2f && LockCooldownLeft <= 0 && _ballVel.x < -4f) { c.CpuAction = 3; return; }
            // Guard the exit if the ball is between us and it; otherwise clear it toward the vault.
            if (ball.x < me.x - 1f)
            {
                var guard = Vector3.Lerp(ball, ExfilGoal, 0.45f);
                var to = guard - me;
                c.CpuMove = Vector2.ClampMagnitude(new Vector2(to.x, to.z) * 0.6f, 1f);
                c.CpuBoost = to.magnitude > 6f;
            }
            else
            {
                ChaseAndShoot(c, v, VaultGoal);
            }
        }

        // ================================================================== cards & result

        string Legend() =>
            $"{Palette.Paint("ATTACKER", Palette.Red)}: push the DATA into the {Palette.Paint("EXFIL", new Color(0.72f, 0.3f, 1f))} portal (left, violet).\n" +
            $"{Palette.Paint("DEFENDER", Palette.Blue)}: return it to the {Palette.Paint("DATA VAULT", new Color(0.3f, 1f, 0.6f))} (right, green), using two tools:\n" +
            $"{Palette.Paint("X / □  ENCRYPT", Palette.Gold)}: stolen data is useless for {EncryptSeconds:0} s (get close first)\n" +
            $"{Palette.Paint("Y / △  DLP LOCK", Palette.Good)}: seal the exit for {LockSeconds:0} s\n\n" +
            Palette.Paint("Both: drive into the data to push it  ·  A / ✕ kick  ·  RT / R1 boost  ·  bumping allowed", Palette.Muted);

        Card IntroCard() => new Card
        {
            Kicker = GameFlow.Kicker(this),
            Title = Title,
            Accent = Palette.Gold,
            Body = "The attacker is inside OmanLNG's network and wants to smuggle data out.\n\n" + Legend() +
                   $"\n\nFirst half: {_s.Players[0].Tag} attacks, {_s.Players[1].Tag} defends.",
            Footer = "Both players: press A / ✕ when ready",
        };

        Card SwapCard() => new Card
        {
            Kicker = "HALF-TIME",
            Title = "ROLES SWAP",
            Accent = _s.Players[1].Color,
            Body =
                $"{_s.Players[0].Tag} stole <b>{StolenAsAttacker[0]:0}%</b> of the data.\n" +
                $"{_s.Players[1].Tag} secured {Recoveries[1]} · encryption saves {EncryptedSaves[1]} · DLP saves {LockSaves[1]}.\n\n" +
                $"Now {_s.Players[1].Tag} attacks and {_s.Players[0].Tag} defends.",
            AutoSeconds = 7f,
        };

        public MiniGameResult ComputeResult()
        {
            var r = new MiniGameResult { Game = Title, ScoreLabel = "data stolen %" };
            for (int i = 0; i < 2; i++)
            {
                r.Scores[i] = StolenAsAttacker[i];
                r.Points[i] = Mathf.RoundToInt(StolenAsAttacker[i]) + RecoveryPoints * Recoveries[i] + SavePoints * (EncryptedSaves[i] + LockSaves[i]);
            }
            if (Mathf.Abs(r.Scores[0] - r.Scores[1]) > 0.01f) r.Winner = r.Scores[0] > r.Scores[1] ? 0 : 1;
            else if (r.Points[0] != r.Points[1]) r.Winner = r.Points[0] > r.Points[1] ? 0 : 1;

            int enc = EncryptedSaves[0] + EncryptedSaves[1], dlp = LockSaves[0] + LockSaves[1];
            r.Lesson = enc >= dlp && enc > 0
                ? $"Encryption saved the data {enc} time(s). Encrypted data is useless to a thief without the key: encrypt laptops, USB drives and sensitive files."
                : dlp > 0
                    ? $"DLP blocked {dlp} escape(s). Data Loss Prevention watches what leaves the network and stops sensitive files going out, like attachments to personal email or uploads to unknown sites."
                    : "Attackers who get inside go for the data. Encryption makes stolen data useless, and Data Loss Prevention (DLP) stops it leaving the network.";
            return r;
        }

        Card ResultCard()
        {
            var r = Result;
            string Row(int i) =>
                $"{_s.Players[i].Tag}   stole <b>{StolenAsAttacker[i]:0}%</b>  ·  secured {Recoveries[i]}  ·  saves {EncryptedSaves[i] + LockSaves[i]}   " +
                Palette.Paint($"= {r.Points[i]} pts", Palette.Gold);
            return new Card
            {
                Kicker = "DATA HEIST BALL · RESULT",
                Title = r.Winner < 0 ? "DRAW" : $"{_s.Players[r.Winner].Name} WINS",
                Accent = r.Winner < 0 ? Palette.Gold : _s.Players[r.Winner].Color,
                Body = Row(0) + "\n" + Row(1) + "\n\n" + Palette.Paint("LESSON", Palette.Gold) + "\n" + r.Lesson,
                Footer = "Both players: press A / ✕ to continue",
            };
        }
    }
}
