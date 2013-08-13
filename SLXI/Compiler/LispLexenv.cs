using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SLXI.Compiler
{
    public class LispLexenv
    {
        private readonly Stack<LispVar> _vars;
        private readonly Dictionary<LispObject, LispObject> _constants;
        private readonly Dictionary<LispObject, LispObject> _macros;
        private readonly Dictionary<LispObject, LispAstNode> _lvars;
        private readonly List<LispDeclaration> _decls;
        private readonly Dictionary<LispObject, LispVariableDeclaration> _varDecls;

        private LispLexenv(
            IEnumerable<LispVar> vars = null,
            IEnumerable<KeyValuePair<LispObject, LispAstNode>> lvars = null,
            IEnumerable<KeyValuePair<LispObject, LispObject>> constants = null,
            IEnumerable<KeyValuePair<LispObject, LispObject>> macros = null,
            IEnumerable<LispDeclaration> decls = null)
        {
            _vars = new Stack<LispVar>(vars ?? Enumerable.Empty<LispVar>());
            _lvars = new Dictionary<LispObject, LispAstNode>();
            if (lvars != null)
                foreach (var v in lvars)
                    _lvars[v.Key] = v.Value;
            _constants = new Dictionary<LispObject, LispObject>();
            if (constants != null)
                foreach (var c in constants)
                    _constants[c.Key] = c.Value;
            _macros = new Dictionary<LispObject, LispObject>();
            if (macros != null)
                foreach (var m in macros)
                    _macros[m.Key] = m.Value;
            _decls = new List<LispDeclaration>(decls ?? Enumerable.Empty<LispDeclaration>());
            _varDecls = _decls.OfType<LispVariableDeclaration>().ToDictionary(d => d.Name, d => d);
        }

        public static LispLexenv NullLexenv
        {
            get { return new LispLexenv(); }
        }

        public IEnumerable<LispDeclaration> Decls
        {
            get { return _decls; }
        }

        public LispLexenv CreateChild(IEnumerable<LispDeclaration> decls)
        {
            return new LispLexenv(_vars.Reverse(), _lvars, _constants, _macros, decls);
        }

        public LispVar Defvar(LispObject name, object value)
        {
            name.CheckSymbol();
            LispVariableDeclaration d;
            var kind = _varDecls.TryGetValue(name, out d) ? d.Kind : LispVarKind.Static;
            var v = new LispVar(name, kind);
            switch (v.Kind)
            {
                case LispVarKind.Constant:
                    _constants[name] = (LispObject)value;
                    break;
                case LispVarKind.Macro:
                    _macros[name] = (LispObject)value;
                    break;
                default:
                    _lvars[name] = value as LispAstNode;
                    break;
            }
            _vars.Push(v);
            return v;
        }
        
        public LispVar GetVar(LispObject name, out object value)
        {
            name.CheckSymbol();
            var var = _vars.FirstOrDefault(v => v.Name.Eq(name));
            value = LispObject.Nil;
            if (var != null)
            {
                switch (var.Kind)
                {
                    case LispVarKind.Constant:
                        value = _constants[name];
                        break;
                    case LispVarKind.Macro:
                        value = _macros[name];
                        break;
                    default:
                        value = _lvars[name];
                        break;
                }
            }
            return var;
        }

        public bool IsDeclaredStatic(LispObject name)
        {
            return IsDeclaredAs(name, LispVarKind.Static);
        }

        public bool IsDeclaredDynamic(LispObject name)
        {
            return IsDeclaredAs(name, LispVarKind.Dynamic);
        }

        public bool IsDeclaredConstant(LispObject name)
        {
            return IsDeclaredAs(name, LispVarKind.Constant);
        }

        public bool IsDeclaredMacro(LispObject name)
        {
            return IsDeclaredAs(name, LispVarKind.Macro);
        }

        private bool IsDeclaredAs(LispObject name, LispVarKind kind)
        {
            name.CheckSymbol();
            LispVariableDeclaration d;
            if (!_varDecls.TryGetValue(name, out d)) return false;
            return d.Kind == kind;
        }

        public bool Validate()
        {
            return _decls.All(decl => decl.Validate(this));
        }
    }
}
