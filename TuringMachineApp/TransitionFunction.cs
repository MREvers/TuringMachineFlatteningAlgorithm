using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringMachineApp
{
    class TransitionFunction : IEquatable<TransitionFunction>
    {
        public State DomainState;
        public List<string> DomainHeadValues;

        public State RangeState;
        public List<string> RangeHeadWrite;
        public List<string> RangeHeadMove;

        public TransitionFunction(params string[] parms)
        {
            DomainHeadValues = new List<string>();
            RangeHeadMove = new List<string>();
            RangeHeadWrite = new List<string>();

            DomainState = new State(parms[0]);
            for(int i = 1; i<parms.Length; i++)
            {
                DomainHeadValues.Add(parms[i]);
            }

        }

        public void DefineRange(params string[] parms)
        {
            RangeState = new State(parms[0]);
            for (int i = 1; i < DomainHeadValues.Count+1; i++)
            {
                RangeHeadWrite.Add(parms[i]);
            }
            for (int i = DomainHeadValues.Count+1; i<parms.Length; i++)
            {
                RangeHeadMove.Add(parms[i]);
            }
        }

        public bool Equals(TransitionFunction other)
        {
            bool same = true;

            same &= this.DomainState.Actual == other.DomainState.Actual;

            for (int i = 0; i < this.DomainHeadValues.Count; i++)
            {
                same &= this.DomainHeadValues[i] == other.DomainHeadValues[i];
            }

            same &= this.RangeState.Actual == other.RangeState.Actual;

            for (int i = 0; i < this.RangeHeadWrite.Count; i++)
            {
                same &= this.RangeHeadWrite[i] == other.RangeHeadWrite[i];
            }

            for (int i = 0; i < this.RangeHeadMove.Count; i++)
            {
                same &= this.RangeHeadMove[i] == other.RangeHeadMove[i];
            }

            return same;
        }

        public override string ToString()
        {
            string doc = "";
            var tf = this;
            doc += tf.DomainState.Actual + ",";
            doc += tf.DomainHeadValues[0] + Environment.NewLine;
            doc += tf.RangeState.Actual + ",";
            doc += tf.RangeHeadWrite[0] + "," + tf.RangeHeadMove[0] + Environment.NewLine;
            return doc;
        }
    }
}
