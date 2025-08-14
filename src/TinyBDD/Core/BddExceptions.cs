namespace TinyBDD;

public sealed class BddStepException(string message, Exception inner) : 
    Exception(message, inner);
