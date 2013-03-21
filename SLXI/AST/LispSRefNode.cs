using System;

namespace SLXI.AST
{
    public class LispSRefNode : LispRefNode
    {
        private LispSRefNode(LispVar var)
            : base(var.Name)
        {
            Var = var;
        }

        public LispVar Var { get; private set; }

        public static LispSRefNode Create(LispVar var)
        {
            if(var.Kind != LispVarKind.Static)
                throw new ArgumentException("LispSRefNode requires static variable", "var");
            return new LispSRefNode(var);
        }
    }
}
