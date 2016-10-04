using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringMachineApp
{
    class State : IEquatable<State>
    {
        public string Actual;
        public List<string> SubScripts;

        public State(string actual, params string[] parms)
        {
            SubScripts = parms.ToArray().ToList();

            Actual = actual;
        }

        public State(string actual, State start, params string[] parms) 
        {
            SubScripts = start.SubScripts.ToList();
            foreach(string parm in parms)
            {
                SubScripts.Add(parm);
            }
            Actual = actual;
        }

        public bool Equals(State other)
        {
            return Actual == other.Actual;
        }
    }
}
