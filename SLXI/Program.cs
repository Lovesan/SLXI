using System;
using System.Numerics;

namespace SLXI
{
    class Program
    {
        static void Main(string[] args)
        {
            Pause();
        }

        static void Pause()
        {
            if (Console.IsOutputRedirected) return;
            if (Console.CursorLeft != 0)
                Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadLine();
        }

        static void WriteError(string message)
        {
            var color = Console.ForegroundColor;
            var redirected = Console.IsErrorRedirected;
            if (!redirected)
            {
                if(Console.CursorLeft != 0)
                    Console.Error.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.Error.WriteLine(message);
            if (!redirected)
            {
                Console.ForegroundColor = color;
            }
        }
    }
}
