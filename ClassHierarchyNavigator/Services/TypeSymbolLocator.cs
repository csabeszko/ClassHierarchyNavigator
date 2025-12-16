using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClassHierarchyNavigator.Services
{
    public static class TypeSymbolLocator
    {
        public static async Task<INamedTypeSymbol?> GetTypeSymbolAtPositionAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return null;
            }

            SyntaxNode syntaxRoot = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken);

            int adjustedPosition = position;
            if (adjustedPosition > 0 && adjustedPosition >= syntaxRoot.Span.End)
            {
                adjustedPosition = syntaxRoot.Span.End - 1;
            }

            SyntaxToken token = syntaxRoot.FindToken(adjustedPosition);

            TypeDeclarationSyntax? typeDeclarationSyntax =
                token.Parent?
                     .AncestorsAndSelf()
                     .OfType<TypeDeclarationSyntax>()
                     .FirstOrDefault();

            if (typeDeclarationSyntax != null)
            {
                if (typeDeclarationSyntax.Identifier.Span.Contains(adjustedPosition))
                {
                    ISymbol? declaredSymbol = semanticModel.GetDeclaredSymbol(typeDeclarationSyntax, cancellationToken);
                    if (declaredSymbol is INamedTypeSymbol declaredTypeSymbol)
                    {
                        return declaredTypeSymbol;
                    }
                }
            }

            SyntaxNode nodeAtPosition = token.Parent ?? syntaxRoot;

            NameSyntax? nameSyntax =
                nodeAtPosition.AncestorsAndSelf()
                              .OfType<NameSyntax>()
                              .FirstOrDefault();

            if (nameSyntax != null)
            {
                ISymbol? resolvedSymbol = semanticModel.GetSymbolInfo(nameSyntax, cancellationToken).Symbol;
                if (resolvedSymbol is INamedTypeSymbol namedTypeSymbolFromName)
                {
                    return namedTypeSymbolFromName;
                }

                if (resolvedSymbol is IAliasSymbol aliasSymbol && aliasSymbol.Target is INamedTypeSymbol aliasedNamedTypeSymbol)
                {
                    return aliasedNamedTypeSymbol;
                }
            }

            TypeSyntax? typeSyntax =
                nodeAtPosition.AncestorsAndSelf()
                              .OfType<TypeSyntax>()
                              .FirstOrDefault();

            if (typeSyntax != null)
            {
                ITypeSymbol? typeSymbol = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
                if (typeSymbol is INamedTypeSymbol namedTypeSymbolFromType)
                {
                    return namedTypeSymbolFromType;
                }
            }

            return null;
        }
    }
}
