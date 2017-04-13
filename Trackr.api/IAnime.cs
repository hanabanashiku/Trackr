﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trackr.api {
    /// <summary>
    /// Represents an anime API client
    /// </summary>
    public interface IAnime {

        Task<bool> VerifyCredentials();
        Task<bool> AddAnime(int id, ApiEntry.ListStatuses listStatus);
        Task<bool> RemoveAnime(int id);
        Task<List<Anime>> FindAnime(string keywords);
    }
}