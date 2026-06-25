using Amazon.DynamoDBv2.Model.Internal.MarshallTransformations;
using System.Net;

namespace taskmanager.api
{
    public class TaskService
    {
        private readonly DataRepository _dataRepository;

        public TaskService(DataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }

        public async Task<Result<string>> DeleteListTask(string listTaskId, string userId, CancellationToken cancellationToken) 
        {
            var deleteListTaskResponse = await _dataRepository.DeleteListTask(listTaskId, userId, cancellationToken);

            if (deleteListTaskResponse is null) 
            {
                return Result<string>.CreateErrorResult(new TaskNotFoundError(listTaskId));
            }

            return Result<string>.CreateSuccessfulResult(listTaskId, HttpStatusCode.NoContent);
        }
    }
}
