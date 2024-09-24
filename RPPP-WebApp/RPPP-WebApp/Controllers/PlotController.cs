using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Model;
using RPPP_WebApp.ViewModels;
using System.Text.Json;

namespace RPPP_WebApp.Controllers;

public class PlotController : Controller
{
  private readonly Rppp14Context ctx;
  private readonly ILogger<PlotController> logger;
  private readonly AppSettings appSettings;

  public PlotController(Rppp14Context ctx, IOptionsSnapshot<AppSettings> options, ILogger<PlotController> logger)
  {
    this.ctx = ctx;
    this.logger = logger;
    appSettings = options.Value;
  }

    // FUNCION INDEX
    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int pagesize = appSettings.PageSize;

        var query = ctx.Plot
                       .Include(w => w.IdPersonNavigation)
                       .AsNoTracking();

        int count = query.Count();
        if (count == 0)
        {
            string message = "There is no plot in the database";
            logger.LogInformation(message);
            TempData[Constants.Message] = "message";
            TempData[Constants.ErrorOccurred] = false;
            return RedirectToAction(nameof(Index));
        }

        var pagingInfo = new PagingInfo
        {
            CurrentPage = page,
            Sort = sort,
            Ascending = ascending,
            ItemsPerPage = pagesize,
            TotalItems = count
        };
        if (page < 1 || page > pagingInfo.TotalPages)
        {
            return RedirectToAction(nameof(Index), new { page = 1, sort, ascending });
        }

        query = query.ApplySort(sort, ascending);

        var plot = await query
                            .Select(m => new Plot2ViewModel
                            {
                                IdPlot = m.IdPlot,
                                IdPerson = m.IdPerson,
                                IdCrop = m.IdCrop,
                                CommonName = m.CommonName,
                                IdSoilQuality = m.IdSoilQuality,
                                IdSoilCategory = m.IdSoilCategory,
                                IdInfrastructure = m.IdInfrastructure,
                                NamePerson = m.IdPersonNavigation.Name,
                                Size = m.Size,
                                Gpslocation = m.Gpslocation,
                                NameSoilCategory = m.IdSoilCategoryNavigation.CategoryName,
                                NameSoilQuality = m.IdSoilQualityNavigation.Quality,
                                NameInfrastructure = m.IdInfrastructureNavigation.TypeMaterial
                            })
                            .Skip((page - 1) * pagesize)
                            .Take(pagesize)
                           .ToListAsync();

        var model = new PlotViewModel
        {
            Plot = plot,
            PagingInfo = pagingInfo
        };

        return View(model);
    }

    //FUNCION CREATE

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareDropDownLists();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Plot plot)
    {
        logger.LogTrace(JsonSerializer.Serialize(plot));
        if (ModelState.IsValid)
        {
            try
            {
                ctx.Add(plot);
                ctx.SaveChanges();
                string message = $"Order {plot.IdPlot} added.";
                logger.LogInformation(new EventId(1000), message);

                TempData[Constants.Message] = message;
                TempData[Constants.ErrorOccurred] = false;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exc)
            {
                logger.LogError("Error adding new order: {0}", exc.CompleteExceptionMessage());
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return View(plot);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return View(plot);
        }
    }
    //DROPDOWN LIST
    private async System.Threading.Tasks.Task PrepareDropDownLists()
    {
        var crop_id = await ctx.Crop
                          .Where(d => d.IdCrop == 3)
                          .Select(d => d.IdCrop)
                          .FirstOrDefaultAsync();

        var crop_id2 = await ctx.Crop
                              .Where(d => d.IdCrop != 3)
                              .OrderBy(d => d.IdCrop)
                              .Select(d => d.IdCrop)
                              .ToListAsync();

        if (crop_id != null)
        {
            crop_id2.Insert(0, crop_id);
        }

        ViewBag.CropName = new SelectList(crop_id2, crop_id);


        var pn = await ctx.Person
                          .Where(d => d.IdPerson == 3)
                          .Select(d => new { d.Name, d.IdPerson })
                          .FirstOrDefaultAsync();
        var personName = await ctx.Person
                              .Where(d => d.IdPerson != 3)
                              .OrderBy(d => d.Name)
                              .Select(d => new { d.Name, d.IdPerson })
                              .ToListAsync();
        if (pn != null)
        {
            personName.Insert(0, pn);
        }
        ViewBag.PersonName = new SelectList(personName, nameof(pn.IdPerson), nameof(pn.Name));



        var sq = await ctx.SoilQuality
                          .Where(d => d.IdSoilQuality == 1)
                          .Select(d => new { d.Quality, d.IdSoilQuality })
                          .FirstOrDefaultAsync();
        var soilQname = await ctx.SoilQuality
                              .Where(d => d.IdSoilQuality != 1)
                              .OrderBy(d => d.Quality)
                              .Select(d => new { d.Quality, d.IdSoilQuality })
                              .ToListAsync();
        if (sq != null)
        {
            soilQname.Insert(0, sq);
        }
        ViewBag.SoilQName = new SelectList(soilQname, nameof(sq.IdSoilQuality), nameof(sq.Quality));

        var sc = await ctx.SoilCategory
                          .Where(d => d.IdSoilCategory == 1)
                          .Select(d => new { d.CategoryName, d.IdSoilCategory })
                          .FirstOrDefaultAsync();
        var soilCname = await ctx.SoilCategory
                              .Where(d => d.IdSoilCategory != 1)
                              .OrderBy(d => d.CategoryName)
                              .Select(d => new { d.CategoryName, d.IdSoilCategory })
                              .ToListAsync();
        if (sc != null)
        {
            soilCname.Insert(0, sc);
        }
        ViewBag.SoilCName = new SelectList(soilCname, nameof(sc.IdSoilCategory), nameof(sc.CategoryName));

        var inf = await ctx.Infrastructure
                          .Where(d => d.IdInfrastructure == 1)
                          .Select(d => new { d.TypeMaterial, d.IdInfrastructure })
                          .FirstOrDefaultAsync();
        var infName = await ctx.Infrastructure
                              .Where(d => d.IdInfrastructure != 1)
                              .OrderBy(d => d.TypeMaterial)
                              .Select(d => new { d.TypeMaterial, d.IdInfrastructure })
                              .ToListAsync();
        if (inf != null)
        {
            infName.Insert(0, inf);
        }
        ViewBag.InfName = new SelectList(infName, nameof(inf.IdInfrastructure), nameof(inf.TypeMaterial));
    }
    // aqui acaba index - create - prepare dropdown

    // aqui empieza delete, edit y get INLINE
    #region Methods for dynamic update and delete
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        ActionResponseMessage responseMessage;
        var plot = await ctx.Plot.FindAsync(id);
        if (plot != null)
        {
            try
            {
                ctx.Remove(plot);
                await ctx.SaveChangesAsync();
                responseMessage = new ActionResponseMessage(MessageType.Success, $"Plot with id {id} has been deleted.");
            }
            catch (Exception exc)
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Error deleting plot: {exc.CompleteExceptionMessage()}");
            }
        }
        else
        {
            responseMessage = new ActionResponseMessage(MessageType.Error, $"Plot with id {id} does not exist.");
        }

        // Puedes ajustar el manejo de la respuesta HX según sea necesario

        return responseMessage.MessageType == MessageType.Success ?
          new EmptyResult() : await Get(id);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var plot = await ctx.Plot
                              .AsNoTracking()
                              .Where(m => m.IdPlot == id)
                              .SingleOrDefaultAsync();
        if (plot != null)
        {
            await PrepareDropDownLists();
            return PartialView(plot);
        }
        else
        {
            return NotFound($"Invalid plot id: {id}");
        }
    }

    [HttpPost]

    public async Task<IActionResult> Edit(Plot plot)
    {
        if (plot == null)
        {
            return NotFound("No data submitted!?");
        }
        bool checkId = await ctx.Plot.AnyAsync(m => m.IdPlot == plot.IdPlot);
        if (!checkId)
        {
            return NotFound($"Invalid plot id: {plot?.IdPlot}");
        }

        if (ModelState.IsValid)
        {
            try
            {
                ctx.Update(plot);
                await ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Get), new { id = plot.IdPlot });
            }
            catch (Exception exc)
            {
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return PartialView(plot);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return PartialView(plot);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var plot = await ctx.Plot
                            .Where(m => m.IdPlot == id)
                            .Select(m => new Plot2ViewModel
                            {
                                IdPlot = m.IdPlot,
                                IdPerson = m.IdPerson,
                                IdCrop = m.IdCrop,
                                CommonName = m.CommonName,
                                IdSoilQuality = m.IdSoilQuality,
                                IdSoilCategory = m.IdSoilCategory,
                                IdInfrastructure = m.IdInfrastructure,
                                NamePerson = m.IdPersonNavigation.Name,
                                Size = m.Size,
                                Gpslocation = m.Gpslocation,
                                NameSoilCategory = m.IdSoilCategoryNavigation.CategoryName,
                                NameSoilQuality = m.IdSoilQualityNavigation.Quality,
                                NameInfrastructure = m.IdInfrastructureNavigation.TypeMaterial
                            })
                            .SingleOrDefaultAsync();
        if (plot != null)
        {
            return PartialView(plot);
        }
        else
        {
            return NotFound($"Invalid plot id: {id}");
        }
    }
    #endregion
}
