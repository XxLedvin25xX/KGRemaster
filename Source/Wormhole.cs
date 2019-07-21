using UnityEngine;

namespace KerbalGalaxyRemaster
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Wormhole : MonoBehaviour
    {
        private static CelestialBody sun, gargantua;

        // Settings
        private static readonly string settingsFile = $"{KSPUtil.ApplicationRootPath}GameData/KerbalGalaxyRemaster/WormholeSettings.cfg";

        private static bool loaded;
        private static ConfigNode settings;

        private static bool modEnabled = true;
        private static double jumpAltitude = 100.0;
        private static double speedLimit = 500.0;

        private void Awake()
        {
            if (gargantua == null || sun == null)
            {
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body.gameObject.name == "Gargantua")
                        gargantua = body;

                    if (body.gameObject.name == "Sun")
                        sun = body;
                }

                Debug.Log("[KerbalGalaxyRemaster]: Celestial bodies loaded.");
            }

            if (!loaded)
            {
                LoadSettings();

                if (!modEnabled)
                {
                    Debug.Log("[KerbalGalaxyRemaster]: Mod disabled. Check settings.");
                    Destroy(this);
                }

                Debug.Log($"[KerbalGalaxyRemaster]: Settings loaded. Jump Altitude: {jumpAltitude}, Speed Limit: {speedLimit}");
            }
        }

        private void Update()
        {
            if (FlightGlobals.ActiveVessel.mainBody.name.Equals(nameof(Wormhole)) && FlightGlobals.ActiveVessel.altitude < jumpAltitude)
            {
                if (FlightGlobals.ActiveVessel.speed > speedLimit)
                {
                    ScreenMessages.PostScreenMessage("Too fast! Hyperspace jump failed.", 5f, ScreenMessageStyle.UPPER_CENTER);
                    ChangeOrbit(sun, 1.0);
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Hyperspace jump!", 5f, ScreenMessageStyle.UPPER_CENTER);
                    ChangeOrbit(gargantua, RandomAlt() * 1000000000);
                }
            }

            if (!FlightGlobals.ActiveVessel.mainBody.name.Equals(nameof(Wormhole)) || FlightGlobals.ActiveVessel.altitude >= jumpAltitude + 900.0)
                return;

            NearWormhole();
        }

        private static ConfigNode LoadSettingsFile()
        {
            return settings ?? (settings = ConfigNode.Load(settingsFile) ?? new ConfigNode());
        }

        private static void LoadSettings()
        {
            if (settings == null)
                LoadSettingsFile();

            if (settings != null)
            {
                ConfigNode node = settings.HasNode("Settings") ? settings.GetNode("Settings") : settings.AddNode("Settings");

                modEnabled = node.HasValue("ModEnabled") ? bool.Parse(node.GetValue("ModEnabled")) : modEnabled;
                jumpAltitude = node.HasValue("JumpAltitude") ? double.Parse(node.GetValue("JumpAltitude")) : jumpAltitude;
                speedLimit = node.HasValue("SpeedLimit") ? double.Parse(node.GetValue("SpeedLimit")) : speedLimit;

                loaded = true;
            }
        }

        private static int RandomAlt()
        {
            return new System.Random().Next(1, 150);
        }

        private void ChangeOrbit(CelestialBody body, double alt)
        {
            Orbit randomOrbitAround = Orbit.CreateRandomOrbitAround(body, body.Radius + alt, body.Radius + alt);
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            CelestialBody referenceBody = activeVessel.orbit.referenceBody;

            if (activeVessel != null)
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
                activeVessel.GoOnRails();
                activeVessel.orbitDriver.orbit.inclination = randomOrbitAround.inclination;
                activeVessel.orbitDriver.orbit.eccentricity = randomOrbitAround.eccentricity;
                activeVessel.orbitDriver.orbit.LAN = randomOrbitAround.LAN;
                activeVessel.orbitDriver.orbit.semiMajorAxis = randomOrbitAround.semiMajorAxis;
                activeVessel.orbitDriver.orbit.argumentOfPeriapsis = randomOrbitAround.argumentOfPeriapsis;
                activeVessel.orbitDriver.orbit.meanAnomalyAtEpoch = randomOrbitAround.meanAnomalyAtEpoch;
                activeVessel.orbitDriver.orbit.epoch = randomOrbitAround.epoch;
                activeVessel.orbitDriver.orbit.referenceBody = randomOrbitAround.referenceBody;
                activeVessel.orbitDriver.orbit.Init();
                activeVessel.orbitDriver.orbit.UpdateFromUT(Planetarium.GetUniversalTime());
            }

            activeVessel.orbitDriver.OnReferenceBodyChange?.Invoke(randomOrbitAround.referenceBody);

            activeVessel.orbitDriver.pos = activeVessel.orbit.pos.xzy;
            activeVessel.orbitDriver.vel = activeVessel.orbit.vel;
            GameEvents.onVesselSOIChanged.Fire(new GameEvents.HostedFromToAction<Vessel, CelestialBody>(activeVessel, referenceBody, activeVessel.orbitDriver.referenceBody));
        }

        private void NearWormhole()
        {
            if (FlightGlobals.ActiveVessel.speed < speedLimit)
                ScreenMessages.PostScreenMessage($"Current speed: {(int)FlightGlobals.ActiveVessel.speed}m/s. || Max speed: {(int)speedLimit}m/s.", 0.0f, ScreenMessageStyle.UPPER_CENTER);
            else
                ScreenMessages.PostScreenMessage($"Current speed: {(int)FlightGlobals.ActiveVessel.speed}m/s. || Exceeded the maximum speed!", 0.0f, ScreenMessageStyle.UPPER_CENTER);

            if (!FlightGlobals.ActiveVessel.mainBody.name.Equals(nameof(Wormhole)) || FlightGlobals.ActiveVessel.altitude >= jumpAltitude + 100.0)
                return;

            ScreenMessages.PostScreenMessage("Transition into hyperspace...", 0.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
