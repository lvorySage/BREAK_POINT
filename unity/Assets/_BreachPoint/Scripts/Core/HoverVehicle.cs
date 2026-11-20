using UnityEngine;

namespace BreachPoint
{
    /// <summary>
    /// The player's hover vehicle, shared by every mini-game. Placeholder geometry for now;
    /// the art pass swaps the body for a model without changing the driving code.
    /// </summary>
    public sealed class HoverVehicle : MonoBehaviour
    {
        public float MaxSpeed = 11f;
        public float Accel = 48f;
        public float BoostMultiplier = 1.8f;
        public float BoostTime = 0.35f;
        public float BoostCooldown = 1.8f;

        public Vector3 Velocity { get; private set; }
        public bool Boosting => _boostLeft > 0f;
        public float BoostReady => 1f - Mathf.Clamp01(_boostCd / BoostCooldown);

        Transform _body;
        Team _team;
        float _boostLeft, _boostCd, _bobSeed;
        Vector3 _face = Vector3.right;

        public Transform Body => _body;
        public bool HasModel { get; private set; }
        public Transform Mount { get; private set; }

        public static HoverVehicle Create(Transform parent, Team team, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var v = root.AddComponent<HoverVehicle>();
            v._team = team;
            v.BuildVisuals();
            return v;
        }

        void BuildVisuals()
        {
            var c = Palette.Of(_team);
            _bobSeed = Random.value * 10f;
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            _body.localPosition = new Vector3(0, 0.75f, 0);
            _body.localScale = Vector3.one * 1.35f; // bigger body; shield/ground rings keep true gameplay radii

            var hull = ArtKit.Textured("MetalPlate", new Color(0.32f, 0.33f, 0.38f), 1f, 0.75f, 0.6f);
            var paint = Mats.Lit(c, 0.75f, 0.3f, Palette.Hdr(c, 0.5f));
            var glow = Mats.Glow(c, 2.4f);
            var cyan = Mats.Glow(new Color(0.4f, 0.9f, 1f), 2f);

            Shapes.Box(_body, "Hull", Vector3.zero, new Vector3(2.3f, 0.45f, 1.5f), hull);
            Shapes.Box(_body, "Paint", new Vector3(-0.15f, 0.26f, 0), new Vector3(1.6f, 0.12f, 1.25f), paint);
            Shapes.Box(_body, "Cockpit", new Vector3(0.25f, 0.42f, 0), new Vector3(0.8f, 0.3f, 0.8f), Mats.Lit(new Color(0.05f, 0.08f, 0.12f), 0.95f, 0.2f, Palette.Hdr(c, 0.6f)));
            Shapes.Box(_body, "StripL", new Vector3(0, 0, 0.77f), new Vector3(2.2f, 0.08f, 0.05f), glow, false);
            Shapes.Box(_body, "StripR", new Vector3(0, 0, -0.77f), new Vector3(2.2f, 0.08f, 0.05f), glow, false);
            Shapes.Box(_body, "Nose", new Vector3(1.2f, 0, 0), new Vector3(0.12f, 0.2f, 1.3f), glow, false);
            foreach (var x in new[] { -0.8f, 0.8f })
            foreach (var z in new[] { -0.55f, 0.55f })
                Shapes.Prim(PrimitiveType.Cylinder, _body, "Pad", new Vector3(x, -0.28f, z), new Vector3(0.45f, 0.04f, 0.45f), cyan, false);

            Shapes.PointLight(_body, new Vector3(0, -0.6f, 0), c, 4.5f, 3f);
            var ground = Shapes.Ring(transform, "GroundRing", 1.9f, 0.08f, Mats.Glow(c, 1.6f));
            ground.transform.localPosition = new Vector3(0, 0.05f, 0);

            Mount = new GameObject("Mount").transform;
            Mount.SetParent(_body, false);
            Mount.localPosition = new Vector3(0, 0.5f, 0);

            // Art pass: a real hover-craft model replaces the placeholder hull (team glow strips stay).
            var model = ArtKit.Model("HoverVehicle", _body);
            if (model != null)
            {
                // Quaternius ships are modelled nose-forward along +Z.
                ArtKit.Fit(model, new Vector3(3.0f, 1.1f, 2.8f), new Vector3(0, 0.1f, 0), zForward: true);
                // Prefer the real team livery texture; otherwise repaint the accent colour.
                var livery = Resources.Load<Texture2D>("Art/Models/HoverVehicle_" + (_team == Team.Red ? "Red" : "Blue"));
                if (livery != null) ArtKit.ApplyTexture(model, livery);
                else ArtKit.TeamPaint(model, c);
                foreach (Transform t in _body)
                {
                    if (t.name is "Hull" or "Paint" or "Cockpit" or "Nose" or "StripL" or "StripR" or "Pad") t.gameObject.SetActive(false);
                }
                HasModel = true;
                // Soft key light so the livery reads under the night lighting.
                Shapes.PointLight(_body, new Vector3(0.4f, 2.2f, -0.6f), new Color(1f, 0.95f, 0.9f), 5f, 2.2f);
            }
        }

        public void Face(Vector3 dir)
        {
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) _face = dir.normalized;
        }

        public void Teleport(Vector3 pos)
        {
            transform.position = pos;
            Velocity = Vector3.zero;
        }

        /// <summary>Moves inside an XZ rectangle. Call once per simulation step.</summary>
        public void Drive(Vector2 input, bool boost, float dt, Rect zone, float speedScale = 1f)
        {
            if (_boostCd > 0f) _boostCd -= dt;
            if (_boostLeft > 0f) _boostLeft -= dt;
            if (boost && _boostCd <= 0f && input.sqrMagnitude > 0.05f)
            {
                _boostLeft = BoostTime;
                _boostCd = BoostCooldown;
                Sfx.Play("boost", 0.6f);
            }

            float top = MaxSpeed * speedScale * (Boosting ? BoostMultiplier : 1f);
            var target = new Vector3(input.x, 0, input.y) * top;
            Velocity = Vector3.MoveTowards(Velocity, target, Accel * (Boosting ? 2f : 1f) * dt);

            var p = transform.position + Velocity * dt;
            var v = Velocity;
            if (p.x < zone.xMin) { p.x = zone.xMin; v.x = 0; }
            if (p.x > zone.xMax) { p.x = zone.xMax; v.x = 0; }
            if (p.z < zone.yMin) { p.z = zone.yMin; v.z = 0; }
            if (p.z > zone.yMax) { p.z = zone.yMax; v.z = 0; }
            Velocity = v;
            transform.position = p;
            Animate(dt);
        }

        void Animate(float dt)
        {
            float t = Time.time + _bobSeed;
            _body.localPosition = new Vector3(0, 0.75f + Mathf.Sin(t * 3.1f) * 0.07f, 0);
            // Body's long axis is local +X; face it along _face.
            var yaw = Quaternion.FromToRotation(Vector3.right, _face);
            var local = Quaternion.Inverse(transform.rotation) * Velocity;
            var tilt = Quaternion.Euler(local.z * 1.6f, 0f, -local.x * 1.6f);
            _body.rotation = Quaternion.Slerp(_body.rotation, yaw * tilt, 1f - Mathf.Exp(-12f * dt));
        }
    }

    /// <summary>Gas-flare flicker: scales the flame and its light with noise.</summary>
    public sealed class Flicker : MonoBehaviour
    {
        public Light Light;
        Vector3 _base;
        float _seed;

        void Start()
        {
            _base = transform.localScale;
            _seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * 3f);
            transform.localScale = new Vector3(_base.x * (0.85f + 0.3f * n), _base.y * (0.75f + 0.5f * n), _base.z * (0.85f + 0.3f * n));
            if (Light != null) Light.intensity = 6f + 6f * n;
        }
    }

    /// <summary>Procedural cyberpunk skyline with neon windows and awareness billboards.</summary>
    public static class City
    {
        /// <summary>LNG storage tanks, process columns and a gas flare on the horizon (OmanLNG theme).</summary>
        public static void LngPlant(Transform parent, Vector3 origin)
        {
            var root = new GameObject("LNG Plant").transform;
            root.SetParent(parent, false);
            root.localPosition = origin;
            var white = Mats.Lit(new Color(0.78f, 0.8f, 0.84f), 0.45f, 0.2f);
            var steel = Mats.Lit(new Color(0.35f, 0.37f, 0.42f), 0.6f, 0.7f);
            var red = Mats.Glow(Palette.Red, 3f);

            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(-48f + i * 24f, 0, (i % 2) * 10f);
                Shapes.Prim(PrimitiveType.Cylinder, root, "LNG Tank", p + new Vector3(0, 9f, 0), new Vector3(17f, 9f, 17f), white, false);
                Shapes.Ball(root, "Dome", p + new Vector3(0, 18f, 0), 1f, white).transform.localScale = new Vector3(17f, 5f, 17f);
                Shapes.Ball(root, "AirLight", p + new Vector3(0, 20.6f, 0), 0.6f, red);
                Shapes.Box(root, "Band", p + new Vector3(0, 12f, 0), new Vector3(17.2f, 0.5f, 0.3f), Mats.Glow(new Color(0.3f, 0.8f, 1f), 1.2f), false);
            }
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(-30f + i * 9f, 0, -16f);
                float h = 14f + (i % 3) * 6f;
                Shapes.Prim(PrimitiveType.Cylinder, root, "Column", p + new Vector3(0, h / 2f, 0), new Vector3(2.2f, h / 2f, 2.2f), steel, false);
                Shapes.Ball(root, "ColumnLight", p + new Vector3(0, h + 0.4f, 0), 0.5f, Mats.Glow(new Color(1f, 0.75f, 0.3f), 3f));
            }
            Shapes.Box(root, "PipeRack", new Vector3(0, 6f, -22f), new Vector3(110f, 0.8f, 1.2f), steel, false);
            Shapes.Box(root, "PipeRack", new Vector3(0, 8f, -24f), new Vector3(110f, 0.6f, 0.9f), steel, false);

            // Flare stack with a living flame.
            var flareBase = new Vector3(62f, 0, 4f);
            Shapes.Prim(PrimitiveType.Cylinder, root, "FlareStack", flareBase + new Vector3(0, 20f, 0), new Vector3(1.4f, 20f, 1.4f), steel, false);
            var flame = Shapes.Ball(root, "Flame", flareBase + new Vector3(0, 42.5f, 0), 1f, Mats.Glow(new Color(1f, 0.55f, 0.15f), 4f));
            flame.transform.localScale = new Vector3(3.2f, 5.5f, 3.2f);
            var fl = flame.AddComponent<Flicker>();
            fl.Light = Shapes.PointLight(root, flareBase + new Vector3(0, 42f, 0), new Color(1f, 0.6f, 0.25f), 60f, 8f);
        }

        static readonly string[] Signs =
        {
            "THINK BEFORE YOU CLICK", "PATCH • UPDATE • REPEAT", "MFA = ON", "REPORT PHISHING",
            "LOCK YOUR SCREEN", "STRONG PASSWORDS", "VERIFY THE SENDER", "BACK UP YOUR DATA",
        };

        static readonly Color[] Neon =
        {
            new Color(1f, 0.2f, 0.6f), new Color(0.2f, 0.9f, 1f), new Color(0.6f, 0.3f, 1f),
            new Color(1f, 0.7f, 0.2f), new Color(0.3f, 1f, 0.6f),
        };

        public static Transform Build(Transform parent, Vector2 innerHalfSize, int seed = 7)
        {
            var root = new GameObject("City").transform;
            root.SetParent(parent, false);
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            var dark = Mats.Lit(new Color(0.05f, 0.055f, 0.08f), 0.55f, 0.3f);
            var darker = Mats.Lit(new Color(0.03f, 0.035f, 0.05f), 0.3f, 0.1f);

            // Street level far below the rooftop.
            Shapes.Box(root, "Street", new Vector3(0, -30f, 0), new Vector3(400, 1, 400), darker, false);

            int signIndex = 0;
            for (int i = 0; i < 90; i++)
            {
                float ang = R() * Mathf.PI * 2f;
                float dist = 34f + R() * 70f;
                var pos = new Vector3(Mathf.Cos(ang) * dist, 0, Mathf.Sin(ang) * dist);
                if (Mathf.Abs(pos.x) < innerHalfSize.x + 8 && Mathf.Abs(pos.z) < innerHalfSize.y + 8) continue;
                // keep the camera's view (south side, close) clear
                if (pos.z < -20f && Mathf.Abs(pos.x) < 40f && dist < 60f) continue;
                // keep the LNG plant area (north) clear
                if (pos.z > 45f && Mathf.Abs(pos.x) < 80f) continue;
                float w = 6f + R() * 10f, d = 6f + R() * 10f, h = 12f + R() * 55f;
                var b = Shapes.Box(root, "Tower", pos + new Vector3(0, h / 2f - 30f, 0), new Vector3(w, h, d), R() > 0.4f ? dark : darker, false).transform;

                var neon = Neon[rnd.Next(Neon.Length)];
                int strips = 1 + rnd.Next(4);
                for (int s = 0; s < strips; s++)
                {
                    float y = (R() - 0.5f) * 0.9f;
                    bool vertical = R() > 0.7f;
                    var m = Mats.Glow(neon, 1.5f + R() * 3f);
                    Shapes.Box(b, "Neon", new Vector3(0, y, 0),
                        vertical ? new Vector3(1.02f, 0.5f, 0.04f) : new Vector3(1.02f, 0.012f, 1.02f), m, false);
                }

                if (signIndex < Signs.Length && dist < 70f && h > 30f && R() > 0.35f)
                {
                    var toCenter = (-pos).normalized;
                    var sign = new GameObject("Sign").transform;
                    sign.SetParent(root, false);
                    sign.position = pos + toCenter * (Mathf.Max(w, d) * 0.55f + 0.5f) + Vector3.up * (h - 30f - 6f - R() * 8f);
                    sign.rotation = Quaternion.LookRotation(-toCenter, Vector3.up);
                    var col = Neon[rnd.Next(Neon.Length)];
                    Shapes.Box(sign, "Board", Vector3.zero, new Vector3(14f, 3.2f, 0.2f), Mats.Lit(new Color(0.02f, 0.02f, 0.03f), 0.2f), false);
                    Shapes.Box(sign, "Frame", new Vector3(0, 1.65f, 0), new Vector3(14f, 0.12f, 0.25f), Mats.Glow(col, 3f), false);
                    Shapes.Box(sign, "Frame", new Vector3(0, -1.65f, 0), new Vector3(14f, 0.12f, 0.25f), Mats.Glow(col, 3f), false);
                    // Text reads correctly when viewed along its +Z, i.e. from the arena looking out.
                    Shapes.Text3D(sign, Signs[signIndex++], new Vector3(0, 0, -0.15f), 0.2f, col, false, Fonts.Title);
                }
            }
            return root;
        }
    }
}
