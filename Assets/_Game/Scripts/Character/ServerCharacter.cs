using UnityEngine;

namespace OceanGame
{
    public class ServerCharacter : MonoBehaviour
    {
        [SerializeField] private StateMachineType _stateType;
        public StateMachineType StateMachineType => _stateType;
        
        [SerializeField] private CharacterSO _data;
        public CharacterSO Data => _data;
        
        [SerializeField] private GameObject _visualsGO;
        public GameObject VisualsGO => _visualsGO;
        
        [SerializeField] private bool _debugStateOn;
        [SerializeField] private bool _ignoreCollision;

        [HideInInspector] public Vector2 DesiredDirection;
        [HideInInspector] public Vector2 Velocity;
        [HideInInspector] public Vector2 CurrentBodyColliderSize;

        public StateMachine Machine { get; private set; }
        public GridPhysics.CollisionResult CollisionResult { get; private set; }

        private void Awake()
        {
            State rootState = CharacterCommons.GetNpcHSMRootState(this, _stateType);
            if (rootState == null) return;

            Machine = new StateMachineBuilder(rootState).Build(_debugStateOn);
            Machine.Start();

            CurrentBodyColliderSize = _data.BodyColliderSize;
            DesiredDirection = Vector2.zero;
            Velocity = Vector2.zero;
        }

        private void Update()
        {
            if (!WorldManager.Instance.IsWorldReady) return;

            Machine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!WorldManager.Instance.IsWorldReady) return;

            Machine.FixedTick(Time.fixedDeltaTime);

            CollisionResult = GridPhysics.MoveAndResolve(transform.position, Velocity, CurrentBodyColliderSize, Time.fixedDeltaTime, _ignoreCollision);
            transform.position = CollisionResult.NewPosition;
        }
    }
}
