namespace TestApp;

[AttributeUsage(AttributeTargets.Field)]
public class TestAttribute(string description) : Attribute
{
    public string Description { get; set; } = description;
}
