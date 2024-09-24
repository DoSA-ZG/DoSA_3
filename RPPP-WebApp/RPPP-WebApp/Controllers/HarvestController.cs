using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using PdfRpt.Core.Contracts;
using PdfRpt.FluentInterface;
using RPPP_WebApp.Extensions;
using RPPP_WebApp.Extensions.Selectors;
using RPPP_WebApp.Model;
using RPPP_WebApp.ViewModels;
using System;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Threading.Tasks;
using static iTextSharp.text.pdf.AcroFields;
using System.Globalization;

namespace RPPP_WebApp.Controllers;

public class HarvestController : Controller
{
    private readonly Rppp14Context ctx;
    private readonly ILogger<HarvestController> logger;
    private readonly AppSettings appSettings;
    private readonly IWebHostEnvironment environment;
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public HarvestController(Rppp14Context ctx, IOptionsSnapshot<AppSettings> options, ILogger<HarvestController> logger, IWebHostEnvironment environment)
    {
        this.ctx = ctx;
        this.logger = logger;
        appSettings = options.Value;
        this.environment = environment;
    }

    public async Task<IActionResult> Index(int page = 1, int sort = 1, bool ascending = true)
    {
        int pagesize = appSettings.PageSize;

        var query = ctx.Harvest
                .Include(t => t.IdPersonNavigation) // Include Workers entity
                .ThenInclude(w => w.IdPersonNavigation) // Include Person entity within Workers
                .AsNoTracking();


        int count = await query.CountAsync();
        if (count == 0)
        {
            string message = "There is no harvest in the database";
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

        var harvest = await query
                            .Select(m => new Harvest2ViewModel
                            {
                                IdHarvest = m.IdHarvest,
                                IdCrop = m.IdCrop,
                                Quantity = m.Quantity,
                                FromDate = m.FromDate,
                                ToDate = m.ToDate,
                                IdPerson = m.IdPerson,
                                NamePerson = m.IdPersonNavigation.IdPersonNavigation.Name
                            })
                            .Skip((page - 1) * pagesize)
                            .Take(pagesize)
                           .ToListAsync();

        var model = new HarvestViewModel
        {
            Harvest = harvest,
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
    public async Task<IActionResult> Create(Harvest harvest)
    {
        logger.LogTrace(JsonSerializer.Serialize(harvest));
        if (ModelState.IsValid)
        {
            try
            {
                ctx.Add(harvest);
                ctx.SaveChanges();
                string message = $"Harvest {harvest.IdHarvest} added.";
                logger.LogInformation(new EventId(1000), message);

                TempData[Constants.Message] = message;
                TempData[Constants.ErrorOccurred] = false;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exc)
            {
                logger.LogError("Error adding new harvest: {0}", exc.CompleteExceptionMessage());
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return View(harvest);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return View(harvest);
        }
    }



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

        ViewBag.CropNumber = new SelectList(crop_id2, crop_id);

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

    public async Task<IActionResult> Show(int id, int page = 1, int sort = 1, bool ascending = true, string viewName = nameof(Show))
    {
        var harvest = await ctx.Harvest
                                .Where(d => d.IdHarvest == id)
                                .Select(d => new Harvest2ViewModel
                                {
                                    IdHarvest = d.IdHarvest,
                                    IdCrop = d.IdCrop,
                                    Quantity = d.Quantity,
                                    FromDate = d.FromDate,
                                    ToDate = d.ToDate,
                                    NamePerson = d.IdPersonNavigation.IdPersonNavigation.Name,
                                })
                                .FirstOrDefaultAsync();
        if (harvest == null)
        {
            return NotFound($"Harvest {id} does not exist.");
        }
        else
        {

            //loading items
            var items = await ctx.Order
                                 .Include(t => t.IdPersonNavigation)
                                 .Where(s => s.IdHarvest == harvest.IdHarvest)
                                 .OrderBy(s => s.IdHarvest)
                                 .Select(s => new Order2ViewModel
                                 {
                                     IdOrder = s.IdOrder,
                                     IdHarvest = s.IdHarvest,
                                     NamePerson = s.IdPersonNavigation.Name,
                                     Quantity = s.Quantity,
                                     Price = s.Price,
                                     DateOfOrder = s.DateOfOrder,

                                 })
                                 .ToListAsync();
            harvest.ItemsH = items;



            ViewBag.Page = page;
            ViewBag.Sort = sort;
            ViewBag.Ascending = ascending;


            return View(viewName, harvest);
        }
    }

    #region Methods for dynamic update and delete
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        ActionResponseMessage responseMessage;
        var harvest = await ctx.Harvest.FindAsync(id);
        if (harvest != null)
        {
            try
            {
                ctx.Remove(harvest);
                await ctx.SaveChangesAsync();
                responseMessage = new ActionResponseMessage(MessageType.Success, $"Harvest with id {id} has been deleted.");
            }
            catch (Exception exc)
            {
                responseMessage = new ActionResponseMessage(MessageType.Error, $"Error deleting harvest: {exc.CompleteExceptionMessage()}");
            }
        }
        else
        {
            responseMessage = new ActionResponseMessage(MessageType.Error, $"Harvest with id {id} does not exist.");
        }

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { showMessage = responseMessage });
        return responseMessage.MessageType == MessageType.Success ?
          new EmptyResult() : await Get(id);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var harvest = await ctx.Harvest
                              .AsNoTracking()
                              .Where(m => m.IdHarvest == id)
                              .SingleOrDefaultAsync();
        if (harvest != null)
        {
            await PrepareDropDownLists();
            return PartialView(harvest);
        }
        else
        {
            return NotFound($"Invalid harvest id: {id}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Harvest harvest)
    {
        if (harvest == null)
        {
            return NotFound("No data submitted!?");
        }
        bool checkId = await ctx.Harvest.AnyAsync(m => m.IdHarvest == harvest.IdHarvest);
        if (!checkId)
        {
            return NotFound($"Invalid harvest id: {harvest?.IdHarvest}");
        }

        if (ModelState.IsValid)
        {
            try
            {
                ctx.Update(harvest);
                await ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Get), new { id = harvest.IdHarvest });
            }
            catch (Exception exc)
            {
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                await PrepareDropDownLists();
                return PartialView(harvest);
            }
        }
        else
        {
            await PrepareDropDownLists();
            return PartialView(harvest);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(int id)
    {
        var harvest = await ctx.Harvest
                            .Where(m => m.IdHarvest == id)
                            .Select(m => new Harvest2ViewModel
                            {
                                IdHarvest = m.IdHarvest,
                                IdCrop = m.IdCrop,
                                Quantity = m.Quantity,
                                FromDate = m.FromDate,
                                ToDate = m.ToDate,
                                IdPerson = m.IdPerson,
                                NamePerson = m.IdPersonNavigation.IdPersonNavigation.Name
                            })
                            .SingleOrDefaultAsync();
        if (harvest != null)
        {
            return PartialView(harvest);
        }
        else
        {
            return NotFound($"Invalid harvest id: {id}");
        }
    }

    // NUEVO EDIT PARA LA PAGINA DE DETAILS

    public async Task<IActionResult> Edit2(int id, int? position, string filter, int page = 1, int sort = 1, bool ascending = true)
    {
        await PrepareDropDownLists();
        return await Show(id,page, sort, ascending, viewName: nameof(Edit2));
        
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit2(Harvest2ViewModel model, int page = 1, int sort = 1, bool ascending = true)
    {
        ViewBag.Page = page;
        ViewBag.Sort = sort;
        ViewBag.Ascending = ascending;

        

        if (ModelState.IsValid)
        {
            var harvest = await ctx.Harvest
                .Include(d => d.Order) // Incluir la colección Order al cargar la entidad Harvest
                .Where(d => d.IdHarvest == model.IdHarvest)
                .FirstOrDefaultAsync();

            if (harvest == null)
            {
                return NotFound("There is no harvest with id: " + model.IdHarvest);
            }

            
            harvest.IdHarvest = model.IdHarvest;
            harvest.IdCrop = model.IdCrop;
            harvest.Quantity = model.Quantity;
            harvest.FromDate = model.FromDate;
            harvest.ToDate = model.ToDate;
            harvest.IdPerson = model.IdPerson;

            List<int> itemsIds = model.ItemsH
                                      .Where(s => s.IdHarvest == harvest.IdHarvest)
                                      .Select(s => s.IdHarvest)
                                      .ToList();
            //remove all not anymore in the model
            ctx.RemoveRange(harvest.Order.Where(i => !itemsIds.Contains(i.IdHarvest)));

            foreach (var item in model.ItemsH)
            {
                // Intenta encontrar el Order correspondiente en la colección de Harvest
                var existingOrder = harvest.Order.FirstOrDefault(o => o.IdOrder == item.IdOrder);

                if (existingOrder == null)
                {
                    // Si no existe, crea uno nuevo y agrégalo a la colección
                    existingOrder = new Order();
                    harvest.Order.Add(existingOrder);
                }

                // Actualiza los campos del Order existente o recién creado
                existingOrder.IdPerson = item.IdPerson;
                existingOrder.Quantity = item.Quantity;
                existingOrder.Price = item.Price;
                existingOrder.DateOfOrder = item.DateOfOrder;
            }


            try
            {
                await ctx.SaveChangesAsync();

                TempData[Constants.Message] = $"Harvest {harvest.IdHarvest} updated.";
                TempData[Constants.ErrorOccurred] = false;
                return RedirectToAction(nameof(Edit2), new
                {
                    id = harvest.IdHarvest,
                    page,
                    sort,
                    ascending
                });

            }
            catch (Exception exc)
            {
                ModelState.AddModelError(string.Empty, exc.CompleteExceptionMessage());
                return View(model);
            }
        }
        else
        {
            return View(model);
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

    public async Task<IActionResult> Harvest_PDF()
    {
        string title = "Harvests";
        var harvest = await ctx.Harvest
                                 .Include(t => t.IdPersonNavigation) // Include Workers entity
                                 .ThenInclude(w => w.IdPersonNavigation) // Include Person entity within Workers
                                 .AsNoTracking()
                                 .OrderBy(d => d.IdHarvest)
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
        report.MainTableDataSource(dataSource => dataSource.StronglyTypedList(harvest));

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
                column.PropertyName(nameof(Harvest.IdHarvest));
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(1);
                column.Width(1);
                column.HeaderCell("ID Harvest");
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Harvest>(x => x.IdCrop);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(2);
                column.Width(1);
                column.HeaderCell("ID Crop", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Harvest>(x => x.Quantity);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(1);
                column.HeaderCell("Quantity", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Harvest>(x => x.FromDate);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(2);
                column.HeaderCell("From Date", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Harvest>(x => x.ToDate);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(2);
                column.HeaderCell("To Date", horizontalAlignment: HorizontalAlignment.Center);
            });

            columns.AddColumn(column =>
            {
                column.PropertyName<Harvest>(x => x.IdPersonNavigation.IdPersonNavigation.Name);
                column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                column.IsVisible(true);
                column.Order(3);
                column.Width(2);
                column.HeaderCell("Worker assigned", horizontalAlignment: HorizontalAlignment.Center);
            });
        });

        #endregion
        byte[] pdf = report.GenerateAsByteArray();

        if (pdf != null)
        {
            Response.Headers.Add("content-disposition", "inline; filename=harvests.pdf");
            return File(pdf, "application/pdf");
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Harvests_Excel_Details()
    {
        var harvests = await ctx.Harvest
                                 .Include(h => h.IdPersonNavigation) // Include Worker entity
                                 .ThenInclude(w => w.IdPersonNavigation)
                                 .AsNoTracking()
                                 .OrderBy(d => d.IdHarvest)
                                 .ToListAsync();

        byte[] content;
        using (ExcelPackage excel = new ExcelPackage())
        {
            excel.Workbook.Properties.Title = "Harvests list";
            excel.Workbook.Properties.Author = "Group-14";

            foreach (var harvest in harvests)
            {
                var worksheet = excel.Workbook.Worksheets.Add($"Harvest ID: {harvest.IdHarvest} of Crop ID: {harvest.IdCrop}");
                worksheet.Cells["1:1"].Style.Fill.PatternType = ExcelFillStyle.LightGray;
                worksheet.Cells["4:4"].Style.Fill.PatternType = ExcelFillStyle.LightGray;

                // First add the headers
                worksheet.Cells[1, 1].Value = "Id Harvest";
                worksheet.Cells[1, 2].Value = "Id Crop";
                worksheet.Cells[1, 3].Value = "Quantity";
                worksheet.Cells[1, 4].Value = "From Date";
                worksheet.Cells[1, 5].Value = "To Date";
                worksheet.Cells[1, 6].Value = "Worker assigned";

                worksheet.Cells[2, 1].Value = harvest.IdHarvest;
                worksheet.Cells[2, 2].Value = harvest.IdCrop;
                worksheet.Cells[2, 3].Value = harvest.Quantity;
                worksheet.Cells[2, 4].Value = harvest.FromDate.ToString("MM/dd/yyyy HH:mm:ss");
                worksheet.Cells[2, 5].Value = harvest.ToDate.ToString("MM/dd/yyyy HH:mm:ss");
                worksheet.Cells[2, 6].Value = harvest.IdPersonNavigation?.IdPersonNavigation?.Name ?? "Unknown";

                worksheet.Cells[1, 1, 2, 7].AutoFitColumns();

                // Add orders related to the harvest
                var orders = await ctx.Order
                                      .Include(o => o.IdPersonNavigation)
                                      .Where(o => o.IdHarvest == harvest.IdHarvest)
                                      .OrderBy(o => o.IdOrder)
                                      .ToListAsync();

                if (orders.Any())
                {
                    int orderRow = 5; // Start from row 4 for orders
                    worksheet.Cells[4, 1].Value = "ID Order";
                    worksheet.Cells[4, 2].Value = "Customer";
                    worksheet.Cells[4, 3].Value = "Quantity";
                    worksheet.Cells[4, 4].Value = "Price (€)";
                    worksheet.Cells[4, 5].Value = "Date of Order";

                    foreach (var order in orders)
                    {
                        worksheet.Cells[orderRow, 1].Value = order.IdOrder;
                        worksheet.Cells[orderRow, 2].Value = order.IdPersonNavigation?.Name ?? "Unknown";
                        worksheet.Cells[orderRow, 3].Value = order.Quantity;
                        worksheet.Cells[orderRow, 4].Value = order.Price;
                        worksheet.Cells[orderRow, 5].Value = order.DateOfOrder.ToString("MM/dd/yyyy HH:mm:ss");
                        orderRow++;
                    }

                    worksheet.Cells[3, 1, orderRow - 1, 6].AutoFitColumns();
                }
            }

            content = excel.GetAsByteArray();
        }

        return File(content, ExcelContentType, "harvests_with_orders.xlsx");
    }

    public async Task<IActionResult> ProcessImportedExcel_Harvests(IFormFile importedFile)
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

                // Asumo que la columna 7 contiene el estado de importación
                int importStatusColumn = 7;

                // Asegúrate de que el encabezado de la columna de estado de importación existe
                if (worksheet.Cells[1, importStatusColumn].Value == null)
                {
                    worksheet.Cells[1, importStatusColumn].Value = "Import Status";
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    // Obtener datos del Excel
                    int idHarvest = worksheet.Cells[row, 1].GetValue<int>();
                    int idCrop = worksheet.Cells[row, 2].GetValue<int>();
                    double quantity = worksheet.Cells[row, 3].GetValue<double>();
                    string fromDateStr = worksheet.Cells[row, 4].GetValue<string>();
                    string toDateStr = worksheet.Cells[row, 5].GetValue<string>();
                    string workerName = worksheet.Cells[row, 6].GetValue<string>();

                    // Convertir las fechas usando ParseExact o TryParseExact
                    if (DateTime.TryParseExact(fromDateStr, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDate) &&
                        DateTime.TryParseExact(toDateStr, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDate))
                    {
                        var worker = await ctx.Workers
                            .Where(w => w.IdPersonNavigation.Name == workerName)
                            .FirstOrDefaultAsync();

                        if (worker != null)
                        {
                            var harvestToUpdate = await ctx.Harvest.FindAsync(idHarvest);

                            if (harvestToUpdate != null)
                            {
                                // Actualizar los datos en la base de datos
                                harvestToUpdate.IdCrop = idCrop;
                                harvestToUpdate.Quantity = quantity;
                                harvestToUpdate.FromDate = fromDate;
                                harvestToUpdate.ToDate = toDate;
                                harvestToUpdate.IdPerson = worker.IdPerson;

                                worksheet.Cells[row, importStatusColumn].Value = "Successfully Imported";
                            }
                            else
                            {
                                worksheet.Cells[row, importStatusColumn].Value = "Harvest not found";
                            }
                        }
                        else
                        {
                            worksheet.Cells[row, importStatusColumn].Value = "Worker not found";
                        }
                    }
                    else
                    {
                        // Manejar el caso en el que las fechas no se puedan analizar correctamente
                        worksheet.Cells[row, importStatusColumn].Value = "Invalid Date Format";
                    }
                }


                // Guardar cambios en la base de datos
                await ctx.SaveChangesAsync();

                byte[] content = package.GetAsByteArray();
                return File(content, ExcelContentType, "imported_harvests.xlsx");
            }
        }
    }

    public async Task<IActionResult> Harvests_Excel_Simple()
    {
        var harvests = await ctx.Harvest
                                 .Include(h => h.IdPersonNavigation) // Include Worker entity
                                 .ThenInclude(w => w.IdPersonNavigation)
                                 .AsNoTracking()
                                 .OrderBy(d => d.IdHarvest)
                                 .ToListAsync();

        byte[] content;
        using (ExcelPackage excel = new ExcelPackage())
        {
            excel.Workbook.Properties.Title = "Harvests list";
            excel.Workbook.Properties.Author = "Group-14";
            var worksheet = excel.Workbook.Worksheets.Add("Harvests");

            //First add the headers
            worksheet.Cells[1, 1].Value = "Id Harvest";
            worksheet.Cells[1, 2].Value = "Id Crop";
            worksheet.Cells[1, 3].Value = "Quantity";
            worksheet.Cells[1, 4].Value = "From Date";
            worksheet.Cells[1, 5].Value = "To Date";
            worksheet.Cells[1, 6].Value = "Worker assigned";

            for (int i = 0; i < harvests.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = harvests[i].IdHarvest;
                worksheet.Cells[i + 2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[i + 2, 2].Value = harvests[i].IdCrop;
                worksheet.Cells[i + 2, 3].Value = harvests[i].Quantity;
                worksheet.Cells[i + 2, 4].Value = harvests[i].FromDate.ToString("MM/dd/yyyy HH:mm:ss");
                worksheet.Cells[i + 2, 5].Value = harvests[i].ToDate.ToString("MM/dd/yyyy HH:mm:ss");
                worksheet.Cells[i + 2, 6].Value = harvests[i].IdPersonNavigation?.IdPersonNavigation?.Name;
            }

            worksheet.Cells[1, 1, harvests.Count + 1, 7].AutoFitColumns();

            content = excel.GetAsByteArray();
        }
        return File(content, ExcelContentType, "harvests.xlsx");
    }

    #endregion
}
