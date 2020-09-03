﻿using System;

namespace TournamentAssistantShared.Models
{
    [Serializable]
    public class ServerSettings
    {
        public string ServerName { get; set; }
        public Team[] Teams { get; set; }
        public int ScoreUpdateFrequency { get; set; }
        public string[] BannedMods { get; set; }
    }
}