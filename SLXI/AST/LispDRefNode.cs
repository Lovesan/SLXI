namespace SLXI.AST
{
    public class LispDRefNode : LispRefNode
    {
        private LispDRefNode(LispSymbol name)
            : base(name)
        {
        }

        public static LispDRefNode Create(LispSymbol name)
        {
            return new LispDRefNode(name);
        }
    }
}
