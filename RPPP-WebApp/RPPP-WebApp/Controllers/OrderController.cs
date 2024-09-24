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

public class OrderController : Controller
{
  private readonly Rppp14Context ctx;
  private readonly ILogger<OrderController> logger;
  private readonly AppSettings appSettings;

  public OrderController(Rppp14Context ctx, IOptionsSnapshot<AppSettings> options, ILogger<OrderController> logger)
  {
    this.ctx = ctx;
    this.logger = logger;
    appSettings = options.Value;
  }
    // FUNCION INDEX
  public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
  {
    int pagesize = appSettings.PageSize;

    var query = ctx.Order
                   .Include(w => w.IdPersonNavigation)
                   .AsNoTracking();

    int count = query.Count();
    if (count == 0)
    {
      string message = "There is no order in the database";
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

        var order = await query
                            .Select(m => new Order2ViewModel
                            {
                                IdOrder = m.IdOrder,
                                IdHarvest = m.IdHarvest,
                                IdPerson = m.IdPerson,
                                Quantity = m.Quantity,
                                Price = m.Price,
                                DateOfOrder = m.DateOfOrder,
                                NamePerson = m.IdPersonNavigation.Name
                            })
                            .Skip((page - 1) * pagesize)
                            .Take(pagesize)
                           .ToListAsync();

        var model = new OrderViewModel
        {
            Order = order,
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
  public async Task<IActionResult> Create(Order order)
  {
    logger.LogTrace(JsonSerializer.Serialize(order));
    if (ModelState.IsValid)
    {
      try
      {
        ctx.Add(order);
        ctx.SaveChanges();
        string message = $"Order {order.IdOrder} added.";
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
                return View(order);
      }
    }
    else
    {
            await PrepareDropDownLists();
            return View(order);
    }
  }

    //DROPDOWN LIST

    private async System.Threading.Tasks.Task PrepareDropDownLists()
    {

        var harvest_id = await ctx.Harvest
                            .Where(d => d.IdHarvest == 4)
                            .Select(d => d.IdHarvest)
                            .FirstOrDefaultAsync();

        var harvest_id2 = await ctx.Harvest
                              .Where(d => d.IdHarvest != 4)
                              .OrderBy(d => d.IdHarvest)
                              .Select(d => d.IdHarvest)
                              .ToListAsync();

        if (harvest_id != null)
        {
            harvest_id2.Insert(0, harvest_id);
        }

        ViewBag.HarvestNumber = new SelectList(harvest_id2, harvest_id);

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
    }

    // aqui acaba index - create - prepare dropdown

    // aqui empieza delete, edit y get INLINE

    //ACCIÓN SHOW

    #region Methods for dynamic update and delete
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        ActionResponseMessage responseMessage;
        var order = await ctx.Order.FindAsync(id);
        if (order != null)
        {
            try
            {
                ctx.Remove(order);
                await ctx.SaveChangesAsync();
                responseMessage = new ActionResponseMessage(MessageType.Success, $"Order with id {id} has been deleted.");
            }
            catch (Exception exc)
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Error deleting order: {exc.CompleteExceptionMessage()}");
            }
        }
        else
        {
            responseMessage = new ActionResponseMessage(MessageType.Error, $"Order with id {id} does not exist.");
        }

        // Puedes ajustar el manejo de la respuesta HX según sea necesario

        return responseMessage.MessageType == MessageType.Success ?
          new EmptyResult() : await Get(id);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await ctx.Order
                              .AsNoTracking()
                              .Where(m => m.IdOrder == id)
                              .SingleOrDefaultAsync();
        if (order != null)
        {
            await PrepareDropDownLists();
            return PartialView(order);
        }
        else
        {
            return NotFound($"Invalid order id: {id}");
        }
    }

    [HttpPost]
   
    public async Task<IActionResult> Edit(Order order)
    {
        if (order == null)
        {
            return NotFound("No data submitted!?");
        }
        bool checkId = await ctx.Order.AnyAsync(m => m.IdOrder == order.IdOrder);
        if (!checkId)
        {
            return NotFound($"Invalid order id: {order?.IdOrder}");
        }

        if (ModelState.IsValid)
        {
            try
            {
                ctx.Update(order);
                await ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Get), new { id = order.IdOrder });
            }
            catch (Exception exc)
            {
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return PartialView(order);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return PartialView(order);
        }
    }

   
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var order = await ctx.Order
                            .Where(m => m.IdOrder == id)
                            .Select(m => new Order2ViewModel
                            {
                                IdOrder = m.IdOrder,
                                IdHarvest = m.IdHarvest,
                                IdPerson = m.IdPerson,
                                Quantity = m.Quantity,
                                Price = m.Price,
                                DateOfOrder = m.DateOfOrder,
                                NamePerson = m.IdPersonNavigation.Name
                            })
                            .SingleOrDefaultAsync();
        if (order != null)
        {
            return PartialView(order);
        }
        else
        {
            return NotFound($"Invalid order id: {id}");
        }
    }
    #endregion
}

