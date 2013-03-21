using System;

namespace SLXI
{
    public class LispVar : LispObject
    {
        private LispVar(LispSymbol name, LispVarKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public LispSymbol Name { get; private set; }

        public LispVarKind Kind { get; private set; }

        public static LispVar Create(LispSymbol name, LispVarKind kind)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            return new LispVar(name, kind);
        }
    }
}
