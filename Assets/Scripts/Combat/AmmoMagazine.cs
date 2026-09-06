using System;

namespace GunQuest.Combat
{
    /// <summary>Ammo accounting shared by gameplay and the editor validation suite.</summary>
    public sealed class AmmoMagazine
    {
        public int Capacity { get; }
        public int Loaded { get; private set; }
        public int Reserve { get; private set; }
        public bool CanReload => Loaded < Capacity && Reserve > 0;

        public AmmoMagazine(int capacity, int reserve)
        {
            Capacity = Math.Max(1, capacity);
            Loaded = Capacity;
            Reserve = Math.Max(0, reserve);
        }

        public bool TryFire()
        {
            if (Loaded == 0) return false;
            Loaded--;
            return true;
        }

        public void Reload()
        {
            int transfer = Math.Min(Capacity - Loaded, Reserve);
            Loaded += transfer;
            Reserve -= transfer;
        }

        public void Supply(int amount) => Reserve = (int)Math.Min(999L, (long)Reserve + Math.Max(0, amount));
    }
}
