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

Algorithm Overview from code comments. See code comments (flatener.cs) for full details.

Generally, it takes each tape, puts them on a single tape separated by #s.
The head then sweeps right and determines what symbol each "Simulated Head" 
lies on. Once each Simulated Head is determined, the head then sweeps left
again to perform the changes to each simulated head.

