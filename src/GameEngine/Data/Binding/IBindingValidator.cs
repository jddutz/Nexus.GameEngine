namespace Nexus.GameEngine.Data.Binding
{
    /// <summary>
    /// Defines validation logic for data binding.
    /// </summary>
    public interface IBindingValidator
    {
        ValueValidationResult Validate(object? value, string propertyName);
        bool CanValidate(Type valueType);
    }
}
