namespace Micser.Engine.DataAccess
{
    public class ConnectionString
    {
        public ConnectionString(string connectionString)
        {
            Value = connectionString;
        }

        public string Value { get; }

        public static implicit operator string(ConnectionString cs)
        {
            return cs.Value;
        }
    }
}