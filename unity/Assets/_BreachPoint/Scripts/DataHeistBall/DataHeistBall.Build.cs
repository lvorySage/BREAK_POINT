using UnityEngine;

namespace BreachPoint
{
    /// <summary>Field, goals and ball visuals for Data Heist Ball.</summary>
    public sealed partial class DataHeistBall
    {
        Transform _ball, _ballCore, _ballRing;
        Material _ballMat, _ballRingMat;
        TextMesh _ballLabel;
        GameObject _lockBars;
        Material _exfilGlow;

        static readonly Color DataCyan = new Color(0.3f, 0.95f, 1f);

        void BuildField()
        {
            var root = transform;
            var slab = ArtKit.TexturedSized("RoofConcrete", new Color(0.33f, 0.35f, 0.42f), new Vector2(14f, 9f), 0.6f, 0.1f);
            var wall = ArtKit.Textured("MetalPlate", new Color(0.3f, 0.3f, 0.36f), 1f, 0.6f, 0.5f);
            var line = Mats.Glow(new Color(0.85f, 0.9f, 1f), 1.3f);
            var magenta = Mats.Glow(new Color(1f, 0.2f, 0.7f), 1.8f);

            Shapes.Box(root, "Roof", new Vector3(0, -0.5f, 0), new Vector3(54, 1, 36), slab);

            // Pitch markings.
            Shapes.Box(root, "CentreLine", new Vector3(0, 0.02f, 0), new Vector3(0.12f, 0.02f, WallZ * 2), line, false);
            Shapes.Ring(root, "CentreCircle", 3.2f, 0.12f, line).transform.localPosition = new Vector3(0, 0.03f, 0);
            foreach (var sx in new[] { -1f, 1f })
            {
                float x = sx * (WallX - 4f);
                Shapes.Box(root, "BoxLine", new Vector3(x, 0.02f, 0), new Vector3(0.12f, 0.02f, GoalHalf * 2 + 4f), line, false);
                Shapes.Box(root, "BoxLine", new Vector3(sx * (WallX - 2f), 0.02f, GoalHalf + 2f), new Vector3(4f, 0.02f, 0.12f), line, false);
                Shapes.Box(root, "BoxLine", new Vector3(sx * (WallX - 2f), 0.02f, -(GoalHalf + 2f)), new Vector3(4f, 0.02f, 0.12f), line, false);
            }

            // Side walls and end walls (with goal mouths).
            void Wall(Vector3 p, Vector3 s)
            {
                Shapes.Box(root, "Wall", p, s, wall);
                Shapes.Box(root, "WallNeon", p + Vector3.up * (s.y / 2f + 0.03f), new Vector3(s.x, 0.06f, s.z), magenta, false);
            }
            Wall(new Vector3(0, 0.45f, WallZ + 0.2f), new Vector3(WallX * 2 + 0.8f, 0.9f, 0.4f));
            Wall(new Vector3(0, 0.45f, -WallZ - 0.2f), new Vector3(WallX * 2 + 0.8f, 0.9f, 0.4f));
            float endLen = WallZ - GoalHalf;
            foreach (var sx in new[] { -1f, 1f })
            {
                Wall(new Vector3(sx * (WallX + 0.2f), 0.45f, GoalHalf + endLen / 2f), new Vector3(0.4f, 0.9f, endLen));
                Wall(new Vector3(sx * (WallX + 0.2f), 0.45f, -(GoalHalf + endLen / 2f)), new Vector3(0.4f, 0.9f, endLen));
            }

            // Corner deflectors (match CornerCut in the physics).
            foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
            {
                float d = CornerCut / 2f;
                var pos = new Vector3(sx * (WallX - d / 2f + 0.15f), 0.45f, sz * (WallZ - d / 2f + 0.15f));
                var block = Shapes.Box(root, "Corner", pos, new Vector3(CornerCut * 0.75f, 0.9f, 0.4f), wall);
                block.transform.localRotation = Quaternion.Euler(0, sx * sz > 0 ? -45f : 45f, 0);
                var neon = Shapes.Box(root, "CornerNeon", pos + Vector3.up * 0.48f, new Vector3(CornerCut * 0.75f, 0.06f, 0.4f), magenta, false);
                neon.transform.localRotation = block.transform.localRotation;
            }

            BuildExfil(root);
            BuildVault(root);
            BuildBall(root);
            BuildProps(root);

            Shapes.PointLight(root, new Vector3(-10, 6, 0), new Color(1f, 0.4f, 0.5f), 22, 2.2f);
            Shapes.PointLight(root, new Vector3(10, 6, 0), new Color(0.4f, 0.7f, 1f), 22, 2.2f);
            Shapes.PointLight(root, new Vector3(0, 7, 0), new Color(0.8f, 0.85f, 1f), 18, 1.5f);
        }

        void BuildExfil(Transform root)
        {
            var red = new Color(1f, 0.15f, 0.25f);
            var g = new GameObject("Exfil").transform;
            g.SetParent(root, false);
            g.localPosition = new Vector3(-WallX - GoalDepth / 2f, 0, 0);
            _exfilGlow = Mats.GlowInstance(red, 2.2f);
            var dark = Mats.Lit(new Color(0.08f, 0.04f, 0.06f), 0.6f, 0.5f);
            Shapes.Box(g, "Floor", new Vector3(0, 0.03f, 0), new Vector3(GoalDepth, 0.02f, GoalHalf * 2), _exfilGlow, false);
            Shapes.Box(g, "Back", new Vector3(-GoalDepth / 2f - 0.2f, 1.2f, 0), new Vector3(0.3f, 2.4f, GoalHalf * 2 + 0.6f), dark);
            foreach (var z in new[] { -GoalHalf, GoalHalf })
                Shapes.Box(g, "Post", new Vector3(GoalDepth / 2f, 1.3f, z), new Vector3(0.35f, 2.6f, 0.35f), _exfilGlow, false);
            Shapes.Box(g, "Bar", new Vector3(GoalDepth / 2f, 2.6f, 0), new Vector3(0.3f, 0.3f, GoalHalf * 2 + 0.35f), _exfilGlow, false);
            var portal = ArtKit.Model("ExfilPortal", g);
            if (portal != null) ArtKit.Fit(portal, new Vector3(2.4f, 3.2f, 2.4f), new Vector3(-GoalDepth - 1.2f, 1.6f, 0), zForward: false);
            Shapes.Text3D(g, "EXFIL", new Vector3(0, 4.4f, 0), 0.13f, red, true, Fonts.Title);
            Shapes.Text3D(g, "→ DARK WEB", new Vector3(0, 3.6f, 0), 0.06f, Palette.Muted);
            Shapes.PointLight(g, new Vector3(1.5f, 2f, 0), red, 8f, 3f);

            // DLP lock: laser bars across the mouth (shown while locked).
            _lockBars = new GameObject("DlpLock");
            _lockBars.transform.SetParent(g, false);
            var laser = Mats.Glow(new Color(0.3f, 1f, 0.5f), 3f);
            for (int i = 0; i < 4; i++)
                Shapes.Box(_lockBars.transform, "Laser", new Vector3(GoalDepth / 2f, 0.4f + i * 0.55f, 0), new Vector3(0.08f, 0.08f, GoalHalf * 2), laser, false);
            Shapes.Text3D(_lockBars.transform, "DLP LOCK", new Vector3(GoalDepth / 2f + 0.5f, 2.9f, 0), 0.07f, new Color(0.3f, 1f, 0.5f));
            _lockBars.SetActive(false);
        }

        void BuildVault(Transform root)
        {
            var blue = Palette.Blue;
            var gold = new Color(1f, 0.78f, 0.35f);
            var g = new GameObject("Vault").transform;
            g.SetParent(root, false);
            g.localPosition = new Vector3(WallX + GoalDepth / 2f, 0, 0);
            var glow = Mats.Glow(blue, 2f);
            var stone = ArtKit.Textured("FortPlaster", new Color(0.98f, 0.84f, 0.62f), 0.6f, 0.12f);
            Shapes.Box(g, "Floor", new Vector3(0, 0.03f, 0), new Vector3(GoalDepth, 0.02f, GoalHalf * 2), Mats.Glow(blue, 0.8f), false);
            Shapes.Box(g, "Back", new Vector3(GoalDepth / 2f + 0.3f, 1.4f, 0), new Vector3(0.5f, 2.8f, GoalHalf * 2 + 1f), stone);
            foreach (var z in new[] { -GoalHalf, GoalHalf })
            {
                Shapes.Box(g, "Tower", new Vector3(-GoalDepth / 2f, 1.6f, z + Mathf.Sign(z) * 0.4f), new Vector3(1.1f, 3.2f, 1.1f), stone);
                Shapes.Box(g, "Lamp", new Vector3(-GoalDepth / 2f - 0.6f, 2.6f, z), new Vector3(0.12f, 0.4f, 0.12f), Mats.Glow(gold, 2.2f), false);
            }
            Shapes.Box(g, "Bar", new Vector3(-GoalDepth / 2f, 3.1f, 0), new Vector3(0.4f, 0.25f, GoalHalf * 2), glow, false);
            var terminal = ArtKit.Model("VaultTerminal", g);
            if (terminal != null) ArtKit.Fit(terminal, new Vector3(1.6f, 2.6f, 3f), new Vector3(GoalDepth / 2f + 1.4f, 1.3f, 0), zForward: false);
            Shapes.Text3D(g, "OmanLNG DATA VAULT", new Vector3(0, 4.4f, 0), 0.085f, gold, true, Fonts.Title);
            Shapes.PointLight(g, new Vector3(-1.5f, 2f, 0), blue, 8f, 3f);
        }

        void BuildBall(Transform root)
        {
            _ball = new GameObject("DataPacket").transform;
            _ball.SetParent(root, false);
            _ballMat = Mats.GlowInstance(DataCyan, 1.4f);
            _ballRingMat = Mats.GlowInstance(DataCyan, 2.5f);
            _ballCore = new GameObject("Core").transform;
            _ballCore.SetParent(_ball, false);
            var chip = ArtKit.Model("DataChip", _ballCore);
            if (chip != null)
            {
                // The circuit-board model is the packet; a glowing core and a crossed ring read as a "ball".
                ArtKit.Fit(chip, Vector3.one * (BallRadius * 2f), Vector3.zero, zForward: false);
                Shapes.Ball(_ballCore, "Glow", Vector3.zero, BallRadius * 0.7f, _ballMat);
                Shapes.Ring(_ballCore, "Cross", BallRadius + 0.2f, 0.06f, _ballRingMat).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            }
            else
            {
                Shapes.Ball(_ball, "Shell", Vector3.zero, BallRadius * 2f, _ballMat);
            }
            _ballRing = Shapes.Ring(_ball, "Ring", BallRadius + 0.35f, 0.08f, _ballRingMat).transform;
            _ballLabel = Shapes.Text3D(_ball, "DATA", new Vector3(0, 1.6f, 0), 0.07f, DataCyan);
            Shapes.PointLight(_ball, Vector3.zero, DataCyan, 5f, 2.5f);
            // Shadow-catcher ring on the floor so the ball's position reads from above.
            var floorRing = Shapes.Ring(root, "BallShadow", BallRadius + 0.2f, 0.06f, Mats.Glow(DataCyan, 1f));
            floorRing.gameObject.AddComponent<FollowXZ>().Target = _ball;
        }

        void BuildProps(Transform root)
        {
            foreach (var p in new[] { new Vector3(-23f, 0, -14f), new Vector3(23f, 0, 14f), new Vector3(-23f, 0, 14f) })
            {
                var ac = ArtKit.Model("RoofAC", root);
                if (ac != null) ArtKit.Fit(ac, new Vector3(3f, 2f, 3f), p + Vector3.up, zForward: false);
                else Shapes.Box(root, "ACUnit", p + Vector3.up * 0.9f, new Vector3(3.2f, 1.8f, 2.4f), Mats.Lit(new Color(0.15f, 0.15f, 0.18f), 0.4f, 0.6f));
            }
            var ant = ArtKit.Model("RoofAntenna", root);
            if (ant != null) ArtKit.Fit(ant, new Vector3(1.5f, 8f, 1.5f), new Vector3(24f, 4f, -13f), zForward: false);
        }

        void UpdateBallVisual(float dt)
        {
            if (_ball == null) return;
            _ball.localPosition = _ballPos;
            var spin = new Vector3(_ballVel.z, 0, -_ballVel.x) * (dt * 60f / BallRadius);
            _ballCore.Rotate(spin, Space.World);
            _ballRing.Rotate(0, 120f * dt, 0, Space.Self);
        }

        void UpdateVisuals(float dt)
        {
            if (!_built) return;
            UpdateBallVisual(dt);
            bool enc = EncryptedLeft > 0f;
            var c = enc ? Palette.Gold : DataCyan;
            Mats.SetGlow(_ballMat, c, enc ? 1.6f : 1.2f + 0.2f * Mathf.Sin(Time.time * 5f));
            Mats.SetGlow(_ballRingMat, c, 2.5f);
            _ballLabel.text = enc ? $"ENCRYPTED {EncryptedLeft:0}s" : "DATA";
            _ballLabel.color = c;
            _lockBars.SetActive(LockLeft > 0f);
            Mats.SetGlow(_exfilGlow, new Color(1f, 0.15f, 0.25f), 1.8f + 0.6f * Mathf.Sin(Time.time * 4f));
        }
    }

    /// <summary>Keeps an object on the floor under a moving target.</summary>
    public sealed class FollowXZ : MonoBehaviour
    {
        public Transform Target;

        void LateUpdate()
        {
            if (Target == null) return;
            var p = Target.position;
            transform.position = new Vector3(p.x, 0.04f, p.z);
        }
    }
}
