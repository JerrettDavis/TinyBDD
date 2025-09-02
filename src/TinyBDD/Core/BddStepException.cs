namespace TinyBDD;

/// <summary>
/// Represents an exception that occurs during a BDD (Behavior-Driven Development) step execution.
/// This class is used to wrap exceptions occurring during the execution of BDD steps, including information about the failed step context such as its kind and title.
/// </summary>
[Serializable]
public class BddStepException(
    string message,
    ScenarioContext context,
    Exception innerException
) : Exception(message, innerException)
{
    public ScenarioContext Context => context;
}