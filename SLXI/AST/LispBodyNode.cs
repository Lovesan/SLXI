using System;
using System.Collections.Generic;

namespace SLXI.AST
{
    public class LispBodyNode : LispAstNode
    {
        private LispBodyNode(IEnumerable<LispAstNode> forms)
        {
            Forms = forms;
        }

        public IEnumerable<LispAstNode> Forms { get; private set; }

        public static LispBodyNode Create(IEnumerable<LispAstNode> forms)
        {
            if(forms == null)
                throw new ArgumentNullException("forms");
            return new LispBodyNode(forms);
        }
    }
}
