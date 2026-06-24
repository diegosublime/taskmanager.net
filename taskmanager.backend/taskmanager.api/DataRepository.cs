using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System.Text.Json;

namespace taskmanager.api
{
    public class DataRepository
    {
        private readonly IAmazonDynamoDB _amazonDynamoDB;

        public DataRepository(IAmazonDynamoDB dynamoDB)
        {
            _amazonDynamoDB = dynamoDB;
        }

        public async Task<string> GetTableName()
        {
            var tables = await _amazonDynamoDB.ListTablesAsync();
            return string.Join(",", tables.TableNames);
        }

        public async Task<ListTask?> GetListByIdAsync(string listId, CancellationToken cancellationToken)
        {
            var response = await _amazonDynamoDB.GetItemAsync(new GetItemRequest
            {
                TableName = "TaskLists",
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"LIST#{listId}" },
                    ["SK"] = new AttributeValue { S = $"METADATA" }
                }
            }, cancellationToken);

            var document = Document.FromAttributeMap(response.Item);
            var json = document.ToJson();

            var listTaskResult = JsonSerializer.Deserialize<ListTask>(json);

            return listTaskResult;
        }

        public async Task<List<ListTask>> GetListsByUserAsync(string userId, CancellationToken cancellationToken)
        {
            var response = await _amazonDynamoDB.QueryAsync(new QueryRequest
            {
                TableName = "TaskLists",
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)", // this filters during data gathering, very efficient cause it works with the indexes
                //FilterExpression = "Entity = :entity", this filters after the data is obtained not efficient and not recommended
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":skPrefix"] = new AttributeValue { S = $"LIST#" }
                }
            }, cancellationToken);

            var mappedResult = response.Items.Select(item =>
            {
                var document = Document.FromAttributeMap(item);
                return JsonSerializer.Deserialize<ListTask>(document.ToJson());
            })
            .OfType<ListTask>() // avoids null, taking only the non nullable types out of the list
            .ToList();

            return mappedResult;
        }
    }
}
