using System.Reflection;
using Maynard.Extensions;
using Maynard.Json;

namespace Maynard.Web;

/// <summary>
/// All FlexJson APIs should standardize on the way they de/serialize objects for mutators with JSON bodies (PATCH, POST, PUT).
/// This interface provides that standard.  Let's say we want to send a request with a single User object and an array of Permissions.
/// That request should look like:
/// <para>{</para>
/// <para>    "user": User,</para>
/// <para>    "permissionArray": [ Permission, Permission, ... ]</para>
/// <para>}</para>
/// </summary>
internal static class Jsonifier
{
    /// <summary>
    /// Converts an array of objects to a FlexJson object, specifically for use within Flex-enabled APIs to standardize
    /// JSON formatting.  For Flex API code style, view the comments for IJsonifier.
    /// <para>If there is a single object, it is serialized into JSON as:</para>
    /// <para>{ "class": object }</para>
    /// <para>If there are multiple objects, they are serialized into JSON as:</para>
    /// <para>{ "classArray": [ object1, object2, ... ] }</para>
    /// </summary>
    /// <param name="models">The FlexModels to serialize into JSON.</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>A FlexJson object ready to be sent off to endpoints as a body.</returns>
    public static FlexJson Jsonify<T>(params T[] models) where T : FlexModel
    {
        if (models.Length == 1)
            return new()
            {
                { models[0].GetType().Name.ToCamelCase(), models[0] }
            };
            
        IGrouping<string, T>[] groups = models
            .Where(model => model != null)
            .GroupBy(model => model.GetType().Name)
            .ToArray();

        if (!groups.Any())
            return null;
        
        FlexJson output = new();
        foreach (IGrouping<string, T> group in groups)
        {
            string name = group.Key.ToCamelCase();
            if (group.Count() == 1)
                output[name] = group.First();
            else
                output[$"{name}Array"] = group.ToArray();
        }

        return output;
    }
    
    public static T[] UnJsonify<T>(FlexJson json) where T : FlexModel
    {
        if (json == null)
            return null;
        
        string name = typeof(T).Name.ToCamelCase();
        
        T[] array = json.Optional<T[]>($"{name}Array");
        T individual = json.Optional<T>(name);

        List<T> output = [];
        if (array != null)
            output.AddRange(array.Where(t => t != null));
        if (individual != null)
            output.Add(individual);
        return output.ToArray();
    }
    
    

    public static T[] UnJsonify2<T>(FlexJson json) where T : FlexModel
    {
        if (json == null)
            return [];

        List<T> output = [];

        foreach (string key in json.Keys)
            if (key.EndsWith("Array"))
            {
                string name = key[..^5];
                if (_modelNames.TryGetValue(name, out Type type))
                {
                    Type arrayType = type.MakeArrayType();
                    MethodInfo method = typeof(FlexJson)
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .Where(m => m.Name == nameof(FlexJson.Require))
                        .Where(m => m.IsGenericMethodDefinition)
                        .Single(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string))
                        .MakeGenericMethod(arrayType);
                    
                    // MethodInfo method = typeof(FlexJson)
                    //     .GetMethod("Require", BindingFlags.Public | BindingFlags.Instance)!
                    //     .MakeGenericMethod(arrayType);
                    T[] models = (T[])method.Invoke(json, [key]) ?? [];
                    output.AddRange(models);
                }
            }
            else if (_modelNames.TryGetValue(key, out Type type))
            {
                MethodInfo method = typeof(FlexJson)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == nameof(FlexJson.Require))
                    .Where(m => m.IsGenericMethodDefinition)
                    .Single(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string))
                    .MakeGenericMethod(type);
                output.Add((T)method.Invoke(json, [key]));
            }

        return output.ToArray();
    }
    
    private static readonly Dictionary<string, Type> _modelNames = Assembly
        .GetEntryAssembly()
        ?.GetExportedTypes()                                        // Add the project's types 
        .Concat(Assembly.GetExecutingAssembly().GetExportedTypes()) // Add calling assembly's types
        .Where(type => !type.IsAbstract)
        .Where(type => type.IsAssignableTo(typeof(FlexModel)))
        .ToDictionary(
            key => key.Name.ToCamelCase(),
            value => value
        )
        ?? new();
}