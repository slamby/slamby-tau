﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.Design
{
    class DesignDocumentManager : IDocumentManager
    {
        public Task<ClientResponseWithObject<PaginatedList<object>>> GetSampleDocumentsAsync(DocumentSampleSettings sampleSettings)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponseWithObject<PaginatedList<object>>> GetFilteredDocumentsAsync(DocumentFilterSettings filterSettings, string scrollId)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponseWithObject<object>> GetDocumentAsync(string documentId)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> CreateDocumentAsync(object document)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponseWithObject<object>> UpdateDocumentAsync(string documentId, object document)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> DeleteDocumentAsync(string documentId)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> CopyDocumentsToAsync(DocumentCopySettings settings)
        {
            throw new NotImplementedException();
        }

        public Task<ClientResponse> MoveDocumentsToAsync(DocumentMoveSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}
