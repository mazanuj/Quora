using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using QuoraLib.DataTypes;
using QuoraLib.Utilities;

namespace QuoraLib.XlsxHelper
{
    public static class XlsxSave
    {
        public static async Task SaveInXls(IList<PersonStruct> collection, string fileName)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!collection.Any())
                        return;

                    var xssf = new XSSFWorkbook();
                    var sheet = xssf.CreateSheet();

                    AddRowsToXls(collection.Where(x => x != null), ref sheet);

                    using (var file = new FileStream(fileName, FileMode.Create))
                    {
                        xssf.Write(file);
                    }
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex.Message);
                }
                Informer.RaiseOnResultReceived($"{fileName} successfully saved");
            });
        }

        private static void AddRowsToXls(IEnumerable<PersonStruct> list, ref ISheet sheet)
        {
            //Headers
            var rowIndex = 0;
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue("Email");
            row.CreateCell(1).SetCellValue("Password");
            row.CreateCell(2).SetCellValue("Name");
            row.CreateCell(3).SetCellValue("Last Name");
            row.CreateCell(4).SetCellValue("Host");
            row.CreateCell(5).SetCellValue("Proxy login");
            row.CreateCell(6).SetCellValue("Type");
            row.CreateCell(7).SetCellValue("Result");
            rowIndex++;

            //Info
            foreach (var val in list)
            {
                row = sheet.CreateRow(rowIndex);
                row.CreateCell(0).SetCellValue(val.Mail);
                row.CreateCell(1).SetCellValue(val.Pass);
                row.CreateCell(2).SetCellValue(val.FirstName);
                row.CreateCell(3).SetCellValue(val.LastName);
                row.CreateCell(4).SetCellValue(val.Proxy.Host);
                row.CreateCell(5).SetCellValue(val.Proxy.Login);
                row.CreateCell(6).SetCellValue(val.Proxy.Type.ToString());
                row.CreateCell(7).SetCellValue(val.Result);
                rowIndex++;
            }
        }
    }
}