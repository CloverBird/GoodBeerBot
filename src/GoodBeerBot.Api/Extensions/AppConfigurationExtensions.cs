using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace GodBeerBot.Api.Extensions;

public static class AppConfigurationExtensions
{
    public static TAppConfiguration LoadAppConfiguration<TAppConfiguration>(this IServiceCollection serviceCollection, IConfiguration configuration) where TAppConfiguration : class, new()
    {
        TAppConfiguration val = new TAppConfiguration();
        configuration.Bind(val);
        RegisterSingletonInstance(serviceCollection, val);
        return val;
    }

    private static void RegisterSingletonInstance(IServiceCollection serviceCollection, object obj)
    {
        if (obj == null)
            return;

        serviceCollection.Add(ServiceDescriptor.Singleton(obj.GetType(), obj));
        PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo propertyInfo in properties)
            if (!propertyInfo.PropertyType.IsValueType && 
                !(propertyInfo.PropertyType == typeof(string)) && 
                (!(propertyInfo.PropertyType.GetInterface(typeof(IEnumerable).Name) != null) || 
                !(propertyInfo.PropertyType.GetInterface(typeof(IEnumerable<>).Name) != null)))
            {
                object value = propertyInfo.GetValue(obj);
                if (value != null)
                    RegisterSingletonInstance(serviceCollection, value);
            }
    }
}