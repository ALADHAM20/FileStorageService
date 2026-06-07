using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FileStorageService.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        RemoveFactory<FormValueProviderFactory>(context.ValueProviderFactories);
        RemoveFactory<FormFileValueProviderFactory>(context.ValueProviderFactories);
        RemoveFactory<JQueryFormValueProviderFactory>(context.ValueProviderFactories);
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    private static void RemoveFactory<TFactory>(IList<IValueProviderFactory> factories)
        where TFactory : IValueProviderFactory
    {
        var factory = factories.OfType<TFactory>().FirstOrDefault();

        if (factory is not null)
        {
            factories.Remove(factory);
        }
    }
}
