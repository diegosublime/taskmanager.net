using System.Net;

namespace taskmanager.api
{
    public class Result<T>
    {
        public bool IsSuccessful { get; private set; }
        public HttpStatusCode HttpCode { get; private set; }
        public string Message { get; private set; }
        public T? Value { get; private set; }
        public Error? Error { get; private set; }

        private Result(bool isSuccessful, HttpStatusCode httpCode, string message, T? value = default, Error? error = default)
        {
            IsSuccessful = isSuccessful;
            HttpCode = httpCode;
            Message = message;
            Value = value;
            Error = error;
        }

        public static Result<T> CreateSuccessfulResult(T? value = default, HttpStatusCode successCode = HttpStatusCode.OK)
        {
            return new Result<T>(true, successCode, "Operation success", value);
        }

        public static Result<T> CreateErrorResult(Error error)
        {
            return new Result<T>(false, error.Code, error.Description, error: error);
        }
    }

    public static class ResultMapper
    {
        public static IResult ToAPIResults<T>(this Result<T> result)
        {
            switch (result.HttpCode)
            {
                case HttpStatusCode.OK:
                    return Results.Ok(result.Value);

                case HttpStatusCode.Created:
                    return Results.Created();

                case HttpStatusCode.Accepted:
                    return Results.Accepted();

                case HttpStatusCode.NoContent:
                    return Results.NoContent();

                case HttpStatusCode.BadRequest:
                    return Results.Problem(
                            type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                            title: "Bad Request",
                            statusCode: (int)result.HttpCode,
                            extensions: new Dictionary<string, object?>
                            {
                                { "errors", new[]{ result.Message } }
                            });

                case HttpStatusCode.Unauthorized:
                    return Results.Unauthorized();

                case HttpStatusCode.Forbidden:
                    return Results.Forbid();

                case HttpStatusCode.NotFound:
                    return Results.Problem(
                            type: "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                            title: "Resource not Found",
                            statusCode: (int)result.HttpCode,
                            extensions: new Dictionary<string, object?>
                            {
                                { "errors", new[]{ result.Message } }
                            });

                case HttpStatusCode.Conflict:
                    return Results.Problem(
                            type: "conflict TBD",
                            title: "Resource in conflict",
                            statusCode: (int)result.HttpCode,
                            extensions: new Dictionary<string, object?>
                            {
                                { "errors", new[]{ result.Message } }
                            });

                case HttpStatusCode.InternalServerError:
                default:
                    return Results.InternalServerError();

            }
        }
    }

    public abstract class Error
    {
        public Error(string name, HttpStatusCode code, string description)
        {
            Name = name;
            Code = code;
            Description = description;
        }
        public string Name { get; private set; }
        public HttpStatusCode Code { get; private set; }
        public string Description { get; private set; }
    }

    public class TaskNotFoundError : Error
    {
        public TaskNotFoundError(string task) :
            base(nameof(TaskNotFoundError), HttpStatusCode.NotFound, $"Task: '{task}' was not found in the system.")
        {
        }
    }


}
