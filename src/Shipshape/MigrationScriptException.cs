namespace Shipshape;

/// <summary>A script file is stale or fails a check. It is a plain exception, so every test framework reports it the same way.</summary>
public sealed class MigrationScriptException(string message, Exception? innerException = null) : Exception(message, innerException);
