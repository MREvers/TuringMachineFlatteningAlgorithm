using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringMachineApp
{
    class DeterminedState : State
    {
        public TransitionFunction TF;
        public DeterminedState(string actual, TransitionFunction tf, params string[] parms) : base(actual, parms)
        {
            TF = tf;
        }

        public DeterminedState(string actual, State start, TransitionFunction tf, params string[] parms) : base(actual, start, parms)
        {
            TF = tf;
        }
    }
}
