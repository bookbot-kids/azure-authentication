using System;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Authentication.Shared.Services
{
    public class StorageService
    {
        private BlobContainerClient containerClient;
        public static StorageService Instance { get; } = new StorageService();

        private StorageService()
        {
            containerClient = new BlobContainerClient(Configurations.Storage.UserStorageConnection, Configurations.Storage.UserStorageContainerName);           
        }

        public Uri CreateSASUri()
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                Resource = "c", // shared container type
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(1)
            };

            sasBuilder.SetPermissions(BlobContainerSasPermissions.Create);           
            return containerClient.GenerateSasUri(sasBuilder);
        }
    }
}
