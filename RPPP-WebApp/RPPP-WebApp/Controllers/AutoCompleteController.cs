using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RPPP_WebApp.Models;
using RPPP_WebApp.ViewModels;

namespace RPPP_WebApp.Controllers;

public class AutoCompleteController : Controller
{
    private readonly Rppp14Context ctx;
    private readonly AppSettings appData;

    public AutoCompleteController(Rppp14Context ctx, IOptionsSnapshot<AppSettings> options)
    {
        this.ctx = ctx;
        appData = options.Value;
    }

    // ID CROP
    public async Task<List<IdLabel>> Harvest(string term)
    {
        var query = ctx.Harvest
                        .Select(c => new IdLabel
                        {
                            Id = c.IdHarvest,
                            Label = c.IdCrop
                        })
                        .Where(l => l.Label.Contains(term));

        var list = await query.OrderBy(l => l.Label)
                              .ThenBy(l => l.Id)
                              .Take(appData.AutoCompleteCount)
                              .ToListAsync();
        return list;
    }

    // ID PERSON
    public async Task<List<IdLabel>> Worker(string term)
    {
        var query = ctx.Workers
                        .Select(c => new IdLabel
                        {
                            Id = c.IdPerson,
                            Label = c.IdPerson + " " + c.IdPersonNavigation?.Name
                        })
                        .Where(l => l.Label.Contains(term));

    var list = await query.OrderBy(l => l.Label)
                          .ThenBy(l => l.Id)
                          .Take(appData.AutoCompleteCount)
                          .ToListAsync();
        return list;
    }
}
