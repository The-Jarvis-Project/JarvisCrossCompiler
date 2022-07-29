using JCC.Java;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JCC
{
    internal static class NodeExtensions
    {
        public static NodeData ProcessNode(this SyntaxNode node) =>
            new NodeData
        {
            kind = node.Kind(),
            children = node.ChildNodes().ToArray(),
            tokens = node.ChildTokens().ToArray()
        };

        public static string Text(this BaseTypeSyntax node) => node.GetFirstToken().Text;
        public static string Text(this TypeSyntax node)
        {
            if (node.IsKind(SyntaxKind.GenericName))
            {
                GenericNameSyntax syntax = (GenericNameSyntax)node;
                return node.GetFirstToken().Text + syntax.TypeArgumentList.ToFullString();
            }
            else if (node.IsKind(SyntaxKind.QualifiedName))
            {
                QualifiedNameSyntax syntax = (QualifiedNameSyntax)node;
                return syntax.Left.Text() + "." + syntax.Right.Text();
            }
            else return node.GetFirstToken().Text;
        }

        public static bool Public(this SyntaxToken token) => token.IsKind(SyntaxKind.PublicKeyword);
        public static bool Private(this SyntaxToken token) => token.IsKind(SyntaxKind.PrivateKeyword);
        public static bool Protected(this SyntaxToken token) => token.IsKind(SyntaxKind.ProtectedKeyword);
        public static bool Static(this SyntaxToken token) => token.IsKind(SyntaxKind.StaticKeyword);
        public static bool Internal(this SyntaxToken token) => token.IsKind(SyntaxKind.InternalKeyword);

        public static bool BinaryExpression(this SyntaxNode node) =>
            node.IsKind(SyntaxKind.AddExpression) ||
            node.IsKind(SyntaxKind.SubtractExpression) ||
            node.IsKind(SyntaxKind.MultiplyExpression) ||
            node.IsKind(SyntaxKind.DivideExpression) ||
            node.IsKind(SyntaxKind.ModuloExpression) ||
            node.IsKind(SyntaxKind.LessThanExpression) ||
            node.IsKind(SyntaxKind.GreaterThanExpression) ||
            node.IsKind(SyntaxKind.LessThanOrEqualExpression) ||
            node.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
            node.IsKind(SyntaxKind.EqualsExpression) ||
            node.IsKind(SyntaxKind.LogicalAndExpression) ||
            node.IsKind(SyntaxKind.LogicalOrExpression) ||
            node.IsKind(SyntaxKind.NotEqualsExpression) ||
            node.IsKind(SyntaxKind.BitwiseAndExpression) ||
            node.IsKind(SyntaxKind.BitwiseOrExpression) ||
            node.IsKind(SyntaxKind.LeftShiftExpression) ||
            node.IsKind(SyntaxKind.RightShiftExpression);

        public static SyntaxNode? FindChild(this SyntaxNode node, SyntaxKind kind)
        {
            foreach (SyntaxNode child in node.ChildNodes())
            {
                if (child.IsKind(kind)) return child;
                else
                {
                    SyntaxNode? searched = child.FindChild(kind);
                    if (searched != null) return searched;
                }
            }
            return null;
        }

        public static List<SyntaxNode> TopChildren(this SyntaxNode node, SyntaxKind kind)
        {
            List<SyntaxNode> children = new List<SyntaxNode>();
            foreach (SyntaxNode child in node.ChildNodes())
                if (child.IsKind(kind))
                    children.Add(child);
            return children;
        }
    }

    internal static class StringExtensions
    {
        public static string CamelConvention(this string s)
        {
            if (Compiler.UseConvention && s.Length > 0)
                s = char.ToLower(s[0]) + s[1..];
            return s;
        }

        public static string UnAtIfy(this string s)
        {
            if (s.StartsWith("@"))
            {
                s = s.Remove(0, 1);
                string newS = string.Empty;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == '\\') newS += "\\\\";
                    else newS += s[i];
                }
                return newS;
            }
            return s;
        }
    }

    internal static class JavaExtensions
    {
        public static string[] JavaSubTypes(this string s)
        {
            return s.Split(new[] { '<', '>' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    internal static class SymbolExtensions
    {
        public static bool Null(this SymbolInfo? info) => info == null || info.Value.Symbol == null;
        public static bool GetKind(this SymbolInfo? info, out SymbolKind kind)
        {
            if (info != null && info.Value.Symbol != null)
            {
                kind = info.Value.Symbol.Kind;
                return true;
            }
            kind = SymbolKind.Alias;
            return false;
        }
    }
}
