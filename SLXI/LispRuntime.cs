using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SLXI.Exceptions;

namespace SLXI
{
    public class LispRuntime
    {
        private static LispRuntime _instance;

        public static LispRuntime Current
        {
            get { return _instance ?? (_instance = new LispRuntime()); }
        }

        private readonly ConcurrentDictionary<LispSymbol, LispVar> _vars;
        private readonly ThreadLocal<Dictionary<LispSymbol, LispObject>> _bindings;

        private LispRuntime()
        {
            _vars = new ConcurrentDictionary<LispSymbol, LispVar>();
            _bindings = new ThreadLocal<Dictionary<LispSymbol, LispObject>>(() => new Dictionary<LispSymbol, LispObject>());
        }

        public LispVar GetVar(LispSymbol name)
        {
            LispVar var;
            _vars.TryGetValue(name, out var);
            return var;
        }

        public IDisposable Bind(LispSymbol name, LispObject value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            var bindings = _bindings.Value;
            LispObject oldValue;
            var oldValueExisted = _bindings.Value.TryGetValue(name, out oldValue);
            bindings[name] = value;
            return new DelegateDisposable(() =>
                {
                    if (oldValueExisted)
                        bindings[name] = oldValue;
                    else
                        bindings.Remove(name);
                });
        }

        public LispObject GetValue(LispSymbol name)
        {
            if(name == null)
                throw new ArgumentNullException("name");
            LispObject value;
            if(_bindings.Value.TryGetValue(name, out value))
                return value;
            value = name.GlobalValue;
            if (value == null)
                throw new LispUnboundVariableException(name);
            return value;
        }
        
        public void SetValue(LispSymbol name, LispObject value)
        {
            if(name == null)
                throw new ArgumentNullException("name");
            LispObject oldValue;
            if (_bindings.Value.TryGetValue(name, out oldValue))
            {
                _bindings.Value[name] = value;
                return;
            }
            var var = GetVar(name);
            if (var == null)
            {
                name.GlobalValue = value;
                return;
            }
            if(var.Kind == LispVarKind.Constant)
                throw new LispConstantModificationException(name);
            name.GlobalValue = value;
        }
    }
}
