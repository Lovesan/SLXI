using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SLXI.Exceptions;
using SLXI.AST;

namespace SLXI
{
    class Program
    {
        static void Main(string[] args)
        {
            var ast = LispSymbol.Lambda.ConvertToAst();
            Console.Read();
        }
    }
}
