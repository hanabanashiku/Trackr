﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trackr.api {
    /// <summary>
    /// Represents a manga API client.
    /// </summary>
    public interface IManga {

        Task<bool> VerifyCredentials();
        Task<bool> AddManga();
        Task<bool> RemoveManga();
        Task<List<Manga>> FindManga(string keywords);
        Task<List<Manga>> PullMangaList();
    }
}