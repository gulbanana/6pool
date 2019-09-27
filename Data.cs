using System;
using System.Collections.Generic;
using System.Linq;

class ConfigData
{
    public decimal CommissionRate { get; set; }
    public decimal LeadsBonus { get; set; }
}

class TeamsData
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public ResourcesData Resources { get; set; }
}

class ResourcesData : Dictionary<string, ResourceData> { }

class ResourceData : Dictionary<string, AssignmentData> { }

class AssignmentData
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public RevenueData Leads { get; set; }
}

class RevenueData : Dictionary<string, decimal[]> 
{ 
    public Dictionary<DateTime, decimal[]> Parse()
    {
        return this.ToDictionary(kvp => DateTime.Parse(kvp.Key), kvp => kvp.Value);
    }
}