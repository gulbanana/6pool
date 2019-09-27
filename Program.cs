using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        var options = new JsonSerializerOptions 
        { 
            Converters =
            {
                new LaxDateTimeConverter()
            }
        };

        if (!File.Exists("data/config.json")) Error("Missing configuration file data/config.json");
        var configJson = File.ReadAllText("data/config.json");
        var configData = JsonSerializer.Deserialize<ConfigData>(configJson, options);

        if (!File.Exists("data/teams.json")) Error("Missing input file data/teams.json");
        var teamsJson = File.ReadAllText("data/teams.json");
        var teamsData = JsonSerializer.Deserialize<TeamsData>(teamsJson, options);
        
        var startDate = teamsData.Start.Date;
        var endDate = teamsData.End.Date;
        var revenue = teamsData.Resources;

        if (endDate <= startDate) Error("Start date must be before end date.");

        Message($"Calculating pools from {startDate.ToShortDateString()} to {endDate.ToShortDateString()} inclusive");
        var teams = revenue.Values.SelectMany(r => r.Keys).ToArray();

        Message($"Processing {revenue.Count} resources across {teams.Length} teams");
        Message(string.Empty);

        // sanity-check
        foreach (var resource in revenue)
        {
            foreach (var assignment in resource.Value)
            {
                foreach (var leadGroup in assignment.Value.Leads.Parse())
                {
                    if (leadGroup.Key < assignment.Value.Start || leadGroup.Key > assignment.Value.End)
                    {
                        Warning($"{resource.Key} booked lead for {assignment.Key} on {leadGroup.Key}, but was not a team member at that time");
                    }
                }
            }
        }

        // build pool memberships
        var pools = new List<Pool>();
        for (var today = startDate; today <= endDate; today = today.AddDays(1))
        {
            foreach (var team in teams)
            {
                var lastPool = pools.Where(p => p.TeamName == team).LastOrDefault();
                var members = (from resource in revenue
                               let resourceName = resource.Key
                               from assignment in resource.Value
                               where assignment.Key == team
                               from date in DateRange(assignment.Value.Start, assignment.Value.End)
                               where date == today
                               select resourceName).ToArray();

                if (lastPool == null && members.Any())
                {
                    lastPool = new Pool
                    {
                        TeamName = team,
                        ResourceNames = members,
                        FirstDay = today
                    };
                    pools.Add(lastPool);
                }
                else if (lastPool != null && members.Any() && !members.SequenceEqual(lastPool.ResourceNames))
                {
                    lastPool = new Pool
                    {
                        TeamName = team,
                        ResourceNames = members,
                        FirstDay = today
                    };
                    pools.Add(lastPool);
                }

                if (lastPool != null)
                {
                    lastPool.LastDay = today;
                }
            }
        }

        // create a flattened view of revenue events for the period
        var leads = (from resource in revenue
                     let resourceName = resource.Key
                     from assignment in resource.Value
                     where assignment.Value.Start >= startDate || assignment.Value.End <= endDate
                     let teamName = assignment.Key
                     from sales in assignment.Value.Leads.Parse()
                     let date = sales.Key
                     where date >= startDate && date <= endDate
                     from amount in sales.Value
                     select new Lead
                     {
                         TeamName = teamName,
                         ResourceName = resourceName,
                         Date = date,
                         Revenue = amount
                     }).ToList();                    

        // sort the leads into pools
        foreach (var pool in pools)
        {
            pool.Leads = leads.Where(l => l.TeamName == pool.TeamName && l.Date >= pool.FirstDay && l.Date <= pool.LastDay).ToArray();
        }

        foreach (var pool in pools.OrderBy(p => p.TeamName).ThenBy(p => p.FirstDay))
        {
            if (pool.FirstDay == pool.LastDay)
            {
                Headline($"Pool: {pool.TeamName} {pool.FirstDay.ToShortDateString()}");
            }
            else
            {
                Headline($"Pool: {pool.TeamName} {pool.FirstDay.ToShortDateString()}-{pool.LastDay.ToShortDateString()}");
            }
            Message(string.Join(", ", pool.ResourceNames.Select(r => $"{r} ({pool.Leads.Where(l => l.ResourceName == r).Count()})")));
            Message($"{pool.TotalLeads} leads, {Format.Money(pool.TotalRevenue)} revenue");
            Message(string.Empty);
        }

        Headline("All leads (debug)");
        foreach (var lead in leads.OrderBy(l => l.Date))
        {
            Message($"{lead.Date.ToShortDateString(),10} {lead.TeamName,10} {lead.ResourceName,10} {Format.Money(lead.Revenue),10}");
        }
    }

    private static IEnumerable<DateTime> DateRange(DateTime start, DateTime end)
    {
        return Enumerable.Range(0, 1 + end.Subtract(start).Days).Select(offset => start.AddDays(offset));
    }

    static void Message(string message)
    {
        Console.Out.WriteLine(message);
    }

    static void Headline(string message)
    {
        Console.Out.WriteLine(message);
        Console.Out.WriteLine(new string(Enumerable.Repeat('-', message.Length).ToArray()));
    }

    static void Warning(string message)
    {
        Console.Error.WriteLine("Warning: " + message);
    }

    static void Error(string message)
    {
        Console.WriteLine("Error: " + message);
        Environment.Exit(0);
    }
}
