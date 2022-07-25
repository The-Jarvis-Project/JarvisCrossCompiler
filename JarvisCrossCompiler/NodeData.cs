using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JCC
{
    internal struct NodeData
    {
        public SyntaxKind kind;
        public SyntaxNode[] children;
        public SyntaxToken[] tokens;

        public bool HasChildren => children.Length > 0;
    }
}