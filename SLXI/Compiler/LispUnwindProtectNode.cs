using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLXI.Compiler
{
    public class LispUnwindProtectNode : LispAstNode
    {
        public LispAstNode ProtectedForm { get; private set; }

        public IEnumerable<LispAstNode> CleanupForms { get; private set; }

        public LispUnwindProtectNode(LispLexenv env, LispAstNode protectedForm, IEnumerable<LispAstNode> cleanupForms)
            : base(env)
        {
            ProtectedForm = protectedForm;
            CleanupForms = cleanupForms;
        }
    }
}
