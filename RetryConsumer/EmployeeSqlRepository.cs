using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Consumer
{
    internal class EmployeeSqlRepository : IEmployeeSqlRepository
    {
        private readonly IConfiguration _configuration;

        public EmployeeSqlRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool UpdateEmployeeName(int id, string employeeName, DateTime timestamp)
        {
            const string sql = @"UPDATE [Employees] SET FirstName = @name, SyncTime = @curTime 
                WHERE SyncTime <= @msgTime AND EmployeeID = @empId;";

            try 
            {
                using (var conn = new SqlConnection(_configuration["SqlConnStr"]))
                {
                    var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.Add("@name", SqlDbType.VarChar);
                    cmd.Parameters["@name"].Value = employeeName;

                    cmd.Parameters.Add("@curTime", SqlDbType.DateTime);
                    cmd.Parameters["@curTime"].Value = DateTime.UtcNow;

                    cmd.Parameters.Add("@msgTime", SqlDbType.DateTime);
                    cmd.Parameters["@msgTime"].Value = timestamp;

                    cmd.Parameters.Add("@empId", SqlDbType.Int);
                    cmd.Parameters["@empId"].Value = id;
                    
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                // notify failure for SQL server update fail,
                // to send to the next Kafka topic
                return false;
            }
        }
    }
}