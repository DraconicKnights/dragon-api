using MySql.Data.MySqlClient;

namespace DragonAPI.Server;

public abstract class DataBuilder
{
    public static void InsertData()
    {
        try
        {
            using var connection = new MySqlConnection(Core.Instance.ConnectionString);
            connection.Open();

            const string checkDataQuery = "SELECT * FROM valid_token_user LIMIT 1";
            using MySqlCommand checkDataCommand = new MySqlCommand(checkDataQuery, connection);
            using MySqlDataReader reader = checkDataCommand.ExecuteReader();

            if (!reader.HasRows)  // If there is no data
            {
                // Close the reader before inserting new data
                reader.Close();

                DateTime dateTime = DateTime.Now;
                string formatDate = dateTime.ToString("dd/MM/yyyy");
                string formatCreatedAt = dateTime.ToString("hh:mm:ss");

                const string insertQuery = "INSERT INTO valid_token_user (token_hash, valid_token_user_date, created_at) " +
                                           "VALUES (@ServerUsername, @ValidTokenUserCreationDate, @ValidTokenUserCreatedAt)";
                using MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@ServerUsername", Core.Instance.Configuration["Data:TokenHash"]);
                insertCommand.Parameters.AddWithValue("@ValidTokenUserCreationDate", formatDate);
                insertCommand.Parameters.AddWithValue("@ValidTokenUserCreatedAt", formatCreatedAt);

                insertCommand.ExecuteNonQuery();
            }
    
            connection.Close();
        }
        catch (Exception e)
        {
            Core.Instance.Logger.Log(LogLevel.Error, e.Message);
        }
    }
}