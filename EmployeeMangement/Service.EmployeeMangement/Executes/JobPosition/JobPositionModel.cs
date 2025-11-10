using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EmployeeMangement.Executes
{
    public class JobPositionModel
    {
        public class JobPositionResponse
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Keyword { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public int? Status { get; set; }

        }

        public class DeleteJobPositionRequest
        {
            public int? Id { get; set; }
        }

        public class JobPositionViewModel
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Keyword { get; set; }
            public string? Address { get; set; }
            public int? Status { get; set; }
            public int? CreateBy { get; set; }
            public int? UpdatedBy { get; set; }
        }

    }
}
