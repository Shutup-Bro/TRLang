﻿using System;
using TRLang.src.Parser.AbstractSyntaxTree;
using TRLang.src.CallStack;
using TRLang.src.SymbolTable.Symbols;
using System.Collections.Generic;
using TRLang.src.Lexer;

namespace TRLang.src.Interpreter
{
    class Interpreter : AstNodeVisitor<object>
    {
        private readonly AstNode RootNode;
        public readonly CallStack.CallStack CallStack = new CallStack.CallStack();

        public Interpreter(AstNode node)
        {
            this.RootNode = node;
        }

        public object Interpret() => this.GenericVisit(this.RootNode);

        protected override void BeforeVisit(AstNode node) => Log($"Visit: {node.GetTypeName()}");

        protected override object Visit(Int node) => node.Value;

        protected override object Visit(Float node) => node.Value;

        protected override object Visit(BinOp node)
        {
            //object leftResult = this.GenericVisit(node.LeftNode);
            //object rightResult = this.GenericVisit(node.RightNode);

            //bool returnFloat = leftResult.HasValue(leftResult.FloatValue) || rightResult.HasValue(rightResult.FloatValue);

            //if (returnFloat)
            //{
            //    // Convert value to float
            //    float left = (node.LeftNode is Int) ? ((Int)node.LeftNode).Value : ((Float)node.LeftNode).Value;
            //    float right = (node.RightNode is Int) ? ((Int)node.RightNode).Value : ((Float)node.RightNode).Value;

            //    switch (node.Op.Type)
            //    {
            //        case Lexer.TokenType.Plus: return new InterpreterVisitResult(left + right);
            //        case Lexer.TokenType.Minus: return new InterpreterVisitResult(left - right);
            //        case Lexer.TokenType.Mul: return new InterpreterVisitResult(left * right);
            //        case Lexer.TokenType.Div: return new InterpreterVisitResult(left / right);

            //        default: Error(); return new InterpreterVisitResult();
            //    }
            //}
            //else
            //{
            //    // Convert value to int
            //    int left = ((Int)node.LeftNode).Value;
            //    int right = ((Int)node.RightNode).Value;

            //    switch (node.Op.Type)
            //    {
            //        case Lexer.TokenType.Plus: return new InterpreterVisitResult(left + right);
            //        case Lexer.TokenType.Minus: return new InterpreterVisitResult(left - right);
            //        case Lexer.TokenType.Mul: return new InterpreterVisitResult(left * right);
            //        case Lexer.TokenType.Div: return new InterpreterVisitResult(left / right);

            //        default: Error(); return new InterpreterVisitResult();
            //    }
            //}

            object leftResult = this.GenericVisit(node.LeftNode);
            object rightResult = this.GenericVisit(node.RightNode);

            bool leftIsInt = leftResult is int;
            bool rightIsInt = rightResult is int;

            //object left = leftIsInt ? (int)leftResult : (float)leftResult;
            //object right = rightIsInt ? (int)rightResult : (float)rightResult;
            int? intLeft = leftIsInt ? (int)leftResult : null;
            float? floatLeft = !leftIsInt ? (float)leftResult : null;
            int? intRight = rightIsInt ? (int)rightResult : null;
            float? floatRight = !rightIsInt ? (float)rightResult : null;

            float? result = node.Op.Type switch
            {
                TokenType.Plus => (intLeft ?? floatLeft) + (intRight ?? floatRight),
                TokenType.Minus => (intLeft ?? floatLeft) - (intRight ?? floatRight),
                TokenType.Mul => (intLeft ?? floatLeft) * (intRight ?? floatRight),
                TokenType.Div => (intLeft ?? floatLeft) / (intRight ?? floatRight),

                _ => null
            };

            if (result == null)
            {
                Error();
                return null;
            }

            if (leftIsInt && rightIsInt) return Convert.ToInt32(result!);
            else return (result!);
        }

        protected override object Visit(UnaryOp node)
        {
            object exprResult = this.GenericVisit(node.ExprNode);

            switch (node.Op.Type)
            {
                case TokenType.Minus: return -(exprResult is int ? (int)exprResult : (float)exprResult);
                case TokenType.Plus: return exprResult is int ? (int)exprResult : (float)exprResult;

                default: Error(); return null;
            }
        }

        protected override object Visit(Compound node)
        {
            foreach (var child in node.Children) this.GenericVisit(child);

            return null;
        }

        protected override object Visit(NoOp node) => null;

        protected override object Visit(Assign node)
        {
            string key = ((Var)node.LeftNode).Name;
            object result = this.GenericVisit(node.RightNode);

            ActivationRecord ar = this.CallStack.Peek();
            ar.Set(key, result);

            return null;
        }

        protected override object Visit(Var node)
        {
            ActivationRecord ar = this.CallStack.Peek();

            if (ar.ContainsKey(node.Name)) return ar.Get(node.Name);
            else
            {
                Error();
                return null;
            }
        }

        protected override object Visit(TypeSpec node) => null;

        protected override object Visit(VarDecl node)
        {
            if (node.ValueNode != null)
            {
                string key = ((Var)node.VarNode).Name;
                object result = this.GenericVisit(node.ValueNode);

                ActivationRecord ar = this.CallStack.Peek();
                ar.Set(key, result);
            }

            return null;
        }

        protected override object Visit(FuncDecl node) => null;

        protected override object Visit(FuncCall node)
        {
            string funcName = node.FuncName;

            ActivationRecord ar = new ActivationRecord(funcName, ARType.Function, 2);

            FuncSymbol funcSymbol = node.FuncSymbol;

            List<VarSymbol> formal = funcSymbol.Params;
            List<AstNode> actual = node.ActualParams;

            for (int i = 0; i < formal.Count; i++)
            {
                object result = this.GenericVisit(actual[i]);
                ar.Set(formal[i].Name, result);
            }

            this.CallStack.Push(ar);

            this.GenericVisit(funcSymbol.Body);

            this.LogCallStack();

            this.CallStack.Pop();

            return new InterpreterVisitResult();
        }

        protected override object Visit(Program node)
        {
            ActivationRecord ar = new ActivationRecord("<GLOBAL>", ARType.Program, 1);
            this.CallStack.Push(ar);

            foreach (AstNode n in node.Nodes) this.GenericVisit(n);

            this.LogCallStack();

            this.CallStack.Pop();

            return null;
        }

        private static void Error()
        {
            throw new Exception("ERROR IN INTERPRETER SHOULD NOT BE POSSIBLE.");
        }

        private static void Log(string message)
        {
            if (Flags.LogInterpreter) Console.WriteLine($"Interpreter: {message}");
        }

        private void LogCallStack()
        {
            if (Flags.LogCallStack) Console.WriteLine(this.CallStack);
        }
    }
}
