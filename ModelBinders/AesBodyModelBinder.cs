using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SimApi.Communications;
using SimApi.Helpers;

namespace SimApi.ModelBinders;

public class AesBodyModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var request = bindingContext.HttpContext.Request;
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrEmpty(requestBody))
        {
            bindingContext.ModelState.AddModelError("", "请求体不能为空");
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        SimApiOneFieldRequest<string>? aesRequest;
        try
        {
            aesRequest = JsonSerializer.Deserialize<SimApiOneFieldRequest<string>>(requestBody, SimApiUtil.JsonOption);
        }
        catch (JsonException ex)
        {
            bindingContext.ModelState.AddModelError("", $"请求体格式错误：{ex.Message}");
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        if (aesRequest == null || string.IsNullOrEmpty(aesRequest.Data))
        {
            bindingContext.ModelState.AddModelError("", "请求体缺少密文Data字段");
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        // 3. 根据配置获取appId（参考上一节代码）
        var keyProvider =
            bindingContext.HttpContext.RequestServices.GetService(Type.GetType(bindingContext.BinderModelName!)!) as
                AesBodyProviderBase;
        string? appId = null;
        if (!string.IsNullOrEmpty(keyProvider!.AppIdName))
        {
            appId = bindingContext.HttpContext.Request.Query[keyProvider.AppIdName]
                .FirstOrDefault() ?? bindingContext.HttpContext.Request.Headers[keyProvider.AppIdName]
                .FirstOrDefault();

            if (string.IsNullOrEmpty(appId))
            {
                bindingContext.ModelState.AddModelError("",
                    $"未找到{keyProvider.AppIdName}");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }
        }

        // 4. 通过接口获取密钥（解耦的核心：不再直接依赖数据库）
        var key = keyProvider.GetKey(appId);
        if (string.IsNullOrEmpty(key))
        {
            bindingContext.ModelState.AddModelError("", "获取密钥失败（应用不存在或密钥未配置）");
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        // 5. 解密和反序列化（保持原有逻辑）
        try
        {
            var jsonStr = SimApiAesUtil.Decrypt(aesRequest.Data, key);
            if (string.IsNullOrEmpty(jsonStr))
            {
                bindingContext.ModelState.AddModelError("", "解密失败");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            var targetType = bindingContext.ModelType;
            var deserializedModel = JsonSerializer.Deserialize(jsonStr, targetType, SimApiUtil.JsonOption);
            if (deserializedModel == null)
            {
                bindingContext.ModelState.AddModelError("", "反序列化失败");
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(deserializedModel);
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError("", $"处理异常：{ex.Message}");
            bindingContext.Result = ModelBindingResult.Failed();
        }
    }
}