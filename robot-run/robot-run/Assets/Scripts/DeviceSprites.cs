using System.Collections.Generic;
using UnityEngine;

namespace RobotRun
{
    internal static class DeviceSprites
    {
        private const string Hatches = "botole-e-pedane/";
        private const string Crates = "casse-e-tnt/";
        private const string Belts = "nastri/";
        private static readonly Dictionary<string, Sprite> Sprites = new();

        internal static Sprite Get(DeviceState state) => Load(AssetName(state));

        internal static string AssetName(DeviceState state) =>
            state.Definition.Kind switch
            {
                DeviceKind.Conveyor => Belts + (state.Active ? "nastro-verso-destra-acceso" : "nastro-verso-destra-spento"),
                DeviceKind.PressurePlate => Hatches + (state.Active ? "pedana-premuta" : "pedana-libera"),
                DeviceKind.ToggleSwitch => Hatches + (state.Active ? "pulsante-a-scatto-acceso" : "pulsante-a-scatto-spento"),
                DeviceKind.OneShotSwitch => OneShotName(state.Active),
                DeviceKind.TimedSwitch => TimerSwitchName(state),
                DeviceKind.FragileHatch => Hatches + (state.Active ? "botola-fragile-aperta" : "botola-fragile-integra"),
                DeviceKind.SignalHatch => Hatches + (state.Active ? "botola-comandata-aperta" : "botola-comandata-chiusa"),
                DeviceKind.Trap => Hatches + (state.Active ? "trappola-attiva" : "trappola-spenta"),
                DeviceKind.FixedBlock => Crates + "blocco-fisso",
                DeviceKind.Crate => Crates + "cassa-mobile",
                DeviceKind.Tnt => Crates + (state.Armed ? state.Remaining switch
                {
                    0 => "tnt-0-cicli",
                    1 => "tnt-1-ciclo",
                    2 => "tnt-2-cicli",
                    _ => "tnt-pronta"
                } : "tnt-pronta"),
                DeviceKind.HardeningCrate => Crates + (state.Hardened ? "cassa-indurita"
                    : state.Armed && state.Remaining == 1 ? "cassa-indurente-1-ciclo" : "cassa-indurente-pronta"),
                _ => throw new System.ArgumentOutOfRangeException(nameof(state))
            };

        internal static Sprite OneShot(bool active) => Load(OneShotName(active));
        internal static Sprite ExplosionArea => Load(Crates + "area-esplosione-tnt");
        internal static Sprite TntZero => Load(Crates + "tnt-0-cicli");
        internal static Sprite HardeningZero => Load(Crates + "cassa-indurente-0-cicli");

        internal static Sprite TimerDisplay(DeviceState state) => Load(Belts + (state.Armed
            ? $"display-nastro-{state.Remaining}" : "display-nastro-pronto"));

        private static string OneShotName(bool active) =>
            Hatches + (active ? "pulsante-monouso-attivato" : "pulsante-monouso-pronto");

        private static string TimerSwitchName(DeviceState state) => Belts + (state.Armed ? state.Remaining switch
        {
            0 => "pulsante-a-timer-0-cicli",
            1 => "pulsante-a-timer-1-ciclo",
            2 => "pulsante-a-timer-2-cicli",
            _ => "pulsante-a-timer-pronto"
        } : "pulsante-a-timer-pronto");

        private static Sprite Load(string name)
        {
            if (Sprites.TryGetValue(name, out var sprite))
                return sprite;

            sprite = Resources.Load<Sprite>($"Mockup/Devices/{name}");
            if (sprite == null)
                Debug.LogError($"Missing device sprite: {name}");
            Sprites.Add(name, sprite);
            return sprite;
        }
    }
}
