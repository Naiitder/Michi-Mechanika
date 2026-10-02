using UnityEngine;
using Mono.Data.Sqlite;

public class SQLiteDB : MonoBehaviour
{
    public static SQLiteDB instance;
    public string dbName = "URI=file:DataBase.db";
    
    [Header("Current Level")]
    public PlayerProgress playerProgress;
        
    private void Awake()
    {        
        if (instance == null) 
            instance = this;
        else 
            Destroy(gameObject);
        
        CreateDatabase();
        playerProgress = GetCurrentProgress();
    }
    private void CreateDatabase() 
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                string sqlcreation = "CREATE TABLE IF NOT EXISTS player_progress ("+
                                     "id INTEGER PRIMARY KEY,"+
                                     "current_chapter INTEGER NOT NULL DEFAULT 1,"+
                                     "current_level INTEGER NOT NULL DEFAULT 1,"+
                                     "completed_at TEXT NOT NULL DEFAULT (datetime('now'))"+
                                     ");";
                command.CommandText = sqlcreation;
                command.ExecuteNonQuery();
                
                command.CommandText = "INSERT OR IGNORE INTO player_progress (id, current_chapter, current_level) VALUES (1, 1, 1);";
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }
    
    public void SaveLevelCompleted(int chapter, int level)
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = 
                    "UPDATE player_progress SET current_chapter = @chapter, current_level = @level WHERE id = 1;";

                command.Parameters.AddWithValue("@chapter", chapter);
                command.Parameters.AddWithValue("@level", level);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

    private PlayerProgress GetCurrentProgress()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = 
                    "SELECT current_chapter, current_level FROM player_progress WHERE id = 1;";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int chapter = reader.GetInt32(0);
                        int level = reader.GetInt32(1);
                        return new PlayerProgress(chapter, level);
                    }
                }
            }

            connection.Close();
        }
        
        return new PlayerProgress(1, 1);
    }

}
