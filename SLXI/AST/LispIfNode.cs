using System;

namespace SLXI.AST
{
    public class LispIfNode : LispAstNode
    {
        private LispIfNode(LispAstNode cond, LispAstNode trueBranch, LispAstNode falseBranch)
        {
            Cond = cond;
            TrueBranch = trueBranch;
            FalseBranch = falseBranch;
        }

        public LispAstNode Cond { get; private set; }

        public LispAstNode TrueBranch { get; private set; }

        public LispAstNode FalseBranch { get; private set; }

        public static LispIfNode Create(LispAstNode cond, LispAstNode trueBranch, LispAstNode falseBranch = null)
        {
            if(cond == null)
                throw new ArgumentNullException("cond");
            if(trueBranch == null)
                throw new ArgumentNullException("trueBranch");
            if (falseBranch == null)
                falseBranch = LispConstantNode.Create(LispSymbol.Nil);
            return new LispIfNode(cond, trueBranch, falseBranch);
        }
    }
}
