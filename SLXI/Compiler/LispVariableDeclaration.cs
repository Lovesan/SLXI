using System.Linq;

namespace SLXI.Compiler
{
    public abstract class LispVariableDeclaration : LispDeclaration
    {
        public LispObject Name { get; private set; }

        public LispVarKind Kind { get; private set; }

        public LispVariableDeclaration(LispObject name, LispVarKind kind)
        {
            name.CheckSymbol();
            Name = name;
            Kind = kind;
        }

        public override bool Validate(LispLexenv env)
        {
            return true;
        }
    }
}
