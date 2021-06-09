using System;
using System.Collections.Generic;
using Azure.Storage;
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

        public (BlobSasQueryParameters, Uri) CreateSASToken(string id)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                Resource = "c", // shared container type
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                Identifier = id
            };

            sasBuilder.SetPermissions(BlobContainerSasPermissions.Write);

            var token = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(GetKeyValueFromConnectionString("AccountName"),
                GetKeyValueFromConnectionString("AccountKey")));
            var uri = containerClient.GenerateSasUri(sasBuilder);
            return (token, uri);
        }

        private string GetKeyValueFromConnectionString(string key)
        {
            var settings = new Dictionary<string, string>();
            var splitted = Configurations.Storage.UserStorageConnection.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var nameValue in splitted)
            {
                var splittedNameValue = nameValue.Split(new char[] { '=' }, 2);
                settings.Add(splittedNameValue[0], splittedNameValue[1]);
            }

            return settings[key];
        }
    }
}
