using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TuringMachineApp
{
    class Program
    {
        static void Main(string[] args)
        {
            TuringMachine tm = new TuringMachine();
            tm.GetInput("Input.txt");

            string doc = "";
            Flattener fl = new Flattener(tm);
            fl.Flatten(ref doc);

            tm.FinalizeOutput(ref doc);

            File.WriteAllText("Output.txt", doc);
            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}

