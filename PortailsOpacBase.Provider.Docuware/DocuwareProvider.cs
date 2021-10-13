using DocuWare.Platform.ServerClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PortailsOpacBase.Provider.Docuware
{
    public static class DocuwareProvider
    {
        static public ServiceConnection Connect(string serveur, string user, string pass)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            return ServiceConnection.Create(new System.Uri(serveur + "/Platform/"), user, pass, "");

        }

        public static Organization GetOrganization(ServiceConnection conn)
        {
            return conn.Organizations[0];
        }

        public static FileDownloadResult DownloadDocumentContent(this Document document)
        {
            if (document.FileDownloadRelationLink == null)
                document = document.GetDocumentFromSelfRelation();

            var downloadResponse = document.PostToFileDownloadRelationForStreamAsync(
                new FileDownload()
                {
                    TargetFileType = FileDownloadType.PDF,
                    KeepAnnotations = true
                }).Result;

            var contentHeaders = downloadResponse.ContentHeaders;
            return new FileDownloadResult()
            {
                Stream = downloadResponse.Content,
                ContentLength = contentHeaders.ContentLength,
                ContentType = contentHeaders.ContentType.MediaType,
                FileName = ((contentHeaders.ContentDisposition.FileName != null) ? contentHeaders.ContentDisposition.FileName : contentHeaders.ContentDisposition.FileNameStar)
            };
        }

        public static FileDownloadResult DownloadDocumentContentOriginal(this Document document)
        {
            if (document.FileDownloadRelationLink == null)
                document = document.GetDocumentFromSelfRelation();

            var downloadResponse = document.PostToFileDownloadRelationForStreamAsync(
                new FileDownload()
                {
                    TargetFileType = FileDownloadType.Auto,
                    KeepAnnotations = true
                }).Result;

            var contentHeaders = downloadResponse.ContentHeaders;
            return new FileDownloadResult()
            {
                Stream = downloadResponse.Content,
                ContentLength = contentHeaders.ContentLength,
                ContentType = contentHeaders.ContentType.MediaType,
                FileName = ((contentHeaders.ContentDisposition.FileName != null) ? contentHeaders.ContentDisposition.FileName : contentHeaders.ContentDisposition.FileNameStar)
            };
        }

        public static FileDownloadResult DownloadDocumentContentZip(this Document document)
        {
            if (document.FileDownloadRelationLink == null)
                document = document.GetDocumentFromSelfRelation();

            var downloadResponse = document.PostToFileDownloadRelationForStreamAsync(
                new FileDownload()
                {
                    TargetFileType = FileDownloadType.Zip
                }).Result;

            var contentHeaders = downloadResponse.ContentHeaders;
            return new FileDownloadResult()
            {
                Stream = downloadResponse.Content,
                ContentLength = contentHeaders.ContentLength,
                ContentType = contentHeaders.ContentType.MediaType,
                FileName = ((contentHeaders.ContentDisposition.FileName != null) ? contentHeaders.ContentDisposition.FileName : contentHeaders.ContentDisposition.FileNameStar)
            };
        }

        public static DocumentsQueryResult TransferBetweenCabinetsWithFields(
            Document sourceDocument,
            string sourceCabinetId,
            FileCabinet destCabinet)
        {
            //Create Document with fields from the destination cabinet.
            var transferInfo = new DocumentsTransferInfo()
            {
                Documents = new List<Document>() { sourceDocument },
                KeepSource = true,
                SourceFileCabinetId = sourceCabinetId
            };

            return destCabinet.PostToTransferRelationForDocumentsQueryResult(transferInfo);
        }

        public static DocumentsQueryResult ListAllDocuments(ServiceConnection conn, string fileCabinetId, int? count = int.MaxValue)
        {
            DocumentsQueryResult queryResult = conn.GetFromDocumentsForDocumentsQueryResultAsync(
                fileCabinetId,
                count: count).Result;
            foreach (var document in queryResult.Items)
            {
                Console.WriteLine("Document {0} created at {1}", document.Id, document.CreatedAt);
            }
            return queryResult;
        }

        public static Section ReplaceFileContent(Document document, String CheminFichier)
        {
            var sections = document.Sections;
            if (sections == null || sections.Count == 0)
                sections = document.GetSectionsFromSectionsRelation().Section;
            return sections[0].ChunkUploadSection(new FileInfo(CheminFichier));
        }
    }

    public class FileDownloadResult
    {
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public long? ContentLength { get; set; }
        public System.IO.Stream Stream { get; set; }
    }
}
