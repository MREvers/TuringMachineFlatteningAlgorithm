Transforms a k-tape Turing Machine into a 1-tape machine. Utilizes special symbols and notation
understood by the TuringMachineSimulator project.

Expects to run in a directory with a file named "Input.txt". This file should conform
to the following style

name: <string_name>
init: <Start_State>
accept: [<End_State>, ...]
   

// Then one or more of the following Transition Function Form.
<Domain_State>, [<Domain_Input>, ...]
<Range_State>, [<Range_Output>, ...], [<Head_Moves>, ...]

The output will be placed in a file "Output.txt"

Algorithm Overview from code comments.

Generally, it takes each tape, puts them on a single tape separated by #s.
The head then sweeps right and determines what symbol each "Simulated Head" 
lies on. Once each Simulated Head is determined, the head then sweeps left
again to perform the changes to each simulated head.

Each simulated head represents a head on another tape prior to flattenning.

THIS IS THE ALGORITHM FOR NON BIDIRECTIONAL TURING MACHINE FLATTENNER

Details:
Transition functions must be constructed for each process in the algorithm.
First thing that needs to be done is the sweep right to determine the
states of the "simulated heads". This is done so the flat machine can determine
which transition function to simulate from the original k-tape machine.
While sweeping right, the transition functions operate on "Undetermined States"

TODO: EXAMPLE HERE

Once all simulated heads are determined, the state of the flat machine can
be uniquely assigned to a transition function from the machine prior to
flattenning. The states with this ability are called the  "Determined States"
The determined state transition functions sweep left until they reach a 
simulated head. Once a simulated head is reached, the transition functions
move the simulated head, then perform the change to that "simulated tape"
The head then continues sweeping left for each simulated head.
The machine then goes back to undetermined states and the process repeats
until an accept or reject state is reached.

Summary:
1: Create Transition Functions for sweeping right - undetermined states
    a: Transition functions for going from no known simulated heads to the first.
       - Sweep_Right_XthParm_MoveToNext   
    b: Transition functions for transitioning to the correct state that knows the
        location of each head on the previous simulated tapes
       - Sweep_Right_XthParm_FoundNextChangeToNextState
    c: Transition functions for going from first to second. 
       - Sweep_Right_XthParm_MoveToNext   
    d: Transition functions for going from second to third... etc. 
       - Sweep_Right_XthParm_MoveToNext   
    e: Transition functions for going from the last head to a determined state. 
       - Sweep_Right_Complete_BeginActionStates   
2: Create Transition Functions for sweeping left - determined states
    a: Transition functions for going left until the last simulated head. 
       - Sweep_Right_Complete_BeginActionStates
    b: Transition functions for transtioning to states that perform changes on the simulated heads.
        i: Transition functions for performing the change on the simulated tape. 
           CAVEAT: For each write operation that can possible write on a blank space "_"
                    include a transition function that can also write on "#". This is done
                    in ii.
           - Sweep_Left_ActorState_N_FoundPreviousWriteThenChangeToMoveHeadState
       ii: Transition functions for extending the simulated tape if a write was
             performed on a blank space that was actually a tape end "#". See the
             function for extending the tape.
      iii: Transition functions for moving the simulated head and returning to the sweep left.
           - Sweep_Left_ActorState_N_MoveHeadThenChangeToNLess1MoveToPrevious
    c: Transition functions for moving the the previous (N less 1) simulated head.
           - Sweep_Left_ActorState_N_MoveToPrevious
    d: Transition functions for meving from the first simulated head to a completed state.
           - Sweep_Left_Complete_ActorState_0F_ChangeToNext
3: Create Transition functions for transition from determined states to undetermined states
    that then perform another sweep right OR check for completion.
           - Sweep_Left_Complete_ActorState_0F_ChangeToNext


