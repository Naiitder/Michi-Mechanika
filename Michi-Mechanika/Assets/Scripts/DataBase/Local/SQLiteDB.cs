using UnityEngine;
using Mono.Data.Sqlite;

public class SQLiteDB : MonoBehaviour
{
    public static SQLiteDB instance;
    public string dbName = "URI=file:DataBase.db";
        
    private void Awake()
    {        
        if (instance == null) 
            instance = this;
        else 
            Destroy(gameObject);
    }
        
    void Start()
    {
        CreateDatabase();
        
    } 
    private void CreateDatabase() 
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                string sqlcreation = "CREATE TABLE IF NOT EXISTS player_progress ("+
                                     "id INTEGER PRIMARY KEY AUTOINCREMENT,"+
                                     "current_chapter INTEGER NOT NULL DEFAULT 1,"+
                                     "current_level INTEGER NOT NULL DEFAULT 1,"+
                                     "completed_at TEXT NOT NULL DEFAULT (datetime('now'))"+
                                     ");";
                command.CommandText = sqlcreation;
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
            { command.CommandText = "INSERT OR REPLACE INTO player_progress (id, current_chapter, current_level) VALUES (1, @chapter, @level);";
                command.Parameters.AddWithValue("@chapter", chapter);
                command.Parameters.AddWithValue("@level", level);
                command.ExecuteNonQuery();
            }

            connection.Close();
        }
    }

}
