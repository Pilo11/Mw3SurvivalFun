using System;

namespace FunExecuter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                GscIwdMode.Run(args);
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
