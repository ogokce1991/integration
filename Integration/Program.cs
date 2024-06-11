using Integration.Common;
using Integration.Service;
using StackExchange.Redis;

namespace Integration;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Redis Connection and get Db
            var db = RedisConnectionManager.GetDatabase(5); // My Redis Db 5 is empty :)

            var service = new ItemIntegrationService(db);
        
            var tasks = new List<Task>
            {
                service.SaveItemAsync("Content A"),
                service.SaveItemAsync("Content B"),
                service.SaveItemAsync("Content A"), // Duplicate Test
                service.SaveItemAsync("Content C"),
                service.SaveItemAsync("Content B")  // Duplicate Test
            };

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var result = ((Task<Result>)task).Result;
                Console.WriteLine(result.Message);
            }

            var allItems = service.GetAllItems();
            Console.WriteLine("All Items in the Backend:");
            foreach (var item in allItems)
            {
                 Console.WriteLine($"Id: {item.Id}, Content: {item.Content}");
            }
            }
            finally
            {
                //Close Redis Connection
                RedisConnectionManager.CloseConnection();
            }
        
    }
}