using UnityEngine;

namespace BreachPoint
{
    /// <summary>Arena geometry and role visuals (placeholder art; replaced by the art pass).</summary>
    public sealed partial class FirewallArena
    {
        sealed class Server
        {
            public Vector3 Pos;
            public Material Strip;
            public Light Light;
            public float Flash;
            public GameObject TargetRing;
        }

        sealed class RoleVisuals
        {
            public GameObject Cannon;
            public GameObject Shield;
            public LineRenderer RingLow, RingHigh;
            public Material RingMat;
            public TextMesh ShieldLabel;
            public LineRenderer Aim;
            public Material AimMat;
        }

        Server[] _servers;
        RoleVisuals[] _roleVis;
        Material[] _zoneLine = new Material[2];   // 0 = attacker zone, 1 = defender zone
        Material[] _zoneFloor = new Material[2];

        void BuildArena()
        {
            var root = transform;
            var slab = ArtKit.TexturedSized("RoofConcrete", new Color(0.36f, 0.37f, 0.43f), new Vector2(14f, 9f), 0.6f, 0.1f);
            var wall = ArtKit.Textured("MetalPlate", new Color(0.3f, 0.3f, 0.36f), 1f, 0.6f, 0.5f);
            var grid = Mats.Glow(new Color(0.25f, 0.45f, 1f), 0.6f);
            var magenta = Mats.Glow(new Color(1f, 0.2f, 0.7f), 1.8f);

            // Rooftop slab with margin for props.
            Shapes.Box(root, "Roof", new Vector3(0, -0.5f, 0), new Vector3(54, 1, 36), slab);
            for (float x = -18; x <= 18.01f; x += 4) Shapes.Box(root, "GridX", new Vector3(x, 0.01f, 0), new Vector3(0.05f, 0.02f, 22), grid, false);
            for (float z = -10; z <= 10.01f; z += 4) Shapes.Box(root, "GridZ", new Vector3(0, 0.01f, z), new Vector3(38, 0.02f, 0.05f), grid, false);

            // Perimeter walls with neon top edge.
            void Wall(Vector3 p, Vector3 s)
            {
                Shapes.Box(root, "Wall", p, s, wall);
                Shapes.Box(root, "WallNeon", p + Vector3.up * (s.y / 2f + 0.03f), new Vector3(s.x, 0.06f, s.z), magenta, false);
            }
            Wall(new Vector3(0, 0.4f, 11.6f), new Vector3(39.4f, 0.8f, 0.4f));
            Wall(new Vector3(0, 0.4f, -11.6f), new Vector3(39.4f, 0.8f, 0.4f));
            Wall(new Vector3(-19.6f, 0.4f, 0), new Vector3(0.4f, 0.8f, 23.6f));
            // (east side is the SOC fort, built in BuildFort)

            // Zone floors + outlines (recoloured each half by team).
            for (int i = 0; i < 2; i++)
            {
                var zone = i == 0 ? AttackerZone : DefenderZone;
                _zoneFloor[i] = ArtKit.TexturedSized("RoofConcrete", new Color(0.36f, 0.37f, 0.43f), new Vector2(5f, 6f), 0.6f, 0.1f);
                _zoneLine[i] = Mats.GlowInstance(Color.white, 1.6f);
                var c = new Vector3(zone.center.x, 0.015f, zone.center.y);
                Shapes.Box(root, "ZoneFloor", c, new Vector3(zone.width + 2.4f, 0.01f, zone.height + 2.4f), _zoneFloor[i], false);
                float hx = zone.width / 2f + 1.2f, hz = zone.height / 2f + 1.2f;
                Shapes.Box(root, "ZoneLine", c + new Vector3(0, 0.01f, hz), new Vector3(hx * 2, 0.02f, 0.1f), _zoneLine[i], false);
                Shapes.Box(root, "ZoneLine", c + new Vector3(0, 0.01f, -hz), new Vector3(hx * 2, 0.02f, 0.1f), _zoneLine[i], false);
                Shapes.Box(root, "ZoneLine", c + new Vector3(hx, 0.01f, 0), new Vector3(0.1f, 0.02f, hz * 2), _zoneLine[i], false);
                Shapes.Box(root, "ZoneLine", c + new Vector3(-hx, 0.01f, 0), new Vector3(0.1f, 0.02f, hz * 2), _zoneLine[i], false);
            }

            // The "firewall": a row of glowing posts down the centre line.
            var post = Mats.Glow(new Color(0.85f, 0.9f, 1f), 1.4f);
            Shapes.Box(root, "FirewallBase", new Vector3(0, 0.08f, 0), new Vector3(0.35f, 0.16f, 22.4f), Mats.Lit(new Color(0.1f, 0.1f, 0.13f), 0.7f, 0.5f));
            for (float z = -10.5f; z <= 10.51f; z += 1.5f)
                Shapes.Box(root, "FirewallPost", new Vector3(0, 0.75f, z), new Vector3(0.12f, 1.5f, 0.12f), post, false);

            // Rooftop props in the margins.
            var prop = Mats.Lit(new Color(0.12f, 0.12f, 0.15f), 0.4f, 0.6f);
            foreach (var p in new[] { new Vector3(-23, 0, -14), new Vector3(23, 0, -14), new Vector3(-23, 0, 14), new Vector3(23, 0, 14) })
            {
                Shapes.Box(root, "ACUnit", p + new Vector3(0, 0.9f, 0), new Vector3(3.2f, 1.8f, 2.4f), prop);
                Shapes.Prim(PrimitiveType.Cylinder, root, "Fan", p + new Vector3(0, 1.85f, 0), new Vector3(1.6f, 0.05f, 1.6f), Mats.Lit(new Color(0.04f, 0.04f, 0.05f), 0.2f));
            }
            Shapes.Prim(PrimitiveType.Cylinder, root, "Tank", new Vector3(-24, 2.5f, 3), new Vector3(3, 2.5f, 3), prop);
            Shapes.Box(root, "Antenna", new Vector3(24.5f, 5, 2), new Vector3(0.2f, 10, 0.2f), prop);
            Shapes.Ball(root, "Beacon", new Vector3(24.5f, 10.2f, 2), 0.4f, Mats.Glow(Palette.Red, 3f));

            Shapes.PointLight(root, new Vector3(-10, 6, 0), new Color(1f, 0.4f, 0.6f), 22, 2.5f);
            Shapes.PointLight(root, new Vector3(9, 6, 0), new Color(0.4f, 0.7f, 1f), 22, 2.5f);

            BuildServers();
            BuildFort();
        }

        /// <summary>Omani-fort style SOC compound around the servers, with the Oman LNG × Otech SOC banner and logos.</summary>
        void BuildFort()
        {
            var root = new GameObject("SOC Fort").transform;
            root.SetParent(transform, false);
            // Omani forts are finished in sand-coloured plaster over stone.
            var stone = ArtKit.Textured("FortPlaster", new Color(0.98f, 0.84f, 0.62f), 0.6f, 0.12f);
            var stoneDark = ArtKit.Textured("FortStone", new Color(0.86f, 0.72f, 0.52f), 0.5f, 0.1f);
            var lantern = Mats.Glow(new Color(1f, 0.72f, 0.38f), 2.2f);
            const float back = 19.9f, side = 11.2f;

            void Battlement(Vector3 a, Vector3 b, float h, float thick)
            {
                var mid = (a + b) / 2f;
                var len = Vector3.Distance(a, b);
                bool alongZ = Mathf.Abs(b.z - a.z) > Mathf.Abs(b.x - a.x);
                var size = alongZ ? new Vector3(thick, h, len) : new Vector3(len, h, thick);
                var wallMat = ArtKit.TexturedSized("FortPlaster", new Color(0.98f, 0.84f, 0.62f), new Vector2(Mathf.Max(1f, len / 2.5f), Mathf.Max(1f, h / 2.5f)), 0.12f);
                Shapes.Box(root, "FortWall", mid + Vector3.up * (h / 2f), size, wallMat);
                Shapes.Box(root, "FortTrim", mid + Vector3.up * (h * 0.78f), alongZ ? new Vector3(thick + 0.08f, 0.14f, len) : new Vector3(len, 0.14f, thick + 0.08f), stoneDark);
                int n = Mathf.Max(1, Mathf.FloorToInt(len / 1.1f));
                for (int i = 0; i < n; i++)
                {
                    float t = (i + 0.5f) / n;
                    var p = Vector3.Lerp(a, b, t) + Vector3.up * (h + 0.2f);
                    Shapes.Box(root, "Merlon", p, alongZ ? new Vector3(thick, 0.4f, 0.5f) : new Vector3(0.5f, 0.4f, thick), stone);
                }
            }

            void Tower(Vector3 p, float r, float h)
            {
                Shapes.Prim(PrimitiveType.Cylinder, root, "Tower", p + Vector3.up * (h / 2f), new Vector3(r * 2, h / 2f, r * 2), stone);
                Shapes.Prim(PrimitiveType.Cylinder, root, "TowerBand", p + Vector3.up * (h * 0.8f), new Vector3(r * 2.1f, 0.08f, r * 2.1f), stoneDark);
                for (int i = 0; i < 8; i++)
                {
                    float a = i / 8f * Mathf.PI * 2f;
                    Shapes.Box(root, "Merlon", p + new Vector3(Mathf.Cos(a) * r * 0.85f, h + 0.2f, Mathf.Sin(a) * r * 0.85f), new Vector3(0.4f, 0.4f, 0.4f), stone);
                }
                Shapes.Box(root, "Window", p + new Vector3(-r * 0.98f, h * 0.6f, 0), new Vector3(0.06f, 0.6f, 0.3f), lantern, false);
            }

            Battlement(new Vector3(back, 0, -side), new Vector3(back, 0, side), 2.6f, 0.8f);
            Battlement(new Vector3(FortFrontX, 0, side), new Vector3(back, 0, side), 2.0f, 0.6f);
            Battlement(new Vector3(FortFrontX, 0, -side), new Vector3(back, 0, -side), 2.0f, 0.6f);

            // Front wall: low battlements with a gate in front of each system.
            float prev = -side;
            foreach (var lane in Lanes)
            {
                Battlement(new Vector3(FortFrontX, 0, prev), new Vector3(FortFrontX, 0, lane - GateHalfWidth), 1.1f, 0.6f);
                foreach (var dz in new[] { -GateHalfWidth, GateHalfWidth })
                {
                    Shapes.Box(root, "GatePost", new Vector3(FortFrontX, 0.9f, lane + dz), new Vector3(0.8f, 1.8f, 0.45f), stoneDark);
                    Shapes.Box(root, "Lantern", new Vector3(FortFrontX - 0.45f, 1.6f, lane + dz), new Vector3(0.18f, 0.28f, 0.18f), lantern, false);
                }
                prev = lane + GateHalfWidth;
            }
            Battlement(new Vector3(FortFrontX, 0, prev), new Vector3(FortFrontX, 0, side), 1.1f, 0.6f);

            Tower(new Vector3(FortFrontX, 0, side), 1.1f, 2.8f);
            Tower(new Vector3(FortFrontX, 0, -side), 1.1f, 2.8f);
            Tower(new Vector3(back, 0, side), 1.3f, 3.4f);
            Tower(new Vector3(back, 0, -side), 1.3f, 3.4f);
            Shapes.Box(root, "Courtyard", new Vector3((FortFrontX + back) / 2f, 0.02f, 0), new Vector3(back - FortFrontX, 0.02f, side * 2), Mats.Lit(new Color(0.5f, 0.4f, 0.28f), 0.3f), false);
            Shapes.PointLight(root, new Vector3(17f, 3.5f, 0), new Color(1f, 0.75f, 0.45f), 14f, 2.2f);
            // Warm floodlights wash the fort walls (sand colour reads at night).
            foreach (var z in new[] { -9f, -3f, 3f, 9f })
                Shapes.PointLight(root, new Vector3(11.5f, 4.5f, z), new Color(1f, 0.82f, 0.6f), 11f, 3.2f);
            Shapes.PointLight(root, new Vector3(17f, 6f, -14f), new Color(1f, 0.8f, 0.55f), 16f, 3f);

            // Banner above the fort, always facing the camera.
            var banner = new GameObject("Banner").transform;
            banner.SetParent(root, false);
            banner.localPosition = new Vector3(19.6f, 9.5f, 2f);
            banner.gameObject.AddComponent<Billboard>();
            var gold = new Color(1f, 0.78f, 0.35f);
            Shapes.Box(banner, "Board", Vector3.zero, new Vector3(11f, 1.9f, 0.15f), Mats.Lit(new Color(0.05f, 0.05f, 0.07f), 0.5f), false);
            Shapes.Box(banner, "Edge", new Vector3(0, 0.97f, 0), new Vector3(11f, 0.07f, 0.2f), Mats.Glow(gold, 2f), false);
            Shapes.Box(banner, "Edge", new Vector3(0, -0.97f, 0), new Vector3(11f, 0.07f, 0.2f), Mats.Glow(gold, 2f), false);
            bool hasLogo = ArtKit.Logo(banner, "OmanLNG_Logo", new Vector3(-4.05f, 0f, -0.12f), 1.7f, 1.25f) != null;
            float tx = hasLogo ? 1.1f : 0f;
            Shapes.Text3D(banner, "Oman LNG  ×  Otech SOC", new Vector3(tx, 0.22f, -0.12f), hasLogo ? 0.1f : 0.12f, gold, false, Fonts.Title);
            Shapes.Text3D(banner, "SECURITY OPERATIONS CENTRE", new Vector3(tx, -0.55f, -0.12f), 0.055f, Palette.Text, false);
            // The Oman LNG fort emblem flies above the fort's front towers.
            foreach (var z in new[] { side, -side })
                ArtKit.Logo(root, "OmanLNG_Emblem", new Vector3(FortFrontX, 4.3f, z), 1.7f, 1.05f, billboard: true);
        }

        void BuildServers()
        {
            _servers = new Server[Lanes.Length];
            var rack = ArtKit.Textured("MetalPlate", new Color(0.22f, 0.23f, 0.28f), 1f, 0.7f, 0.7f);
            string[] names = { "CORPORATE IT", "PLANT CONTROL", "LNG SHIPPING" };
            for (int i = 0; i < Lanes.Length; i++)
            {
                var s = new Server { Pos = new Vector3(ServerX, 1.2f, Lanes[i]) };
                var root = new GameObject(names[i]).transform;
                root.SetParent(transform, false);
                root.localPosition = new Vector3(ServerX, 0, Lanes[i]);
                var rackBox = Shapes.Box(root, "Rack", new Vector3(0.4f, 1.8f, 0), new Vector3(2f, 3.6f, 2.4f), rack);
                var rackModel = ArtKit.Model("ServerRack", root);
                if (rackModel != null)
                {
                    ArtKit.Fit(rackModel, new Vector3(2f, 3.6f, 2.4f), new Vector3(0.4f, 1.8f, 0), zForward: false);
                    rackBox.SetActive(false);
                }
                s.Strip = Mats.GlowInstance(new Color(0.3f, 0.95f, 1f), 1.6f);
                for (int k = 0; k < 5; k++)
                    Shapes.Box(root, "Led", new Vector3(-0.62f, 0.5f + k * 0.62f, 0), new Vector3(0.05f, 0.08f, 2f), s.Strip, false);
                s.Light = Shapes.PointLight(root, new Vector3(-1.5f, 2f, 0), new Color(0.3f, 0.9f, 1f), 7f, 2.5f);
                Shapes.Text3D(root, names[i], new Vector3(-0.6f, 4.3f, 0), 0.085f, Palette.Text);
                var ring = Shapes.Ring(root, "Target", 1.9f, 0.12f, Mats.Glow(Palette.Gold, 1.8f));
                ring.transform.localPosition = new Vector3(0.4f, 0.06f, 0);
                s.TargetRing = ring.gameObject;
                _servers[i] = s;
            }
        }

        void BuildRoleVisuals()
        {
            _roleVis = new RoleVisuals[2];
            for (int i = 0; i < 2; i++)
            {
                var v = _veh[i];
                var rv = new RoleVisuals();
                var team = _s.Players[i].Color;

                rv.Cannon = new GameObject("Cannon");
                rv.Cannon.transform.SetParent(v.Mount, false);
                if (!v.HasModel) // the ship model is already armed; the box turret is only for the placeholder hull
                {
                    Shapes.Box(rv.Cannon.transform, "Turret", new Vector3(-0.1f, 0.05f, 0), new Vector3(0.7f, 0.3f, 0.7f), Mats.Lit(new Color(0.1f, 0.1f, 0.12f), 0.8f, 0.7f));
                    Shapes.Box(rv.Cannon.transform, "Barrel", new Vector3(0.55f, 0.08f, 0), new Vector3(1.1f, 0.14f, 0.14f), Mats.Lit(new Color(0.15f, 0.15f, 0.18f), 0.8f, 0.8f));
                    Shapes.Box(rv.Cannon.transform, "Muzzle", new Vector3(1.12f, 0.08f, 0), new Vector3(0.08f, 0.2f, 0.2f), Mats.Glow(team, 5f), false);
                }

                rv.Shield = new GameObject("Shield");
                rv.Shield.transform.SetParent(v.transform, false);
                rv.RingMat = Mats.GlowInstance(Color.white, 3f);
                rv.RingLow = Shapes.Ring(rv.Shield.transform, "RingLow", ShieldRadius, 0.14f, rv.RingMat, 64);
                rv.RingLow.transform.localPosition = new Vector3(0, 0.25f, 0);
                rv.RingHigh = Shapes.Ring(rv.Shield.transform, "RingHigh", ShieldRadius * 0.92f, 0.08f, rv.RingMat, 64);
                rv.RingHigh.transform.localPosition = new Vector3(0, 1.35f, 0);
                rv.ShieldLabel = Shapes.Text3D(rv.Shield.transform, "", new Vector3(0, 2.6f, 0), 0.075f, Color.white);

                var aimGo = new GameObject("Aim");
                aimGo.transform.SetParent(transform, false);
                rv.AimMat = Mats.GlowInstance(team, 1.2f);
                rv.Aim = aimGo.AddComponent<LineRenderer>();
                rv.Aim.positionCount = 2;
                rv.Aim.widthMultiplier = 0.06f;
                rv.Aim.sharedMaterial = rv.AimMat;
                rv.Aim.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _roleVis[i] = rv;
            }
        }

        void ApplyRoleVisuals()
        {
            for (int i = 0; i < 2; i++)
            {
                bool atk = i == AttackerIndex;
                _roleVis[i].Cannon.SetActive(atk);
                _roleVis[i].Aim.gameObject.SetActive(atk);
                _roleVis[i].Shield.SetActive(!atk);
            }
            _zoneLine[0].SetColor("_BaseColor", Palette.Hdr(_s.Players[AttackerIndex].Color, 2.5f));
            _zoneLine[1].SetColor("_BaseColor", Palette.Hdr(_s.Players[DefenderIndex].Color, 2.5f));
            var concrete = new Color(0.33f, 0.34f, 0.4f);
            _zoneFloor[0].SetColor("_BaseColor", Color.Lerp(concrete, _s.Players[AttackerIndex].Color, 0.22f));
            _zoneFloor[1].SetColor("_BaseColor", Color.Lerp(concrete, _s.Players[DefenderIndex].Color, 0.22f));
        }

        void UpdateVisuals(float dt)
        {
            // Servers: hit flash fades back to cyan; target ring marks the attacker's lane.
            for (int i = 0; i < _servers.Length; i++)
            {
                var s = _servers[i];
                s.Flash = Mathf.Max(0, s.Flash - dt * 3f);
                var c = Color.Lerp(new Color(0.3f, 0.95f, 1f), Palette.Bad, s.Flash);
                Mats.SetGlow(s.Strip, c, 1.6f + s.Flash * 2.5f);
                s.Light.color = c;
                s.TargetRing.SetActive(_playing && i == _lane);
                if (s.TargetRing.activeSelf) s.TargetRing.transform.Rotate(0, 90f * dt, 0);
            }

            // Defender shield colour = current defense; flashes red on a wrong defense.
            var rv = _roleVis[DefenderIndex];
            _wrongFlash = Mathf.Max(0, _wrongFlash - dt * 3f);
            _blockFlash = Mathf.Max(0, _blockFlash - dt * 4f);
            var sc = Color.Lerp(Attacks.Color(_shield), Palette.Bad, _wrongFlash);
            Mats.SetGlow(rv.RingMat, sc, 1.8f + _blockFlash * 3f);
            rv.ShieldLabel.text = Attacks.Defense(_shield);
            rv.ShieldLabel.color = sc;
            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * 6f) + _blockFlash * 0.12f;
            rv.Shield.transform.localScale = new Vector3(pulse, 1f, pulse);
            rv.RingHigh.transform.Rotate(0, 60f * dt, 0);

            // Attacker aim line to the targeted server.
            var av = _veh[AttackerIndex];
            var aim = _roleVis[AttackerIndex].Aim;
            aim.SetPosition(0, av.transform.position + Vector3.up * 1.3f);
            aim.SetPosition(1, _servers[_lane].Pos);
            Mats.SetGlow(_roleVis[AttackerIndex].AimMat, _s.Players[AttackerIndex].Color, _stun > 0 ? 0.2f : 1.2f);
            _roleVis[AttackerIndex].Cannon.transform.localRotation = Quaternion.identity;
        }
    }
}
