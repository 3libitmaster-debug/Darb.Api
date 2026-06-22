namespace Darb.Api.Models.Enums
{

    public enum AccountRoles
    {
        Admin = 0,
        Company = 1,
        Customer = 2,
        Passenger = Customer // legacy stored value support for existing users in the database
    }
}
