using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SLXI
{
    public class LispSymbol : LispObject
    {
        public static readonly LispSymbol T;
        public static readonly LispSymbol Nil;
        public static readonly LispSymbol Lambda;
        public static readonly LispSymbol Let;
        public static readonly LispSymbol LetSeq;
        public static readonly LispSymbol LetDyn;
        public static readonly LispSymbol LetRec;
        public static readonly LispSymbol Bind;
        public static readonly LispSymbol Body;
        public static readonly LispSymbol Tagbody;
        public static readonly LispSymbol Go;
        public static readonly LispSymbol Throw;
        public static readonly LispSymbol Catch;
        public static readonly LispSymbol Block;
        public static readonly LispSymbol ReturnFrom;
        public static readonly LispSymbol UnwindProtect;
        public static readonly LispSymbol If;
        public static readonly LispSymbol ToplevelExpansionOnly;
        public static readonly LispSymbol ToplevelExpansionToo;
        public static readonly LispSymbol Set;
        public static readonly LispSymbol Quote;
        public static readonly LispSymbol Backquote;
        public static readonly LispSymbol Unquote;
        public static readonly LispSymbol UnquoteList;
        public static readonly LispSymbol The;
        public static readonly LispSymbol Declare;
        public static readonly LispSymbol Key;
        public static readonly LispSymbol Rest;
        public static readonly LispSymbol Slxi;
        public static readonly LispSymbol SlxiCurrentForm;
        public static readonly LispSymbol SlxiCurrentExceptionFactory;
        public static readonly LispSymbol SlxiCurrentLexenv;

        static LispSymbol()
        {
            T = Create("t");
            Nil = Create("nil");
            T._children.AddOrUpdate("nil", str => Nil, (str, old) => Nil);
            Nil._children.AddOrUpdate("t", str => T, (str, old) => T);
            T.Parent = Nil;
            Nil.Parent = T;
            Lambda = T.Intern("lambda");
            Let = T.Intern("let");
            LetSeq = T.Intern("let*");
            LetDyn = T.Intern("letd");
            LetRec = T.Intern("letr");
            Bind = T.Intern("bind");
            Body = T.Intern("body");
            Tagbody = T.Intern("tagbody");
            Go = T.Intern("go");
            Throw = T.Intern("throw");
            Catch = T.Intern("catch");
            Block = T.Intern("block");
            ReturnFrom = T.Intern("return-from");
            UnwindProtect = T.Intern("unwind-protect");
            If = T.Intern("if");
            ToplevelExpansionOnly = T.Intern("toplevel-expansion-only");
            ToplevelExpansionToo = T.Intern("toplevel-expansion-too");
            Set = T.Intern("set");
            Quote = T.Intern("quote");
            Backquote = T.Intern("backquote");
            Unquote = T.Intern("unquote");
            UnquoteList = T.Intern("unquote-list");
            The = T.Intern("the");
            Declare = T.Intern("declare");
            Key = T.Intern("&key");
            Rest = T.Intern("&rest");
            Slxi = T.Intern("slxi");
            SlxiCurrentForm = Slxi.Intern("*current-form*");
            SlxiCurrentExceptionFactory = Slxi.Intern("*current-exception-factory*");
            SlxiCurrentLexenv = Slxi.Intern("*current-lexenv*");
        }

        private readonly ConcurrentDictionary<string, LispSymbol> _children;

        private LispSymbol(LispString name, LispSymbol parent = null)
        {
            _children = new ConcurrentDictionary<string, LispSymbol>();
            Name = name;
            Parent = parent;
        }

        public LispString Name { get; private set; }

        public LispSymbol Parent { get; private set; }

        public LispObject GlobalValue { get; set; }

        public IEnumerable<LispSymbol> Children
        {
            get { return _children.Values; }
        }

        public LispSymbol this[LispString name]
        {
            get
            {
                LispSymbol child;
                _children.TryGetValue(name.StringValue, out child);
                return child;
            }
        }

        public LispSymbol this[IEnumerable<char> name]
        {
            get
            {
                LispSymbol child;
                _children.TryGetValue(new string(name.ToArray()), out child);
                return child;
            }
        }

        public LispSymbol Intern(LispString name)
        {
            return _children.GetOrAdd(name.StringValue, str =>
                {
                    var symbol = Create(name);
                    symbol.Parent = this;
                    return symbol;
                });
        }

        public LispSymbol Intern(IEnumerable<char> name)
        {
            return Intern(LispString.Create(name));
        }

        public void Unintern()
        {
            if (Parent == null)
                return;
            LispSymbol old;
            Parent._children.TryRemove(Name.StringValue, out old);
            Parent = null;
        }

        public static LispSymbol Create(LispString name)
        {
            return new LispSymbol(name);
        }

        public static LispSymbol Create(IEnumerable<char> name)
        {
            return new LispSymbol(LispString.Create(name));
        }
    }
}
