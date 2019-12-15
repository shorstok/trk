using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;
using trackvisualizer.Properties;
using trackvisualizer.Vm;

namespace trackvisualizer.Service.ReportExporters
{
    [Localizable(false)]
    public class HtmlReportExporter : ITrackReportExporter
    {
        public string Id { get; } = @"html_exporter";
        public string Description { get; } = Resources.HtmlReportExporter_Description;
       
        public async Task<bool> Export(TrackReportVm source)
        {
            var saveFileDialog = new SaveFileDialog
            {
                DefaultExt = @".html",
                Filter =@"HTML (.html)|*.html"
            };

            if (source.Source.SourceTrackFileName != null)
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(source.Source.SourceTrackFileName);

            saveFileDialog.FileName =  Path.GetFileNameWithoutExtension(source.Source.SourceTrackFileName + "-report.html");

            if (saveFileDialog.ShowDialog() != true)
                return false;

            var table = new TableBuilder();

            table.Setval(0,0,Resources.HtmlReportExporter_Export_SectionNumHeader);
            table.Setval(1,0,Resources.HtmlReportExporter_Export_SectionNameHeader);
            table.Setval(2,0,Resources.HtmlReportExporter_Export_SectionDistanceHeader);
            table.Setval(3,0,Resources.HtmlReportExporter_Export_TravelTimeHeader);
            table.Setval(4,0,Resources.HtmlReportExporter_Export_AscentHeader);
            table.Setval(5,0,Resources.HtmlReportExporter_Export_DescentHeader);
            table.Setval(6,0,Resources.HtmlReportExporter_Export_MaxHeightHeader);

            foreach (var reportItemVm in source.Results)
            {
                table.Setval(0,reportItemVm.SectionNumber,reportItemVm.SectionNumber.ToString());
                table.Setval(1,reportItemVm.SectionNumber,$@"{reportItemVm.SectionStartName} — {reportItemVm.NextSectionName}");
                table.Setval(2,reportItemVm.SectionNumber,(reportItemVm.DistanceMeters/1e3).ToString(@"0.0",CultureInfo.InvariantCulture));
                table.Setval(3,reportItemVm.SectionNumber,(reportItemVm.LebedevHours).ToString(@"0.0",CultureInfo.InvariantCulture));
                table.Setval(4,reportItemVm.SectionNumber,(reportItemVm.AscentPerDay).ToString(@"0.0",CultureInfo.InvariantCulture));
                table.Setval(5,reportItemVm.SectionNumber,(reportItemVm.DescentPerDay).ToString(@"0.0",CultureInfo.InvariantCulture));
                table.Setval(6,reportItemVm.SectionNumber,(reportItemVm.MaxHeight).ToString(@"0.0",CultureInfo.InvariantCulture));
            }

            var finalrow = source.Results.Count + 2;

            table.Setval(0,finalrow, Resources.HtmlReportExporter_Export_TotalsRowLabel);

            table.Setval(2,finalrow, source.Totals.DistanceTotalKilometers?.ToString(@"0.0",CultureInfo.InvariantCulture) ?? String.Empty);
            table.Setval(4,finalrow, source.Totals.AscentTotalMeters?.ToString(@"0.0",CultureInfo.InvariantCulture) ?? String.Empty);
            table.Setval(5,finalrow, source.Totals.DescentTotal?.ToString(@"0.0",CultureInfo.InvariantCulture) ?? String.Empty);
            

            var result = table.Condense();

            File.WriteAllText(saveFileDialog.FileName,BuildHtml(result,source));
            
            return true;
        }


        public string BuildHtml(TableBuilder.CondensedItem[,] celldata,TrackReportVm source)
        {
            var rt = "";

            var ncols = celldata.GetUpperBound(0);
            var nrows = celldata.GetUpperBound(1);
     
            var htmlBody = "<table border=1 bordercolor=black cellpadding=10 cellspacing=0>";     

            for (var yc = 0; yc <= nrows; ++yc)
            {
                htmlBody += "<tr>";
                for (var xc = 0; xc <= ncols; ++xc)
                {
                    if (celldata[xc, yc].Xspan > 1)
                        htmlBody += "<td colspan='" + celldata[xc, yc].Xspan + "'>";
                    else
                        htmlBody += "<td>";

                    if (celldata[xc, yc].V == null)
                        htmlBody += "&nbsp;";
                    else
                        htmlBody += celldata[xc, yc].V;

                    htmlBody += "</td>";
                }
                htmlBody += "</tr>";
            }

            htmlBody += "</table>";

            var assembly = Assembly.GetExecutingAssembly();
            
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(rn => rn.EndsWith("report-template.html"));

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();

                result = result.Replace("%TITLE%", string.Format(Resources.HtmlReportExporter_BuildHtml_ReportTitleTemplateFormatted, source.Source.SourceTrackFileName));
                result = result.Replace("%BODY%", htmlBody);

                return result;
            }

        }

    }
}