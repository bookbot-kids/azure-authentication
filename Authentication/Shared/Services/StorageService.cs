﻿using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Authentication.Shared.Services
{
    public class StorageService
    {
        private string connection;

        public StorageService(string connection)
        {
            this.connection = connection;          
        }

        public Uri CreateContainerSASUri(string container)
        {
            var containerClient = new BlobContainerClient(connection, container);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                Resource = "c", // shared container type
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(5)
            };

            sasBuilder.SetPermissions(BlobContainerSasPermissions.Create | BlobContainerSasPermissions.Write);           
            return containerClient.GenerateSasUri(sasBuilder);
        }

        public Uri CreateFileSASUriAsync(string container, string filePath)
        {
            var containerClient = new BlobContainerClient(connection, container);
            var blobClient = containerClient.GetBlobClient(filePath);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                BlobName = blobClient.Name,
                Resource = "f", // shared file type
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(5),
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);
            return blobClient.GenerateSasUri(sasBuilder);
        }

        public async Task UploadFile(string container, string blobPath, string localPath)
        {
            var containerClient = new BlobContainerClient(connection, container);
            var blob = containerClient.GetBlobClient(blobPath);
            await blob.UploadAsync(localPath);
        }

        public async Task UploadFile(string container, string blobPath, Stream stream)
        {
            var containerClient = new BlobContainerClient(connection, container);
            var blob = containerClient.GetBlobClient(blobPath);
            await blob.UploadAsync(stream, true);
        }

        public async Task CopyFile(string container, string source, string dest)
        {
            var containerClient = new BlobContainerClient(connection, container);
            var sourceBlob = containerClient.GetBlobClient(source);
            var destBlob = containerClient.GetBlobClient(dest);
            var uri = sourceBlob.Uri;
            await destBlob.StartCopyFromUriAsync(uri);
        }
    }
}
