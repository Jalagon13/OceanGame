using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace OceanGame
{
    public class Item : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sprite;
    
        [Header("Detection Setup")]
        [SerializeField] private LayerMask _playerLayer;
        [SerializeField] private float _ableToCollectTimer = 0.5f;
        [SerializeField] private float _detectRange = 5f;
    
        public ItemContext Ctx = new();

        private const float DETECTION_INTERVAL = 0.125f;
        private float _timer;
        private readonly List<Collider2D> _detectionResults = new();
        private ContactFilter2D _playerFilter;
        private StateMachine _machine;
        private State _root;

        private void Awake() 
        {
            _root = new ItemRootState(null, Ctx);
            var builder = new StateMachineBuilder(_root);
            _machine = builder.Build();
            _machine.Start();
            
            Ctx.Transform = transform;
            Ctx.ItemCollider = GetComponent<BoxCollider2D>();
            Ctx.ClosestPlayer = null;
            Ctx.CanBeCollected = false;
            Ctx.HasBeenCollected = false;
            Ctx.Item = this;
            Ctx.IgnoreCollisions = false;

            _playerFilter = new ContactFilter2D();
            _playerFilter.SetLayerMask(_playerLayer);
            _playerFilter.useLayerMask = true;
            _playerFilter.useTriggers = true; // Allows detecting trigger colliders too
            _playerFilter.useDepth = false;   // Disables Z-depth filtering
        }
        
        private IEnumerator Start() 
        {
            yield return new WaitForSeconds(_ableToCollectTimer);
            
            Ctx.CanBeCollected = true;
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
            
            _timer -= Time.deltaTime;
            if(_timer <= 0 && Ctx.CanBeCollected && Ctx.ItemSlot != null && !Ctx.HasBeenCollected)
            {
                _timer = DETECTION_INTERVAL;
                DetectPlayer();
            }
        }

        private void FixedUpdate()
        {
            _machine.FixedTick(Time.fixedDeltaTime);

            Vector2 boxSize = Ctx.ItemCollider.size; 
            Ctx.CollisionResult = GridPhysics.MoveAndResolve(transform.position, Ctx.Velocity, boxSize, Time.fixedDeltaTime, Ctx.IgnoreCollisions);
            transform.position = Ctx.CollisionResult.NewPosition;
        }
        
        public void InitializeItem(InventorySlot item, Vector2 startingVelocity = default)
        {
            Ctx.ItemSlot = item;
            Ctx.Velocity = startingVelocity;

            // Visuals
            _sprite.sprite = GameDataRegistry.Instance.GetItemSOFromItemId(item.ItemId).DisplayIcon;
        }

        private void DetectPlayer()
        {
            int hitCount = Physics2D.OverlapCircle(transform.position, _detectRange, _playerFilter, _detectionResults);
            Player closestPlayer = null;
            float closestDistance = _detectRange;

            for (int i = 0; i < hitCount; i++)
            {
                var currentCollider = _detectionResults[i];

                if (currentCollider.TryGetComponent(out Player player))
                {
                    // Only detect players who can accept this item. In the future, query each player for can accept item somehow
                    bool canThisPlayerAcceptThisItem = InventoryManager.Instance.CanAcceptItem(Ctx.ItemSlot.ItemId, Ctx.ItemSlot.CurrentAmount);
                    
                    if(canThisPlayerAcceptThisItem)
                    {
                        float distance = Vector2.Distance(transform.position, player.transform.position);

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestPlayer = player;
                        }
                    }
                }
            }
            
            Ctx.ClosestPlayer = closestPlayer;
        }
        
        public void OnItemCollected()
        {
            int remainder = InventoryManager.Instance.AddItem(Ctx.ItemSlot.ItemId, Ctx.ItemSlot.CurrentAmount);

            if (remainder <= 0)
            {
                Ctx.HasBeenCollected = true;
                Destroy(gameObject);
            }
            else // If has a remainder, assign the item to the remainder and re calculate closest player again to see if it can accept the item again
            {
                Ctx.ItemSlot.AssignItem(Ctx.ItemSlot.ItemId, remainder);
                Ctx.ClosestPlayer = null;
            }
            
        }
    }
    
    [Serializable]
    public class ItemContext
    {
        public InventorySlot ItemSlot; // Not hidden for debug purposes
        
        [Header("Attraction")]
        public float CollectRange = 0.2f;
        public float AttractSpeed = 20f;
        public float TurnSharpness = 20f;
        public float ThrowAirResistance = 5;
        
        [Header("Gravity")]
        public float GravityForce = 25f;
        public float TerminalVelocity = -40f;

        [HideInInspector] public Transform Transform;
        [HideInInspector] public Vector2 DesiredDirection;
        [HideInInspector] public Vector2 Velocity;
        [HideInInspector] public GridPhysics.CollisionResult CollisionResult;
        [HideInInspector] public BoxCollider2D ItemCollider;
        [HideInInspector] public Player ClosestPlayer;
        [HideInInspector] public bool CanBeCollected;
        [HideInInspector] public bool HasBeenCollected;
        [HideInInspector] public bool IgnoreCollisions;
        [HideInInspector] public Item Item;
    }
}
