using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TuringMachineApp
{
    // To DO
    // NullReplaceSymbol
    // Input Setup
    class Program
    {
        static void Main(string[] args)
        {
            TuringMachine tm = new TuringMachine();
            tm.GetInput("Input2.txt");

            Flattener fl = new Flattener(tm);

            string doc = "";
            tm.NullInput(ref doc, "~");
            fl.Flatten(ref doc);
            tm.FinalizeOutput(ref doc);
            File.WriteAllText("Output.txt", doc);
            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}

