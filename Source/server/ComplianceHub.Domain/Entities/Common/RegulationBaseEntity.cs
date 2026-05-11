using System;
using System.Collections.Generic;
using System.Text;

namespace ComplianceHub.Domain.Entities.Common
{
    public abstract class RegulationBaseEntity : BaseEntity
    {
        public required string Identifier { get; set; }
    }
}
