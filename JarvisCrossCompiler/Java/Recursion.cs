using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JCC.Java
{
    internal static class Recursion
    {
        public static string ProcessExpressionToken(SyntaxToken token)
        {
            string java = string.Empty;
            switch (token.Kind())
            {
                case SyntaxKind.AsteriskToken:
                    java += " * ";
                    break;
                case SyntaxKind.OpenParenToken:
                    java += "(";
                    break;
                case SyntaxKind.CloseParenToken:
                    java += ")";
                    break;
                case SyntaxKind.MinusToken:
                    java += " - ";
                    break;
                case SyntaxKind.PlusToken:
                    java += " + ";
                    break;
                case SyntaxKind.EqualsToken:
                    java += " = ";
                    break;
                case SyntaxKind.BackslashToken:
                    java += "\\";
                    break;
                case SyntaxKind.DoubleQuoteToken:
                    java += "\"";
                    break;
                case SyntaxKind.SingleQuoteToken:
                    java += "\'";
                    break;
                case SyntaxKind.CommaToken:
                    java += ", ";
                    break;
                case SyntaxKind.NewKeyword:
                    java += "new ";
                    break;
                default:
                    break;
            }
            return java;
        }
    }
}
