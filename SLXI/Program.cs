using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SLXI.Exceptions;
using SLXI.AST;
using Symbol = SLXI.LispSymbol;
using Cons = SLXI.LispCons;

namespace SLXI
{
    class Program
    {
        static void Main(string[] cmdline)
        {
            using (
                LispRuntime.Current.Bind(Symbol.SlxiCurrentExceptionFactory, LispInternalObject<Func<LispException>>.Create(() => new LispException("Unexpected error"))))
            {
                IEnumerable<LispSymbol> req;
                IDictionary<LispSymbol, Tuple<LispSymbol, LispObject, LispSymbol>> key;
                LispSymbol rest;
                IDictionary<LispSymbol, LispObject> aux;
                bool hasKeys, allowOtherKeys;
                var args = List(
                    Intern("req-arg1"),
                    Intern("req-arg2"),
                    Symbol.Rest,
                    Intern("rest-arg"),
                    Symbol.Key,
                    List(List(Symbol.Nil.Intern("keyword"), Intern("key-name")), Intern("key-value"), Intern("key-predicate")),
                    Intern("another-key"),
                    Symbol.Aux,
                    List(Intern("aux-arg"), Intern("aux-value")),
                    Intern("another-aux"));
                args.ParseLambdaArgs(out req, out key, out hasKeys, out allowOtherKeys, out rest, out aux);
                foreach (var arg in req)
                {
                    Console.Write(arg.Name.StringValue + " ");
                }
                if (rest != null)
                {
                    Console.Write("&rest " + rest.Name.StringValue + " ");
                }
                if (key.Count > 0)
                {
                    Console.Write("&key ");
                    foreach (var kv in key)
                    {
                        Console.Write("((");
                        Console.Write(kv.Value.Item1.Name.StringValue + " ");
                        Console.Write(kv.Key.Name.StringValue);
                        Console.Write(") ");
                        Console.Write(((LispSymbol)kv.Value.Item2).Name.StringValue);
                        if (kv.Value.Item3 != null)
                        {
                            Console.Write(" ");
                            Console.Write(kv.Value.Item3.Name.StringValue);
                        }
                        Console.Write(") ");
                    }
                }
                if (aux.Count > 0)
                {
                    Console.Write("&aux ");
                    foreach (var kv in aux)
                    {
                        Console.Write("(" + kv.Key.Name.StringValue + " ");
                        Console.Write(((LispSymbol)kv.Value).Name.StringValue + ")");
                    }
                }
                Console.Read();
            }
        }

        private static LispCons Cons(LispObject car, LispObject cdr)
        {
            return LispCons.Create(car, cdr);
        }

        private static LispObject List(params LispObject[] args)
        {
            return args.List();
        }

        private static LispSymbol Intern(string name)
        {
            return Symbol.T.Intern(name);
        }
    }
}
