namespace SLXI.AST
{
    public class LispGRefNode : LispRefNode
    {
        private LispGRefNode(LispVar var)
            : base(var.Name)
        {
            Var = var;
        }

        public LispVar Var { get; private set; }

        public static LispGRefNode Create(LispVar var)
        {
            return new LispGRefNode(var);
        }
    }
}
