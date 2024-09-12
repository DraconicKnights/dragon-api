using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;

namespace DragonAPI.Server;

public class Authentication()
{
    public static string GenerateToken(string hash, string nodeName)
    {
        // Check if the credentials are valid
        // Return null if the credentials are invalid
        Core.Instance.Logger.Log(LogLevel.Information, "Checking to see if user is valid");
        if (!IsValidUser(hash))
        {
            Core.Instance.Logger.Log(LogLevel.Warning, $"Authentication: User failed to validate and has been sent a null response.");
            return null!;
        }
        
        StoreTokenUser(nodeName);
        
        Core.Instance.Logger.Log(LogLevel.Information, "User passed validation check");
        
        Core.Instance.Logger.Log(LogLevel.Information, "Setting token claims");
        // Create the claims
        var claims = new[]
        {
            new Claim(ClaimTypes.Hash, hash),
            new Claim(ClaimTypes.Role, "Admin")
        };

        Core.Instance.Logger.Log(LogLevel.Information, "Generating new API Token");
        // Create the key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Core.Instance.Configuration["Jwt_Auth:Key"] ?? string.Empty));

        // Create the credentials
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create the token
        var token = new JwtSecurityToken(
            issuer: Core.Instance.Configuration["Jwt_Auth:Issuer"],
            audience: Core.Instance.Configuration["Jwt_Auth:Issuer"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials);

        // Return the token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private static bool IsValidUser(string hash)
    {
        try
        {
            using var connection = new MySqlConnection(Core.Instance.ConnectionString);
            connection.Open();
            
            const string query = "SELECT COUNT(*) FROM valid_token_user WHERE token_hash = @Hash";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Hash", hash);

            var result = (long)command.ExecuteScalar();
            
            connection.Close();
            return result == 1;
        }
        catch (Exception e)
        {
            Core.Instance.Logger.Log(LogLevel.Error, e.Message);
            return false;
        }
        
    }

    private static void StoreTokenUser(string nodeName)
    {
        try
        {
            DateTime dateTime = DateTime.Now;
            DateTime createdAt = DateTime.Now;

            string formatDate = dateTime.ToString("dd/MM/yyyy");
            string formatCreatedAt = createdAt.ToString("hh:mm:ss");
            
            using var connection = new MySqlConnection(Core.Instance.ConnectionString);
            connection.Open();
            
            const string insertQuery = "INSERT INTO token (node, token_creation_date, token_created_at) " +
                                       "VALUES (@SessionNode, @DateTimeRequest, @TokenCreatedAt)";

            using MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@SessionNode", nodeName);
            insertCommand.Parameters.AddWithValue("@DateTimeRequest", formatDate);
            insertCommand.Parameters.AddWithValue("@TokenCreatedAt", formatCreatedAt);
            
            insertCommand.ExecuteNonQuery();
            
            connection.Close();
            
            Core.Instance.Logger.Log(LogLevel.Information, "A new Token has be successfully created and stored");
        }
        catch (Exception e)
        {
            Core.Instance.Logger.Log(LogLevel.Error, e.Message);
        }
    }
    
}