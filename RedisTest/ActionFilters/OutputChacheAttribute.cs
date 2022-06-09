using System.Reflection;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace RedisTest.ActionFilters
{
    public class OutputChacheAttribute : ActionFilterAttribute
    {
        private bool setCache = false;
        private MemoryStream bodyStream = new();
        private Stream originalStream;

        private bool isSetFromCache = false;

        public string[] VaryByParams { get; set; }

        public OutputChacheAttribute(int Duration = 60)
        {
            this.Duration = Duration;
        }

        public int Duration { get; }

        public async override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            this.isSetFromCache = true;
            dynamic action = context.ActionDescriptor;
            MethodInfo method = action.MethodInfo;
            ActionExecutedContext act = null;
            if ((method?.ReturnType?.BaseType?.Name) != "Task")
            {
                return;
            }

            var redisClient = context.HttpContext.RequestServices.GetRequiredService<IRedisClient>();
            var key = context.ActionDescriptor.DisplayName;
            if (this.VaryByParams.Any())
            {
                var postFixBuilder = new StringBuilder("(");
                foreach (var p in this.VaryByParams)
                {
                    postFixBuilder.Append($"{p}:{context.ActionArguments[p]}||");
                }
                postFixBuilder.Append(")");

                key += postFixBuilder.ToString();

            }

            var result = await redisClient.Db0.Database.StringGetAsync(key);

            if (result.HasValue)
            {
                var resultType = method.ReturnParameter.ParameterType.GenericTypeArguments[0];
                var serialized = JsonSerializer.Deserialize(result.ToString(), resultType);
                context.Result = new ObjectResult(serialized);
            }
            else
            {
                this.setCache = true;
                act = await next();
            }



            if (this.setCache && act?.Result is ObjectResult ok)
            {
                var actioRresult = JsonSerializer.Serialize(ok.Value);
                await redisClient.Db0.Database.StringSetAsync(key, actioRresult, TimeSpan.FromSeconds(this.Duration));
                this.setCache = false;
            }
        }

        public async override Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await next();
            //if (this.isSetFromCache)
            //{
            //    return;
            //}

            //await next();
            //if (setCache)
            //{
            //    var key = context.ActionDescriptor.DisplayName;
            //    var redisClient = context.HttpContext.RequestServices.GetRequiredService<IRedisClient>();


            //    try
            //    {

            //        // Call the next middleware
            //        await next();

            //        // Set stream pointer position to 0 before reading
            //        this.bodyStream.Seek(0, SeekOrigin.Begin);

            //        // Read the body from the stream
            //        var responseBodyText = await new StreamReader(this.bodyStream).ReadToEndAsync();
            //        await redisClient.Db0.Database.StringSetAsync(key, responseBodyText, TimeSpan.FromSeconds(this.Duration));

            //        // Reset the position to 0 after reading
            //        this.bodyStream.Seek(0, SeekOrigin.Begin);

            //        // Do this last, that way you can ensure that the end results end up in the response.
            //        // (This resulting response may come either from the redirected route or other special routes if you have any redirection/re-execution involved in the middleware.)
            //        // This is very necessary. ASP.NET doesn't seem to like presenting the contents from the memory stream.
            //        // Therefore, the original stream provided by the ASP.NET Core engine needs to be swapped back.
            //        // Then write back from the previous memory stream to this original stream.
            //        // (The content is written in the memory stream at this point; it's just that the ASP.NET engine refuses to present the contents from the memory stream.)
            //        context.HttpContext.Response.Body = this.originalStream;
            //        await this.bodyStream.CopyToAsync(context.HttpContext.Response.Body);
            //        originalStream.Seek(0, SeekOrigin.Begin);
            //        await this.bodyStream.DisposeAsync();
            //        this.originalStream = null;
            //    }
            //    finally
            //    {
            //    }


            //    //Request.Body.Seek(0, SeekOrigin.Begin);
            //    //body = reader.ReadToEnd();



            //    setCache = false;
            //}

            //this.isSetFromCache = false;
            // base.OnResultExecutionAsync(context, next);
        }
    }
}
