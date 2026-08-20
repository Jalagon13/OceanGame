using System;
using System.Linq;
using UnityEngine;

namespace OceanGame
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
        
        public PlayerContext Ctx = new();
        
        private StateMachine _machine;
        private State _root;
        private string _lastPath;
        
        private void Awake() 
        {
            Instance = this;    
            
            _root = new PlayerRootState(null, Ctx);
            var builder = new StateMachineBuilder(_root);
            _machine = builder.Build();
            _machine.Start();

            Ctx.Transform = transform;
        }
        
        private void Start() 
        {
            GameInput.Instance.MoveInputPressed += OnMoveInputPressed;
        }
        
        private void OnDestroy() 
        {
            GameInput.Instance.MoveInputPressed -= OnMoveInputPressed;
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
            
            var path = StatePath(_machine.Root.Leaf());
            if (path != _lastPath)
            {
                Debug.Log($"{name}: {path}");
                _lastPath = path;
            }
        }
        
        private void FixedUpdate()
        {
            Vector2 boxSize = Ctx.PlayerBodyCollider.size; // Player's size

            transform.position = GridPhysics.MoveAndResolve(transform.position, Ctx.Velocity, boxSize, Time.fixedDeltaTime);
        }

        private void OnMoveInputPressed(Vector2 rawMoveInput)
        {
            Ctx.DesiredDirection = rawMoveInput;
        }

        private static string StatePath(State s)
        {
            return string.Join(" > ", s.PathToRoot().Reverse().Select(n => n.GetType().Name));
        }
    }
    
    [Serializable]
    public class PlayerContext
    {
        public float MoveSpeed = 3.5f;
        public float TurnSharpness = 5f;
        public BoxCollider2D PlayerBodyCollider;
    
        [HideInInspector] public Transform Transform;
        [HideInInspector] public Vector2 DesiredDirection;
        [HideInInspector] public Vector2 Velocity;
        [HideInInspector] public bool Grounded = true;
        [HideInInspector] public bool Swimming = false;

    }
}
