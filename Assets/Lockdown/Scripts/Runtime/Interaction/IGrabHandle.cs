using UnityEngine;

namespace Lockdown
{
    // anything a hand can hold with the grip button
    public interface IGrabHandle
    {
        Transform transform { get; }
        int Priority { get; }
        bool CanGrab(HandController hand);
        void BeginGrab(HandController hand);
        void UpdateGrab(HandController hand);
        void EndGrab(HandController hand);
    }
}
