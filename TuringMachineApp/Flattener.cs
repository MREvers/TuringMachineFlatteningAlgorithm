﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringMachineApp
{
    class Flattener
    {
        static string SZMOVE_RIGHT = ">";
        static string SZMOVE_LEFT = "<";
        static string SZSTAY = "-";

        static string END_SYMBOL = "$";
        static string RETURN_SYMBOL = "R";
        static string NULL_SYMBOL = "~";

        readonly int NUM_TAPES;
        readonly int ITERATION;
        TuringMachine TM;


        /// <summary>
        /// Used to map symbols to their equivalent symbol with a virtual header.
        /// </summary>
        class SampleCollection
        {
            int ITERATION;
            /// <summary>
            /// Iteration is the number of the virtual head.
            /// If the flattening algorithm is being used for the first time,
            /// the virtual heads should all be 1s (as in 1 level down).
            /// Second Time -> 2s
            /// etc.
            /// </summary>
            /// <param name="iter"></param>
            public SampleCollection(int iter)
            {
                ITERATION = iter;
            }
            public string this[string i]
            {
                get { return i + "[" + ITERATION + "]"; }
            }
        }

        SampleCollection SymMap;

        /// <summary>
        /// Iteration is the number of the virtual head.
        /// If the flattening algorithm is being used for the first time,
        /// the virtual heads should all be 1s (as in 1 level down).
        /// Second Time -> 2s
        /// etc.
        /// </summary>
        /// <param name="iter"></param>
        public Flattener(TuringMachine tf, int iteration = 1)
        {
            ITERATION = iteration;
            SymMap = new SampleCollection(ITERATION);
            NUM_TAPES = tf.TransitionFunctions.First().DomainHeadValues.Count;
            TM = tf;

        }

        /// <summary>
        /// Generally, it takes each tape, puts them on a single tape separated by #s.
        /// The head then sweeps right and determines what symbol each "Simulated Head" 
        /// lies on. Once each Simulated Head is determined, the head then sweeps left
        /// again to perform the changes to each simulated head.
        /// 
        /// Each simulated head represents a head on another tape prior to flattenning.
        /// 
        /// THIS IS THE ALGORITHM FOR NON BIDIRECTIONAL TURING MACHINE FLATTENNER
        /// 
        /// Details:
        /// Transition functions must be constructed for each process in the algorithm.
        /// First thing that needs to be done is the sweep right to determine the
        /// states of the "simulated heads". This is done so the flat machine can determine
        /// which transition function to simulate from the original k-tape machine.
        /// While sweeping right, the transition functions operate on "Undetermined States"
        /// 
        /// TODO: EXAMPLE HERE
        /// 
        /// Once all simulated heads are determined, the state of the flat machine can
        /// be uniquely assigned to a transition function from the machine prior to
        /// flattenning. The states with this ability are called the  "Determined States"
        /// The determined state transition functions sweep left until they reach a 
        /// simulated head. Once a simulated head is reached, the transition functions
        /// move the simulated head, then perform the change to that "simulated tape"
        /// The head then continues sweeping left for each simulated head.
        /// The machine then goes back to undetermined states and the process repeats
        /// until an accept or reject state is reached.
        /// 
        /// Summary:
        /// 1: Create Transition Functions for sweeping right - undetermined states
        ///     a: Transition functions for going from no known simulated heads to the first.
        ///        - Sweep_Right_XthParm_MoveToNext   
        ///     b: Transition functions for transitioning to the correct state that knows the
        ///         location of each head on the previous simulated tapes
        ///        - Sweep_Right_XthParm_FoundNextChangeToNextState
        ///     c: Transition functions for going from first to second. 
        ///        - Sweep_Right_XthParm_MoveToNext   
        ///     d: Transition functions for going from second to third... etc. 
        ///        - Sweep_Right_XthParm_MoveToNext   
        ///     e: Transition functions for going from the last head to a determined state. 
        ///        - Sweep_Right_Complete_BeginActionStates   
        /// 2: Create Transition Functions for sweeping left - determined states
        ///     a: Transition functions for going left until the last simulated head. 
        ///        - Sweep_Right_Complete_BeginActionStates
        ///     b: Transition functions for transtioning to states that perform changes on the simulated heads.
        ///         i: Transition functions for performing the change on the simulated tape. 
        ///            CAVEAT: For each write operation that can possible write on a blank space "_"
        ///                     include a transition function that can also write on "#". This is done
        ///                     in ii.
        ///            - Sweep_Left_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState
        ///        ii: Transition functions for extending the simulated tape if a write was
        ///              performed on a blank space that was actually a tape end "#". See the
        ///              function for extending the tape.
        ///       iii: Transition functions for moving the simulated head and returning to the sweep left.
        ///            - Sweep_Left_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious
        ///     c: Transition functions for moving the the previous (N less 1) simulated head.
        ///            - Sweep_Left_ActorState_N_MoveToPrevious
        ///     d: Transition functions for meving from the first simulated head to a completed state.
        ///            - Sweep_Left_Complete_ActorState_0F_ChangeToNext
        /// 3: Create Transition functions for transition from determined states to undetermined states
        ///     that then perform another sweep right OR check for completion.
        ///            - Sweep_Left_Complete_ActorState_0F_ChangeToNext
        ///    
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="leftSafety"></param>
        public void Flatten(ref string doc, bool leftSafety = false)
        {
            #region Setup
            TuringMachine tm = TM;
            Push_Symbols(ref TM.TransitionFunctions);
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
            List<TransitionFunction> OutputTFs = new List<TransitionFunction>();
            List<TransitionFunction> UnbranchedFinalizedList = null;
            #endregion Setup

            #region Iterate of States Of Multitape Machine Mk
            foreach (State mkState in mkStates)
            {

                //Build the states... These are really just used for counting.
                IEnumerable<TransitionFunction> TransitionFunction_State_ = Get_Domain_States(mkState, tm.TransitionFunctions);

                IEnumerable<State> undeterminedStates = Construct_Undetermined_States(mkState, TransitionFunction_State_)
                    .Distinct();
                IList<DeterminedState> determinedStates = Construct_Determined_States(mkState, TransitionFunction_State_).ToList();

                IEnumerable<DeterminedState> determinedStates_ActorStates_N_Write =
                    Construct_Transition_States_Primary(determinedStates);

                IEnumerable<DeterminedState> determinedStates_ActorStates_N_MoveHead =
                    Construct_Transition_States_Secondary(determinedStates_ActorStates_N_Write);

                IEnumerable<DeterminedState> determinedStates_ActorStates_0F_Complete =
                    Construct_Transition_Complete_States(determinedStates);

                //Build the transition functions.
                IEnumerable<TransitionFunction> tmp = new List<TransitionFunction>();
                IEnumerable<TransitionFunction> undeterminedStates_TransitionFunctions_XthParm_MoveToNextI =
                    Sweep_Right_XthParm_MoveToNext(undeterminedStates, NonHeaderLibrary);
                tmp = tmp.Concat(undeterminedStates_TransitionFunctions_XthParm_MoveToNextI).ToList();

                IEnumerable<TransitionFunction> undeterminedStates_TransitionFunction_XthParm_FoundNextChangeToNextI =
                    Sweep_Right_XthParm_FoundNextChangeToNextState(undeterminedStates);
                tmp = tmp.Concat(undeterminedStates_TransitionFunction_XthParm_FoundNextChangeToNextI);

                IEnumerable<TransitionFunction> determinedStates_TransitionFunction_ChangeToActionStates =
                    Sweep_Right_Complete_BeginActionStates(determinedStates, NonHeaderLibrary);
                tmp = tmp.Concat(determinedStates_TransitionFunction_ChangeToActionStates);

                IEnumerable<TransitionFunction> determinedStates_TransitionFunction_ActorState_N_MoveToPreviousAndWrite =
                    Sweep_Left_ActorState_N_MoveToPrevious(
                        determinedStates_ActorStates_N_Write,
                        NonHeaderLibrary);
                tmp = tmp.Concat(determinedStates_TransitionFunction_ActorState_N_MoveToPreviousAndWrite);

                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState =
                    Sweep_Left_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState(
                        determinedStates);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState);

                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious =
                    Sweep_Left_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious(
                        determinedStates,
                        NonHeaderLibrary);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious);

                IEnumerable<TransitionFunction> determinedState_TransitionFunction_ActorState_0F_MoveToBeginning =
                    Sweep_Left_Complete_ActorState_0F_ChangeToNext(
                        determinedStates_ActorStates_0F_Complete,
                        TapeLibrary);
                tmp = tmp.Concat(determinedState_TransitionFunction_ActorState_0F_MoveToBeginning);

                OutputTFs = OutputTFs.Concat(tmp).ToList();
                //UniqueAdd(ref OutputTFs, tmp.ToList());
                Console.WriteLine("Presafety");
            }
            Console.WriteLine(OutputTFs.Count());
            List<TransitionFunction> withBranches = Include_Shift_Right_Safety(
                     ref OutputTFs,
                     TapeLibrary).ToList();
            Console.WriteLine("RightSafety");

            List<TransitionFunction> withBranchesRH;
            if (leftSafety)
            {
                withBranchesRH = Include_Shift_Left_Safety_RightHanded(
                 ref OutputTFs,
                 TapeLibrary).ToList();
                Console.WriteLine("LeftSafety");
                Console.WriteLine(withBranchesRH.Count());
            }
            else
            {
                withBranchesRH = new List<TransitionFunction>();
            }
            
            //UniqueAdd(ref withBranchesRH, withBranches);

            //UnbranchedFinalizedList = OutputTFs.Concat(withBranchesRH).ToList();
            UnbranchedFinalizedList = Unique(OutputTFs.Concat(withBranchesRH).Concat(withBranches).ToList());
            Console.WriteLine(UnbranchedFinalizedList.Count());
            for (int i = 0; i < UnbranchedFinalizedList.Count(); i++)
            {
                doc += UnbranchedFinalizedList[i].ToString();
                if (i % 100 == 0)
                {
                    Console.WriteLine(i + "/" + UnbranchedFinalizedList.Count());
                }
            }


            #endregion Iterate of States Of Multitape Machine Mk
        }


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
            Construct_Undetermined_States(
            State baseState,
            IEnumerable<TransitionFunction> determiningTFs)
        {
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
            Construct_Determined_States(
            State baseState,
            IEnumerable<TransitionFunction> determiningTFs)
        {
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

        /// <summary>
        /// Takes in a list of states that can be directly mapped to a mkTF.
        /// i.e. TF(q1, 0, 1, _) has determined state q101_.
        /// 
        /// For each determined state input, this function outputs a state
        ///  signifying that the Nth virtual tape needs to be executed for
        ///  each virtual tape according to the mkTF
        ///  
        /// Input: q101_
        /// Yields: q101_N, q101_(N-1), q101_(N-2)...
        /// 
        /// These are called "Nth_Tape_Actor_States"
        /// </summary>
        /// <param name="determinedStates"></param>
        /// <returns></returns>
        private IEnumerable<DeterminedState>
            Construct_Transition_States_Primary(
            IEnumerable<DeterminedState> determinedStates)
        { 
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

        /// <summary>
        /// Takes in a list of determined states.
        /// 
        /// Input: q101_
        /// Output: q101_F
        /// </summary>
        /// <param name="determinedStates"></param>
        /// <returns></returns>
        private IEnumerable<DeterminedState>
            Construct_Transition_Complete_States(
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

        /// <summary>
        /// Takes in a list of actor states and includes
        ///  a state indicating that that state is currently
        ///  'active' in that the real head is moving the Nth
        ///  virtual head.
        ///  
        /// Input: q101_2
        /// Output: q101_2a
        /// 
        /// These are called Active "Nth_Tape_Actor_States" 
        /// </summary>
        /// <param name="determinedStates_ActorStates_Write"></param>
        /// <returns></returns>
        private IEnumerable<DeterminedState>
            Construct_Transition_States_Secondary(
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

        /// <summary>
        /// Starts from each undetermined state and builds all the necesary transition
        ///  functions to get to the next.
        /// 
        /// Builds tfs to go
        /// FROM: q1
        /// TO: q1 and move right UNTIL a VIRTUAL head
        /// 
        /// DOES NOT ACTUALY TRANSITION TO 'q1X' see next function
        /// </summary>
        /// <param name="undeterminedStates"></param>
        /// <param name="NonHeaderLibrary"></param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction>
        Sweep_Right_XthParm_MoveToNext(
                    IEnumerable<State> undeterminedStates,
                    List<string> NonHeaderLibrary)
        {
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

        /// <summary>
        /// Starts from each undetermined state and builds the transition from
        /// 
        /// FROM: q1
        /// TO: q1X where X is the symbol under the next 'virtual' head.
        /// 
        /// X is limited by the mkTFs.
        /// e.g. mkTFs (q1,0,1) and (q1,0,0), match q1o. So 1, and 0, are potential X's
        /// </summary>
        /// <param name="undeterminedStates"></param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction>
        Sweep_Right_XthParm_FoundNextChangeToNextState(
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

         /// <summary>
         /// Takes all input determined state, and produces the transition function
         ///  that transition to Nth_Tape_Action_States.
         ///  
         /// FROM: q... (determined state)
         /// TO: q...N (Where N is the number of tapes)
         /// </summary>
         /// <param name="determinedStates"></param>
         /// <param name="NonHeaderLibrary"></param>
         /// <returns></returns>
        private IEnumerable<TransitionFunction>
        Sweep_Right_Complete_BeginActionStates(
                    IEnumerable<DeterminedState> determinedStates,
                    List<string> NonHeaderLibrary)
        {
            foreach (State permState in determinedStates)
            {
                foreach (string nonheader in NonHeaderLibrary)
                {
                    TransitionFunction permState_MoveToNext = new TransitionFunction(
                        permState.Actual, nonheader);
                    permState_MoveToNext.DefineRange(permState.Actual + NUM_TAPES, nonheader, SZSTAY);
                    yield return permState_MoveToNext;
                }

                // Include the end symbol so that if the last head was on #, we can continue.
                TransitionFunction permState_MoveToNext2 = new TransitionFunction(
                        permState.Actual, END_SYMBOL);
                permState_MoveToNext2.DefineRange(permState.Actual + NUM_TAPES, END_SYMBOL, SZMOVE_LEFT);
                yield return permState_MoveToNext2;
            }
        }

        /// <summary>
        /// For each of the input active Nth_Tape_Action_State, it produces the transition
        /// functions required to move left until the next virtual head.
        /// 
        /// FROM: q...(K)
        /// TO: q...(K) until the 'previous' virtual head.
        /// </summary>
        /// <param name="determinedStates_ActorStates_N_Write"></param>
        /// <param name="NonHeaderLibrary"></param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction> 
        Sweep_Left_ActorState_N_MoveToPrevious(
                    IEnumerable<DeterminedState> determinedStates_ActorStates_N_Write,
                    List<string> NonHeaderLibrary)
        {
            foreach (DeterminedState dstateNthTape in determinedStates_ActorStates_N_Write)
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

        /// <summary>
        /// For each of the input active Nth_Tape_Action_State, this function produces
        ///  the transition functions required to go from the Kth Virtual tape Action state
        ///  to the ACTIVE Kth Virtual tape action state.
        ///  
        /// FROM: q...K
        /// TO: q...K until the 'previous' virtual head.
        /// 
        /// AND
        /// 
        /// FROM: q...K
        /// TO: q...Ka
        /// 
        /// </summary>
        /// <param name="determinedStates"></param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction>
        Sweep_Left_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState(
                    IEnumerable<DeterminedState> determinedStates)
        {
            foreach (DeterminedState dstate in determinedStates)
            {
                for (int i = NUM_TAPES; i > 0; i--)
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

        /// <summary>
        /// For each of the input active Nth_Tape_Action_State, this function produces
        ///  the transition functions required to go from the Kth Virtual tape active Action state
        ///  to the previous (K-1)th ActionState.
        ///  
        /// FROM: q...Ka
        /// TO: q...(K-1)
        /// </summary>
        /// <param name="determinedStates"></param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction>
        Sweep_Left_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious(
                    IEnumerable<DeterminedState> determinedStates,
                    List<string> NonHeaderLibrary)
        {
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
                                //if (!(character == "#" && dstate.TF.RangeHeadMove[i - 1] == SZMOVE_LEFT))
                                //{
                                    TransitionFunction determinedNState_MoveHead = new TransitionFunction(
                                            dstate.Actual + i + "a", character);
                                    determinedNState_MoveHead.DefineRange(
                                        dstate.Actual + (i - 1), SymMap[character], SZMOVE_LEFT);
                                    yield return determinedNState_MoveHead;
                                //}

                            }
                        }
                        else
                        {
                            foreach (string character in NonHeaderLibrary)
                            {
                                // Exclude this combination because the head has moved before the beginning of the tape.
                                //if (!(character == "#" && dstate.TF.RangeHeadMove[i - 1] == SZMOVE_LEFT))
                                //{
                                    TransitionFunction determinedNState_MoveHead = new TransitionFunction(
                                            dstate.Actual + i + "a", character);
                                    determinedNState_MoveHead.DefineRange(
                                        dstate.Actual + "F", SymMap[character], SZSTAY);
                                    yield return determinedNState_MoveHead;
                                //}
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// For each complete determined state, this function produces
        /// the transition functions that go from a completed determined
        /// state to the NEXT mkState anologous.
        /// 
        /// FROM: qA...0F
        /// TO: qB
        /// </summary>
        /// <param name="determinedStates_ActorStates_0F_Complete"></param>
        /// <param name="TapeLibrary"></param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction> 
        Sweep_Left_Complete_ActorState_0F_ChangeToNext(
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
        /// Detects states and transition functions that could potentially
        /// write passed the virtual tape 'end' and replaces those TFs and States
        /// with a "Subroutine" to shift the remaining characters on the real tape
        /// right, the return to the state that would've written passed then end.
        /// </summary>
        /// <param name="allTFS"></param>
        /// <param name="TapeLibrary"></param>
        /// <returns></returns>
        private List<TransitionFunction>
        Include_Shift_Right_Safety(
                    ref List<TransitionFunction> allTFS,
                    List<string> TapeLibrary)
        {
            List<TransitionFunction> removeTFs = new List<TransitionFunction>();
            List<TransitionFunction> branchTFs = new List<TransitionFunction>();
            List<TransitionFunction> outputTFs = new List<TransitionFunction>();
            Dictionary<string, string> branchOn = new Dictionary<string, string>() { { SymMap["#"], "#" } };
            foreach (TransitionFunction tf in allTFS)
            {

                // Don't extend the virtual tape if its just going to be written with a blank...
                if ( ( tf.DomainHeadValues[0] == SymMap["#"] ) &&
                     ( tf.RangeHeadWrite[0]   != SymMap["#"] ) &&
                     ( tf.RangeHeadWrite[0]   != "#"         )  )
                {
                    removeTFs.Add(tf);
                    foreach(TransitionFunction btf in Include_Shift_Right_On(
                        tf.DomainState.Actual, tf.DomainState.Actual, true, branchOn, TapeLibrary))
                    {
                        branchTFs.Add(btf);
                    }
                    
                }
            }

            foreach(TransitionFunction tf in removeTFs)
            {
                allTFS.Remove(tf);
            }

            return outputTFs.Concat(branchTFs).ToList();

        }

        /// <summary>
        /// THIS NEEDS WORK
        /// </summary>
        /// <param name="targetTFs">TFs to inc</param>
        /// <param name="allTFS"></param>
        /// <param name="TapeLibrary"></param>
        /// <returns></returns>
        private List<TransitionFunction>
        Include_Shift_Left_Safety_RightHanded(
                    ref List<TransitionFunction> allTFS,
                    List<string> TapeLibrary)
        {
            List<TransitionFunction> outputTFs = new List<TransitionFunction>();

            // Find each left moving tf
            List<TransitionFunction> leftMoveTFs = new List<TransitionFunction>();
            foreach(TransitionFunction tf in allTFS)
            {
                if (tf.RangeHeadMove[0] == SZMOVE_LEFT)
                {
                    leftMoveTFs.Add(tf);
                }
      
                 //outputTFs.Add(tf);  
                
            }

            // Now check all the states that those tfs turn into, these states potentialStates
            // Look for tfs for each potential state that writes on "#"
            List<TransitionFunction> offendingTFs = new List<TransitionFunction>();
            foreach(TransitionFunction tf in leftMoveTFs)
            {
                foreach(TransitionFunction otf in allTFS)
                {
                    if ((otf.DomainState.Actual == tf.RangeState.Actual) &&
                        (otf.DomainHeadValues[0] == "#") &&
                        (otf.RangeHeadWrite[0] != "#")    )
                    {
                        offendingTFs.Add(otf);
                    }
                }
            }

            // Remove the offendingTFs from allTFs
            foreach(TransitionFunction tf in offendingTFs)
            {
                allTFS.Remove(tf);
                //outputTFs.Remove(tf);
            }

            // Create a subprocedure that moves right one,
            // then shifts everything right, writing blank in the new space
            // then returning to the state that it was in.
            string startState;
            string shiftStartState;
            string returnState;
            foreach(TransitionFunction tf in offendingTFs)
            {
                startState = tf.DomainState.Actual;
                shiftStartState = startState + "B";
                returnState = tf.RangeState.Actual;
                // Create the initial right shift.
                TransitionFunction moveRightTF = new TransitionFunction(startState, "#");
                moveRightTF.DefineRange(shiftStartState, "#", SZMOVE_RIGHT);

                // Create all the starts that get it to the shift right
                Dictionary<string, string> branchStarterPairs = new Dictionary<string, string>();
                foreach(string sz in TapeLibrary)
                {
                    branchStarterPairs.Add(sz, sz);
                }

                List<TransitionFunction> branch
                    = Include_Shift_Right_On(shiftStartState, startState, false, branchStarterPairs, TapeLibrary).ToList();
                outputTFs.Add(moveRightTF);
                outputTFs = outputTFs.Concat(branch).ToList();
            }

            return outputTFs;
        }

        /// <summary>
        /// Expands the simulated tape between two #'s. Adds a blank character, then returns
        /// to the state that it was in.
        /// Only branches off of states that could potentially lead to this situation. I.e.
        /// if these states potentially arrived from a MOVE_RIGHT.
        /// </summary>
        /// <param name="branchState"></param>
        /// <param name="returnState"></param>
        /// <param name="putHead"></param>
        /// <param name="branchStarterPairs"></param>
        /// <param name="TapeLibrary"></param>
        /// <returns></returns>
        private IEnumerable<TransitionFunction>
        Include_Shift_Right_On(
                    string branchState,
                    string returnState,
                    bool putHead,
                    Dictionary<string,string> branchStarterPairs, //(from, to)
                    List<string> TapeLibrary)
        {
            // This needs to be done prior to calling this.
            
            foreach(string sz in branchStarterPairs.Keys)
            {
                TransitionFunction determinedNState_BranchOnPound = new TransitionFunction(
                                     branchState, sz);//symmap["#"]
                determinedNState_BranchOnPound.DefineRange(
                     branchState + "S" + branchStarterPairs[sz], RETURN_SYMBOL, SZMOVE_RIGHT);

                // Start the branch
                yield return determinedNState_BranchOnPound;
            }
            
            
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
                        string sourceState = branchState + "S" + anySymbol;
                        TransitionFunction dstateNthTape_ShiftRight = new TransitionFunction(
                            sourceState, targetSymbol);

                        string targetState = branchState + "S" + targetSymbol;
                        dstateNthTape_ShiftRight.DefineRange(
                            targetState, anySymbol, SZMOVE_RIGHT);

                        yield return dstateNthTape_ShiftRight;

                    }

                }
            }
            #endregion

            #region Build Found End Shift Transition To (Move To Return to Proc) State.
            TransitionFunction dstateNthTape_ShiftFoundEnd = new TransitionFunction(
                 branchState + "S" + END_SYMBOL, "_");

            dstateNthTape_ShiftFoundEnd.DefineRange(
                 branchState + "S" + RETURN_SYMBOL, END_SYMBOL, SZMOVE_LEFT);
            yield return dstateNthTape_ShiftFoundEnd;
            #endregion

            #region Build Move To Return to Proc TFs and Transition to Proc
            foreach (string anySymbol in TapeLibrary)
            {

                TransitionFunction dstateNthTape_ReturnToProcedure = new TransitionFunction(
                    branchState + "S" + RETURN_SYMBOL, anySymbol);

                dstateNthTape_ReturnToProcedure.DefineRange(
                     branchState + "S" + RETURN_SYMBOL, anySymbol, SZMOVE_LEFT);
                yield return dstateNthTape_ReturnToProcedure;

            }

            TransitionFunction dstateNthTape_TransitionToProcedure = new TransitionFunction(
                     branchState + "S" + RETURN_SYMBOL, RETURN_SYMBOL);

            // Determines whether or not for the 
            string puthead = putHead ? SymMap["_"] : "_";
            dstateNthTape_TransitionToProcedure.DefineRange(
                returnState, puthead, SZSTAY);
            yield return dstateNthTape_TransitionToProcedure;
            #endregion
        }

        #region Support Functions

        private List<TransitionFunction> GetPossibleTFs(State state)
        {
            TuringMachine tm = TM;
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

        private List<string> GetTapeLibrary()
        {
            List<string> lstRetVal = new List<string>();

            TuringMachine tm = TM;

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
            

            return lstRetVal.Where(x => x != END_SYMBOL && x != RETURN_SYMBOL && x != NULL_SYMBOL).ToList();
        }

        private List<State> GetStates()
        {
            TuringMachine tm = TM;
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

        private IEnumerable<TransitionFunction>
                    Get_Domain_States(
                    State state,
                    List<TransitionFunction> tfs)
        {
            foreach (TransitionFunction tf in tfs)
            {
                if (tf.DomainState.Actual == state.Actual)
                {
                    yield return tf;
                }
            }
        }

        private List<TransitionFunction> Unique(List<TransitionFunction> lst)
        {
            List<TransitionFunction> lstRetVal = new List<TransitionFunction>();

            for (int i = 0; i < lst.Count(); i++)
            {
                if (!lstRetVal.Contains(lst[i]))
                {
                    lstRetVal.Add(lst[i]);
                }
                if (i % 100 == 0)
                {
                    Console.WriteLine(i + "/" + lst.Count() + "   " + lstRetVal.Count());
                }
            }
            Console.WriteLine(lst.Count() + "/" + lst.Count() + "   " + lstRetVal.Count());

            return lstRetVal;
        }

        private void UniqueAdd(ref List<TransitionFunction> lst, List<TransitionFunction> newList)
        {

            for (int i = 0; i < newList.Count(); i++)
            {
                if (!lst.Contains(newList[i]))
                {
                    lst.Add(newList[i]);
                }
            }
        }


        private void Push_Symbols(ref List<TransitionFunction> tfs)
        {
            foreach(TransitionFunction tf in tfs)
            {
                for (int i = 0; i < tf.DomainHeadValues.Count; i++)
                {
                    if (tf.DomainHeadValues[i][0] == '#')
                    {

                        tf.DomainHeadValues[i] = Put_In_Second(tf.DomainHeadValues[i]);
                    }
                    else if (tf.DomainHeadValues[i][0] == '$')
                    {
                        tf.DomainHeadValues[i] = Put_In_Second(tf.DomainHeadValues[i]);
                    }
                    else if (tf.DomainHeadValues[i][0] == 'R')
                    {
                        tf.DomainHeadValues[i] = Put_In_Second(tf.DomainHeadValues[i]);
                    }

                }

                for (int i = 0; i < tf.RangeHeadWrite.Count; i++)
                {
                    if (tf.RangeHeadWrite[i][0] == '#')
                    {
                        tf.RangeHeadWrite[i] = Put_In_Second(tf.RangeHeadWrite[i]);
                    }
                    else if (tf.RangeHeadWrite[i][0] == '$')
                    {
                        tf.RangeHeadWrite[i] = Put_In_Second(tf.RangeHeadWrite[i]);
                    }
                    else if (tf.RangeHeadWrite[i][0] == 'R')
                    {
                        tf.RangeHeadWrite[i] = Put_In_Second(tf.RangeHeadWrite[i]);
                    }

                }
            }
        }

        private string Put_In_Second(string input, char insecond = '*')
        {
            string szOut = "";
            if (input.Count() > 1)
            {
                szOut = input.Substring(0,1) + "*" + input.Substring(1);
            }
            else
            {
                szOut = input + "*";
            }
            return szOut;
        }

        #endregion
    }
}
