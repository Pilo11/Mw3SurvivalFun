using AsmJitter.Model;
using AsmJitter.Model.Instruction;
using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static AsmJitter.AsmInterface;

namespace FunExecuter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                GameManager.Init();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine(ex);
            }
            
            Console.ReadLine();
        }
        
    }
}
