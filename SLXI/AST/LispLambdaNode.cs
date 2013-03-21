using System;
using System.Collections.Generic;

namespace SLXI.AST
{
    public class LispLambdaNode : LispAstNode
    {
        private LispLambdaNode(LispSymbol name, IEnumerable<LispVar> requiredArgs, IDictionary<LispSymbol, LispVar> keywordArgs, LispVar restArg, LispBodyNode body)
        {
            Name = name;
            RequiredArgs = requiredArgs;
            KeywordArgs = keywordArgs;
            RestArg = restArg;
            Body = body;
        }

        public LispSymbol Name { get; private set; }

        public IEnumerable<LispVar> RequiredArgs { get; private set; }

        public IDictionary<LispSymbol, LispVar> KeywordArgs { get; private set; }

        public LispVar RestArg { get; private set; }

        public LispBodyNode Body { get; private set; }

        public static LispLambdaNode Create(LispSymbol name, IEnumerable<LispVar> requiredArgs, IDictionary<LispSymbol, LispVar> keywordArgs, LispVar restArg, LispBodyNode body)
        {
            if(requiredArgs == null)
                throw new ArgumentNullException("requiredArgs");
            return new LispLambdaNode(name, requiredArgs, keywordArgs, restArg, body);
        }
    }
}
