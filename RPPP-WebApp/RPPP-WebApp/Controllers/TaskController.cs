using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Model;
using RPPP_WebApp.ViewModels;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPPP_WebApp.Controllers;

public class TaskController : Controller
{
    private readonly Rppp14Context ctx;
    private readonly ILogger<TaskController> logger;
    private readonly AppSettings appSettings;

    public TaskController(Rppp14Context ctx, IOptionsSnapshot<AppSettings> options, ILogger<TaskController> logger)
    {
        this.ctx = ctx;
        this.logger = logger;
        appSettings = options.Value;
    }

    // FUNCION INDEX
    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int pagesize = appSettings.PageSize;

        var query = ctx.Task
                       .Include(w => w.IdPersonNavigation)
                       .AsNoTracking();

        int count = query.Count();
        if (count == 0)
        {
            string message = "There is no task in the database";
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

        var task = await query
                            .Select(m => new Task2ViewModel
                            {
                                IdTask = m.IdTask,
                                Task1 = m.Task1,
                                IdTaskStatus = m.IdTaskStatus,
                                IdPerson = m.IdPerson,
                                NamePerson = m.IdPersonNavigation.IdPersonNavigation.Name,
                                Status = m.IdTaskStatusNavigation.Status,
                                
                            })
                            .Skip((page - 1) * pagesize)
                            .Take(pagesize)
                           .ToListAsync();

        var model = new TaskViewModel
        {
            Task = task,
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
    public async Task<IActionResult> Create(Tasks task)
    {
        logger.LogTrace(JsonSerializer.Serialize(task));
        if (ModelState.IsValid)
        {
            try
            {
                ctx.Add(task);
                ctx.SaveChanges();
                string message = $"Task {task.Task1} added.";
                logger.LogInformation(new EventId(1000), message);

                TempData[Constants.Message] = message;
                TempData[Constants.ErrorOccurred] = false;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exc)
            {
                logger.LogError("Error adding new task: {0}", exc.CompleteExceptionMessage());
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return View(task);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return View(task);
        }
    }

    //DROPDOWN LIST
    private async System.Threading.Tasks.Task PrepareDropDownLists()
    {

        var st = await ctx.TaskStatus
                          .Where(d => d.IdTaskStatus == 1)
                          .Select(d => new { d.Status, d.IdTaskStatus })
                          .FirstOrDefaultAsync();
        var statusName = await ctx.TaskStatus
                              .Where(d => d.IdTaskStatus != 1)
                              .OrderBy(d => d.Status)
                              .Select(d => new { d.Status, d.IdTaskStatus })
                              .ToListAsync();
        if (st != null)
        {
            statusName.Insert(0, st);
        }
        ViewBag.StatusTaskName = new SelectList(statusName, nameof(st.IdTaskStatus), nameof(st.Status));

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

    #region Methods for dynamic update and delete

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        ActionResponseMessage responseMessage;
        var order = await ctx.Task.FindAsync(id);
        if (order != null)
        {
            try
            {
                ctx.Remove(order);
                await ctx.SaveChangesAsync();
                responseMessage = new ActionResponseMessage(MessageType.Success, $"Task with id {id} has been deleted.");
            }
            catch (Exception exc)
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Error deleting task: {exc.CompleteExceptionMessage()}");
            }
        }
        else
        {
            responseMessage = new ActionResponseMessage(MessageType.Error, $"Task with id {id} does not exist.");
        }

        // Puedes ajustar el manejo de la respuesta HX según sea necesario

        return responseMessage.MessageType == MessageType.Success ?
          new EmptyResult() : await Get(id);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var task = await ctx.Task
                              .AsNoTracking()
                              .Where(m => m.IdTask == id)
                              .SingleOrDefaultAsync();
        if (task != null)
        {
            await PrepareDropDownLists();
            return PartialView(task);
        }
        else
        {
            return NotFound($"Invalid task id: {id}");
        }
    }

    [HttpPost]

    public async Task<IActionResult> Edit(Tasks task)
    {
        if (task == null)
        {
            return NotFound("No data submitted!?");
        }
        bool checkId = await ctx.Task.AnyAsync(m => m.IdTask == task.IdTask);
        if (!checkId)
        {
            return NotFound($"Invalid task id: {task?.IdTask}");
        }

        if (ModelState.IsValid)
        {
            try
            {
                ctx.Update(task);
                await ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Get), new { id = task.IdTask });
            }
            catch (Exception exc)
            {
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return PartialView(task);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return PartialView(task);
        }
    }
    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var task = await ctx.Task
                            .Where(m => m.IdTask == id)
                            .Select(m => new Task2ViewModel
                            {
                                IdTask = m.IdTask,
                                Task1 = m.Task1,
                                IdTaskStatus = m.IdTaskStatus,
                                IdPerson = m.IdPerson,
                                NamePerson = m.IdPersonNavigation.IdPersonNavigation.Name,
                                Status = m.IdTaskStatusNavigation.Status,
                            })
                            .SingleOrDefaultAsync();
        if (task != null)
        {
            return PartialView(task);
        }
        else
        {
            return NotFound($"Invalid task id: {id}");
        }
    }
    #endregion
}