using Ardalis.Result;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace JedoxTranslator.Core.Utils;

public class HttpResult<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("errors")]
    public IEnumerable<string>? Errors { get; set; }
}

//This is a custom extension i wrote and usually use everywhere i use the Result pattern to make logging and data returns easier
public static class ResultExtensions
{
    public static Microsoft.AspNetCore.Http.IResult ToHttpTypedResult<T>(
        this Result<T> result,
        Serilog.ILogger log,
        bool writeBody,
        [CallerMemberName] string callerName = "",
        params object[] logArgs
    )
    {
        var jsonResult = new HttpResult<T>
        {
            Data = result.IsSuccess ? result.Value : default,
            IsSuccess = result.IsSuccess,
            Errors = result.Errors,
        };

        if (!result.IsSuccess)
        {
            if (result.ValidationErrors.Any())
            {
                jsonResult.Errors = result.ValidationErrors.Select(x => x.ErrorMessage);

                if (result.ValidationErrors.Any(x => x.Severity == Ardalis.Result.ValidationSeverity.Error))
                {
                    log.Error(
                        "Request {@CallerName} with params {@Params} is NOT success. Error Code: {@ErrorCode} Error Message: {@Errors}",
                        callerName,
                        logArgs.FirstOrDefault(),
                        result.ValidationErrors.FirstOrDefault()?.ErrorCode,
                        result.ValidationErrors.FirstOrDefault()?.ErrorMessage
                    );
                }
                else
                {
                    log.Warning(
                        "Request {@CallerName} with params {@Params} is NOT success. Error Code: {@ErrorCode} Error Message: {@Errors}",
                        callerName,
                        logArgs.FirstOrDefault(),
                        result.ValidationErrors.FirstOrDefault()?.ErrorCode,
                        result.ValidationErrors.FirstOrDefault()?.ErrorMessage
                    );
                }
            }
            else
            {
                log.Warning(
                    "Request {@CallerName} with params {@Params} is NOT success: {@Errors}",
                    callerName,
                    logArgs.FirstOrDefault(),
                    result.Errors
                );
            }
        }
        else
        {
            if (writeBody)
            {
                log.Information(
                    "Request {@CallerName} with params {@Params} is success: {@Data}",
                    callerName,
                    logArgs.FirstOrDefault(),
                    result.Value
                );
            }
            else
            {
                log.Information(
                    "Request {@CallerName} with params {@Params} is success",
                    callerName,
                    logArgs.FirstOrDefault()
                );
            }
        }

        return result.Status switch
        {
            ResultStatus.Ok => Microsoft.AspNetCore.Http.TypedResults.Ok(jsonResult),
            ResultStatus.Error => Microsoft.AspNetCore.Http.TypedResults.BadRequest(jsonResult),
            ResultStatus.Forbidden => Microsoft.AspNetCore.Http.TypedResults.Forbid(),
            ResultStatus.Unauthorized => Microsoft.AspNetCore.Http.TypedResults.Unauthorized(),
            ResultStatus.Invalid => Microsoft.AspNetCore.Http.TypedResults.BadRequest(jsonResult),
            ResultStatus.NotFound => Microsoft.AspNetCore.Http.TypedResults.NotFound(jsonResult),
            ResultStatus.Conflict => Microsoft.AspNetCore.Http.TypedResults.Conflict(jsonResult),
            _ => throw new NotImplementedException(),
        };
    }

    public static Microsoft.AspNetCore.Http.IResult ToHttpTypedResult<T>(
        this Result<T> result,
        Serilog.ILogger log,
        [CallerMemberName] string callerName = "",
        params object[] logArgs
    )
    {
        return result.ToHttpTypedResult(log, true, callerName, logArgs);
    }

    public static Microsoft.AspNetCore.Http.IResult ToHttpTypedResult(
        this Result result,
        Serilog.ILogger log,
        bool writeBody,
        [CallerMemberName] string callerName = "",
        params object[] logArgs
    )
    {
        var jsonResult = new HttpResult<object>
        {
            Data = null,
            IsSuccess = result.IsSuccess,
            Errors = result.Errors,
        };

        if (!result.IsSuccess)
        {
            log.Warning(
                "Request {@CallerName} with params {@Params} is NOT success: {@Errors}",
                callerName,
                logArgs.FirstOrDefault(),
                result.Errors
            );
        }
        else
        {
            log.Information(
                "Request {@CallerName} with params {@Params} is success",
                callerName,
                logArgs.FirstOrDefault()
            );
        }

        return result.Status switch
        {
            ResultStatus.Ok => Microsoft.AspNetCore.Http.TypedResults.Ok(jsonResult),
            ResultStatus.Error => Microsoft.AspNetCore.Http.TypedResults.BadRequest(jsonResult),
            ResultStatus.Forbidden => Microsoft.AspNetCore.Http.TypedResults.Forbid(),
            ResultStatus.Unauthorized => Microsoft.AspNetCore.Http.TypedResults.Unauthorized(),
            ResultStatus.Invalid => Microsoft.AspNetCore.Http.TypedResults.BadRequest(jsonResult),
            ResultStatus.NotFound => Microsoft.AspNetCore.Http.TypedResults.NotFound(jsonResult),
            ResultStatus.Conflict => Microsoft.AspNetCore.Http.TypedResults.Conflict(jsonResult),
            _ => throw new NotImplementedException(),
        };
    }
}
