﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trackr.Api {
    /// <summary>
    /// Represents a manga API client.
    /// </summary>
    public interface IManga {
        string Name { get; }
        string Username { get; }

        Task<bool> VerifyCredentials();
        Task<bool> AddManga(int id, ApiEntry.ListStatuses listStatus);
        Task<bool> RemoveManga(int id);
        Task<bool> UpdateManga(Manga manga);
        Task<List<Manga>> FindManga(string keywords);
        Task<List<Manga>> PullMangaList();
    }
}