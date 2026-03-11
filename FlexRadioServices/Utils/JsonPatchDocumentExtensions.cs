using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlexRadioServices.Utils;

public static class JsonPatchDocumentExtensions
{
    public static void ApplyToSafely<T>(this JsonPatchDocument<T> patchDoc, T objectToApplyTo,
        ModelStateDictionary modelState)
        where T : class
    {
        if (patchDoc == null) throw new ArgumentNullException(nameof(patchDoc));
        if (objectToApplyTo == null) throw new ArgumentNullException(nameof(objectToApplyTo));
        if (modelState == null) throw new ArgumentNullException(nameof(modelState));

        patchDoc.ApplyToSafely(objectToApplyTo: objectToApplyTo, modelState: modelState, prefix: string.Empty);
    }

    public static void ApplyToSafely<T>(this JsonPatchDocument<T> patchDoc, T objectToApplyTo,
        ModelStateDictionary modelState,
        string prefix)
        where T : class
    {
        if (patchDoc == null) throw new ArgumentNullException(nameof(patchDoc));
        if (objectToApplyTo == null) throw new ArgumentNullException(nameof(objectToApplyTo));
        if (modelState == null) throw new ArgumentNullException(nameof(modelState));

        var allowedPropertyNames = GetAllowedJsonPropertyNames(typeof(T));

        foreach (var op in patchDoc.Operations)
        {
            if (!string.IsNullOrWhiteSpace(op.path))
            {
                var segments = op.path.TrimStart('/').Split('/');
                var target = segments.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                target = target.Replace("~1", "/").Replace("~0", "~");

                if (!allowedPropertyNames.Contains(target))
                {
                    var key = string.IsNullOrEmpty(prefix) ? target : prefix + "." + target;
                    modelState.TryAddModelError(key,
                        $"The property at path '{op.path}' is immutable or does not exist.");
                }
            }
        }

        if (!modelState.IsValid) return;

        patchDoc.ApplyTo(objectToApplyTo, jsonPatchError =>
        {
            var affectedObjectName = jsonPatchError.AffectedObject?.GetType().Name ?? typeof(T).Name;
            var key = string.IsNullOrEmpty(prefix) ? affectedObjectName : $"{prefix}.{affectedObjectName}";

            modelState.TryAddModelError(key, jsonPatchError.ErrorMessage);
        });
    }

    private static HashSet<string> GetAllowedJsonPropertyNames(Type type)
    {
        var attrs = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance;

        return type
            .GetProperties(attrs)
            .Where(p => p.GetMethod != null && p.GetMethod.IsPublic)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .SelectMany(p =>
            {
                var jsonPropertyName = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
                var clrName = p.Name;
                var camelCaseName = JsonNamingPolicy.CamelCase.ConvertName(clrName);

                return string.IsNullOrWhiteSpace(jsonPropertyName)
                    ? new[] { clrName, camelCaseName }
                    : new[] { clrName, camelCaseName, jsonPropertyName };
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}