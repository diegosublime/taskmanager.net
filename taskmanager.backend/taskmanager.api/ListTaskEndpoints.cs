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

            app.MapPost("api/lists", async (ListTask newListTask,DataRepository dataRepository,CancellationToken cancellationToken) => 
            {
                var listResponse = await dataRepository.CreateListTask(newListTask, newListTask.UserId, cancellationToken);
                return Results.Created();
            })
            .WithName("CreateList");
            //.RequireAuthorization("ValidateAudiencePolicy");;

            app.MapDelete("api/Lists/{listId}",async(string listId,string userId, DataRepository dataRepository,CancellationToken cancellationToken) => 
            {
                var response = await dataRepository.DeleteListTask(listId, userId, cancellationToken);
                return Results.NoContent();
            });
        }
    }
}
