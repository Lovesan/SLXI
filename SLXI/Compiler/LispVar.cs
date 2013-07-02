namespace SLXI.Compiler
{
    public class LispVar
    {
        public LispObject Name { get; private set; }

        public LispVarKind Kind { get; private set; }
        
        public LispVar(LispObject name, LispVarKind kind)
        {
            name.CheckSymbol();
            Name = name;
            Kind = kind;
        }
    }
}
