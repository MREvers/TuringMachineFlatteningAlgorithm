using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TuringMachineApp
{

    

    class TuringMachine
    {
        
        class SampleCollection
        {

            // Define the indexer, which will allow client code
            // to use [] notation on the class instance itself.
            // (See line 2 of code in Main below.)        
            public string this[string i]
            {
                get
                {
                    // This indexer is very simple, and just returns or sets
                    // the corresponding element from the internal array.
                    return i + ".";
                }
            }
        }

        SampleCollection SymMap = new SampleCollection();

        public string Name = "";
        public string StartState = "";
        public List<string> AcceptStates = new List<string>();

        private List<string> m_lstTapeLibrary = null;

        public List<TransitionFunction> TransitionFunctions = new List<TransitionFunction>();

        static string SZMOVE_RIGHT = ">";
        static string SZMOVE_LEFT = "<";
        static string SZSTAY = "-";

        static string END_SYMBOL = "$";
        static string RETURN_SYMBOL = "R";
        static string NULL_SYMBOL = "~";
       
        public void GetInput(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            System.IO.StreamReader file =
                 new System.IO.StreamReader(fileName);

            Regex findAcceptStates = new Regex(@"^accept:\.*?");
            Regex findStartStates = new Regex(@"^init:\.*?");
            Regex findName = new Regex(@"^name:\.*");

            TransitionFunction TF = null;
            string line;
            while ((line = file.ReadLine()) != null)
            {
                string[] splitLine = ParseLine(line).ToArray();
                bool tryParseLine = true;
                if (AcceptStates.Count == 0)
                {
                    Match match = findAcceptStates.Match(splitLine[0]);
                    // Get the accept states.
                    if (match.Success)
                    {
                        AcceptStates = ParseLine(line.Substring(match.Length)).ToList();
                        tryParseLine = !match.Success;
                    }
                    
                }

                if (Name == "")
                {
                    Match match = findName.Match(splitLine[0]);
                    if (match.Success)
                    {
                        Name = splitLine[0].Substring(match.Length).Trim();
                        tryParseLine = false;
                    }
                    
                }

                if (StartState == "")
                {
                    Match match = findStartStates.Match(splitLine[0]);
                    if (match.Success)
                    {
                        StartState = splitLine[0].Substring(match.Length).Trim();
                        tryParseLine = false;
                    }
                    
                }

                if (splitLine.Length > 1 && tryParseLine)
                {
                    if (TF == null)
                    {
                        TF = new TransitionFunction(splitLine);
                    }
                    else
                    {
                        TF.DefineRange(splitLine);
                        TransitionFunctions.Add(TF);
                        TF = null;
                    }
                  
                }


            }
        }

        private IEnumerable<string> ParseLine(string line)
        {
            return line.Split(',').Select(x => x.Trim());
        }

        public void NullInput(ref string doc, string nullChar)
        {
            List<string> TapeLibrary = GetTapeLibrary();
            TapeLibrary.Add("#");
            TapeLibrary = TapeLibrary.Concat(TapeLibrary.Select(x => SymMap[x])).ToList();
            TapeLibrary.Add(END_SYMBOL);

            List<TransitionFunction> OutputTFs = new List<TransitionFunction>();

            TransitionFunction SetStart = new TransitionFunction(
                        "qNullInputStart", "#");
            SetStart.DefineRange(
                "qNullInput", RETURN_SYMBOL, SZMOVE_RIGHT);
            OutputTFs.Add(SetStart);

            TransitionFunction DeleteChar = new TransitionFunction(
                        "qNullInput", nullChar);
            DeleteChar.DefineRange(
                "qNullInput", "_", SZMOVE_RIGHT);
            OutputTFs.Add(DeleteChar);

            foreach (string symbol in TapeLibrary)
            {
                if (symbol != END_SYMBOL)
                {
                    TransitionFunction MoveToNext = new TransitionFunction(
                        "qNullInput", symbol);
                    MoveToNext.DefineRange(
                        "qNullInput", symbol, SZMOVE_RIGHT);
                    OutputTFs.Add(MoveToNext);

                    TransitionFunction Return = new TransitionFunction(
                        "qNullInputReturn", symbol);
                    Return.DefineRange(
                        "qNullInputReturn", symbol, SZMOVE_LEFT);
                    OutputTFs.Add(Return);
                }
                else
                {
                    TransitionFunction MoveToNext = new TransitionFunction(
                        "qNullInput", symbol);
                    MoveToNext.DefineRange(
                        "qNullInputReturn", symbol, SZMOVE_LEFT);
                    OutputTFs.Add(MoveToNext);
                }
                
            }

            TransitionFunction Complete = new TransitionFunction(
                        "qNullInputReturn", RETURN_SYMBOL);
            Complete.DefineRange(
                this.StartState, "#", SZSTAY);
            OutputTFs.Add(Complete);

            this.StartState = "qNullInputStart";

            CompileOutputTFs(ref doc, OutputTFs);
        }
        
        #region Support Functions
        private List<string> GetTapeLibrary()
        {
            List<string> lstRetVal = new List<string>();
            if (m_lstTapeLibrary == null)
            {
                TuringMachine tm = this;
                
                foreach (TransitionFunction tf in tm.TransitionFunctions)
                {
                    foreach (string sz in tf.DomainHeadValues)
                    {
                        if (!lstRetVal.Contains(sz))
                        {
                            lstRetVal.Add(sz);
                        }
                    }

                    foreach (string sz in tf.RangeHeadWrite)
                    {
                        if (!lstRetVal.Contains(sz))
                        {
                            lstRetVal.Add(sz);
                        }
                    }
                }
            }
            else
            {
                lstRetVal = m_lstTapeLibrary.ToList();
            }
            

            return lstRetVal.Where(x => x != END_SYMBOL && x != RETURN_SYMBOL && x != NULL_SYMBOL).ToList();
        }

        private void CompileOutputTFs(ref string doc, List<TransitionFunction> output)
        {
            foreach (TransitionFunction tf in output)
            {
                doc += tf.DomainState.Actual + ",";
                doc += tf.DomainHeadValues[0] + Environment.NewLine;
                doc += tf.RangeState.Actual + ",";
                doc += tf.RangeHeadWrite[0] + "," + tf.RangeHeadMove[0] + Environment.NewLine;
                doc += Environment.NewLine;
            }
        }
        #endregion Support Functions

        public void FinalizeOutput(ref string doc)
        {
            string intermed = "accept: ";
            foreach (string accstate in AcceptStates)
            {
                intermed += accstate;
                if (accstate != AcceptStates.Last())
                {
                    intermed += ", ";
                }
            }
            doc = intermed + Environment.NewLine + Environment.NewLine + doc;
            doc = "init: " + this.StartState + Environment.NewLine + doc;
            doc = "name: " + this.Name + Environment.NewLine + doc;

            doc += Environment.NewLine;
        }
    }
}
