using System;

namespace TFlexOmpFix
{
    public interface IDocLogging : ILogging
    {
        TimeSpan Span { get; set; }

        string User { get; set; }

        string Document { get; set; }

        string Section { get; set; }

        string Position { get; set; }

        string Sign { get; set; }

        string Name { get; set; }

        decimal? Qty { get; set; }

        string Doccode { get; set; }

        string FilePath { get; set; }

        string Error { get; set; }
    }
}