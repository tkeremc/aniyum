namespace Aniyum.Helpers;

public static class UpdateCheckHelper
{
    public static T Checker<T>(T oldModel, T newModel) where T : class
    {
        var paramaters = typeof(T).GetProperties();

        foreach (var parameter in paramaters)
        {
            var oldValue = parameter.GetValue(oldModel);
            var newValue = parameter.GetValue(newModel);
            // mapper
            
            if (newValue == null || (newValue is string stringValue && string.IsNullOrEmpty(stringValue)))
                parameter.SetValue(newModel, oldValue);
        }
        return newModel;
    }
}