using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Bolcko.Web.App.Binders
{
    public class InvariantDecimalModelBinder : IModelBinder
    {
        private readonly SimpleTypeModelBinder _fallbackBinder;

        public InvariantDecimalModelBinder(Type modelType, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            _fallbackBinder = new SimpleTypeModelBinder(modelType, loggerFactory);
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return _fallbackBinder.BindModelAsync(bindingContext);
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var valueStr = valueProviderResult.FirstValue;
            if (string.IsNullOrWhiteSpace(valueStr))
            {
                return _fallbackBinder.BindModelAsync(bindingContext);
            }

            // Replace comma with dot to support both 25.5 and 25,5
            valueStr = valueStr.Replace(',', '.');

            if (decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            return _fallbackBinder.BindModelAsync(bindingContext);
        }
    }

    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?))
            {
                var loggerFactory = (Microsoft.Extensions.Logging.ILoggerFactory)context.Services.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory))!;
                return new InvariantDecimalModelBinder(context.Metadata.ModelType, loggerFactory);
            }

            return null;
        }
    }
}
