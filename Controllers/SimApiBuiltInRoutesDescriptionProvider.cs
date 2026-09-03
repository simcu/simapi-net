using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using SimApi.Configurations;

namespace SimApi.Controllers;

/// <summary>
/// 将 SimApiRouteOptions 动态配置的内置约定路由(UserInfo / Logout / WebConfig)
/// 翻译成 ApiDescription, 使它们能像属性路由接口一样出现在 Swagger 文档中:
/// 请求/响应 schema 由 Swashbuckle 自动分析, 方法上已有的 [SimApiDoc] 注解自动生效。
/// 路径为 null / 空 的端点在此一并跳过(与路由注册保持一致)。
/// </summary>
public class SimApiBuiltInRoutesDescriptionProvider(
    IServiceProvider services,
    SimApiOptions options) : IApiDescriptionProvider
{
    /// <summary>
    /// 默认 provider(DefaultApiDescriptionProvider)的 Order 为 -1000, 位于其后执行。
    /// </summary>
    public int Order => -900;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        var modelMetadata = services.GetService<IModelMetadataProvider>();

        foreach (var route in SimApiBuiltInRoutes.Get(options.SimApiRouteOptions))
        {
            var action = context.Actions.OfType<ControllerActionDescriptor>()
                .FirstOrDefault(a =>
                    string.Equals(a.ControllerName, route.Controller, StringComparison.Ordinal) &&
                    string.Equals(a.ActionName, route.Action, StringComparison.Ordinal));
            if (action is null)
            {
                continue;
            }

            // 若该动作已被属性路由 / 其他 provider 收录, 跳过避免重复
            if (context.Results.Any(d => ReferenceEquals(d.ActionDescriptor, action)))
            {
                continue;
            }

            foreach (var httpMethod in route.HttpMethods)
            {
                context.Results.Add(CreateDescription(action, route, httpMethod, modelMetadata));
            }
        }
    }

    private static ApiDescription CreateDescription(
        ControllerActionDescriptor action,
        SimApiBuiltInRoute route,
        string httpMethod,
        IModelMetadataProvider? modelMetadata)
    {
        var apiDescription = new ApiDescription
        {
            ActionDescriptor = action,
            HttpMethod = httpMethod,
            RelativePath = route.Path.TrimStart('/')
        };

        // 输入: 按真实方法参数翻译(内置端点均为零参, 此处保证将来加参也可分析)
        foreach (var parameter in action.Parameters)
        {
            var parameterDescription = new ApiParameterDescription
            {
                Name = parameter.Name,
                Type = parameter.ParameterType,
                ParameterDescriptor = parameter,
                Source = parameter.BindingInfo?.BindingSource ?? BindingSource.Body,
                IsRequired = true
            };
            if (modelMetadata is not null)
            {
                parameterDescription.ModelMetadata = modelMetadata.GetMetadataForType(parameter.ParameterType);
            }

            apiDescription.ParameterDescriptions.Add(parameterDescription);
        }

        // 输出: 方法返回类型 => 200 + json
        var returnType = Unwrap(action.MethodInfo.ReturnType);
        if (returnType != typeof(void))
        {
            var responseType = new ApiResponseType
            {
                StatusCode = 200,
                Type = returnType,
                IsDefaultResponse = true
            };
            if (modelMetadata is not null)
            {
                responseType.ModelMetadata = modelMetadata.GetMetadataForType(returnType);
            }

            responseType.ApiResponseFormats.Add(new ApiResponseFormat { MediaType = "application/json" });
            apiDescription.SupportedResponseTypes.Add(responseType);
        }

        return apiDescription;
    }

    private static Type Unwrap(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
            ? type.GetGenericArguments()[0]
            : type;
}
