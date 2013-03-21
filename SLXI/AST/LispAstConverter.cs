using System;
using System.Linq;
using System.Collections.Generic;
using SLXI.Exceptions;

namespace SLXI.AST
{
    public static class LispAstConverter
    {
        public static LispAstNode ConvertToAst(this LispObject obj, LispLexenv env)
        {
            using (BindCurrentExceptionFactory())
            using (BindCurrentLexenv(env))
            {
                return obj.Convert();
            }
        }

        public static LispAstNode ConvertToAst(this LispObject obj)
        {
            return obj.ConvertToAst(LispLexenv.Null);
        }

        private static LispObject CurrentForm
        {
            get
            {
                return LispRuntime.Current.GetValue(LispSymbol.SlxiCurrentForm)
                    .AsSymbol(() => new LispCompilerException("Internal compiler error"));
            }
        }
        
        private static LispLexenv CurrentLexenv
        {
            get
            {
                return LispRuntime.Current.GetValue(LispSymbol.SlxiCurrentLexenv)
                                  .As<LispLexenv>(() => new LispCompilerException("Internal compiler error"));
            }
        }

        private static IDisposable BindCurrentForm(LispObject form)
        {
            return LispRuntime.Current.Bind(LispSymbol.SlxiCurrentForm, form);
        }

        private static IDisposable BindCurrentExceptionFactory(string message = "Syntax error")
        {
            return LispRuntime.Current.Bind(
                LispSymbol.SlxiCurrentExceptionFactory,
                LispInternalObject<Func<LispException>>.Create(() => new LispSyntaxException(CurrentForm, message)));
        }

        private static IDisposable BindCurrentLexenv(LispLexenv env)
        {
            return LispRuntime.Current.Bind(LispSymbol.SlxiCurrentLexenv, env);
        }

        private static LispAstNode Convert(this LispObject form)
        {
            using (BindCurrentForm(form))
            {
                if (form == null)
                    throw new LispRuntimeException("Invalid object passed to ast transformer");
                var cons = form as LispCons;
                if (cons != null)
                    return cons.Convert();
                var symbol = form as LispSymbol;
                return symbol != null ? symbol.ConvertSymbol() : form.ConvertConstant();
            }
        }

        private static LispAstNode Convert(this LispCons form)
        {
            var firstSymbol = form.Car as LispSymbol;
            if (firstSymbol == null)
                return form.Car.ConvertFuncall(form.Cdr);

            if (firstSymbol == LispSymbol.Body)
                return form.Cdr.ConvertBody();
            if (firstSymbol == LispSymbol.If)
                return form.Cdr.ConvertIf();
            if (firstSymbol == LispSymbol.Lambda)
                return form.Cdr.ConvertLambda();

            LispObject value;
            var var = CurrentLexenv.GetVar(firstSymbol, out value);
            if (var != null && var.Kind == LispVarKind.Macro)
                return form.ConvertMacroexpand(value);
            var = LispRuntime.Current.GetVar(firstSymbol);
            if (var != null && var.Kind == LispVarKind.Macro)
                return form.ConvertMacroexpand(LispRuntime.Current.GetValue(firstSymbol));
            return firstSymbol.ConvertFuncall(form.Cdr);
        }

        private static LispAstNode ConvertBody(this LispObject form)
        {
            return LispBodyNode.Create(form.AsList(-1).Select(Convert));
        }

        private static LispAstNode ConvertIf(this LispObject form)
        {
            var cond = form.First();
            form = form.Rest<LispCons>();
            var ifTrue = form.First();
            form = form.Rest();
            var ifFalse = form.First();
            form.Rest().AsNil();
            return LispIfNode.Create(Convert(cond), Convert(ifTrue), Convert(ifFalse));
        }

        private static LispAstNode ConvertLambda(this LispObject form)
        {
            var name = form.First() as LispSymbol;
            if (name != null)
                form = form.Rest<LispCons>();
            else
                name = LispSymbol.Nil;
            LispObject arglist;
            using (BindCurrentExceptionFactory("Lambda list expected"))
            {
                arglist = form.First().AsList();
            }
            var body = form.Rest().AsList();
            throw new NotImplementedException();
        }

        private static void ParseLambdaBody(this LispObject form, out IEnumerable<LispAstNode> bodyNodes)
        {
            throw new NotImplementedException();
        }

        private static void ParseLambdaArgs(
            this LispObject form,
            out IEnumerable<LispSymbol> reqArgs,
            out IDictionary<LispSymbol, Tuple<LispSymbol, LispObject, LispSymbol>> keyArgs,
            out bool allowOtherKeys,
            out LispSymbol restArg,
            out IDictionary<LispSymbol, LispObject> auxArgs)
        {
            throw new NotImplementedException();
        }

        private static LispAstNode ConvertMacroexpand(this LispCons whole, LispObject expander)
        {
            throw new NotImplementedException("Macros not implemented");
        }

        private static LispAstNode ConvertFuncall(this LispObject functionForm, LispObject arglist)
        {
            LispObject restArg;
            return LispFuncallNode.Create(
                functionForm.Convert(), 
                arglist.AsDotted(-1, out restArg).Select(Convert),
                restArg.Convert());
        }

        private static LispAstNode ConvertSymbol(this LispSymbol symbol)
        {
            LispObject value;
            var var = CurrentLexenv.GetVar(symbol, out value);
            if (var != null)
                return var.ConvertLRef(value);
            var = LispRuntime.Current.GetVar(symbol);
            if (var != null)
                return var.ConvertGRef();
            return LispURefNode.Create(symbol);
        }

        private static LispAstNode ConvertLRef(this LispVar var, LispObject value)
        {
            switch (var.Kind)
            {
                case LispVarKind.Static:
                    return LispSRefNode.Create(var);
                case LispVarKind.Dynamic:
                    return LispDRefNode.Create(var.Name);
                case LispVarKind.Constant:
                    return LispConstantNode.Create(value);
                case LispVarKind.Macro:
                    return Convert(value);
                default:
                    throw new LispCompilerException("Invalid variable type");
            }
        }

        private static LispAstNode ConvertGRef(this LispVar var)
        {
            switch (var.Kind)
            {
                case LispVarKind.Static:
                    return LispGRefNode.Create(var);
                case LispVarKind.Dynamic:
                    return LispDRefNode.Create(var.Name);
                case LispVarKind.Constant:
                    return LispConstantNode.Create(LispRuntime.Current.GetValue(var.Name));
                case LispVarKind.Macro:
                    return Convert(LispRuntime.Current.GetValue(var.Name));
                default:
                    throw new LispCompilerException("Invalid variable type");
            }
        }

        private static LispAstNode ConvertConstant(this LispObject constant)
        {
            return LispConstantNode.Create(constant);
        }
    }
}
