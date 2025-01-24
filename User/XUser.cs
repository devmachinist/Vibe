using System.Dynamic;
using System.Security.Principal;

namespace Vibe
{
    public class XUser : DynamicObject, IPrincipal
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string Provider { get; set; } = "Default";
        public dynamic Claims { get; set; } = new ExpandoObject();
        public State? State { get; set; }
        public IIdentity Identity { get; private set; }
        public CsxDocument? Document { get; set; }

        public XUser(string? id = null)
        {
            Id = id?? Guid.NewGuid().ToString();
            Name = "";
            Identity = new GenericIdentity(Name);
        }
        public XUser()
        {
            Id = Guid.NewGuid().ToString();
            Name = "";
            Identity = new GenericIdentity(Name);
        }
        public bool IsInRole(string role)
        {
            var claims = Claims;
            claims.Roles = claims.Roles ?? new List<string>();

            return claims.Roles.Contains(role);
        }
    }
}
