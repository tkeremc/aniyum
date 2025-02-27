namespace Aniyum.Helpers;

public static class NullCheckHelper
{
    public static T Checker<T>(T model) where T : class
    {
        var parameters = typeof(T).GetProperties();

        foreach (var parameter in parameters)
        {
            var value = parameter.GetValue(model);
            if (value == null)
            {
                if (parameter.Name != "Id") throw new NullReferenceException($"Parameter {parameter.Name} cannot be null.");
            }
        }
        return model;
    }
}