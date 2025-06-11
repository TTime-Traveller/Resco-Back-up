// This is a simplified skeleton of the C# console app for extracting completed Resco Questionnaires from Dataverse,
// generating PDFs, and storing them in AWS S3.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Identity.Client;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace RescoBackupApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Resco Backup App...");

            string accessToken = await GetDataverseAccessTokenAsync();
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://yourorg.crm.dynamics.com/api/data/v9.2/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("resco_questionnaireanswers?$filter=statecode eq 1");
            string content = await response.Content.ReadAsStringAsync();
            JsonDocument results = JsonDocument.Parse(content);

            foreach (var item in results.RootElement.GetProperty("value").EnumerateArray())
            {
                string id = item.GetProperty("resco_questionnaireanswerid").GetString();
                string name = item.GetProperty("name").GetString();
                string workOrderId = item.GetProperty("_msdyn_workorder_value").GetString();

                // 1. Generate PDF
                string pdfPath = $"./{name}.pdf";
                GeneratePdfReport(name, item.ToString(), pdfPath);

                // 2. Upload to S3
                await UploadToS3Async(pdfPath, $"backup/questionnaires/{name}.pdf");
            }

            Console.WriteLine("Backup complete.");
        }

        static async Task<string> GetDataverseAccessTokenAsync()
        {
            var app = ConfidentialClientApplicationBuilder.Create("your-client-id")
                .WithClientSecret("your-client-secret")
                .WithAuthority(new Uri("https://login.microsoftonline.com/your-tenant-id"))
                .Build();

            string[] scopes = new[] { "https://yourorg.crm.dynamics.com/.default" };
            var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        static void GeneratePdfReport(string title, string content, string path)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Verdana", 12);

            gfx.DrawString(title, font, XBrushes.Black, new XRect(0, 0, page.Width, 50), XStringFormats.TopCenter);
            gfx.DrawString(content, font, XBrushes.Black, new XRect(20, 60, page.Width - 40, page.Height - 100));

            document.Save(path);
        }

        static async Task UploadToS3Async(string filePath, string s3Key)
        {
            var s3Client = new AmazonS3Client("your-aws-access-key", "your-aws-secret-key", Amazon.RegionEndpoint.APSoutheast2);
            var fileTransferUtility = new TransferUtility(s3Client);

            await fileTransferUtility.UploadAsync(filePath, "your-s3-bucket-name", s3Key);
            Console.WriteLine($"Uploaded {filePath} to S3 as {s3Key}");
        }
    }
}
