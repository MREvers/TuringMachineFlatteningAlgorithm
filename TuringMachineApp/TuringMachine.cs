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

        /*
        static Dictionary<string, string> SymMap = new Dictionary<string, string>()
        {
            {"1","i"},
            {"0","o"},
            {"#","H"},
            {"_","B"},
            {"|","V" }
            
        };
        */


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

        public void Flatten2(ref string doc)
        {
            TuringMachine tm = this;

            int NUM_TAPES = tm.TransitionFunctions.First().DomainHeadValues.Count;
            List<string> TapeLibrary = GetTapeLibrary();
            TapeLibrary.Add("#");

            List<string> NonHeaderLibrary = TapeLibrary.ToList();
            List<string> HeaderLibrary = NonHeaderLibrary.Select(x => SymMap[x]).ToList();
            TapeLibrary.Add("B");
            HeaderLibrary.Add("B");
            

            // Get the mkStates.
            List<State> mkStates = GetStates();

            // Record the Final States
            List<State> OutputStates = new List<State>();

            // Record the Final Transition Functions
            List<TransitionFunction> OutputTFs = new List<TransitionFunction>();

            //Build states until each mkstate can be uniquely determined.
            foreach(State mkState in mkStates)
            {
                // Get a determined state for each TF branching from mkState.
                #region TF Determination

                // TRACK determined states 
                // These states uniquely determine which TF operation should be carried out.
                List<DeterminedState> determinedStates = new List<DeterminedState>();

                #region Determine and Build 1,2,...,n Parameter Branching States and TFs from this mkState.
                // First we need to find the appropriate state parameters.
                // Look at all the transition function that operate on state
                //  to find all combinations of parms.
                #region Determine Possible Parameters from TransitionFunctions that Branch from the mkState
                List<TransitionFunction> mkStateTFs = new List<TransitionFunction>();
                foreach (TransitionFunction tf in tm.TransitionFunctions)
                {
                    if (tf.DomainState.Actual == mkState.Actual)
                    {
                        mkStateTFs.Add(tf);
                    }
                }
                #endregion

                // Second, we need to construct all state permutations.
                // Duplicates may occur here so they need to be checked later.
                // This basically FLATTENS a TF.
                #region Construct the States and TFs that Correspond to each TF and...
                // ...and transition functions that move each permutation state right until
                //  the next head symbol.
                List<State> permutationStates = new List<State>(); // This needs to be filtered later.
                List<TransitionFunction> permState_MoveAndTransToNext_TransitionFunctions = new List<TransitionFunction>(); // This needs to be filtered later.
                foreach(TransitionFunction matchedTF in mkStateTFs)
                {
                    for (int i = 0; i <= NUM_TAPES; i++)
                    {
                        State permState;

                        #region Build the States
                        if (i == 0)
                        {
                            permState = new State(mkState.Actual, mkState);
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
                            permutationStates.Add(permState);
                        }
                        #endregion

                        #region Build the TFs that Move Each 'permState' to the Next. Does Not Transition To NEXT nor DETERMINED N.
                        // If the 'permState' uniquely determines an mkTF, 
                        //  include it in determined states as well.

                        // Each permState will continue right until a Header Char.
                        foreach(string nonheader in NonHeaderLibrary)
                        {
                            TransitionFunction permState_MoveToNext = new TransitionFunction(
                                permState.Actual, nonheader);
                            permState_MoveToNext.DefineRange(permState.Actual, nonheader, SZMOVE_RIGHT);
                            permState_MoveAndTransToNext_TransitionFunctions.Add(permState_MoveToNext);
                        }
                        #endregion

                        #region Build TFs that Transition each 'permState' to NEXT. If last permState, skip this.
                        // Find all the transition functions that correspond to the permState.
                        // Build all the tfs that transition to the next restrictive permState.

                        if (i < NUM_TAPES)
                        {
                            // e.g. mkTFs (q1,0,1) and (q1,0,0), match q1o. So 1, and 0, are potential
                            //  transition parameters.
                            List<TransitionFunction> potentialPermStates = GetPossibleTFs(permState);
                            foreach (TransitionFunction permMatchedTF in potentialPermStates)
                            {
                                string permMatchedPotentialChar = permMatchedTF.DomainHeadValues[i];
                             
                                TransitionFunction permState_TransitToNext = new TransitionFunction(
                                    permState.Actual, SymMap[permMatchedPotentialChar]);
                                string szNextState = permState.Actual + SymMap[permMatchedPotentialChar];
                                permState_TransitToNext.DefineRange(szNextState, SymMap[permMatchedPotentialChar], SZMOVE_RIGHT);
                                permState_MoveAndTransToNext_TransitionFunctions.Add(permState_TransitToNext);

                                // Branch on _
                                if (permMatchedPotentialChar == "_")
                                {
                                    TransitionFunction permState_TransitToNextBranch = new TransitionFunction(
                                        permState.Actual, SymMap["#"]);
                                    string szNextState2 = permState.Actual + SymMap["_"];
                                    permState_TransitToNextBranch.DefineRange(szNextState2, SymMap["#"], SZMOVE_RIGHT);
                                    permState_MoveAndTransToNext_TransitionFunctions.Add(permState_TransitToNextBranch);
                                }
                                
                            }
                        }
                        #endregion

                        #region If last, build TF that Transition determined 'permState' to the DETERMINED N state. Determined n state is q(y...y)n and...
                        // ...additionaly, replace the state with a DeterminedState.
                        if (i == NUM_TAPES)
                        {

                            TransitionFunction permState_TransitToNext = new TransitionFunction(
                                permState.Actual, END_SYMBOL);
                            string szNextState = permState.Actual + i;
                            permState_TransitToNext.DefineRange(szNextState, END_SYMBOL, SZMOVE_LEFT);

                            permState_MoveAndTransToNext_TransitionFunctions.Add(permState_TransitToNext);

                            DeterminedState determinedPermState = new DeterminedState(permState.Actual, permState, matchedTF);
                            determinedStates.Add(determinedPermState);

                        }
                        #endregion
                    }
                }
                #endregion

                #region Filter Possible Duplicates from PermStates
                foreach(State state in permutationStates)
                {
                    if (OutputStates.Where(x => x.Actual == state.Actual).Count() == 0)
                    {
                        OutputStates.Add(state);
                    }
                }

                foreach (TransitionFunction intTF in permState_MoveAndTransToNext_TransitionFunctions)
                {

                    bool found = false;
                    foreach (TransitionFunction hasTf in OutputTFs)
                    {
                        found |= CompareTFs(intTF, hasTf);
                    }
                    if (!found)
                    {
                        OutputTFs.Add(intTF);
                    }

                }

                #endregion

                #endregion

                #endregion

                // Build states and TFs needed to carry out TF.
                #region TF Perform
                List<State> performStates = new List<State>();
                List<TransitionFunction> performTransitions = new List<TransitionFunction>();
                foreach (DeterminedState dstate in determinedStates)
                {
                    // Since each dstate is unique, states and tfs can be added immediately.
                    #region Build Each Determined N State's n Transitions, PerformWrite, and PerformMove States and TFs
                    for(int i = 1; i <= NUM_TAPES; i++)
                    {
                        #region Build Determined Nth Write State, and Determined nth Move Head State.
                        DeterminedState dstateNthTape = new DeterminedState(
                            dstate.Actual + i,
                            dstate,
                            dstate.TF,
                            (i).ToString());
                        performStates.Add(dstateNthTape);

                        DeterminedState dstateNthTape_MoveHead = new DeterminedState(
                            dstateNthTape.Actual + "a",
                            dstateNthTape,
                            dstateNthTape.TF,
                            ("a").ToString());
                        performStates.Add(dstateNthTape_MoveHead);
                        #endregion

                        #region Build Transition Functions that Move to Previous Head Symbol.
                        foreach (string nonheadSymbol in NonHeaderLibrary)
                        {
                            TransitionFunction determinedNState_MoveToPrevious = new TransitionFunction(
                                        dstateNthTape.Actual, nonheadSymbol);
                           
                            determinedNState_MoveToPrevious.DefineRange(
                                dstateNthTape.Actual, nonheadSymbol, SZMOVE_LEFT);

                            performTransitions.Add(determinedNState_MoveToPrevious);
                        }
                        #endregion

                        #region Build Transition Functions that Perform the Write.
                        string NthParameterOfDeterminedState = dstateNthTape.SubScripts[i-1];
                        TransitionFunction determinedNState_FoundPreviousAndWrite = new TransitionFunction(
                                    dstateNthTape.Actual, NthParameterOfDeterminedState);

                        string NthTapeTransitionWrite = dstateNthTape_MoveHead.TF.RangeHeadWrite[i-1];
                        string NthTapeTransitionMove = dstateNthTape_MoveHead.TF.RangeHeadMove[i-1];
                        if (NthTapeTransitionMove != SZSTAY)
                        {
                            determinedNState_FoundPreviousAndWrite.DefineRange(
                                dstateNthTape_MoveHead.Actual, NthTapeTransitionWrite, NthTapeTransitionMove);
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

                        performTransitions.Add(determinedNState_FoundPreviousAndWrite);
                        #endregion

                        #region If Write On Blank, Branch into Shift All Right Procedure.
                        // "H"
                        if (NthParameterOfDeterminedState == SymMap["_"] && NthTapeTransitionWrite != "_")
                        {
                            // Build the return to proc shift state
                            DeterminedState dstateNthTape_ReturnToProc = new DeterminedState(
                                dstateNthTape + "S" + RETURN_SYMBOL,
                                dstateNthTape,
                                dstateNthTape.TF,
                                RETURN_SYMBOL);
                                
                            #region Build Transition to Branch Transition Function
                            TransitionFunction determinedNState_BranchOnPound = new TransitionFunction(
                                dstateNthTape.Actual, SymMap["#"]);
                            determinedNState_BranchOnPound.DefineRange(
                                dstateNthTape.Actual + "S" + "#", RETURN_SYMBOL, SZMOVE_RIGHT);
                            performTransitions.Add(determinedNState_BranchOnPound);
                            #endregion

                            #region Build Shift Right TFs and Shift States
                            foreach (string anySymbol in TapeLibrary.Concat(new List<string>() { END_SYMBOL }))
                            {
                                string sourceState = dstateNthTape.Actual + "S" + anySymbol;
                                DeterminedState dstateNthTape_ShiftRightState = new DeterminedState(
                                            sourceState,
                                            dstateNthTape,
                                            dstateNthTape.TF,
                                            anySymbol);
                                performStates.Add(dstateNthTape_ShiftRightState);
                            }

                            var TPLWD = TapeLibrary.ToList().Concat(new List<string>() { END_SYMBOL });
                            foreach (string anySymbol in TapeLibrary)
                            {
                                foreach (string targetSymbol in TPLWD)
                                {
                                    // (q#, $) --> (q$, #, R)
                                    if ((targetSymbol == END_SYMBOL && anySymbol == "#") ||
                                        (targetSymbol != END_SYMBOL))
                                    {
                                        string sourceState = dstateNthTape.Actual + "S" + anySymbol;
                                        TransitionFunction dstateNthTape_ShiftRight = new TransitionFunction(
                                            sourceState, targetSymbol);

                                        string targetState = dstateNthTape.Actual + "S" + targetSymbol;
                                        dstateNthTape_ShiftRight.DefineRange(
                                            targetState, anySymbol, SZMOVE_RIGHT);

                                        performTransitions.Add(dstateNthTape_ShiftRight);

                                    }
                                        
                                }
                            }
                            #endregion

                            #region Build Found End Shift Transition To (Move To Return to Proc) State.
                            TransitionFunction dstateNthTape_ShiftFoundEnd = new TransitionFunction(
                                dstateNthTape.Actual + "S" + END_SYMBOL, "_");

                            dstateNthTape_ShiftFoundEnd.DefineRange(
                                dstateNthTape.Actual + "S" + RETURN_SYMBOL, END_SYMBOL, SZMOVE_LEFT);
                            performTransitions.Add(dstateNthTape_ShiftFoundEnd);
                            #endregion

                            #region Build Move To Return to Proc TFs and Transition to Proc
                            foreach(string anySymbol in TapeLibrary)
                            {
                                    
                                TransitionFunction dstateNthTape_ReturnToProcedure = new TransitionFunction(
                                    dstateNthTape.Actual + "S" + RETURN_SYMBOL, anySymbol);

                                dstateNthTape_ReturnToProcedure.DefineRange(
                                    dstateNthTape.Actual + "S" + RETURN_SYMBOL, anySymbol, SZMOVE_LEFT);
                                performTransitions.Add(dstateNthTape_ReturnToProcedure);
                                    
                            }

                            TransitionFunction dstateNthTape_TransitionToProcedure = new TransitionFunction(
                                    dstateNthTape.Actual + "S" + RETURN_SYMBOL, RETURN_SYMBOL);

                            dstateNthTape_TransitionToProcedure.DefineRange(
                                dstateNthTape.Actual, SymMap["_"], SZSTAY);
                            performTransitions.Add(dstateNthTape_TransitionToProcedure);
                            #endregion
                        }
                        else if (NthParameterOfDeterminedState == SymMap["_"] && NthTapeTransitionWrite == "_")
                        {
                            string NthParameterOfDeterminedStateBranchOnBlank = SymMap["#"];
                            TransitionFunction determinedNState_FoundPreviousAndWriteBranchOnBlank = new TransitionFunction(
                                        dstateNthTape.Actual, NthParameterOfDeterminedStateBranchOnBlank);

                            string NthTapeTransitionMoveBranchOnBlank = dstateNthTape_MoveHead.TF.RangeHeadMove[i - 1];
                            if (NthTapeTransitionMoveBranchOnBlank != SZSTAY)
                            {
                                determinedNState_FoundPreviousAndWriteBranchOnBlank.DefineRange(
                                    dstateNthTape_MoveHead.Actual, "#", NthTapeTransitionMoveBranchOnBlank);
                            }
                            else
                            {
                                if (i > 1)
                                {
                                    determinedNState_FoundPreviousAndWriteBranchOnBlank.DefineRange(
                                   dstate.Actual + (i - 1), SymMap["#"], SZMOVE_LEFT);
                                }
                                else
                                {
                                    determinedNState_FoundPreviousAndWriteBranchOnBlank.DefineRange(
                                    dstate.Actual + "F", SymMap["#"], SZMOVE_LEFT);
                                }
                                
  
                            }
                  
                            performTransitions.Add(determinedNState_FoundPreviousAndWriteBranchOnBlank);
                        }
                        #endregion

                        #region Build Transition Functions That Perform the Head Move.
                        if (NthTapeTransitionMove != SZSTAY)
                        {
                            if (i > 1)
                            {
                                foreach (string character in NonHeaderLibrary)
                                {
                                    // Exclude this combination because the head has moved before the beginning of the tape.
                                    if (!(character == "#" && dstateNthTape_MoveHead.TF.RangeHeadMove[i - 1] == SZMOVE_LEFT))
                                    {
                                        TransitionFunction determinedNState_MoveHead = new TransitionFunction(
                                                dstateNthTape_MoveHead.Actual, character);
                                        determinedNState_MoveHead.DefineRange(
                                            dstate.Actual + (i - 1), SymMap[character], SZMOVE_LEFT);
                                        performTransitions.Add(determinedNState_MoveHead);
                                    }
                                    
                                }
                            }
                            else
                            {
                                foreach (string character in NonHeaderLibrary)
                                {
                                    // Exclude this combination because the head has moved before the beginning of the tape.
                                    if (!(character == "#" && dstateNthTape_MoveHead.TF.RangeHeadMove[i - 1] == SZMOVE_LEFT))
                                    {
                                        TransitionFunction determinedNState_MoveHead = new TransitionFunction(
                                                dstateNthTape_MoveHead.Actual, character);
                                        determinedNState_MoveHead.DefineRange(
                                            dstate.Actual + "F", SymMap[character], SZMOVE_LEFT);
                                        performTransitions.Add(determinedNState_MoveHead);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                    #endregion

                    #region Build State that Signifies the Corresponding mkTF is complete and...
                    // ...the transition functions that return to the beginning of the tape.
                    // Then Transition to next mkTF.

                    #region Build State that Signifies Completion
                    DeterminedState permStateCompleted = new DeterminedState(
                        dstate.Actual + "F", dstate, dstate.TF, "F");
                    performStates.Add(permStateCompleted);
                    #endregion

                    #region Build the TFs that Move to Beginning of Tape. Does NOT Transit to Next State.
                    foreach(string character in TapeLibrary.Where(x => x != "#"))
                    {
                        TransitionFunction completeState_MoveToBegin = new TransitionFunction(
                            permStateCompleted.Actual, character);
                        completeState_MoveToBegin.DefineRange(permStateCompleted.Actual, character, SZMOVE_LEFT);
                        performTransitions.Add(completeState_MoveToBegin);
                    }
                    #endregion

                    #region Build the TF that TRANSITS to NEXT mkTF
                    TransitionFunction completeState_TransitToNext = new TransitionFunction(
                        permStateCompleted.Actual, "#");
                    completeState_TransitToNext.DefineRange(
                        permStateCompleted.TF.RangeState.Actual,
                        "#",
                        SZMOVE_RIGHT);
                    performTransitions.Add(completeState_TransitToNext);
                    #endregion

                    #endregion 

                }

                #region Filter Possible Duplicates from PermStates
                foreach (State state in performStates)
                {
                    if (OutputStates.Where(x => x.Actual == state.Actual).Count() == 0)
                    {
                        OutputStates.Add(state);
                    }
                }

                foreach (TransitionFunction intTF in performTransitions)
                {

                    bool found = false;
                    foreach (TransitionFunction hasTf in OutputTFs)
                    {
                        found |= CompareTFs(intTF, hasTf);
                    }
                    if (!found)
                    {
                        OutputTFs.Add(intTF);
                    }

                }

                #endregion
                #endregion

            }

            #region OutPut
            //string doc = "";


            CompileOutputTFs(ref doc, OutputTFs);
            foreach (TransitionFunction tf in OutputTFs)
            {
                string output = tf.DomainState.Actual + ":";
                foreach (string sz in tf.DomainHeadValues)
                {
                    output += "," + sz;
                }
                output += " ----> ";
                output += tf.RangeState.Actual;
                foreach (string sz in tf.RangeHeadWrite)
                {
                    output += " ," + sz;
                }
                output += ": (";
                foreach (string sz in tf.RangeHeadMove)
                {
                    output += ", " + sz;
                }
                output += ")";
                Console.WriteLine(output);
            }
            
            #endregion
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

                /*
                IEnumerable<TransitionFunction> determinedStates_TransitionFunction_MoveToEndI =
                    E_BuildTransitionFunctions_Determined_MoveToEnd(determinedStates, NonHeaderLibrary);
                tmp = tmp.Concat(determinedStates_TransitionFunction_MoveToEndI);
                */

                IEnumerable<TransitionFunction> determinedStates_TransitionFunction_ChangeToActionStates =
                    E_BuildTransitionFunctions_Determined_BeginActionStates(determinedStates, NonHeaderLibrary);
                tmp = tmp.Concat(determinedStates_TransitionFunction_ChangeToActionStates);

                /*
                IEnumerable<TransitionFunction> determinedStates_TransitionFunction_FoundEndChangeToNextStateI =
                    E__BuildTransitionFunctions_Determined_FoundEndChangeToActorState_N(determinedStates).ToList();
                tmp = tmp.Concat(determinedStates_TransitionFunction_FoundEndChangeToNextStateI);
                */

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

                /*
                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_0F_MoveToBeginning =
                    E_BuildTransitionFunctions_Determined_ActorState_0F_MoveToBeginning(
                        determinedStates_ActorStates_0F_Complete,
                        TapeLibrary);
                */
                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_0F_MoveToBeginning =
                    E_BuildTransitionFunctions_Determined_ActorState_0F_ChangeToNext(
                        determinedStates_ActorStates_0F_Complete,
                        TapeLibrary);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_0F_MoveToBeginning);

                /*
                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_0F_FoundBeginningChangeToNextMKState =
                    E_BuildTransitionFunctions_Determined_ActorState_0F_FoundBeginningChangeToNextMKState(
                        determinedStates_ActorStates_0F_Complete);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_0F_FoundBeginningChangeToNextMKState);
                */
                

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

        private IEnumerable<TransitionFunction> E_GetMKTransitionFunctionsSuchThat_State_(
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

        private IEnumerable<State> E_BuildUnderterminedStates(
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

        private IEnumerable<DeterminedState> E_BuildDeterminedStates(
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
                    permState.Actual, permState, matchedTF);
                yield return returnState;
            }
        }

        private IEnumerable<DeterminedState>E_BuildDeterminedStates_ActorStates_N_Write(
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
                        dstate.TF,
                        (i).ToString());
                    yield return dstateNthTape;
                }
            }
        }

        private IEnumerable<DeterminedState> E_BuildDeterminedStates_ActorStates_0F_Complete(
            IEnumerable<DeterminedState> determinedStates)
        {
            foreach (DeterminedState dstate in determinedStates)
            {
                DeterminedState permStateCompleted = new DeterminedState(
                        dstate.Actual + "F", dstate, dstate.TF, "F");
                yield return permStateCompleted;
            }
        }

        private IEnumerable<DeterminedState> E_BuildDeterminedStates_ActorStates_N_MoveHead(
            IEnumerable<DeterminedState> determinedStates_ActorStates_Write)
        {
            
            foreach (DeterminedState dstateNthTape in determinedStates_ActorStates_Write)
            {
                DeterminedState dstateNthTape_MoveHead = new DeterminedState(
                        dstateNthTape.Actual + "a",
                        dstateNthTape,
                        dstateNthTape.TF,
                        ("a").ToString());
                yield return dstateNthTape_MoveHead;
            }
        }

        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Undetermined_XthParm_MoveToNext(
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

        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_MoveToEnd(
            IEnumerable<DeterminedState> determinedStates,
            List<string> NonHeaderLibrary)
        {
            foreach (State permState in determinedStates)
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

        //BYPASS MOVE TO END
        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_BeginActionStates(
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


        private IEnumerable<TransitionFunction> E__BuildTransitionFunctions_Determined_FoundEndChangeToActorState_N(
            IEnumerable<DeterminedState> determinedStates)
        {
            int NUM_TAPES = this.TransitionFunctions.First().DomainHeadValues.Count;
            foreach (State permState in determinedStates)
            {
                TransitionFunction permState_TransitToNext = new TransitionFunction(
                                permState.Actual, END_SYMBOL);
                string szNextState = permState.Actual + NUM_TAPES;
                permState_TransitToNext.DefineRange(szNextState, END_SYMBOL, SZMOVE_LEFT);

                yield return permState_TransitToNext;
            }
        }


        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Undetermined_XthParm_FoundNextChangeToNextState(
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

        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState(
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

        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_ActorState_0F_MoveToBeginning(
            IEnumerable<DeterminedState> determinedStates_ActorStates_0F_Complete,
            List<string> TapeLibrary)
        {
            foreach (DeterminedState permStateCompleted in determinedStates_ActorStates_0F_Complete)
            {
                foreach (string character in TapeLibrary.Where(x => x != "#"))
                {
                    TransitionFunction completeState_MoveToBegin = new TransitionFunction(
                        permStateCompleted.Actual, character);
                    completeState_MoveToBegin.DefineRange(permStateCompleted.Actual, character, SZMOVE_LEFT);
                    yield return completeState_MoveToBegin;
                }
            }
        }

        // BYPASS 0F MOVE TO BEGINNING
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

        private IEnumerable<TransitionFunction> E_BuildTransitionFunctions_Determined_ActorState_0F_FoundBeginningChangeToNextMKState(
            IEnumerable<DeterminedState> determinedStates_ActorStates_0F_Complete)
        {
            foreach (DeterminedState permStateCompleted in determinedStates_ActorStates_0F_Complete)
            {

                TransitionFunction completeState_TransitToNext = new TransitionFunction(
                        permStateCompleted.Actual, "#");
                completeState_TransitToNext.DefineRange(
                    permStateCompleted.TF.RangeState.Actual,
                    "#",
                    SZMOVE_RIGHT);
                yield return completeState_TransitToNext;

            }
        }

        // Need the states still
        // Branches by replaceing on anything that would write on H
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
