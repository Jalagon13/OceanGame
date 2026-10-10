using FMODUnity;
using UnityEngine;

namespace OceanGame
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance { get; private set; }

        [field: Header("Ambience")]
        [field: SerializeField] public EventReference OceanAmbience { get; private set; }
        [field: SerializeField] public EventReference SurfaceAmbience { get; private set; }

        [field: Header("UI")]
        [field: SerializeField] public EventReference InventoryOpenSFX { get; private set; }
        [field: SerializeField] public EventReference InventoryCloseSFX { get; private set; }
        [field: SerializeField] public EventReference SelectedSlotChangedSFX { get; private set; }
        [field: SerializeField] public EventReference ItemPickupSFX { get; private set; }
        [field: SerializeField] public EventReference SlotClickedSFX { get; private set; }
        [field: SerializeField] public EventReference OxygenWarningSFX { get; private set; }

        [field: Header("Player")]
        [field: SerializeField] public EventReference OxygenReplenishSFX { get; private set; }
        [field: SerializeField] public EventReference PlayerSwimSFX { get; private set; }
        [field: SerializeField] public EventReference DrillSFX { get; private set; }

        [field: Header("World")]
        [field: SerializeField] public EventReference HatchOpen { get; private set; }
        [field: SerializeField] public EventReference HatchClosed { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
    }
}
