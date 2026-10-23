using FMODUnity;
using UnityEngine;

namespace SketchLand
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance { get; private set; }

        [field: Header("Ambience")]
        [field: SerializeField] public EventReference SurfaceAmbience { get; private set; }

        [field: Header("UI")]
        [field: SerializeField] public EventReference InventoryOpenSFX { get; private set; }
        [field: SerializeField] public EventReference InventoryCloseSFX { get; private set; }
        [field: SerializeField] public EventReference HotbarSlotChangedSFX { get; private set; }
        [field: SerializeField] public EventReference ItemPickupSFX { get; private set; }
        [field: SerializeField] public EventReference SlotClickedSFX { get; private set; }

        [field: Header("Player")]
        [field: SerializeField] public EventReference PlayerAirThrustSFX { get; private set; }
        [field: SerializeField] public EventReference WaterEnterSFX { get; private set; }
        [field: SerializeField] public EventReference WaterExitSFX { get; private set; }

        [field: Header("World")]
        [field: SerializeField] public EventReference DoorOpen { get; private set; }
        [field: SerializeField] public EventReference DoorClosed { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
    }
}
