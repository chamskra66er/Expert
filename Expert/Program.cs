using System;
using System.Configuration;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace Expert
{
    internal class Program
    {
        static string inputPath = Environment.CurrentDirectory + "/input.txt";
        static string outputPath = Environment.CurrentDirectory + "/output.csv";
        static SqlConnection connection;
        static async Task Main(string[] args)
        {
            var tsRes = await ExecuteConnectAndCatch();
            if (!tsRes)
                return;

            var command = ReadFile();
            if (string.IsNullOrEmpty(command))
            {
                Console.WriteLine("файл input.txt пуст");
                Console.ReadLine();
                return;
            }

            await TaskExecuteAndCatch(command, connection);

            Console.ReadLine();
        }

        static string ReadFile()
        {
            if (!File.Exists(inputPath))
            {
                using FileStream fs = File.Create(inputPath);
                return string.Empty;
            }
            using StreamReader stream = new StreamReader(inputPath);
            return stream.ReadToEnd();
        }
        static void WriteFile(object model)
        {
            if (!File.Exists(outputPath))
            {
                using FileStream fs = File.Create(outputPath);
            }
            File.WriteAllText(outputPath, model.ToString());
        }
        static async Task TaskExecuteAndCatch(string command, SqlConnection connection)
        {
            Task readData = GetDataFromDb(command, connection);
            try
            {
                await readData;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Что-то пошло не так] " + "[Error] - " + ex.Message + " | [Stacktrace] - " + ex.StackTrace);
            }
        }
        static async Task GetDataFromDb(string command, SqlConnection connection)
        {
            SqlCommand sqlCommand = new SqlCommand(command, connection);
            SqlDataReader sqlReader = await sqlCommand.ExecuteReaderAsync();
            StringBuilder stringBuilder = new StringBuilder();

            if (sqlReader.HasRows)
            {
                var fieldNames = Enumerable.Range(0, sqlReader.FieldCount).Select(sqlReader.GetName);
                foreach (var item in fieldNames)
                    stringBuilder.Append(item + ";");
                stringBuilder.AppendLine();

                while (sqlReader.Read())
                {
                    for (int i = 0; i < sqlReader.FieldCount; i++)
                        if (sqlReader.GetValue(i) != DBNull.Value)
                        {
                            stringBuilder.Append(Convert.ToString(sqlReader.GetValue(i)) + ";");
                        }
                    stringBuilder.AppendLine();
                }
            }
            WriteFile(stringBuilder);
            Console.WriteLine("запись в output.csv завершена");
        }
        static async Task<bool> ExecuteConnectAndCatch()
        {
            Task<bool> connect = Connect();
            try
            {
                var res = await connect;
                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Что-то пошло не так] " + "[Error] - " + ex.Message + " | [Stacktrace] - " + ex.StackTrace);
                return false;
            }
        }
        static async Task<bool> Connect()
        {
            string connectString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string fixedConnectionString = connectString.Replace("Expert.mdf", AppDomain.CurrentDomain.BaseDirectory + @"Expert.mdf");
            connection = new SqlConnection(fixedConnectionString);
            await connection.OpenAsync();
            if (connection.State == ConnectionState.Closed)
            {
                Console.WriteLine("Статус соединения бд: " + connection.State);
                return false;
            }
            return true;
        }
    }
}
