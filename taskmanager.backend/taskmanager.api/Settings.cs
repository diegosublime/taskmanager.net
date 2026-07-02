namespace taskmanager.api
{
    public class AuthSettings
    {
        public const string KeyName = "Auth";

        public string ClientId { get; set; } = default!;
        public string Issuer { get; set; } = default!;
    }

    public class DBSettings
    {
        public const string KeyName = "Database";

        public string TableName { get; set; } = default!; 
    }

    public class SolaceSettings
    {
        public const string KeyName = "Solace";

        public string Host { get; set; } = default!;
        public string VPNName { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
