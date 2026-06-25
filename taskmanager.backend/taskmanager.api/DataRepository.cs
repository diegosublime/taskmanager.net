using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace taskmanager.api
{
    public class DataRepository
    {
        private readonly IAmazonDynamoDB _amazonDynamoDB;
        private readonly DBSettings _dbSettings;

        public DataRepository(IAmazonDynamoDB dynamoDB, IOptions<DBSettings> optionsDbSettings)
        {
            _amazonDynamoDB = dynamoDB;
            _dbSettings = optionsDbSettings.Value;
        }

        private static string PkForListTaskByUserId(string userId) => $"USER#{userId}";
        private static string SkForListTaskByUserId(string listId) => $"LIST#{listId}";
        private static string PkForListTaskByListId(string listId) => $"LIST#{listId}";
        private static string SkForListTaskByListId() => $"METADATA";

        public async Task<string> GetTableName()
        {
            var tables = await _amazonDynamoDB.ListTablesAsync();
            return string.Join(",", tables.TableNames);
        }

        public async Task<ListTask?> GetListByIdAsync(string listId, CancellationToken cancellationToken)
        {
            var response = await _amazonDynamoDB.GetItemAsync(new GetItemRequest
            {
                TableName = _dbSettings.TableName,
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
                TableName = _dbSettings.TableName,
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

        public async Task<string> CreateListTask(ListTask newListTask, string userId, CancellationToken cancellationToken) 
        { 
            var dbListTaskByUser = newListTask.ToDBListTask(
                PkForListTaskByUserId(userId), 
                SkForListTaskByUserId(newListTask.Id)
                );

            var dbListTaskByList = newListTask.ToDBListTask(
                PkForListTaskByListId(newListTask.Id),
                SkForListTaskByListId()
                );

            var response = await _amazonDynamoDB.TransactWriteItemsAsync(new TransactWriteItemsRequest 
            {
                TransactItems = 
                [
                    new TransactWriteItem
                    {
                         Put = new Put
                         {
                             TableName = _dbSettings.TableName,
                             Item = dbListTaskByUser,
                             ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                         }
                    },
                    new TransactWriteItem
                    {
                        Put = new Put
                        {
                            TableName = _dbSettings.TableName,
                            Item = dbListTaskByList,
                            ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                        }
                    }
                ]
            }, cancellationToken);

            return newListTask.Id; 
        }
         
        public async Task<string> DeleteListTask(string listId, string userId, CancellationToken cancellationToken)
        { 
            var response = await _amazonDynamoDB.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems =
                [
                    new TransactWriteItem
                    {
                         Delete = new Delete
                         {
                             TableName = _dbSettings.TableName,
                             Key = new Dictionary<string, AttributeValue>
                             {
                                 ["PK"] = new AttributeValue { S = PkForListTaskByUserId(userId) },
                                 ["SK"] = new AttributeValue { S = SkForListTaskByUserId(listId) }
                             },
                             ConditionExpression = "attribute_exists(PK) AND attribute_exists(SK)"
                         }
                    },
                    new TransactWriteItem
                    {
                         Delete = new Delete
                         {
                             TableName = _dbSettings.TableName,
                             Key = new Dictionary<string, AttributeValue>
                             {
                                 ["PK"] = new AttributeValue { S = PkForListTaskByListId(listId) },
                                 ["SK"] = new AttributeValue { S = SkForListTaskByListId() }
                             },
                             ConditionExpression = "attribute_exists(PK) AND attribute_exists(SK)"
                         }
                    } 
                ]
            }, cancellationToken);

            return listId;
        } 
    }


    public static class DBMapper
    { 
        public static Dictionary<string, AttributeValue> ToDBListTask(this ListTask listTask, string pK, string sK) 
        {
            var dbListTask = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = pK },
                ["SK"] = new AttributeValue { S = sK },
                ["Description"] = new AttributeValue { S = listTask.Description },
                ["Entity"] = new AttributeValue { S = "List" },
                ["Name"] = new AttributeValue { S = listTask.Name },
                ["Purpose"] = new AttributeValue { S = listTask.Purpose },
                ["UserId"] = new AttributeValue { S = listTask.UserId } 
            };
            return dbListTask;
        }
         
    }
}
