Transforms a k-tape Turing Machine into a 1-tape machine. Utilizes special symbols and notation
understood by the TuringMachineSimulator project.

Expects to run in a directory with a file named "Input.txt". This file should conform
to the following style

name: <string_name>
init: <state>
accept: <state>
   

// Then one or more of the following Transition Function Form.
<Domain_State>, [<Domain_Input>, ...]
<Range_State>, [<Range_Output>, ...], [<Head_Moves>, ...]

The output will be placed in a file "Output.txt"