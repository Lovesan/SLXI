namespace SLXI.Compiler
{
    public abstract class LispDeclaration
    {
        public virtual bool Validate(LispLexenv env)
        {
            return true;
        }
    }
}
