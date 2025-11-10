using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EmployeeMangement.Executes
{
    public class AccountModel
    {
        public class AccountRequest
        {
            public string Email { get; set; }
            public string PasswordHash { get; set; }
        }


        public class AccountApiRespone
        {
            public int Id { get; set; }
            public string Email { get; set; }
            public string PasswordHash { get; set; }
        }
    }
}
