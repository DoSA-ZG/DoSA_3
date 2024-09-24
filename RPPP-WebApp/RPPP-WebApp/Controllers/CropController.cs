using Microsoft.AspNetCore.Mvc;
using RPPP_WebApp.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Model;
using RPPP_WebApp.ViewModels;
using System.Text.Json;
using PdfRpt.FluentInterface;
using PdfRpt.Core.Contracts;
using System;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Globalization;

namespace RPPP_WebApp.Controllers;

public class CropController : Controller
{
    private readonly Rppp14Context ctx;
    private readonly ILogger<CropController> logger;
    private readonly AppSettings appSettings;
    private readonly IWebHostEnvironment environment;
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";


    public CropController(Rppp14Context ctx, IOptionsSnapshot<AppSettings> options, ILogger<CropController> logger, IWebHostEnvironment environment)
    {
        this.ctx = ctx;
        this.logger = logger;
        appSettings = options.Value;
        this.environment = environment;
    }

    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int pagesize = appSettings.PageSize;

        var query = ctx.Crop
                        .Include(w => w.IdPersonNavigation)  // Include the related Person data
                        .Include(w => w.IdSpeciesNavigation) // Include the related WorkerType data
                        .Include(w => w.IdStatusNavigation)
                        .Include(w => w.IdTaskNavigation)
                       .AsNoTracking();

        int count = await query.CountAsync();
        if (count == 0)
        {
            string message = "There is no crop in the database";
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
        var crop = await query
            .Select(m => new Crop2ViewModel
            {
                IdCrop = m.IdCrop,
                IdSpecies = m.IdSpecies,
                IdTask = m.IdTask,
                IdStatus = m.IdStatus,
                IdPerson = m.IdPerson,
                PlantingDate = m.PlantingDate,
                Quantity = m.Quantity,
                NamePerson = m.IdPersonNavigation.Name,
                NameSpecies = m.IdSpeciesNavigation.Name,
                NameTask = m.IdTaskNavigation.Task1,
                NameStatus = m.IdStatusNavigation.Status1
            })
            .Skip((page - 1) * pagesize)
            .Take(pagesize)
            .ToListAsync();


        var model = new CropViewModel
        {
            Crop = crop,
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
    public async Task<IActionResult> Create(Crop crop)
    {
        logger.LogTrace(JsonSerializer.Serialize(crop));
        if (ModelState.IsValid)
        {
            try
            {
                ctx.Add(crop);
                ctx.SaveChanges();
                string message = $"Crop {crop.IdPerson} added.";
                logger.LogInformation(new EventId(1000), message);

                TempData[Constants.Message] = message;
                TempData[Constants.ErrorOccurred] = false;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exc)
            {
                logger.LogError("Error adding new crop: {0}", exc.CompleteExceptionMessage());
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return View(crop);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return View(crop);
        }
    }

    //DROPDOWN LIST
    private async System.Threading.Tasks.Task PrepareDropDownLists()
    {
        var sn = await ctx.Species
                          .Where(d => d.IdSpecies == 1)
                          .Select(d => new { d.Name, d.IdSpecies })
                          .FirstOrDefaultAsync();
        var speciesname = await ctx.Species
                              .Where(d => d.IdSpecies != 1)
                              .OrderBy(d => d.Name)
                              .Select(d => new { d.Name, d.IdSpecies })
                              .ToListAsync();
        if (sn != null)
        {
            speciesname.Insert(0, sn);
        }
        ViewBag.SpeciesName = new SelectList(speciesname, nameof(sn.IdSpecies), nameof(sn.Name));

        var tn = await ctx.Task
                          .Where(d => d.IdTask == 3)
                          .Select(d => new { d.Task1, d.IdTask })
                          .FirstOrDefaultAsync();
        var taskName = await ctx.Task
                              .Where(d => d.IdTask != 3)
                              .OrderBy(d => d.Task1)
                              .Select(d => new { d.Task1, d.IdTask })
                              .ToListAsync();
        if (tn != null)
        {
            taskName.Insert(0, tn);
        }
        ViewBag.TaskName = new SelectList(taskName, nameof(tn.IdTask), nameof(tn.Task1));

        var st = await ctx.Status
                          .Where(d => d.IdStatus == 1)
                          .Select(d => new { d.Status1, d.IdStatus })
                          .FirstOrDefaultAsync();
        var statusName = await ctx.Status
                              .Where(d => d.IdStatus != 1)
                              .OrderBy(d => d.Status1)
                              .Select(d => new { d.Status1, d.IdStatus })
                              .ToListAsync();
        if (st != null)
        {
            statusName.Insert(0, st);
        }
        ViewBag.StatusName = new SelectList(statusName, nameof(st.IdStatus), nameof(st.Status1));

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
        var crop = await ctx.Crop
                                .Where(d => d.IdCrop == id)
                                .Select(d => new Crop2ViewModel
                                {
                                    IdCrop = d.IdCrop,
                                    IdSpecies = d.IdSpecies,
                                    IdTask = d.IdTask,
                                    IdStatus = d.IdStatus,
                                    IdPerson = d.IdPerson,
                                    PlantingDate = d.PlantingDate,
                                    Quantity = d.Quantity,
                                    NamePerson = d.IdPersonNavigation.Name,
                                    NameSpecies = d.IdSpeciesNavigation.Name,
                                    NameTask = d.IdTaskNavigation.Task1,
                                    NameStatus = d.IdStatusNavigation.Status1
                                })
                                .FirstOrDefaultAsync();
        if (crop == null)
        {
            return NotFound($"Crop {id} does not exist.");
        }
        else
        {

            //loading items
            var items = await ctx.Plot
                                 .Include(t => t.IdInfrastructureNavigation)
                                 .Include(t => t.IdPersonNavigation)
                                 .Include(t => t.IdSoilCategoryNavigation)
                                 .Include(t => t.IdSoilQualityNavigation)
                                 .Where(s => s.IdCrop == crop.IdCrop)
                                 .OrderBy(s => s.IdPlot)
                                 .Select(s => new Plot2ViewModel
                                 {
                                     IdPlot = s.IdPlot,
                                     IdPerson = s.IdPerson,
                                     CommonName = s.CommonName,
                                     IdSoilQuality = s.IdSoilQuality,
                                     IdSoilCategory = s.IdSoilCategory,
                                     IdInfrastructure = s.IdInfrastructure,
                                     Size = s.Size,
                                     Gpslocation = s.Gpslocation,
                                     NamePerson = s.IdPersonNavigation.Name,
                                     NameSoilQuality = s.IdSoilQualityNavigation.Quality,
                                     NameSoilCategory = s.IdSoilCategoryNavigation.CategoryName,
                                     NameInfrastructure = s.IdInfrastructureNavigation.TypeMaterial


                                 })
                                 .ToListAsync();
            crop.ItemsC = items;



            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Ascending = ascending;


            return View(viewName, crop);
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
                responseMessage = new ActionResponseMessage(MessageType.Success, $" Crop {id} deleted");
                string message = $"Crop {id} deleted";
            }
            catch (Exception exc)
            {
                responseMessage = new ActionResponseMessage(MessageType.Success, $" Error deleting crop: {exc.CompleteExceptionMessage()}");
            }
        }
        else
        {
            responseMessage = new ActionResponseMessage(MessageType.Error, $"Crop with id {id} does not exist.");

        }

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
        return responseMessage.MessageType == MessageType.Success ? new EmptyResult() : await Get(id);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var crop = await ctx.Crop
                      .AsNoTracking()
                      .Where(d => d.IdCrop == id)
                      .SingleOrDefaultAsync();
        if (crop != null)
        {
            await PrepareDropDownLists();
            return PartialView(crop);
        }
        else
        {
            return NotFound($"Invalid crop id: {id}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Crop crop)
    {
        if (crop == null)
        {
            return NotFound("No data submitted!?");
        }
        bool checkId = await ctx.Crop.AnyAsync(m => m.IdCrop == crop.IdCrop);
        if (!checkId)
        {
            return NotFound($"Invalid crop id: {crop?.IdPerson}");
        }

        if (ModelState.IsValid)
        {
            try
            {
                ctx.Update(crop);
                await ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Get), new { id = crop.IdCrop });
            }
            catch (Exception exc)
            {
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return PartialView(crop);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return PartialView(crop);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var crop = await ctx.Crop
                            .Where(m => m.IdCrop == id)
                            .Select(m => new Crop2ViewModel
                            {
                                IdCrop = m.IdCrop,
                                IdSpecies = m.IdSpecies,
                                IdTask = m.IdTask,
                                IdStatus = m.IdStatus,
                                IdPerson = m.IdPerson,
                                PlantingDate = m.PlantingDate,
                                Quantity = m.Quantity,
                                NamePerson = m.IdPersonNavigation.Name,
                                NameSpecies = m.IdSpeciesNavigation.Name,
                                NameTask = m.IdTaskNavigation.Task1,
                                NameStatus = m.IdStatusNavigation.Status1
                            })
                            .SingleOrDefaultAsync();
        if (crop != null)
        {
            return PartialView(crop);
        }
        else
        {
            return NotFound($"Invalid crop id: {id}");
        }
    }
    #endregion


    #region Export and Import reports

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
    public async Task<IActionResult> Crops_PDF()
    {
        string title = "All crops";
        var crops = await ctx.Crop
                                 .Include(w => w.IdPersonNavigation)  // Include the related Person data
                                 .Include(w => w.IdSpeciesNavigation) // Include the related WorkerType data
                                 .Include(w => w.IdStatusNavigation)
                                 .Include(w => w.IdTaskNavigation)
                                 .AsNoTracking()
                                 .OrderBy(d => d.IdCrop)
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
        report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(crops));

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
                column.PropertyName(nameof(Crop.IdCrop));
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(2);
                column.Width(1);
                column.HeaderCell("ID Crop", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Crop>(x => x.IdSpeciesNavigation.Name);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(3);
                column.HeaderCell("Species planted", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Crop>(x => x.IdTaskNavigation.Task1);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(3);
                column.HeaderCell("Task", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Crop>(x => x.IdStatusNavigation.Status1);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(3);
                column.HeaderCell("Status", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Crop>(x => x.IdPersonNavigation.Name);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(3);
                column.HeaderCell("Worker assigned", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Crop>(x => x.PlantingDate);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(2);
                column.HeaderCell("Planting Date", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Crop>(x => x.Quantity);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(2);
                column.HeaderCell("Quantity", horizontalAlignment: HorizontalAlignment.Center);
            });
        });

        #endregion
        byte[] pdf = report.GenerateAsByteArray();

        if (pdf != null)
        {
            Response.Headers.Add("content-disposition", "inline; filename=crops.pdf");
            return File(pdf, "application/pdf");
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Crops_Excel_Details()
    {
        var crops = await ctx.Crop
                             .Include(c => c.IdSpeciesNavigation)
                             .Include(c => c.IdTaskNavigation)
                             .Include(c => c.IdStatusNavigation)
                             .Include(c => c.IdPersonNavigation)
                             .Include(c => c.Plot)  // Include the related Plots data
                             .AsNoTracking()
                             .OrderBy(c => c.IdCrop)
                             .ToListAsync();

        byte[] content;
        using (ExcelPackage excel = new ExcelPackage())
        {
            excel.Workbook.Properties.Title = "Crops list";
            excel.Workbook.Properties.Author = "Group-14";

            foreach (var crop in crops)
            {
                var worksheet = excel.Workbook.Worksheets.Add($"Crop ID: {crop.IdCrop} - {crop.IdSpeciesNavigation.Name}");
                worksheet.Cells["1:1"].Style.Fill.PatternType = ExcelFillStyle.LightGray;
                worksheet.Cells["4:4"].Style.Fill.PatternType = ExcelFillStyle.LightGray;

                // Add headers for crop attributes
                worksheet.Cells[1, 1].Value = "ID Crop";
                worksheet.Cells[1, 2].Value = "Species planted";
                worksheet.Cells[1, 3].Value = "Task";
                worksheet.Cells[1, 4].Value = "Status";
                worksheet.Cells[1, 5].Value = "Worker assigned";
                worksheet.Cells[1, 6].Value = "Planting Date";
                worksheet.Cells[1, 7].Value = "Quantity";

                worksheet.Cells[2, 1].Value = crop.IdCrop;
                worksheet.Cells[2, 2].Value = crop.IdSpeciesNavigation?.Name ?? "Unknown";
                worksheet.Cells[2, 3].Value = crop.IdTaskNavigation?.Task1 ?? "Unknown";
                worksheet.Cells[2, 4].Value = crop.IdStatusNavigation?.Status1 ?? "Unknown";
                worksheet.Cells[2, 5].Value = crop.IdPersonNavigation?.Name ?? "Unknown";
                worksheet.Cells[2, 6].Value = crop.PlantingDate.ToString("MM/dd/yyyy HH:mm:ss");
                worksheet.Cells[2, 7].Value = crop.Quantity;

                // Add headers for plot attributes
                worksheet.Cells[4, 1].Value = "ID Plot";
                worksheet.Cells[4, 2].Value = "Owner";
                worksheet.Cells[4, 3].Value = "Common Name";
                worksheet.Cells[4, 4].Value = "Soil Quality";
                worksheet.Cells[4, 5].Value = "Soil Category";
                worksheet.Cells[4, 6].Value = "Infrastructure";
                worksheet.Cells[4, 7].Value = "Size (km2)";
                worksheet.Cells[4, 8].Value = "GPS Location";

                var plots = await ctx.Plot
                         .Include(p => p.IdInfrastructureNavigation)
                         .Include(p => p.IdPersonNavigation)
                         .Include(p => p.IdSoilCategoryNavigation)
                         .Include(p => p.IdSoilQualityNavigation)
                         .Where(s => s.IdCrop == crop.IdCrop)
                         .ToListAsync();

                int row = 5; // Start from row 5 to leave space for headers
                foreach (var plot in plots)
                {


                    worksheet.Cells[row, 1].Value = plot.IdPlot;
                    worksheet.Cells[row, 2].Value = plot.IdPersonNavigation?.Name ?? "Unknown";
                    worksheet.Cells[row, 3].Value = plot.CommonName;
                    worksheet.Cells[row, 4].Value = plot.IdSoilQualityNavigation?.Quality ?? "Unknown";
                    worksheet.Cells[row, 5].Value = plot.IdSoilCategoryNavigation?.CategoryName ?? "Unknown";
                    worksheet.Cells[row, 6].Value = plot.IdInfrastructureNavigation?.TypeMaterial ?? "Unknown";
                    worksheet.Cells[row, 7].Value = plot.Size;
                    worksheet.Cells[row, 8].Value = plot.Gpslocation;
                    row++;
                }

                // AutoFitColumns for better visibility
                worksheet.Cells[1, 1, row - 1, 8].AutoFitColumns();
            }

            content = excel.GetAsByteArray();
        }

        return File(content, ExcelContentType, "crops_with_plots.xlsx");
    }

    public async Task<IActionResult> Crops_Excel_Simple()
    {
        var crops = await ctx.Crop
                                 .AsNoTracking()
                                 .OrderBy(d => d.IdPerson)
                                 .Include(c => c.IdSpeciesNavigation)
                                 .Include(c => c.IdTaskNavigation)
                                 .Include(c => c.IdStatusNavigation)
                                 .Include(c => c.IdPersonNavigation)
                                 .ToListAsync();
        byte[] content;
        using (ExcelPackage excel = new ExcelPackage())
        {
            excel.Workbook.Properties.Title = "Crops list";
            excel.Workbook.Properties.Author = "Group-14";
            var worksheet = excel.Workbook.Worksheets.Add("Crops");

            //First add the headers
            worksheet.Cells[1, 1].Value = "Id Crop";
            worksheet.Cells[1, 2].Value = "Species planted";
            worksheet.Cells[1, 3].Value = "Task";
            worksheet.Cells[1, 4].Value = "Status";
            worksheet.Cells[1, 5].Value = "Worker assigned";
            worksheet.Cells[1, 6].Value = "Planting Date";
            worksheet.Cells[1, 7].Value = "Quantity";

            for (int i = 0; i < crops.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = crops[i].IdCrop;
                worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[i + 2, 2].Value = crops[i].IdSpeciesNavigation?.Name;
                worksheet.Cells[i + 2, 3].Value = crops[i].IdTaskNavigation?.Task1;
                worksheet.Cells[i + 2, 4].Value = crops[i].IdStatusNavigation?.Status1;
                worksheet.Cells[i + 2, 5].Value = crops[i].IdPersonNavigation?.Name;
                worksheet.Cells[i + 2, 6].Value = crops[i].PlantingDate.ToString("MM/dd/yyyy HH:mm:ss");
                worksheet.Cells[i + 2, 7].Value = crops[i].Quantity;
            }

            worksheet.Cells[1, 1, crops.Count + 1, 8].AutoFitColumns();

            content = excel.GetAsByteArray();
        }
        return File(content, ExcelContentType, "crops.xlsx");
    }

    public async Task<IActionResult> ProcessImportedExcel_Crop(IFormFile importedFile)
    {
        if (importedFile == null || importedFile.Length == 0)
        {
            // Manejar el caso en el que el archivo importado es nulo o vacío
            // Puedes mostrar un mensaje de error o redirigir a una página de error
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

                worksheet.Cells[1, 8].Value = "Import Status";

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Obtener los datos del Excel
                        int idCrop = Convert.ToInt32(worksheet.Cells[row, 1].Value);
                        string speciesPlanted = worksheet.Cells[row, 2].Text;
                        string task = worksheet.Cells[row, 3].Text;
                        string status = worksheet.Cells[row, 4].Text;
                        string workerAssigned = worksheet.Cells[row, 5].Text;
                        DateTime plantingDate;
                        int quantity;

                        if (DateTime.TryParseExact(worksheet.Cells[row, 6].Value?.ToString(), "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out plantingDate)
                            && int.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out quantity))
                        {
                            // Buscar los identificadores correspondientes a los textos en la base de datos
                            int idSpecies = await ctx.Species
                                .Where(s => s.Name == speciesPlanted)
                                .Select(s => s.IdSpecies)
                                .FirstOrDefaultAsync();

                            int idTask = await ctx.Task
                                .Where(t => t.Task1 == task)
                                .Select(t => t.IdTask)
                                .FirstOrDefaultAsync();

                            int idStatus = await ctx.Status
                                .Where(s => s.Status1 == status)
                                .Select(s => s.IdStatus)
                                .FirstOrDefaultAsync();

                            int idPerson = await ctx.Person
                                .Where(p => p.Name == workerAssigned)
                                .Select(p => p.IdPerson)
                                .FirstOrDefaultAsync();

                            // Buscar el Crop en la base de datos
                            var cropToUpdate = await ctx.Crop.FindAsync(idCrop);

                            if (cropToUpdate != null)
                            {
                                // Actualizar los datos del Crop con los del Excel
                                cropToUpdate.IdSpecies = idSpecies;
                                cropToUpdate.IdTask = idTask;
                                cropToUpdate.IdStatus = idStatus;
                                cropToUpdate.IdPerson = idPerson;
                                cropToUpdate.PlantingDate = plantingDate;
                                cropToUpdate.Quantity = quantity;
                                // Actualizar otras propiedades según sea necesario

                                worksheet.Cells[row, 8].Value = "Successfully Imported";
                            }
                            else
                            {
                                worksheet.Cells[row, 8].Value = "Crop not found in the database";
                            }
                        }
                        else
                        {
                            worksheet.Cells[row, 8].Value = "Invalid date or quantity format";
                        }
                    }
                    catch (Exception ex)
                    {
                        // Manejar excepciones, por ejemplo, si hay un error al convertir datos del Excel
                        worksheet.Cells[row, 8].Value = $"Error: {ex.Message}";
                    }
                }

                // Guardar cambios en la base de datos
                await ctx.SaveChangesAsync();

                byte[] content = package.GetAsByteArray();
                return File(content, ExcelContentType, "imported_crops.xlsx");
            }
        }
    }
    #endregion

}