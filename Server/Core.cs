using MySql.Data.MySqlClient;

namespace DragonAPI.Server;

public class Core
{
    public static Core Instance { get; private set; }
    public ILogger Logger { get; }
    public IConfiguration Configuration { get; }
    public IServiceProvider ServiceProvider { get; }
    public string ConnectionString { get; }
    private NetworkValues NetworkValue { get; set; }
    private bool NetMonitor { get; set; }
    
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole().SetMinimumLevel(LogLevel.Information);
    });
    public Core(IConfiguration configuration, IServiceProvider serviceProvider, string connectionString)
    {
        Console.WriteLine("Setting core protocols");
        Configuration = configuration;
        ServiceProvider = serviceProvider;
        ConnectionString = connectionString;
        Logger = new Logger<Core>(_loggerFactory);
        NetMonitor = true;
        NetworkValue = NetworkValues.Low;
        Instance = this;
        TokenUserCheck();
    }
    
    private void TokenUserCheck()
    {
        Logger.Log(LogLevel.Information, "Checking valid token sets...");
    
        // Get values from appsettings.json
        var defaultUsername = Configuration["Data:TokenUser"];
        var defaultPassword = Configuration["Data:TokenPassword"];

        try
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            // Check if default user exists
            const string checkQuery = "SELECT COUNT(*) FROM valid_token_user WHERE Token_user = @Username AND Token_password = @Password";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@Username", defaultUsername);
            checkCommand.Parameters.AddWithValue("@Password", defaultPassword);

            var result = (long)checkCommand.ExecuteScalar();

            // Log the result
            if(result == 0)
            {
                Logger.Log(LogLevel.Warning, "Default token sets does not exist. Please check the configuration file.");
            }
            else
            {
                Logger.Log(LogLevel.Information, "Default token sets are present!");
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, e.Message);
        }
    }
    
    public void WriteExceptionToFile(Exception ex)
    {
        try
        {
            var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "error_log.txt");
            using var writer = new StreamWriter(logFilePath, true);
            writer.WriteLine("Date: " + DateTime.Now);
            writer.WriteLine("Exception Message: " + ex.Message);
            writer.WriteLine("Stack Trace: " + ex.StackTrace);
                
            if (ex.InnerException != null)
            {
                writer.WriteLine("Inner Exception Message: " + ex.InnerException.Message);
                writer.WriteLine("Inner Exception Stack Trace: " + ex.InnerException.StackTrace);
            }

            writer.WriteLine(new string('-', 80));
        }
        catch (Exception fileEx)
        {
            Instance.Logger.LogError(fileEx, "Failed to write exception details to the log file.");
        }
    }
     
    public static string MySqlStringBuilder(WebApplicationBuilder builder)
    {
        var stringBuilder = new MySqlConnectionStringBuilder
        {
            Server = builder.Configuration["Data:DataLocation"],
            Database = builder.Configuration["Data:DatabaseName"],
            UserID = builder.Configuration["Data:ServerUser"],
            Password = builder.Configuration["Data:ServerPassword"],
        };
        
        return stringBuilder.ConnectionString;
    }
    
}