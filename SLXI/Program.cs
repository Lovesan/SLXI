using System;

namespace SLXI
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var sym = LispObject.T.SymbolIntern(LispObject.CreateString("Hello"));
                sym.SymbolValue = LispObject.CreateFixnum(0);
                using (sym.SymbolBind(LispObject.CreateFixnum(123)))
                {
                    Console.WriteLine(sym.SymbolValue.IntegerValue);
                    sym.SymbolMakunbound();
                    Console.WriteLine(sym.IsSymbolBound);
                    Console.WriteLine(LispObject.Nil.SymbolName.Equals(LispObject.CreateString("nil")));
                }
                Console.WriteLine(sym.SymbolValue.FixnumValue);
                foreach (var s in LispObject.T.SymbolChildren.AsEnumerable())
                {
                    Console.WriteLine(s.SymbolName.StringValue);
                }
            }
            catch (Exception e)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e);
                Console.ForegroundColor = color;
            }
            Pause();
        }

        static void Pause()
        {
            Console.WriteLine("Press any key to continue");
            Console.ReadLine();
        }

        public static void CallWithEscapeContinuation(Action<Action> f)
        {
            var escapeTag = new Exception();
            Action escapeProcedure = () => { throw escapeTag; };
            try
            {
                f(escapeProcedure);
            }
            catch (Exception e)
            {
                if (ReferenceEquals(escapeTag, e)) return;
                throw;
            }
        }
    }
}
