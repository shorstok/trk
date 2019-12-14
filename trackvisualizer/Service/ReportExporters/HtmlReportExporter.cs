using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using trackvisualizer.Vm;

namespace trackvisualizer.Service.ReportExporters
{
    public class HtmlReportExporter : ITrackReportExporter
    {
        public string Id { get; } = "html_exporter";
        public string Description { get; } = "HTML";

        
        private const string header_html = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN""
   ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"" lang=""en"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8""/>
<title>{tableheader}</title>
</head>
<body>";
        private const string footer_html = @"</body></html>";

        
        public async Task<bool> Export(TrackReportVm source)
        {
            var saveFileDialog = new SaveFileDialog
            {
                DefaultExt = ".html",
                Filter ="HTML (.html)|*.html"
            };

            if (source.Source.SourceTrackFileName != null)
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(source.Source.SourceTrackFileName);

            saveFileDialog.FileName =  Path.GetFileNameWithoutExtension(source.Source.SourceTrackFileName + "-report.html");

            if (saveFileDialog.ShowDialog() != true)
                return false;

            var table = new TableBuilder();

            table.Setval(0,0,"#");
            table.Setval(1,0,"Участок");
            table.Setval(2,0,"Ходовое время");
            table.Setval(3,0,"Расстояние, км");
            table.Setval(4,0,"Набор, м");
            table.Setval(5,0,"Сброс, м");
            table.Setval(6,0,"Максимальная высота");

            foreach (var reportItemVm in source.Results)
            {
                table.Setval(0,reportItemVm.SectionNumber,reportItemVm.SectionNumber.ToString());
                table.Setval(1,reportItemVm.SectionNumber,$"{reportItemVm.SectionStartName} — {reportItemVm.NextSectionName}");
                table.Setval(2,reportItemVm.SectionNumber,(reportItemVm.LebedevHours).ToString("0.0",CultureInfo.InvariantCulture));
                table.Setval(3,reportItemVm.SectionNumber,(reportItemVm.DistanceMeters/1e3).ToString("0.0",CultureInfo.InvariantCulture));
                table.Setval(4,reportItemVm.SectionNumber,(reportItemVm.AscentPerDay).ToString("0.0",CultureInfo.InvariantCulture));
                table.Setval(5,reportItemVm.SectionNumber,(reportItemVm.DescentPerDay).ToString("0.0",CultureInfo.InvariantCulture));
                table.Setval(6,reportItemVm.SectionNumber,(reportItemVm.MaxHeight).ToString("0.0",CultureInfo.InvariantCulture));
            }

            var finalrow = source.Results.Count + 2;

            table.Setval(0,finalrow, "Всего за поход");

            table.Setval(3,finalrow, source.Totals.DistanceTotalKilometers?.ToString("0.0",CultureInfo.InvariantCulture) ?? String.Empty);
            table.Setval(4,finalrow, source.Totals.AscentTotalMeters?.ToString("0.0",CultureInfo.InvariantCulture) ?? String.Empty);
            table.Setval(5,finalrow, source.Totals.DescentTotal?.ToString("0.0",CultureInfo.InvariantCulture) ?? String.Empty);
            

            var result = table.Condense();

            File.WriteAllText(saveFileDialog.FileName,BuildHtml(result,source));
            
            return true;
        }


        public string BuildHtml(TableBuilder.CondensedItem[,] celldata,TrackReportVm source)
        {
            
            string rt = "";

            int ncols = celldata.GetUpperBound(0);
            int nrows = celldata.GetUpperBound(1);

            rt = header_html;

            rt = rt.Replace("{tableheader}", $"Отчет на базе трека {source.Source.SourceTrackFileName}");

            rt += "<table border=1 bordercolor=black cellpadding=10 cellspacing=0>";     

            for (int yc = 0; yc <= nrows; ++yc)
            {
                rt += "<tr>";
                for (int xc = 0; xc <= ncols; ++xc)
                {
                    if (celldata[xc, yc].Xspan > 1)
                        rt += "<td colspan='" + celldata[xc, yc].Xspan + "'>";
                    else
                        rt += "<td>";

                    if (celldata[xc, yc].V == null)
                        rt += "&nbsp;";
                    else
                    {
                        rt += celldata[xc, yc].V;
                    }

                    rt += "</td>";
                }
                rt += "</tr>";
            }

            rt += "</table>";

            rt+=footer_html;

            return rt;
        }

    }
}