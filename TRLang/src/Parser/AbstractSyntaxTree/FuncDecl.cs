﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRLang.src.Parser.AbstractSyntaxTree
{
    class FuncDecl : AstNode
    {
        public string FuncName { get; private set; }
        public List<AstNode> Params { get; private set; }
        public AstNode BodyNode { get; private set; }

        public FuncDecl(string funcName, List<AstNode> paramList, AstNode bodyNode)
        {
            this.FuncName = funcName;
            this.Params = paramList;
            this.BodyNode = bodyNode;
        }

        public override string ToString() => $"FuncDecl(FuncName={this.FuncName}, BodyName={this.BodyNode})";
    }
}
