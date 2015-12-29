using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NPOI.XSSF.UserModel;
using QuoraLib.DataTypes;
using QuoraLib.Utilities;
using xNet.Net;

namespace QuoraLib.XlsxHelper
{
    public static class XlsxRegistration
    {
        public static async Task<List<PersonStruct>> ParsePersonsList(string workBookPath)
        {
            return await Task.Run(() =>
            {
                var list = new List<PersonStruct>();
                try
                {
                    var fileFullPath = Path.GetFullPath(workBookPath);

                    XSSFWorkbook xssfwb;
                    using (var file = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
                        xssfwb = new XSSFWorkbook(file);

                    var sheet = xssfwb.GetSheet(xssfwb.GetSheetName(0));

                    for (var num = 1; num <= sheet.LastRowNum; num++)
                    {
                        try
                        {
                            var row = sheet.GetRow(num);
                            if (row == null) //null is when the row only contains empty cells 
                                continue;

                            var proxy = new ProxyStruct
                            {
                                Host = row.GetCell(4).StringCellValue,
                                Login = row.GetCell(5).StringCellValue,
                                Type = (ProxyType)Enum.Parse(typeof(ProxyType), row.GetCell(6).StringCellValue)
                            };

                            var person = new PersonStruct
                            {
                                FirstName = row.GetCell(2).StringCellValue,
                                LastName = row.GetCell(3).StringCellValue,
                                Mail = row.GetCell(0).StringCellValue,
                                Pass = row.GetCell(1).StringCellValue,
                                Proxy = proxy
                            };
                            list.Add(person);
                        }
                        catch (Exception ex)
                        {
                            Informer.RaiseOnResultReceived(ex.Message);
                            Informer.RaiseOnResultReceived($"Is error in {num + 1} row");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex.Message);
                }
                return list;
            });
        }
    }
}