using System;

namespace Trackr.gui {
    public abstract class Api {
        public enum ApiType {
            Anime, AnimeManga, VideoGames, VisualNovels, Streaming
        };

        public static ApiType Type;
        public static string Name;
    }
}