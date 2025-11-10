using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EmployeeMangement.Executes
{
    public class DepartmentModel
    {
        public class DepartmentResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;

            public int? Status { get; set; }
            public string ManagerName { get; set; }
            public int EmployeeCount { get; set; }


        }

        public class DeleteDepartmentRequest
        {
            public int? Id { get; set; }
        }
        public class DepartmentViewModel
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Keyword { get; set; }
            public int? ManagerId { get; set; }
            public int? JobPositionId { get; set; }
            public int? Status { get; set; }
            public int? CreateBy { get; set; }
            public int? UpdatedBy { get; set; }

        }


    }
}
