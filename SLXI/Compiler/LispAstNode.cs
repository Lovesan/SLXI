using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLXI.Compiler
{
    public abstract class LispAstNode 
    {
        public LispLexenv Env { get; private set; }

        protected LispAstNode(LispLexenv env)
        {
            if(env == null)
                throw new ArgumentNullException("env");
            Env = env;
        }
    }
}
