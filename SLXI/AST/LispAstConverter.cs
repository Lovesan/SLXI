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

        private static Func<LispSyntaxException> SyntaxException(string message)
        {
            return () => new LispSyntaxException(CurrentForm, message);
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

            if (firstSymbol.Eq(LispSymbol.Body))
                return form.Cdr.ConvertBody();
            if (firstSymbol.Eq(LispSymbol.If))
                return form.Cdr.ConvertIf();
            if (firstSymbol.Eq(LispSymbol.Lambda))
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

        public static void ParseLambdaArgs(
            this LispObject form,
            out IEnumerable<LispSymbol> reqArgs,
            out IDictionary<LispSymbol, Tuple<LispSymbol, LispObject, LispSymbol>> keyArgs,
            out bool hasKeys,
            out bool allowOtherKeys,
            out LispSymbol restArg,
            out IDictionary<LispSymbol, LispObject> auxArgs)
        {
            LispObject tmp;
            var req = new List<LispSymbol>();
            var key = new Dictionary<LispSymbol, Tuple<LispSymbol, LispObject, LispSymbol>>();
            var aux = new Dictionary<LispSymbol, LispObject>();
            restArg = null;
            hasKeys = false;
            allowOtherKeys = false;
        required:
            if (form.IsEnd(SyntaxException("Dotted lambda list are not allowed")))
                goto end;
            var arg = form.First<LispSymbol>(SyntaxException("Argument name expected"));
            form = form.Rest().AsList(SyntaxException("Dotted lambda list are not allowed"));
            if (arg.Eq(LispSymbol.Rest))
                goto rest;
            if (arg.Eq(LispSymbol.Key))
                goto key;
            if (arg.Eq(LispSymbol.Aux))
                goto aux;
            if (arg.Eq(LispSymbol.AllowOtherKeys))
                throw new LispSyntaxException(CurrentForm, "Misplaced &allow-other-keys");
            req.Add(arg);
            goto required;
        rest:
            if (form.IsEnd(SyntaxException("Dotted lambda list are not allowed")))
                throw new LispSyntaxException(CurrentForm, "Argument name expected");
            restArg = form.First<LispSymbol>(SyntaxException("Argument name must be a symbol"));
            form = form.Rest().AsList(SyntaxException("Dotted lambda list are not allowed"));
            if (form.IsEnd())
                goto end;
            arg = form.First<LispSymbol>();
            if (arg.Eq(LispSymbol.Key))
                goto key;
            if (arg.Eq(LispSymbol.Aux))
                goto aux;
            if (arg.Eq(LispSymbol.AllowOtherKeys))
                throw new LispSyntaxException(CurrentForm, "Misplaced &allow-other-keys");
            throw new LispSyntaxException(CurrentForm, "Invalid syntax");
        key:
            hasKeys = true;
            if (form.IsEnd(SyntaxException("Dotted lambda list are not allowed")))
                goto end;
            tmp = form.First();
            form = form.Rest().AsList(SyntaxException("Dotted lambda list are not allowed"));
            if (tmp.Eq(LispSymbol.Aux))
                goto aux;
            if (tmp.Eq(LispSymbol.AllowOtherKeys))
                goto allowOtherKeys;
            if (tmp.IsSymbol())
            {
                arg = (LispSymbol)tmp;
                if (req.Any(a => a.Eq(arg)) || key.Any(a => a.Key.Eq(arg)) || restArg.Eq(arg))
                    throw new LispSyntaxException(CurrentForm, "Duplicate argument name: " + arg.Name.StringValue);
                key.Add(arg, Tuple.Create<LispSymbol, LispObject, LispSymbol>(LispSymbol.Nil.Intern(arg.Name), LispSymbol.Nil, null));
            }
            else
            {
                var keyForm = tmp.AsList(-1);
                LispObject keyName;
                LispObject keyInitform;
                LispSymbol keyExistsArg;
                LispSymbol keyKeyword;
                switch (keyForm.Count)
                {
                    case 1:
                        keyName = keyForm[0];
                        keyInitform = LispSymbol.Nil;
                        keyExistsArg = null;
                        break;
                    case 2:
                        keyName = keyForm[0];
                        keyInitform = keyForm[1];
                        keyExistsArg = null;
                        break;
                    case 3:
                        keyName = keyForm[0];
                        keyInitform = keyForm[1];
                        keyExistsArg = keyForm[2].AsSymbol(() => new LispSyntaxException(CurrentForm, "Argument name is not a symbol: " + keyForm[3]));
                        break;
                    default:
                        throw new LispSyntaxException(tmp, "Invalid key argument syntax");
                }
                if (keyName.IsSymbol())
                {
                    arg = (LispSymbol) keyName;
                    keyKeyword = LispSymbol.Nil.Intern(arg.Name);
                }
                else
                {
                    var keyNameList = keyName.AsList(2, () => new LispSyntaxException(keyName, "Invalid keyword argument name form"));
                    keyKeyword = keyNameList[0].AsSymbol(() => new LispSyntaxException(keyName, "Keyword name is not a symbol"));
                    arg = keyNameList[1].AsSymbol(() => new LispSyntaxException(keyName, "Argument name is not a symbol"));
                }
                if(req.Any(a => a.Eq(arg)) || key.Any(a => a.Key.Eq(arg)) || restArg == arg)
                    throw new LispSyntaxException(CurrentForm, "Duplicate argument name: " + arg.Name.StringValue);
                key.Add(arg, Tuple.Create(keyKeyword, keyInitform, keyExistsArg));
            }
            goto key;
        allowOtherKeys:
            allowOtherKeys = true;
            if (form.IsEnd())
                goto end;
            form.First().As(LispSymbol.Aux, SyntaxException("Only &aux args can follow &allow-other-keys"));
        aux:
            if (form.IsEnd(SyntaxException("Dotted lambda list are not allowed")))
                goto end;
            tmp = form.First();
            form = form.Rest().AsList(SyntaxException("Dotted lambda list are not allowed"));
            if (tmp.IsSymbol())
            {
                arg = (LispSymbol)tmp;
                aux.Add(arg, LispSymbol.Nil);
                goto aux;
            }
            using (BindCurrentForm(tmp))
            {
                var auxList = tmp.AsList(2, SyntaxException("Invalid &aux argument form"));
                aux.Add(auxList[0].AsSymbol(SyntaxException("Argument name must be a symbol")), auxList[1]);
                goto aux;
            }
        end:
            reqArgs = req;
            keyArgs = key;
            auxArgs = aux;
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
