using System;
using System.Collections.Generic;
using System.Linq;

namespace SLXI.Compiler
{
    public class LispLexenv
    {
        private readonly Stack<LispVar> _vars;
        private readonly List<LispDeclaration> _decls;
        private readonly HashSet<LispObject> _bindings; 

        private LispLexenv(IEnumerable<LispVar> vars = null, IEnumerable<LispObject> bindings = null)
        {
            _vars = new Stack<LispVar>(vars ?? Enumerable.Empty<LispVar>());
            _decls = new List<LispDeclaration>();
            _bindings = new HashSet<LispObject>(bindings ?? Enumerable.Empty<LispObject>());
        }

        public static LispLexenv NullLexenv
        {
            get { return new LispLexenv(); }
        }

        public IEnumerable<LispDeclaration> Decls
        {
            get { return _decls; }
        }

        public ISet<LispObject> Bindings
        {
            get { return _bindings; }
        }

        public LispLexenv CreateChild(IEnumerable<LispObject> bindings)
        {
            return new LispLexenv(_vars.Reverse(), bindings);
        }

        public LispVar Defvar(LispObject name, LispVarKind kind)
        {
            name.CheckSymbol();
            var v = new LispVar(name, kind);
            _vars.Push(v);
            return v;
        }

        public void Declare(LispDeclaration decl)
        {
            if(decl == null)
                throw new ArgumentNullException("decl");
            _decls.Add(decl);
        }

        public void ApplyDeclarations()
        {
            foreach (var decl in _decls)
            {
                decl.Apply(this);
            }
        }
    }
}
