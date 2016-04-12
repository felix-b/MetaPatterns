﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using AutoPatterns.Abstractions;
using AutoPatterns.Extensions;
using AutoPatterns.Runtime;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AutoPatterns.Tests.Examples
{
    [MetaProgram.Annotation.ClassTemplate]
    [DataContract(Name = MetaProgram.Constant.String1, Namespace = MetaProgram.Constant.String2)]
    public partial class ExampleDebugger : ExampleAncestors.ITryDebugging
    {
        [MetaProgram.Annotation.NewMember]
        public void TryDebugging()
        {
            Console.WriteLine("HELLO WORLD!");
            System.Diagnostics.Debug.WriteLine("HELLO DEBUG!");
            System.Diagnostics.Debugger.Launch();
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    public partial class ExampleDebugger : IPatternTemplate
    {
        void IPatternTemplate.Apply(PatternWriterContext context)
        {
            context.Output.ClassWriter.AddBaseType(typeof(ExampleAncestors.ITryDebugging));
            TryDebugging__Apply(context, typeof(ExampleAncestors.ITryDebugging).GetMethod(nameof(ExampleAncestors.ITryDebugging.TryDebugging)));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private void TryDebugging__Apply(PatternWriterContext context, MethodInfo declaration)
        {
            context.Library.EnsureMetadataReference(typeof(System.Diagnostics.Debug));
            context.Library.EnsureMetadataReference(typeof(System.Diagnostics.Debugger));

            var tryDebuggingMethod = context.Output.ClassWriter.AddPublicVoidMethod(nameof(ExampleAncestors.ITryDebugging.TryDebugging), declaration);

            tryDebuggingMethod.Syntax =
                tryDebuggingMethod.Syntax.WithBody(
                    Block(
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Console"), IdentifierName("WriteLine")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("HELLO WORLD!"))))))),
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("System"), IdentifierName("Diagnostics")),
                                        IdentifierName("Debug")),
                                    IdentifierName("WriteLine")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("HELLO DEBUG!"))))))),
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("System"), IdentifierName("Diagnostics")),
                                        IdentifierName("Debugger")),
                                    IdentifierName("Launch"))))));
        }
    }
}
