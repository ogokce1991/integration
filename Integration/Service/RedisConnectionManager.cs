using StackExchange.Redis;
using System;

public class RedisConnectionManager
{
    private static Lazy<ConnectionMultiplexer> lazyConnection;

    static RedisConnectionManager()
    {
        var redisConfig = new ConfigurationOptions
        {
            EndPoints = { "host:port" },
            AbortOnConnectFail = false
        };

        lazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConfig));
    }

    public static ConnectionMultiplexer Connection => lazyConnection.Value;

    public static IDatabase GetDatabase(int db = 0) => Connection.GetDatabase(db);

    public static void CloseConnection()
    {
        if (lazyConnection.IsValueCreated)
        {
            Connection.Close();
        }
    }
}
