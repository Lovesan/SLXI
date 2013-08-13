using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLXI.Compiler
{
    public class LispBodyNode : LispAstNode
    {
        public IEnumerable<LispAstNode> Forms { get; private set; }

        public LispBodyNode(LispLexenv env, IEnumerable<LispAstNode> forms)
            : base(env)
        {
            Forms = forms;
        }
    }
}
