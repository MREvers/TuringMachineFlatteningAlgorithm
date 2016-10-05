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

        /// <summary>
        /// For the philosophy of this algorith, see sipser.
        /// </summary>
        /// <param name="doc"></param>
        public void Flatten3(ref string doc)
        {
            #region Setup
            TuringMachine tm = this;

            int NUM_TAPES = tm.TransitionFunctions.First().DomainHeadValues.Count;
            List<string> TapeLibrary = GetTapeLibrary();
            TapeLibrary.Add("#");

            List<string> NonHeaderLibrary = TapeLibrary.ToList();
            List<string> HeaderLibrary = NonHeaderLibrary.Select(x => SymMap[x]).ToList();
            TapeLibrary = TapeLibrary.Concat(HeaderLibrary).ToList();
            HeaderLibrary.Add("B");


            // Get the mkStates.
            List<State> mkStates = GetStates();

            // Record the Final States
            IEnumerable<State> OutputStates = new List<State>();

            // Record the Final Transition Functions
            IEnumerable<TransitionFunction> OutputTFs = new List<TransitionFunction>();
            IEnumerable<TransitionFunction> UnbranchedFinalizedList = null;
            #endregion Setup

            #region Iterate of States Of Multitape Machine Mk
            foreach (State mkState in mkStates)
            {

                IEnumerable<TransitionFunction> TransitionFunction_State_ = E_GetMKTransitionFunctionsSuchThat_State_(mkState);

                IEnumerable<State> undeterminedStates = E_BuildUnderterminedStates(mkState, TransitionFunction_State_)
                    .Distinct();
                IList<DeterminedState> determinedStates = E_BuildDeterminedStates(mkState, TransitionFunction_State_).ToList();

                IEnumerable<DeterminedState> determinedStates_ActorStates_N_Write =
                    E_BuildDeterminedStates_ActorStates_N_Write(determinedStates);

                IEnumerable<DeterminedState> determinedStates_ActorStates_N_MoveHead =
                    E_BuildDeterminedStates_ActorStates_N_MoveHead(determinedStates_ActorStates_N_Write);

                IEnumerable<DeterminedState> determinedStates_ActorStates_0F_Complete =
                    E_BuildDeterminedStates_ActorStates_0F_Complete(determinedStates);

                ////////
                IEnumerable<TransitionFunction> tmp = new List<TransitionFunction>();
                IEnumerable<TransitionFunction> undeterminedStates_TransitionFunctions_XthParm_MoveToNextI =
                    E_BuildTransitionFunctions_Undetermined_XthParm_MoveToNext(undeterminedStates, NonHeaderLibrary);
                tmp = tmp.Concat(undeterminedStates_TransitionFunctions_XthParm_MoveToNextI).ToList();

                IEnumerable<TransitionFunction> undeterminedStates_TransitionFunction_XthParm_FoundNextChangeToNextI =
                    E_BuildTransitionFunctions_Undetermined_XthParm_FoundNextChangeToNextState(undeterminedStates);
                tmp = tmp.Concat(undeterminedStates_TransitionFunction_XthParm_FoundNextChangeToNextI);

                IEnumerable<TransitionFunction> determinedStates_TransitionFunction_ChangeToActionStates =
                    E_BuildTransitionFunctions_Determined_BeginActionStates(determinedStates, NonHeaderLibrary);
                tmp = tmp.Concat(determinedStates_TransitionFunction_ChangeToActionStates);

                IEnumerable<TransitionFunction> determinedStates_TransitionFunction_ActorState_N_MoveToPreviousAndWrite =
                    E_BuildTransitionFunctions_Determined_ActorState_N_MoveToPrevious(
                        determinedStates_ActorStates_N_Write,
                        NonHeaderLibrary);
                tmp = tmp.Concat(determinedStates_TransitionFunction_ActorState_N_MoveToPreviousAndWrite);

                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState =
                    E_BuildTransitionFunctions_Determined_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState(
                        determinedStates);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState);

                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious =
                    E_BuildTransitionFunctions_Determined_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious(
                        determinedStates,
                        NonHeaderLibrary);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious);

                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_0F_MoveToBeginning =
                    E_BuildTransitionFunctions_Determined_ActorState_0F_ChangeToNext(
                        determinedStates_ActorStates_0F_Complete,
                        TapeLibrary);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_0F_MoveToBeginning);

               OutputTFs = OutputTFs.Concat(tmp).Distinct();


               IEnumerable<TransitionFunction> withBranches =  BuildTransitionFunctions_ShiftRight_Branches_And_Remove_Nodes(
                    OutputTFs,
                    TapeLibrary);

                UnbranchedFinalizedList = withBranches;
            }

            foreach(TransitionFunction tf in UnbranchedFinalizedList)
            {
                doc += tf.ToString();
            }

            #endregion Iterate of States Of Multitape Machine Mk
        }

        private IEnumerable<TransitionFunction> 
            E_GetMKTransitionFunctionsSuchThat_State_(
            State state)
        {
            foreach (TransitionFunction tf in this.TransitionFunctions)
            {
                if (tf.DomainState.Actual == state.Actual)
                {
                    yield return tf;
                }
            }
        }

        #region States

        /// <summary>
        /// Builds up all the undetermined states beginning from the 'basestate'
        ///  based on the provided transition functions. This does not include
        ///  states that map uniquely to the domain of a TF.
        /// E.G.
        /// baseState := 'q1'
        /// TF := q1,_,1,_ -> xxx
        /// 
        /// Yields states q1_, and q1_1
        /// NOT q1_1_
        /// </summary>
        /// <param name="baseState">State that maps to equivalent mKState</param>
        /// <param name="determiningTFs">Transition Functions from the mKTFs that begin at 'baseState'</param>
        /// <returns></returns>
        private IEnumerable<State> 
            E_BuildUnderterminedStates(
            State baseState,
            IEnumerable<TransitionFunction> determiningTFs)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach (TransitionFunction matchedTF in determiningTFs)
            {
                for (int i = 0; i < NUM_TAPES; i++)
                {
                    State permState;

                    if (i == 0)
                    {
                        permState = new State(baseState.Actual, baseState);
                    }
                    else
                    {
                        // If a transition function takes in (q1, 1, 0); then this will generate state q1io 
                        permState = new State(matchedTF.DomainState.Actual);
                        for (int k = 0; k < i; k++)
                        {
                            string headSymbol = SymMap[matchedTF.DomainHeadValues[k]];
                            permState = new State(
                               permState.Actual + headSymbol,
                               permState,
                               headSymbol);
                        }
                    }
                    yield return permState;
                }
            }
        }

        /// <summary>
        /// Builds a unique state that matches the parameters of all
        ///  transition functions that start from 'baseState'
        /// E.G.
        /// baseState := 'q1'
        /// TF := q1,_,1,_ -> xxx
        /// 
        /// Yields q1_1_
        /// </summary>
        /// <param name="baseState">State that maps to equivalent mKState</param>
        /// <param name="determiningTFs">Transition Functions from the mKTFs that begin at 'baseState'</param>
        /// <returns></returns>
        private IEnumerable<DeterminedState>
            E_BuildDeterminedStates(
            State baseState,
            IEnumerable<TransitionFunction> determiningTFs)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            int i = NUM_TAPES;
            foreach (TransitionFunction matchedTF in determiningTFs)
            {
                State permState = new State(matchedTF.DomainState.Actual);
                for (int k = 0; k < i; k++)
                {
                    string headSymbol = SymMap[matchedTF.DomainHeadValues[k]];
                    permState = new State(
                       permState.Actual + headSymbol,
                       permState,
                       headSymbol);
                }
                DeterminedState returnState = new DeterminedState(
                    permState.Actual,
                    permState,
                    permState.Actual,
                    matchedTF);
                yield return returnState;
            }
        }

        private IEnumerable<DeterminedState>
            E_BuildDeterminedStates_ActorStates_N_Write(
            IEnumerable<DeterminedState> determinedStates)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach (DeterminedState dstate in determinedStates)
            {
                // Since each dstate is unique, states and tfs can be added immediately.
                for (int i = 1; i <= NUM_TAPES; i++)
                {
                    
                    DeterminedState dstateNthTape = new DeterminedState(
                        dstate.Actual + i,
                        dstate,
                        dstate.Actual,
                        dstate.TF,
                        (i).ToString());
                    yield return dstateNthTape;
                }
            }
        }

        private IEnumerable<DeterminedState>
            E_BuildDeterminedStates_ActorStates_0F_Complete(
            IEnumerable<DeterminedState> determinedStates)
        {
            foreach (DeterminedState dstate in determinedStates)
            {
                DeterminedState permStateCompleted = new DeterminedState(
                        dstate.Actual + "F",
                        dstate,
                        dstate.Actual,
                        dstate.TF,
                        "F");
                yield return permStateCompleted;
            }
        }

        private IEnumerable<DeterminedState> 
            E_BuildDeterminedStates_ActorStates_N_MoveHead(
            IEnumerable<DeterminedState> determinedStates_ActorStates_Write)
        {
            
            foreach (DeterminedState dstateNthTape in determinedStates_ActorStates_Write)
            {
                DeterminedState dstateNthTape_MoveHead = new DeterminedState(
                        dstateNthTape.Actual + "a",
                        dstateNthTape,
                        dstateNthTape.BaseState,
                        dstateNthTape.TF,
                        ("a").ToString());
                yield return dstateNthTape_MoveHead;
            }
        }

        #endregion

        private IEnumerable<TransitionFunction>
            E_BuildTransitionFunctions_Undetermined_XthParm_MoveToNext(
            IEnumerable<State> undeterminedStates,
            List<string> NonHeaderLibrary)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach (State permState in undeterminedStates)
            {
                foreach (string nonheader in NonHeaderLibrary)
                {
                 TransitionFunction permState_MoveToNext = new TransitionFunction(
                        permState.Actual, nonheader);
                    permState_MoveToNext.DefineRange(permState.Actual, nonheader, SZMOVE_RIGHT);
                    yield return permState_MoveToNext;
                    
                }
            }

        }

        private IEnumerable<TransitionFunction>
            E_BuildTransitionFunctions_Undetermined_XthParm_FoundNextChangeToNextState(
            IEnumerable<State> undeterminedStates)
        {
            foreach (State permState in undeterminedStates)
            {
                // e.g. mkTFs (q1,0,1) and (q1,0,0), match q1o. So 1, and 0, are potential
                //  transition parameters.
                List<TransitionFunction> potentialPermStates = GetPossibleTFs(permState);
                foreach (TransitionFunction permMatchedTF in potentialPermStates)
                {
                    string permMatchedPotentialChar = permMatchedTF.DomainHeadValues[permState.SubScripts.Count];

                    TransitionFunction permState_TransitToNext = new TransitionFunction(
                        permState.Actual, SymMap[permMatchedPotentialChar]);
                    string szNextState = permState.Actual + SymMap[permMatchedPotentialChar];
                    permState_TransitToNext.DefineRange(szNextState, SymMap[permMatchedPotentialChar], SZMOVE_RIGHT);

                    yield return permState_TransitToNext;

                    // Branch on _
                    if (permMatchedPotentialChar == "_")
                    {
                        TransitionFunction permState_TransitToNextBranch = new TransitionFunction(
                            permState.Actual, SymMap["#"]);
                        string szNextState2 = permState.Actual + SymMap["_"];
                        permState_TransitToNextBranch.DefineRange(szNextState2, SymMap["#"], SZMOVE_RIGHT);
                        yield return permState_TransitToNextBranch;
                    }

                }
            }
        }

        private IEnumerable<TransitionFunction>
        E_BuildTransitionFunctions_Determined_BeginActionStates(
        IEnumerable<DeterminedState> determinedStates,
        List<string> NonHeaderLibrary)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach (State permState in determinedStates)
            {
                foreach (string nonheader in NonHeaderLibrary)
                {
                    TransitionFunction permState_MoveToNext = new TransitionFunction(
                        permState.Actual, nonheader);
                    permState_MoveToNext.DefineRange(permState.Actual + NUM_TAPES, nonheader, SZSTAY);
                    yield return permState_MoveToNext;
                }
            }
        }

        // Does not include 0 or finished
        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_ActorState_N_MoveToPrevious(
            IEnumerable<DeterminedState> determinedStates_ActorStates_N_Write,
            List<string> NonHeaderLibrary)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach(DeterminedState dstateNthTape in determinedStates_ActorStates_N_Write)
            {

                foreach (string nonheadSymbol in NonHeaderLibrary)
                {
                    TransitionFunction determinedNState_MoveToPrevious = new TransitionFunction(
                                dstateNthTape.Actual, nonheadSymbol);

                    determinedNState_MoveToPrevious.DefineRange(
                        dstateNthTape.Actual, nonheadSymbol, SZMOVE_LEFT);

                    yield return determinedNState_MoveToPrevious;
                }
                
            }
        }

        private IEnumerable<TransitionFunction> 
            E_BuildTransitionFunctions_Determined_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState(
            IEnumerable<DeterminedState> determinedStates)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach (DeterminedState dstate in determinedStates)
            {
                for (int i = NUM_TAPES; i > 0 ; i--)
                {
                    string NthParameterOfDeterminedState = dstate.SubScripts[i - 1];
                    TransitionFunction determinedNState_FoundPreviousAndWrite = new TransitionFunction(
                                dstate.Actual + (i).ToString(), NthParameterOfDeterminedState);

                    string NthTapeTransitionWrite = dstate.TF.RangeHeadWrite[i - 1];
                    string NthTapeTransitionMove = dstate.TF.RangeHeadMove[i - 1];

                    
                    if (NthTapeTransitionMove != SZSTAY)
                    {
                        determinedNState_FoundPreviousAndWrite.DefineRange(
                            dstate.Actual + (i).ToString() + "a", NthTapeTransitionWrite, NthTapeTransitionMove);     
                    }
                    else
                    {
                        if (i > 1)
                        {
                            determinedNState_FoundPreviousAndWrite.DefineRange(
                            dstate.Actual + (i - 1), SymMap[NthTapeTransitionWrite], SZMOVE_LEFT);
                        }
                        else
                        {
                            determinedNState_FoundPreviousAndWrite.DefineRange(
                            dstate.Actual + "F", SymMap[NthTapeTransitionWrite], SZMOVE_LEFT);
                        }
                    }

                    yield return determinedNState_FoundPreviousAndWrite;

                    // Also accept H as B.
                    if (NthParameterOfDeterminedState == SymMap["_"])
                    {
                        TransitionFunction determinedNState_FoundPreviousAndWrite_H = new TransitionFunction(
                                dstate.Actual + (i).ToString(), SymMap["#"]);

                        if (NthTapeTransitionWrite != "_")
                        {
                            if (NthTapeTransitionMove != SZSTAY)
                            {
                                determinedNState_FoundPreviousAndWrite_H.DefineRange(
                                    dstate.Actual + (i).ToString() + "a", NthTapeTransitionWrite, NthTapeTransitionMove);
                            }
                            else
                            {
                                if (i > 1)
                                {
                                    determinedNState_FoundPreviousAndWrite_H.DefineRange(
                                    dstate.Actual + (i - 1), SymMap["#"], SZMOVE_LEFT);
                                }
                                else
                                {
                                    determinedNState_FoundPreviousAndWrite_H.DefineRange(
                                    dstate.Actual + "F", SymMap["#"], SZMOVE_LEFT);
                                }
                            }
                            
                        }
                        else
                        {
                            if (NthTapeTransitionMove != SZSTAY)
                            {
                                determinedNState_FoundPreviousAndWrite_H.DefineRange(
                                    dstate.Actual + (i).ToString() + "a", "#", NthTapeTransitionMove);
                            }
                            else
                            {
                                if (i > 1)
                                {
                                    determinedNState_FoundPreviousAndWrite_H.DefineRange(
                                    dstate.Actual + (i - 1), SymMap["#"], SZMOVE_LEFT);
                                }
                                else
                                {
                                    determinedNState_FoundPreviousAndWrite_H.DefineRange(
                                    dstate.Actual + "F", SymMap["#"], SZMOVE_LEFT);
                                }
                            }

                           
                        }
                        yield return determinedNState_FoundPreviousAndWrite_H;
                    }



                }
            }
        }

        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious(
            IEnumerable<DeterminedState> determinedStates,
            List<string> NonHeaderLibrary)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach (DeterminedState dstate in determinedStates)
            {
                for (int i = NUM_TAPES; i > 0; i--)
                {
                    if (dstate.TF.RangeHeadMove[i - 1] != SZSTAY)
                    {
                        if (i > 1)
                        {
                            foreach (string character in NonHeaderLibrary)
                            {
                                // Exclude this combination because the head has moved before the beginning of the tape.
                                if (!(character == "#" && dstate.TF.RangeHeadMove[i - 1] == SZMOVE_LEFT))
                                {
                                    TransitionFunction determinedNState_MoveHead = new TransitionFunction(
                                            dstate.Actual + i + "a", character);
                                    determinedNState_MoveHead.DefineRange(
                                        dstate.Actual + (i - 1), SymMap[character], SZMOVE_LEFT);
                                    yield return determinedNState_MoveHead;
                                }

                            }
                        }
                        else
                        {
                            foreach (string character in NonHeaderLibrary)
                            {
                                // Exclude this combination because the head has moved before the beginning of the tape.
                                if (!(character == "#" && dstate.TF.RangeHeadMove[i - 1] == SZMOVE_LEFT))
                                {
                                    TransitionFunction determinedNState_MoveHead = new TransitionFunction(
                                            dstate.Actual + i + "a", character);
                                    determinedNState_MoveHead.DefineRange(
                                        dstate.Actual + "F", SymMap[character], SZSTAY);
                                    yield return determinedNState_MoveHead;
                                }
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_ActorState_0F_ChangeToNext(
            IEnumerable<DeterminedState> determinedStates_ActorStates_0F_Complete,
            List<string> TapeLibrary)
        {
            foreach (DeterminedState permStateCompleted in determinedStates_ActorStates_0F_Complete)
            {
                foreach (string character in TapeLibrary)
                {
                    TransitionFunction completeState_MoveToBegin = new TransitionFunction(
                        permStateCompleted.Actual, character);
                    completeState_MoveToBegin.DefineRange(permStateCompleted.TF.RangeState.Actual, character, SZSTAY);
                    yield return completeState_MoveToBegin;
                }
            }
        }

        /// <summary>
        /// Iterates through a list of 'allTFs' and finds all the states that would write on
        ///  H (Blank) and inserts a subroutine to shift all characters right.
        /// </summary>
        /// <param name="allTFS">List of TFs to check</param>
        /// <param name="TapeLibrary">List of characters needed to shift right</param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction> BuildTransitionFunctions_ShiftRight_Branches_And_Remove_Nodes(
            IEnumerable<TransitionFunction> allTFS,
            List<string> TapeLibrary)
        {
            List<TransitionFunction> ReplacedTFs = new List<TransitionFunction>();
            foreach(TransitionFunction tf in allTFS)
            {
                // Don't extend the virtual tape if its just going to be written with a blank...
                if (tf.DomainHeadValues[0] == SymMap["#"] &&
                    (tf.RangeHeadWrite[0] != SymMap["#"] && tf.RangeHeadWrite[0] != SymMap["_"]) &&
                    (tf.RangeHeadWrite[0] != "#" && tf.RangeHeadWrite[0] != "_"))
                {
                    ReplacedTFs.Add(tf);

                    TransitionFunction determinedNState_BranchOnPound = new TransitionFunction(
                                    tf.DomainState.Actual , SymMap["#"]);
                    determinedNState_BranchOnPound.DefineRange(
                        tf.DomainState.Actual + "S" + "#", RETURN_SYMBOL, SZMOVE_RIGHT);

                    // Start the branch
                    yield return determinedNState_BranchOnPound;

                    #region Build Shift Right TFs and Shift States

                    var TPLWD = TapeLibrary.ToList().Concat(new List<string>() { END_SYMBOL });
                    foreach (string anySymbol in TapeLibrary)
                    {
                        foreach (string targetSymbol in TPLWD)
                        {
                            // (q#, $) --> (q$, #, R)
                            if ((targetSymbol == END_SYMBOL && anySymbol == "#") ||
                                (targetSymbol != END_SYMBOL))
                            {
                                string sourceState = tf.DomainState.Actual + "S" + anySymbol;
                                TransitionFunction dstateNthTape_ShiftRight = new TransitionFunction(
                                    sourceState, targetSymbol);

                                string targetState = tf.DomainState.Actual + "S" + targetSymbol;
                                dstateNthTape_ShiftRight.DefineRange(
                                    targetState, anySymbol, SZMOVE_RIGHT);

                                yield return dstateNthTape_ShiftRight;

                            }

                        }
                    }
                    #endregion

                    #region Build Found End Shift Transition To (Move To Return to Proc) State.
                    TransitionFunction dstateNthTape_ShiftFoundEnd = new TransitionFunction(
                        tf.DomainState.Actual + "S" + END_SYMBOL, "_");

                    dstateNthTape_ShiftFoundEnd.DefineRange(
                        tf.DomainState.Actual + "S" + RETURN_SYMBOL, END_SYMBOL, SZMOVE_LEFT);
                    yield return dstateNthTape_ShiftFoundEnd;
                    #endregion

                    #region Build Move To Return to Proc TFs and Transition to Proc
                    foreach (string anySymbol in TapeLibrary)
                    {

                        TransitionFunction dstateNthTape_ReturnToProcedure = new TransitionFunction(
                           tf.DomainState.Actual + "S" + RETURN_SYMBOL, anySymbol);

                        dstateNthTape_ReturnToProcedure.DefineRange(
                            tf.DomainState.Actual + "S" + RETURN_SYMBOL, anySymbol, SZMOVE_LEFT);
                        yield return dstateNthTape_ReturnToProcedure;

                    }

                    TransitionFunction dstateNthTape_TransitionToProcedure = new TransitionFunction(
                            tf.DomainState.Actual + "S" + RETURN_SYMBOL, RETURN_SYMBOL);

                    dstateNthTape_TransitionToProcedure.DefineRange(
                        tf.DomainState.Actual, SymMap["_"], SZSTAY);
                    yield return dstateNthTape_TransitionToProcedure;
                    #endregion
                }
                else
                {
                    yield return tf;
                }
                
            
            }
            
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

        private List<State> GetStates()
        {
            TuringMachine tm = this;
            List<State> states = new List<State>();
            foreach (TransitionFunction tf in tm.TransitionFunctions)
            {
                bool add = true;

                foreach (State state in states)
                {
                    add &= state.Actual != tf.DomainState.Actual;
                   
                }
                if (add)
                {
                    states.Add(tf.DomainState);
                }

                bool addR = true;
                foreach (State state in states)
                {
                    addR &= state.Actual != tf.RangeState.Actual;

                }
                if (addR)
                {
                    states.Add(tf.RangeState);
                }
            }
            return states;
        }

        private List<TransitionFunction> GetPossibleTFs(State state)
        {
            TuringMachine tm = this;
            List<TransitionFunction> lstRetVal = new List<TransitionFunction>();

            foreach (TransitionFunction tf in tm.TransitionFunctions)
            {
                bool found = true;
                int subsLength = 0;
                foreach (string subscript in state.SubScripts)
                {
                    subsLength += subscript.Length;
                }
                found &= tf.DomainState.Actual == state.Actual.Substring(0, state.Actual.Length - subsLength);

                for (int i = 0; i < state.SubScripts.Count; i++)
                {
                    found &= SymMap[tf.DomainHeadValues[i]] == state.SubScripts[i];
                }

                if (found)
                {
                    lstRetVal.Add(tf);
                }
            }

            return lstRetVal;
        }

        public static bool CompareTFs(TransitionFunction tf1, TransitionFunction tf2)
        {
            bool same = true;

            same &= tf1.DomainState.Actual == tf2.DomainState.Actual;

            for (int i = 0; i < tf1.DomainHeadValues.Count; i++)
            {
                same &= tf1.DomainHeadValues[i] == tf2.DomainHeadValues[i];
            }

            same &= tf1.RangeState.Actual == tf2.RangeState.Actual;

            for (int i = 0; i < tf1.RangeHeadWrite.Count; i++)
            {
                same &= tf1.RangeHeadWrite[i] == tf2.RangeHeadWrite[i];
            }

            for (int i = 0; i < tf1.RangeHeadMove.Count; i++)
            {
                same &= tf1.RangeHeadMove[i] == tf2.RangeHeadMove[i];
            }

            return same;
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
