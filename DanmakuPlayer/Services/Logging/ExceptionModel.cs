using System;

namespace DanmakuPlayer.Services.Logging;

public class ExceptionModel
{
    public ExceptionModel(Exception exception)
    {
        Exception = exception;
        Source = exception.Source;
        Message = exception.Message;
        StackTrace = exception.StackTrace;
        if (exception.InnerException is not null)
            InnerException = new(exception.InnerException);
    }

    public Exception Exception { get; }

    public string? Source { get; }

    public string Message { get; }

    public string? StackTrace { get; }

    public ExceptionModel? InnerException { get; }

    public string ToString(int indent) =>
        $"""
         {Indent(indent)}Type: {Exception.GetType().FullName}
         {Indent(indent)}Source: {Source}
         {Indent(indent)}Message: {Message}
         {Indent(indent)}StackTrace: 
         {Indent(indent + 1)}{StackTrace?.ReplaceLineEndings(Environment.NewLine + Indent(indent + 1)) ?? "null"}
         {InnerException?.ToString(indent + 1)}

         """;

    private static string Indent(int indent) => new('\t', indent);
}
