using System.ComponentModel;
using System.Linq.Expressions;

namespace Main.Data
{
    /// <summary>
    /// Represents a component that can bind to data sources for automatic synchronization
    /// and real-time updates. Supports two-way binding, validation, and conversion.
    /// </summary>
    public interface IDataBinding : INotifyPropertyChanged
    {
        bool IsDataBindingEnabled { get; set; }
        object? DataSource { get; set; }
        object? DataContext { get; set; }
        BindingModeEnum DefaultBindingModeEnum { get; set; }
        bool ValidateOnBinding { get; set; }
        bool UpdateOnPropertyChanged { get; set; }
        UpdateTriggerEnum UpdateTriggerEnum { get; set; }
        int UpdateDelay { get; set; }
        BindingErrorHandlingEnum ErrorHandling { get; set; }
        IReadOnlyList<DataBinding> ActiveBindings { get; }
        IReadOnlyList<BindingError> BindingErrors { get; }
        bool HasBindingErrors { get; }
        bool IsUpdatingFromBinding { get; }
        Dictionary<Type, IValueConverter> TypeConverters { get; set; }
        Dictionary<string, IBindingValidator> PropertyValidators { get; set; }
        Dictionary<string, IValueFormatter> ValueFormatters { get; set; }
        bool UseWeakReferences { get; set; }
        object? SynchronizationContext { get; set; }
        event EventHandler<BindingCreatedEventArgs> BindingCreated;
        event EventHandler<BindingRemovedEventArgs> BindingRemoved;
        event EventHandler<BoundPropertyChangedEventArgs> BoundPropertyChanged;
        event EventHandler<BindingErrorEventArgs> BindingError;
        event EventHandler<DataSourceChangedEventArgs> DataSourceChanged;
        event EventHandler<DataContextChangedEventArgs> DataContextChanged;
        event EventHandler<BindingUpdateEventArgs> BeforeBindingUpdate;
        event EventHandler<BindingUpdateEventArgs> AfterBindingUpdate;
        DataBinding Bind(string sourceProperty, string targetProperty);
        DataBinding Bind(string sourceProperty, string targetProperty, BindingModeEnum mode,
                        IValueConverter? converter = null, IBindingValidator? validator = null);
        DataBinding BindTwoWay(string sourceProperty, string targetProperty);
        DataBinding Bind<TSource, TTarget>(Expression<Func<TSource, TTarget>> sourceExpression,
                                          string targetProperty, BindingModeEnum mode = BindingModeEnum.OneWay);
        bool Unbind(DataBinding binding);
        int Unbind(string targetProperty);
        void UnbindAll();
        IEnumerable<DataBinding> GetBindings(string propertyName);
        bool HasBinding(string propertyName);
        void UpdateAllBindings();
        void UpdateBinding(string propertyName);
        Task UpdateAllBindingsAsync();
        BindingValidationResult ValidateBindings();
        BindingValidationResult ValidateBinding(string propertyName);
        void ClearBindingErrors();
        void ClearBindingErrors(string propertyName);
        void SuspendBindingUpdates();
        void ResumeBindingUpdates();
        bool AreBindingUpdatesSuspended { get; }
        bool SetBoundValue(string propertyName, object? value);
        object? GetBoundValue(string propertyName);
        void RegisterConverter(Type sourceType, Type targetType, IValueConverter converter);
        void UnregisterConverter(Type sourceType, Type targetType);
        void RegisterValidator(string propertyName, IBindingValidator validator);
        void UnregisterValidator(string propertyName);
        void RegisterFormatter(string propertyName, IValueFormatter formatter);
        void UnregisterFormatter(string propertyName);
        DataBinding CreateComputedBinding(string targetProperty, Func<object> computeFunction, params string[] dependencies);
        DataBinding CreateConditionalBinding(string sourceProperty, string targetProperty, Func<object?, bool> condition);
        DataBinding CreateCollectionBinding(string sourceCollection, string targetCollection, object? itemTemplate = null);
        BindingStatistics GetBindingStatistics();
        void ResetBindingStatistics();
        BindingStateSnapshot GetBindingStateSnapshot();
        void ImportBindingConfiguration(BindingConfiguration config);
        BindingConfiguration ExportBindingConfiguration();
    }
}
