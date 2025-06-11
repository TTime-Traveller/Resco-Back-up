I've created a C# console app that does the following:

Connects to Dataverse using OAuth2 and MSAL.

Queries completed Resco questionnaire answers (statecode eq 1).

Generates simple PDF reports using PdfSharp.

Uploads the PDFs to an AWS S3 bucket using AWSSDK.S3.

To make this functional, you’ll need to:

Replace "your-client-id", "your-client-secret", "your-tenant-id" with your Azure AD app details.

Use the correct Dataverse environment URL (https://yourorg.crm.dynamics.com).

Replace AWS credentials and bucket name accordingly.
