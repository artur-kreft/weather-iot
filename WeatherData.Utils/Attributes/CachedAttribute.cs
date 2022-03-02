using System;
using System.Text;
using System.Threading.Tasks;
using WeatherData.Utils.Service;

namespace WeatherData.Utils.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CachedAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _timeToLiveInSeconds;

        public CachedAttribute(int timeToLiveInSeconds)
        {
            _timeToLiveInSeconds = timeToLiveInSeconds;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var cacheService = context.HttpContext.RequestServices.GetRequiredService<IResponseCacheService>();
            string key = GenerateKey(context.HttpContext.Request);
            var cachedResponse = await cacheService.GetCachedResponseAsync(key);

            if (!string.IsNullOrEmpty(cachedResponse))
            {
                var result = new ContentResult
                {
                    Content = cachedResponse,
                    ContentType = "application/json",
                    StatusCode = 200
                };
                context.Result = result;
                return;
            }

            var executedContext = await next();

            if (executedContext.Result is OkObjectResult okObjectResult)
            {
                await cacheService.CacheResponseAsync(key, okObjectResult.Value, TimeSpan.FromSeconds(_timeToLiveInSeconds));
            }
        }

        private string GenerateKey(HttpRequest request)
        {
            var builder = new StringBuilder();
            builder.Append(request.Path.ToString());

            foreach (var (key, value) in request.Query.OrderBy(it => it.Key))
            {
                builder.Append($"|{key}-{value}");
            }

            return builder.ToString();
        }
    }
}