namespace SLXI.Compiler
{
    public abstract class LispDeclaration
    {
        public virtual void Apply(LispLexenv env)
        { }
    }
}
