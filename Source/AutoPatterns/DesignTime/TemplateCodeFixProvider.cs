﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoPatterns.Extensions;
using AutoPatterns.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AutoPatterns.DesignTime
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TemplateCodeFixProvider)), Shared]
    public class TemplateCodeFixProvider : CodeFixProvider
    {
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = syntaxRoot.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: _s_codeFixTitle,
                    createChangedDocument: c => ImplementTemplate(context.Document, syntaxRoot, declaration, c),
                    equivalenceKey: _s_codeFixTitle),
                diagnostic);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            TemplateDiagnosticIds.TemplateIsNotImplemented,
            TemplateDiagnosticIds.TemplateImplementationIsOutOfDate
        );

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private async Task<Document> ImplementTemplate(
            Document document0, 
            SyntaxNode syntaxRoot0,
            ClassDeclarationSyntax handCodedTemplateSyntax, 
            CancellationToken cancellation)
        {
            var editor = await DocumentEditor.CreateAsync(document0, cancellation);

            GeneratePartialWithTemplateImplementation(handCodedTemplateSyntax, cancellation, editor);
            RemoveOldImplementationPartial(handCodedTemplateSyntax, editor);
            EnsureHandCodedPartHasPartialModifier(handCodedTemplateSyntax, editor);

            var document1 = editor.GetChangedDocument();
            var syntaxRoot1 = (CompilationUnitSyntax)(await document1.GetSyntaxRootAsync(cancellation));
            var semanticModel1 = await document1.GetSemanticModelAsync(cancellation);

            var syntaxRoot2 = EnsureNecessaryUsings(syntaxRoot1, semanticModel1);

            return document1.WithSyntaxRoot(syntaxRoot2);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private static readonly string _s_codeFixTitle = "Preprocess this template";
        private static readonly string _s_generatedImplementationAttributeFullName = typeof(GeneratedTemplateImplementationAttribute).FullName;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private static CompilationUnitSyntax EnsureNecessaryUsings(CompilationUnitSyntax syntaxRoot, SemanticModel semanticModel)
        {
            var missingNamespaceUsings = new HashSet<string>(new[] {
                "AutoPatterns",
                "AutoPatterns.Runtime",
                "Microsoft.CodeAnalysis.CSharp",
                "Microsoft.CodeAnalysis.CSharp.Syntax",
            });
            var missingStaticUsings = new HashSet<string>(new[] {
                "Microsoft.CodeAnalysis.CSharp.SyntaxFactory",
            });
            var existingNamespaceUsings = syntaxRoot.Usings
                .Where(u => !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                .Select(u => u.Name.ToFullString());
            var existingStaticUsings = syntaxRoot.Usings
                .Where(u => u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                .Select(u => u.Name.ToFullString());

            missingNamespaceUsings.ExceptWith(existingNamespaceUsings);
            missingStaticUsings.ExceptWith(existingStaticUsings);

            var usingsToAdd = new List<UsingDirectiveSyntax>();

            usingsToAdd.AddRange(missingNamespaceUsings.Select(namespaceName => 
                UsingDirective(ParseName(namespaceName))));

            usingsToAdd.AddRange(missingStaticUsings.Select(className => 
                UsingDirective(ParseName(className)).WithStaticKeyword(Token(SyntaxKind.StaticKeyword))));

            var syntaxRoot1 = syntaxRoot.AddUsings(usingsToAdd.ToArray());
            return syntaxRoot1;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private static void RemoveOldImplementationPartial(ClassDeclarationSyntax handCodedTemplateSyntax, DocumentEditor editor)
        {
            var templateClassSymbol = editor.SemanticModel.GetDeclaredSymbol(handCodedTemplateSyntax);
            var templateImplementationAttributeSymbol = editor.SemanticModel.Compilation.GetTypeByMetadataName(_s_generatedImplementationAttributeFullName);
            var oldImplementationSyntaxRef = 
                templateClassSymbol.DeclaringSyntaxReferences.FirstOrDefault(
                    r => IsClassSyntaxWithAttribute(r.GetSyntax(), templateImplementationAttributeSymbol, editor.SemanticModel));

            if (oldImplementationSyntaxRef != null)
            {
                editor.RemoveNode(oldImplementationSyntaxRef.GetSyntax());
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private static bool IsClassSyntaxWithAttribute(SyntaxNode syntax, INamedTypeSymbol attributeTypeSymbol, SemanticModel semanticModel)
        {
            var classSyntax = (syntax as ClassDeclarationSyntax);

            if (classSyntax != null)
            {
                return classSyntax.HasAttributeSyntax(attributeTypeSymbol, semanticModel);
            }

            return false;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private static void EnsureHandCodedPartHasPartialModifier(ClassDeclarationSyntax handCodedTemplateSyntax, DocumentEditor editor)
        {
            if (!handCodedTemplateSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                var newHandCodedTemplateSyntax = (ClassDeclarationSyntax)editor.Generator.WithModifiers(
                    handCodedTemplateSyntax, 
                    DeclarationModifiers.Partial);

                editor.ReplaceNode(handCodedTemplateSyntax, newHandCodedTemplateSyntax);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private static void GeneratePartialWithTemplateImplementation(
            ClassDeclarationSyntax handCodedPartial,
            CancellationToken cancellation,
            DocumentEditor editor)
        {
            var semanticModel = editor.SemanticModel;
            var templateClassSymbol = semanticModel.GetDeclaredSymbol(handCodedPartial, cancellation);
            var applyMethodBuilder = new TemplateApplyMethodBuilder(handCodedPartial, templateClassSymbol, editor);
            applyMethodBuilder.BuildApplyMethod();

            var generatedPartial = editor.Generator.ClassDeclaration(
                templateClassSymbol.Name,
                accessibility: templateClassSymbol.DeclaredAccessibility,
                modifiers: DeclarationModifiers.Partial,
                interfaceTypes: new[] { ParseTypeName(typeof(IPatternTemplate).Name) },
                members: new[] { applyMethodBuilder.ApplyMethodSyntax });

            generatedPartial = AddGeneratedTemplateImplementationAttribute(
                generatedPartial,
                handCodedPartial.NormalizeWhitespace().ToFullString().GetHashCode());

            editor.InsertAfter(handCodedPartial, generatedPartial);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private static SyntaxNode AddGeneratedTemplateImplementationAttribute(SyntaxNode syntax, int hashValue)
        {
            var classSyntax0 = (ClassDeclarationSyntax)syntax;

            var classSyntax1 = classSyntax0.WithAttributeLists(SingletonList<AttributeListSyntax>(
                AttributeList(
                    SingletonSeparatedList<AttributeSyntax>(
                        Attribute(ParseName(_s_generatedImplementationAttributeFullName))
                        .WithArgumentList(
                            AttributeArgumentList(
                                SingletonSeparatedList<AttributeArgumentSyntax>(
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal(hashValue)
                                        )
                                    )
                                    .WithNameEquals(
                                        NameEquals(
                                            IdentifierName(nameof(GeneratedTemplateImplementationAttribute.Hash)))
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            );

            return classSyntax1;
        }
    }
}
