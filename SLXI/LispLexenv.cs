using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLXI
{
    public class LispLexenv : LispObject
    {
        private readonly ConcurrentDictionary<LispSymbol, LispVar> _vars;
        private readonly ConcurrentDictionary<LispSymbol, LispObject> _constants;
        private readonly ConcurrentDictionary<LispSymbol, LispObject> _macros;

        private LispLexenv(LispLexenv parent = null)
        {
            _vars = new ConcurrentDictionary<LispSymbol, LispVar>();
            _constants = new ConcurrentDictionary<LispSymbol, LispObject>();
            _macros = new ConcurrentDictionary<LispSymbol, LispObject>();
            Parent = parent;
        }

        public LispLexenv Parent { get; private set; }

        public static LispLexenv Null
        {
            get { return new LispLexenv(); }
        }

        public LispVar GetVar(LispSymbol name, out LispObject value)
        {
            value = null;
            var env = this;
            while (env != null)
            {
                LispVar var;
                if (env._vars.TryGetValue(name, out var))
                {
                    switch (var.Kind)
                    {
                        case LispVarKind.Constant:
                            value = _constants[name];
                            break;
                        case LispVarKind.Macro:
                            value = _macros[name];
                            break;
                    }
                    return var;
                }
                env = env.Parent;
            }
            return null;
        }

        public LispLexenv Create()
        {
            return new LispLexenv(this);
        }
    }
}
