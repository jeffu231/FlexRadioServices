using System.Reflection;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
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

        // get public non-static properties up the dependency tree
        var attrs = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance;
        var properties = typeof(T).GetProperties(attrs).Select(p => p.Name).ToList(); 
        
        // check each operation
        foreach (var op in patchDoc.Operations)
        {
            // only consider when the operation path is present
            if (!string.IsNullOrWhiteSpace(op.path))
            {
                var segments = op.path.TrimStart('/').Split('/');
                var target = segments.First();
                if (!properties.Contains(target, StringComparer.OrdinalIgnoreCase))
                {
                    var key = string.IsNullOrEmpty(prefix) ? target : prefix + "." + target;
                    modelState.TryAddModelError(key,
                        $"The property at path '{op.path}' is immutable or does not exist.");
                }
            }
        }

        if (!modelState.IsValid) return;

        // if we get here, there are no changes to the immutable properties
        // we can thus proceed to apply the operations
        patchDoc.ApplyTo(objectToApplyTo: objectToApplyTo, modelState: modelState, prefix: prefix);
    }
}