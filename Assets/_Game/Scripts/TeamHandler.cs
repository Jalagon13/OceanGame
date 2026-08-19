using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LittleGuyGamePrototype
{
    public class TeamHandler : MonoBehaviour
    {
        [field: SerializeField] public Team Team { get; private set; } = Team.Default;

        [SerializeField] private MeshRenderer _meshRenderer;
        
        private void Awake() 
        {
            SetTeam(Team);
        }

        [Button("Set Team")]
        public void SetTeam(Team newTeam)
        {
            Team = newTeam;
            UpdateTeamLogic();
        }

        private void UpdateTeamLogic()
        {
            if (_meshRenderer == null || _meshRenderer.material == null) return;

            switch (Team)
            {
                case Team.Default:
                    _meshRenderer.material.color = Color.gray;
                    break;
                case Team.Red:
                    _meshRenderer.material.color = Color.red;
                    break;
                case Team.Blue:
                    _meshRenderer.material.color = Color.blue;
                    break;
            }
        }
    }

    public enum Team
    {
        Default,
        Red,
        Blue
    }
}
