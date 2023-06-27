//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: DocumentPdf – Подготовка отчёта в формате PDF.
// https://habr.com/ru/articles/112707/
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Web
{
    #region Using
    using iTextSharp;
    using iTextSharp.text;
    using iTextSharp.text.pdf;
    using System.Data;
    using System.Drawing;
    #endregion Using

    /// <summary> Подготовка отчёта в формате PDF.</summary>
    public class DocumentPdf
    {
        Document _doc;
        MemoryStream _stream;
        Font _tahoma, _tahomabold;

        public string Filename { get; set; }

        public DocumentPdf(string filename)
        {
            Filename = filename;
            _doc = new Document();

            var tahoma = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"Print\fonts\tahoma.ttf");
            var basefont = BaseFont.CreateFont(tahoma, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
            _tahoma = new Font(basefont, Font.DEFAULTSIZE, Font.NORMAL);
            _tahomabold = new Font(basefont, Font.DEFAULTSIZE, Font.BOLD);
        }

        public void Open()
        {
            PdfWriter.GetInstance(_doc, _stream = new MemoryStream());
            _doc.Open();
        }

        public void InsertText(string text)
        {
            var txt = new Phrase(text, _tahoma);
            _doc.Add(txt);
        }

        public void InsertTable(DataTable table)
        {
            var tbl = new PdfPTable(table.Columns.Count);
            foreach (DataColumn col in table.Columns)
                tbl.AddCell(new PdfPCell(new Phrase(col.ColumnName, _tahomabold))
                {
                    HorizontalAlignment = PdfPCell.ALIGN_RIGHT,
                    VerticalAlignment = PdfPCell.ALIGN_CENTER,
                    BackgroundColor = new BaseColor(Color.LightGray)
                });

            foreach (DataRow row in table.Rows)
                foreach (DataColumn col in table.Columns)
                    tbl.AddCell(new PdfPCell(new Phrase(row[col.ColumnName].ToString(), _tahoma))
                    {
                        HorizontalAlignment = PdfPCell.ALIGN_RIGHT,
                        VerticalAlignment = PdfPCell.ALIGN_CENTER
                    });

            _doc.Add(tbl);
        }

        public byte[] ToArray()
        {
            _doc.Close();
            return _stream.ToArray();
        }
    }
}
