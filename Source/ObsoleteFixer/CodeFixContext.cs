using System.Threading;
using Microsoft.CodeAnalysis;

namespace ObsoleteFixer
{
    public class CodeFixContext
    {
        public CodeFixContext(Document document, CancellationToken cancellationToken, string replaceWith, bool isObsoleteTypeKey, ISymbol symbol)
        {
            Document = document;
            CancellationToken = cancellationToken;
            ReplaceWith = replaceWith;
            IsObsoleteTypeKey = isObsoleteTypeKey;
            Symbol = symbol;
        }

        /// <summary>
        /// Document. Will be replaced if success
        /// </summary>
        public Document Document { get; set; }
        public CancellationToken CancellationToken { get; }
        public string ReplaceWith { get; }
        public bool IsObsoleteTypeKey { get; }
        public ISymbol Symbol { get; }
    }
}