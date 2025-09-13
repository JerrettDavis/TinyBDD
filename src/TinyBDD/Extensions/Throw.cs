namespace TinyBDD.Extensions;

public static class Throw
{
    public static void ValidationException(string message) => 
        throw new ArgumentException(message);

    public static void ValidationExceptionIf(bool condition, string message)
    {
        if (condition) ValidationException(message);
    }
        

}
