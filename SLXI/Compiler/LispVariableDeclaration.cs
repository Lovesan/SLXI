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

        public override void Apply(LispLexenv env)
        {
            if(Kind != LispVarKind.Dynamic && !env.Bindings.Contains(Name))
                throw new LispUndefinedVariableDeclarationException(Name);
            var varDecls = env.Decls.OfType<LispVariableDeclaration>();
            foreach (var decl in varDecls.Where(decl => decl.Name.Eq(Name)))
            {
                if(decl.Kind != Kind)
                    throw new LispVariableRedeclarationException(Name);
                return;
            }
            env.Defvar(Name, Kind);
        }
    }
}
