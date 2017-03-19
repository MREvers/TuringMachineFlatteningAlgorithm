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

Algorithm Overview.

Takes a k-tape turing machine and simulates all tapes on a single tape.
The machine sweeps right across the single tape machine to determine the state
that each simulated tape is in. Once that is done, it sweeps left performing
the changes on each of the simulated tapes.