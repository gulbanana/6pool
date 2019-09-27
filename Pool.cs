using System;
using System.Linq;

class Pool
{
    public string TeamName { get; set; }
    public string[] ResourceNames { get; set; }
    public DateTime FirstDay { get; set; }
    public DateTime LastDay { get; set; }
    public Lead[] Leads { get; set; }

    public int TotalLeads => Leads.Count();
    public decimal TotalRevenue => Leads.Select(l => l.Revenue).Sum();
}