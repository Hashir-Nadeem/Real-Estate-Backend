namespace Real_Estate_WebAPI.Settings
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string UsersCollection { get; set; }

        public string PropertiesCollection { get; set; }
    }

}
