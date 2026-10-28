namespace LeaveFlow.Domain.Constants
{
    public static class RoleNames
    {
        public const string Admin = "Admin";

        public const string Manager = "Manager";

        public const string Employee = "Employee";

        public const string ManagerOrAdmin = Manager + "," + Admin;
    }
}
