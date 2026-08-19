using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LittleGuyGamePrototype
{
    [RequireComponent(typeof(HealthHandler), typeof(TeamHandler), typeof(Rigidbody))]
    public class Soldier : MonoBehaviour
    {
        [SerializeField] private float _detectTargetInterval = 0.25f;
        [SerializeField] private float _customTurnSpeed = 360f; // Degrees per second

        public SoldierContext Ctx = new();
    
        private StateMachine _machine;
        private State _root;
        
        private string _lastPath;
        
        private void Awake()
        {
            _root = new SoldierRootState(null, Ctx);
            var builder = new StateMachineBuilder(_root);
            _machine = builder.Build();
            _machine.Start();

            Ctx.HealthHandler = GetComponent<HealthHandler>();
            Ctx.HealthHandler.Initialize(Ctx.Data.MaxHealth, Ctx.Data.IFrameDuration);
            Ctx.TeamHandler = GetComponent<TeamHandler>();
            Ctx.Agent = GetComponent<NavMeshAgent>();
            Ctx.Agent.updateRotation = false; // I will control rotation
            Ctx.Transform = transform;
            Ctx.Rb = GetComponent<Rigidbody>();
            Ctx.TargetSoldier = null;
        }
        
        private void Start() 
        {
            Ctx.HealthHandler.OnDeath += OnDeath;
            Ctx.HealthHandler.OnDamage += OnDamage;
        
            InvokeRepeating(nameof(DetectTarget), _detectTargetInterval, _detectTargetInterval);    
        }
        
        private void OnDestroy()
        {
            Ctx.HealthHandler.OnDeath -= OnDeath;
            Ctx.HealthHandler.OnDamage -= OnDamage;
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
            
            HandleRotation(Time.deltaTime);
            
            var path = StatePath(_machine.Root.Leaf());
            if(path != _lastPath)
            {
                Debug.Log($"{name}: {path}");
                _lastPath = path;
            }
        }

        private void HandleRotation(float deltaTime)
        {
            if (Ctx.Agent.desiredVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 lookDirection = Ctx.Agent.desiredVelocity;
                lookDirection.y = 0f;

                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                // Smoothly rotate at a guaranteed fast speed
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _customTurnSpeed * deltaTime);
            }
        }

        private void OnDamage(object sender, HealthHandler.DamageEventArgs e)
        {
            if (Ctx.HealthHandler.CurrentLifeState == HealthHandler.LifeState.Dead) return;

            if (e.PlayKnockback)
            {
                Ctx.DamageInfo = e;
                Ctx.IsKnockedBack = true;
                Ctx.TargetSoldier = e.Inflicter.GetComponent<Soldier>(); // Once Damaged, go for the damager
            }
        }

        private void OnDeath()
        {
            Debug.Log($"{name} has died.");
            Destroy(gameObject);
        }

        private void DetectTarget()
        {
            // Keep current target if it exists, is active, and is still within agro radius
            if (Ctx.TargetSoldier != null && Ctx.TargetSoldier.gameObject.activeSelf)
            {
                float currentTargetDistance = Vector3.Distance(transform.position, Ctx.TargetSoldier.transform.position);
                if (currentTargetDistance <= Ctx.Data.AgroRadius)
                {
                    return;
                }
            }

            Collider[] colliders = Physics.OverlapSphere(transform.position, Ctx.Data.AgroRadius);
            List<Soldier> enemySoldiersFound = new();
            
            foreach (var c in colliders)
            {
                if(c.transform.root == transform) continue; // if self do not detect
                
                if(c.transform.root.TryGetComponent(out Soldier soldier))
                {
                    if(soldier.Ctx.TeamHandler.Team == Ctx.TeamHandler.Team) continue; // if on the same team do not detect
                
                    enemySoldiersFound.Add(soldier);
                }
            }

            Soldier closestSoldier = null;
            float closestDistance = float.MaxValue;
            
            foreach (Soldier s in enemySoldiersFound)
            {
                float distance = Vector3.Distance(transform.position, s.transform.position);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSoldier = s;
                }
            }
            
            Ctx.TargetSoldier = closestSoldier;
        }

        private static string StatePath(State s)
        {
            return string.Join(" > ", s.PathToRoot().Reverse().Select(n => n.GetType().Name));
        }
    }
    
    [Serializable]
    public class SoldierContext
    {
        public SoldierSO Data;
        public Collider AttackCollider;
        [HideInInspector] public HealthHandler HealthHandler;
        [HideInInspector] public NavMeshAgent Agent;
        [HideInInspector] public bool Grounded = true;
        [HideInInspector] public Transform Transform;
        [HideInInspector] public Soldier TargetSoldier;
        [HideInInspector] public Rigidbody Rb;
        [HideInInspector] public HealthHandler.DamageEventArgs DamageInfo;
        [HideInInspector] public bool IsKnockedBack;
        [HideInInspector] public TeamHandler TeamHandler;
    }
}
