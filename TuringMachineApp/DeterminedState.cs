using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringMachineApp
{
    class DeterminedState : State
    {
        public string BaseState;
        public TransitionFunction TF;
        public DeterminedState(string actual, string basestate, TransitionFunction tf, params string[] parms) : base(actual, parms)
        {
            BaseState = basestate;
            TF = tf;
        }

        public DeterminedState(string actual, State start, string basestate, TransitionFunction tf,  params string[] parms) : base(actual, start, parms)
        {
            BaseState = basestate;
            TF = tf;
        }
    }
}
