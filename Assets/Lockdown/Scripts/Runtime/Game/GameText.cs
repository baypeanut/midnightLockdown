namespace Lockdown
{
    public enum PuzzleId { Power, Clock, Safe, Laser, Reboot }
    public enum GameState { Briefing, Playing, Paused, Won, Lost }
    public enum Stage { Power, Catwalk, Office, Laser, Reboot, Done }

    // all player facing text in one place
    public static class GameText
    {
        public static string Objective(Stage s)
        {
            switch (s)
            {
                case Stage.Power: return "Restore power: find three fuses and fill the breaker panel.";
                case Stage.Catwalk: return "Reach Kowalski's office over the maintenance catwalk.";
                case Stage.Office: return "Search Kowalski's office: the clock and the safe.";
                case Stage.Laser: return "Power the laser and bounce it into the vault sensor.";
                case Stage.Reboot: return "Insert the override card and reboot WARDEN.";
                default: return "Lockdown lifted. You made it.";
            }
        }

        public static string Short(Stage s)
        {
            switch (s)
            {
                case Stage.Power: return "Restore power";
                case Stage.Catwalk: return "Cross the catwalk";
                case Stage.Office: return "Clock and safe";
                case Stage.Laser: return "Align the laser";
                case Stage.Reboot: return "Reboot WARDEN";
                default: return "Escaped";
            }
        }

        public static string Hint(Stage s, int tier, RunSecrets k)
        {
            switch (s)
            {
                case Stage.Power:
                    return tier == 0 ? "The breaker panel on the back wall has three empty fuse slots."
                         : tier == 1 ? "Fuses are hidden around the floor: a desk drawer, the cardboard boxes and on top of the tall cabinet."
                         : "Match each fuse rating to the slot label (" + k.FuseOrderText + "), then pull the big lever down.";
                case Stage.Catwalk:
                    return tier == 0 ? "Kowalski's door is sealed from this side. Look up."
                         : tier == 1 ? "The ladder beside the storage shelves leads up to the catwalk."
                         : "Squeeze grip on a rung and pull your hand down to climb. Cross the catwalk and climb down the far ladder.";
                case Stage.Office:
                    return tier == 0 ? "Two things matter in here: the stopped wall clock and the safe."
                         : tier == 1 ? "The backup log on the desk tells you when things last worked. The safe dial talks to your hand, not your eyes."
                         : "Clock: set it to " + k.ClockText + ". Safe: turn slowly, feel the strong click, then reverse to lock it in. Numbers: " + k.SafeText + ".";
                case Stage.Laser:
                    return tier == 0 ? "The vault sensor needs a laser beam, and the emitter needs power."
                         : tier == 1 ? "The heavy battery from the safe fits the emitter cradle. Then turn the mirror pedestals."
                         : "Grab a mirror's handle and swing it in steps. Follow the beam from mirror to mirror until it lands on the sensor.";
                case Stage.Reboot:
                    return tier == 0 ? "The WARDEN console is inside the vault."
                         : tier == 1 ? "Insert the override card into the reader on the console."
                         : "Then hold the red reboot button until the ring fills. Do not let go.";
                default: return "Nothing left to do. Enjoy the view.";
            }
        }
    }
}
