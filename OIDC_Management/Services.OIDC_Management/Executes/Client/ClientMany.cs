using DBContexts.OIDC_Management.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OIDC_Management.Executes
{
    public class ClientMany
    {
        private readonly oidcIdentityContext _context;
        public ClientMany(oidcIdentityContext context)
        {
            _context = context;
        }

    }
}
