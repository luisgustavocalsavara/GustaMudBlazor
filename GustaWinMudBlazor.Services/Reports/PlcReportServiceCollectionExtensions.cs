using GustaMudBlazor.App.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GustaMudBlazor.Services.Reports;

public static class PlcReportServiceCollectionExtensions
{
    public static IServiceCollection AddPlcReportGenerators(this IServiceCollection services)
    {
        services.AddSingleton<IPlcReportGenerator, CsvPlcReportGenerator>();
        services.AddSingleton<IPlcReportGenerator, PdfPlcReportGenerator>();
        return services;
    }
}
