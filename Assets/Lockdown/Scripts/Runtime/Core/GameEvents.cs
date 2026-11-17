using System;

namespace Lockdown
{
    // small global event hub so puzzles stay decoupled
    public static class GameEvents
    {
        public static event Action<Grabbable> OnItemGrabbed;
        public static event Action OnMistake;
        public static event Action<string> OnZoneEntered;

        public static void ItemGrabbed(Grabbable g) => OnItemGrabbed?.Invoke(g);
        public static void Mistake() => OnMistake?.Invoke();
        public static void ZoneEntered(string id) => OnZoneEntered?.Invoke(id);

        public static void Clear()
        {
            OnItemGrabbed = null;
            OnMistake = null;
            OnZoneEntered = null;
        }
    }

    // true while a menu wants pointers visible
    public static class UiFocus
    {
        public static bool Active;
    }
}
