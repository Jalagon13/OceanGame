using System.Collections.Generic;
using UnityEngine;

namespace LittleGuyGamePrototype
{
    public class StateMachine 
    {
        public readonly State Root;
        public readonly TransitionSequencer Sequencer;

        private bool _started;

        public StateMachine(State root)
        {
            Root = root;
            Sequencer = new TransitionSequencer(this);
        }

        public void Start()
        {
            if(_started) return;
            _started = true;
            Root.Enter();
        }

        public void Tick(float deltaTime)
        {
            if(!_started) return;
            InternalTick(deltaTime);
        }

        internal void InternalTick(float deltaTime) => Root.Update(deltaTime);

        // Perform the actual switch from 'from' to 'to' by exiting up to the shared ancestor, then entering down to the target
        public void ChangeState(State from, State to)
        {
            if(from == to || from == null || to == null) return;

            State lca = TransitionSequencer.Lca(from, to);

            // Exit current branch up to (but not including) the LCA
            for(var s = from; s != lca; s = s.Parent) s.Exit();

            // Enter target branch from LCA down to target
            var stack = new Stack<State>();
            for(var s = to; s != lca; s = s.Parent) stack.Push(s);
            while(stack.Count > 0) stack.Pop().Enter();
        }
    }
}