using System.Data;

namespace taskmanager.api
{
    public static class ListTaskEndpoints
    {
        public static void UseListTaskEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("api/lists", async (string userId, DataRepository dataRepository, CancellationToken cancellationToken) =>
            {
                var listResponse = await dataRepository.GetListsByUserAsync(userId, cancellationToken);

                return Results.Ok(listResponse);
            })
            .WithName("GetListsbyUserId");
            //.RequireAuthorization("ValidateAudiencePolicy");

            app.MapGet("api/lists/{listId}", async (string listId, DataRepository dataRepository, CancellationToken cancellationToken) =>
            {
                var listResponse = await dataRepository.GetListByIdAsync(listId, cancellationToken);

                if (listResponse == null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(listResponse);
            })
            .WithName("GetListById");
            //.RequireAuthorization("ValidateAudiencePolicy");

            app.MapGet("api/testdynamo", async (DataRepository dataRepository) =>
            {
                var tableNames = await dataRepository.GetTableName();
                return tableNames;
            });

            app.MapPost("api/lists", async (CreateListTaskAPIRequest listTaskRequest,DataRepository dataRepository,CancellationToken cancellationToken) => 
            {
                //TODO: Add to task service, or Usecase class
                ListTask newListTask = ListTask.Create(
                    listTaskRequest.Id,
                    listTaskRequest.UserId,
                    listTaskRequest.Name,
                    listTaskRequest.Description,
                    listTaskRequest.Purpose);

                var listResponse = await dataRepository.CreateListTask(newListTask, newListTask.UserId, cancellationToken);
                
                //Dispatch domain events (pending to deside custom implementation vs MediatR)

                return Results.Created();
            })
            .WithName("CreateList");
            //.RequireAuthorization("ValidateAudiencePolicy");

            app.MapDelete("api/Lists/{listId}",async(string listId,string userId, TaskService taskService ,CancellationToken cancellationToken) => 
            {
                var deleteListResult = await taskService.DeleteListTask(listId, userId, cancellationToken);
                
                return deleteListResult.ToAPIResults();
            });
        }
    }

    public record CreateListTaskAPIRequest(string Id, string UserId, string Name, string Description, string Purpose)
    { 
    }
}
