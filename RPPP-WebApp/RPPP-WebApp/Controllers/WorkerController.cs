using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using PdfRpt.FluentInterface;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Model;
using RPPP_WebApp.ViewModels;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Threading.Tasks;
using PdfRpt.Core.Contracts;

namespace RPPP_WebApp.Controllers;

public class WorkerController : Controller
{
    private readonly Rppp14Context ctx;
    private readonly ILogger<WorkerController> logger;
    private readonly AppSettings appSettings;
    private readonly IWebHostEnvironment environment;
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public WorkerController(Rppp14Context ctx, IOptionsSnapshot<AppSettings> options, ILogger<WorkerController> logger, IWebHostEnvironment environment)
    {
        this.ctx = ctx;
        this.logger = logger;
        appSettings = options.Value;
        this.environment = environment;
    }

    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int pagesize = appSettings.PageSize;

        var query = ctx.Workers
                        .Include(w => w.IdPersonNavigation)  // Include the related Person data
                        .Include(w => w.IdWorkerTypeNavigation) // Include the related WorkerType data
                       .AsNoTracking();

        int count = await query.CountAsync();
        if (count == 0)
        {
            string message = "There is no worker in the database";
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

        // Cambia esta línea en tu acción Index
        var worker = await query
            .Select(m => new Worker2ViewModel
            {
                IdPerson = m.IdPerson,
                IdWorkerType = m.IdWorkerType,
                Salary = m.Salary,
                NamePerson = m.IdPersonNavigation.Name,
                NameWorkerType = m.IdWorkerTypeNavigation.Type
            })
            .Skip((page - 1) * pagesize)
            .Take(pagesize)
            .ToListAsync();

        // Asegúrate de que Worker en WorkerViewModel sea del tipo correcto
        var model = new WorkerViewModel
        {
            Worker = worker,  // Asegúrate de que Worker coincida con el tipo esperado
            PagingInfo = pagingInfo
        };

        return View(model);
    }


    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareDropDownLists();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Workers worker)
    {
        logger.LogTrace(JsonSerializer.Serialize(worker));
        if (ModelState.IsValid)
        {
            try
            {
                ctx.Add(worker);
                ctx.SaveChanges();
                string message = $"Worker {worker.IdPerson} added.";
                logger.LogInformation(new EventId(1000), message);

                TempData[Constants.Message] = message;
                TempData[Constants.ErrorOccurred] = false;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exc)
            {
                logger.LogError("Error adding new worker: {0}", exc.CompleteExceptionMessage());
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return View(worker);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return View(worker);
        }
    }

    //DROPDOWN LIST
    private async System.Threading.Tasks.Task PrepareDropDownLists()
    {
        var wt = await ctx.WorkerType
                          .Where(d => d.IdWorkerType == 1)
                          .Select(d => new { d.Type, d.IdWorkerType })
                          .FirstOrDefaultAsync();
        var workertype = await ctx.WorkerType
                              .Where(d => d.IdWorkerType != 1)
                              .OrderBy(d => d.Type)
                              .Select(d => new { d.Type, d.IdWorkerType })
                              .ToListAsync();
        if (wt != null)
        {
            workertype.Insert(0, wt);
        }
        ViewBag.WorkerTy = new SelectList(workertype, nameof(wt.IdWorkerType), nameof(wt.Type));

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

    //ACCIÓN SHOW

    public async Task<IActionResult> Show(int id, int page = 1, int sort = 1, bool ascending = true, string viewName = nameof(Show))
    {
        var worker = await ctx.Workers
                                .Where(d => d.IdPerson == id)
                                .Select(d => new Worker2ViewModel
                                {
                                    IdPerson = d.IdPerson,
                                    IdWorkerType = d.IdWorkerType,
                                    Salary = d.Salary,
                                    NamePerson = d.IdPersonNavigation.Name,
                                    NameWorkerType = d.IdWorkerTypeNavigation.Type
                                })
                                .FirstOrDefaultAsync();
        if (worker == null)
        {
            return NotFound($"Worker {id} does not exist.");
        }
        else
        {

            //loading items
            var items = await ctx.Task
                                 .Include(t => t.IdTaskStatusNavigation)
                                 .Where(s => s.IdPerson == worker.IdPerson)
                                 .OrderBy(s => s.IdTask)
                                 .Select(s => new Task2ViewModel
                                 {
                                     IdTask = s.IdTask,
                                     Task1 = s.Task1,
                                     IdTaskStatus = s.IdTaskStatus,
                                     Status = s.IdTaskStatusNavigation.Status,
                                     IdPerson = s.IdPerson,

                                 })
                                 .ToListAsync();
            worker.ItemsW = items;



            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Ascending = ascending;


            return View(viewName, worker);
        }
    }


    #region Methods for dynamic update and delete
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        ActionResponseMessage responseMessage;
        var worker = await ctx.Workers.FindAsync(id);
        if (worker != null)
        {
            try
            {
                ctx.Remove(worker);
                await ctx.SaveChangesAsync();
                responseMessage = new ActionResponseMessage(MessageType.Success, $" Worker {id} deleted");
                string message = $"Worker {id} deleted";
            }
            catch (Exception exc)
            {
                responseMessage = new ActionResponseMessage(MessageType.Success, $" Error deleting worker: {exc.CompleteExceptionMessage()}");
            }
        }
        else
        {
            responseMessage = new ActionResponseMessage(MessageType.Error, $"Worker with id {id} does not exist.");

        }

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
        return responseMessage.MessageType == MessageType.Success ? new EmptyResult() : await Get(id);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var worker = await ctx.Workers
                      .AsNoTracking()
                      .Where(d => d.IdPerson == id)
                      .SingleOrDefaultAsync();
        if (worker != null)
        {
            await PrepareDropDownLists();
            return PartialView(worker);
        }
        else
        {
            return NotFound($"Invalid worker id: {id}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Workers worker)
    {
        if (worker == null)
        {
            return NotFound("No data submitted!?");
        }
        bool checkId = await ctx.Workers.AnyAsync(m => m.IdPerson == worker.IdPerson);
        if (!checkId)
        {
            return NotFound($"Invalid worker id: {worker?.IdPerson}");
        }

        if (ModelState.IsValid)
        {
            try
            {
                ctx.Update(worker);
                await ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Get), new { id = worker.IdPerson });
            }
            catch (Exception exc)
            {
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return PartialView(worker);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return PartialView(worker);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var worker = await ctx.Workers
                    .Where(m => m.IdPerson == id)
                    .Select(m => new Worker2ViewModel
                    {
                        IdPerson = m.IdPerson,
                        IdWorkerType = m.IdWorkerType,
                        Salary = m.Salary,
                        NamePerson = m.IdPersonNavigation.Name,
                        NameWorkerType = m.IdWorkerTypeNavigation.Type
                    })
                    .SingleOrDefaultAsync();
        if (worker != null)
        {
            return PartialView(worker);
        }
        else
        {
            return NotFound($"Invalid worker id: {id}");
        }
    }

    #endregion

    #region Export and import reports

    private PdfReport CreateReport(string title)
    {
        var pdf = new PdfReport();

        pdf.DocumentPreferences(doc =>
        {
            doc.Orientation(PageOrientation.Portrait);
            doc.PageSize(PdfPageSize.A4);
            doc.DocumentMetadata(new DocumentMetadata
            {
                Author = "GROUP-14",
                Application = "RPPP-WebApp",
                Title = title
            });
            doc.Compression(new CompressionSettings
            {
                EnableCompression = true,
                EnableFullCompression = true
            });
        })
        //fix za linux https://github.com/VahidN/PdfReport.Core/issues/40
        .DefaultFonts(fonts => {
            fonts.Path(Path.Combine(environment.WebRootPath, "fonts", "verdana.ttf"),
                         Path.Combine(environment.WebRootPath, "fonts", "tahoma.ttf"));
            fonts.Size(9);
            fonts.Color(System.Drawing.Color.Black);
        })
        //
        .MainTableTemplate(template =>
        {
            template.BasicTemplate(BasicTemplate.ProfessionalTemplate);
        })
        .MainTablePreferences(table =>
        {
            table.ColumnsWidthsType(TableColumnWidthType.Relative);
            //table.NumberOfDataRowsPerPage(20);
            table.GroupsPreferences(new GroupsPreferences
            {
                GroupType = GroupType.HideGroupingColumns,
                RepeatHeaderRowPerGroup = true,
                ShowOneGroupPerPage = true,
                SpacingBeforeAllGroupsSummary = 5f,
                NewGroupAvailableSpacingThreshold = 150,
                SpacingAfterAllGroupsSummary = 5f
            });
            table.SpacingAfter(4f);
        });

        return pdf;
    }

    public async Task<IActionResult> Worker_PDF()
    {
        string title = "Workers";
        var workers = await ctx.Workers
                             .Include(w => w.IdPersonNavigation)  // Include the related Person data
                             .Include(w => w.IdWorkerTypeNavigation)
                             .AsNoTracking()
                             .OrderBy(d => d.IdPerson)
                             .ToListAsync();
        PdfReport report = CreateReport(title);
        #region Header and footer
        report.PagesFooter(footer =>
        {
            footer.DefaultFooter(DateTime.Now.ToString("dd.MM.yyyy."));
        })
        .PagesHeader(header =>
        {
            header.CacheHeader(cache: true); // It's a default setting to improve the performance.
            header.DefaultHeader(defaultHeader =>
            {
                defaultHeader.RunDirection(PdfRunDirection.LeftToRight);
                defaultHeader.Message(title);
            });
        });
        #endregion
        #region Set datasource and define columns
        report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(workers));

        report.MainTableColumns(columns =>
        {
            columns.AddColumn(column =>
            {
                column.IsRowNumber(true);
                column.CellsHorizontalAlignment(HorizontalAlignment.Right);
                column.IsVisible(true);
                column.Order(0);
                column.Width(1);
                column.HeaderCell("#", horizontalAlignment: HorizontalAlignment.Right);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName(nameof(Workers.IdPerson));
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(1);
                column.Width(2);
                column.HeaderCell("ID Person");
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Workers>(x => x.IdPersonNavigation.Name);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(2);
                column.Width(3);
                column.HeaderCell("Worker Name", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Workers>(x => x.IdWorkerTypeNavigation.Type);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(1);
                column.HeaderCell("Worker Type", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Workers>(x => x.Salary);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(1);
                column.HeaderCell("Salary (€/h)", horizontalAlignment: HorizontalAlignment.Center);
            });
        });

        #endregion
        byte[] pdf = report.GenerateAsByteArray();

        if (pdf != null)
        {
            Response.Headers.Add("content-disposition", "inline; filename=workers.pdf");
            return File(pdf, "application/pdf");
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Worker_Excel_Details()
    {
        var workers = await ctx.Workers
                                 .Include(w => w.IdPersonNavigation)
                                 .Include(w => w.IdWorkerTypeNavigation)
                                 .AsNoTracking()
                                 .OrderBy(d => d.IdPerson)
                                 .ToListAsync();

        byte[] content;
        using (ExcelPackage excel = new ExcelPackage())
        {
            excel.Workbook.Properties.Title = "Workers list";
            excel.Workbook.Properties.Author = "Group-14";

            foreach (var worker in workers)
            {
                // Consulta separada para cargar las tareas del trabajador
                var tasks = await ctx.Task
                                      .Include(t => t.IdTaskStatusNavigation)  // Include the related TaskStatus data
                                      .Where(t => t.IdPerson == worker.IdPerson)
                                      .ToListAsync();

                var worksheet = excel.Workbook.Worksheets.Add($"Worker ID: {worker.IdPerson} - {worker.IdPersonNavigation?.Name ?? "Unknown"}");
                worksheet.Cells["1:1"].Style.Fill.PatternType = ExcelFillStyle.LightGray;
                worksheet.Cells["4:4"].Style.Fill.PatternType = ExcelFillStyle.LightGray;

                // Fill worker data
                worksheet.Cells[1, 1].Value = "ID Person";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Worker Type";
                worksheet.Cells[1, 4].Value = "Salary (€)";

                worksheet.Cells[2, 1].Value = worker.IdPerson;
                worksheet.Cells[2, 2].Value = worker.IdPersonNavigation?.Name ?? "Unknown";
                worksheet.Cells[2, 3].Value = worker.IdWorkerTypeNavigation?.Type ?? "Unknown";
                worksheet.Cells[2, 4].Value = worker.Salary;

                // Add headers
                worksheet.Cells[4, 1].Value = "ID Task";
                worksheet.Cells[4, 2].Value = "Task";
                worksheet.Cells[4, 3].Value = "Task Status";

                // Add tasks
                int row = 5; // Start from row 5 to leave space for headers
                foreach (var task in tasks)
                {
                    worksheet.Cells[row, 1].Value = task.IdTask;
                    worksheet.Cells[row, 2].Value = task.Task1;
                    worksheet.Cells[row, 3].Value = task.IdTaskStatusNavigation?.Status ?? "Unknown";
                    row++;
                }

                // AutoFitColumns for better visibility
                worksheet.Cells[1, 1, row - 1, 3].AutoFitColumns();
            }

            content = excel.GetAsByteArray();
        }

        return File(content, ExcelContentType, "workers_with_tasks.xlsx");
    }
    public async Task<IActionResult> Worker_Excel_Simple()
    {
        var workers = await ctx.Workers
                                 .AsNoTracking()
                                 .Include(w => w.IdPersonNavigation)
                                 .Include(w => w.IdWorkerTypeNavigation)
                                 .OrderBy(d => d.IdPerson)
                                 .ToListAsync();
        byte[] content;
        using (ExcelPackage excel = new ExcelPackage())
        {
            excel.Workbook.Properties.Title = "Workers list";
            excel.Workbook.Properties.Author = "Group-14";
            var worksheet = excel.Workbook.Worksheets.Add("Workers");

            //First add the headers
            worksheet.Cells[1, 1].Value = "Id Person";
            worksheet.Cells[1, 2].Value = "Worker Name";
            worksheet.Cells[1, 3].Value = "Worker Type";
            worksheet.Cells[1, 4].Value = "Salary";

            for (int i = 0; i < workers.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = workers[i].IdPerson;
                worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[i + 2, 2].Value = workers[i].IdPersonNavigation?.Name;
                worksheet.Cells[i + 2, 3].Value = workers[i].IdWorkerTypeNavigation?.Type;
                worksheet.Cells[i + 2, 4].Value = workers[i].Salary;
            }

            worksheet.Cells[1, 1, workers.Count + 1, 5].AutoFitColumns();

            content = excel.GetAsByteArray();
        }
        return File(content, ExcelContentType, "workers.xlsx");
    }
    public async Task<IActionResult> ProcessImportedExcel_Worker(IFormFile importedFile)
    {
        if (importedFile == null || importedFile.Length == 0)
        {
            return RedirectToAction("Index");
        }

        using (var stream = importedFile.OpenReadStream())
        {
            using (ExcelPackage package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    return RedirectToAction("Index");
                }

                int rowCount = worksheet.Dimension.Rows;

                if (worksheet.Cells[1, 5].Value == null)
                {
                    worksheet.Cells[1, 5].Value = "Import Status";
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    var name = worksheet.Cells[row, 2].GetValue<string>();
                    var workerTypeName = worksheet.Cells[row, 3].GetValue<string>();

                    var worker = await ctx.Workers
                        .Include(w => w.IdPersonNavigation)
                        .Include(w => w.IdWorkerTypeNavigation)
                        .FirstOrDefaultAsync(w => w.IdPersonNavigation.Name == name);

                    if (worker != null)
                    {
                        var newWorkerTypeId = await ctx.WorkerType
                            .Where(wt => wt.Type == workerTypeName)
                            .Select(wt => wt.IdWorkerType)
                            .FirstOrDefaultAsync();

                        if (newWorkerTypeId != 0)
                        {
                            // Asignar los nuevos valores al objeto worker
                            worker.IdPersonNavigation.Name = name;
                            worker.IdWorkerType = newWorkerTypeId;
                            worker.Salary = worksheet.Cells[row, 4].GetValue<double>();

                            worksheet.Cells[row, 5].Value = "Successfully Imported";

                            // Adjuntar el objeto al contexto y marcarlo como modificado
                            ctx.Attach(worker).State = EntityState.Modified;
                        }
                        else
                        {
                            worksheet.Cells[row, 5].Value = "Failed: Worker Type not found";
                        }
                    }
                    else
                    {
                        worksheet.Cells[row, 5].Value = "Failed: Worker not found";
                    }
                }

                await ctx.SaveChangesAsync();

                byte[] content = package.GetAsByteArray();
                return File(content, ExcelContentType, "imported_workers.xlsx");
            }
        }
    }

    #endregion

}