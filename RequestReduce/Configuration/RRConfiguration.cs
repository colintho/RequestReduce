﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace RequestReduce.Configuration
{
    public enum Store
    {
        LocalDiskStore,
        SqlServerStore
    }
    public interface IRRConfiguration
    {
        string SpriteVirtualPath { get; set; }
        string SpritePhysicalPath { get; set; }
        string ContentHost { get; }
        string ConnectionStringName { get; }
        Store ContentStore { get; }
        int SpriteSizeLimit { get; set; }
        IEnumerable<string> AuthorizedUserList { get; set; }
        bool CssProcesingDisabled { get; set; }
        event Action PhysicalPathChange; 
    }

    public class RRConfiguration : IRRConfiguration
    {
        private readonly RequestReduceConfigSection config = ConfigurationManager.GetSection("RequestReduce") as RequestReduceConfigSection;
        private string spritePhysicalPath;
        private readonly Store contentStore = Store.LocalDiskStore;
        public static readonly IEnumerable<string> Anonymous = new[]{"Anonymous"};

        public bool CssProcesingDisabled { get; set; }

        public event Action PhysicalPathChange;  

        public RRConfiguration()
        {
            AuthorizedUserList = config == null ? Anonymous : config.AuthorizedUserList.Split(',').Length == 0 ? Anonymous : config.AuthorizedUserList.Split(',');
            var val = config == null ? 0 : config.SpriteSizeLimit;
            CssProcesingDisabled = config == null ? false : config.CssProcesingDisabled;
            SpriteSizeLimit =  val == 0 ? 50000 : val;
            SpriteVirtualPath = config == null || string.IsNullOrWhiteSpace(config.SpriteVirtualPath) ? "/RequestReduceContent" : config.SpriteVirtualPath;
            spritePhysicalPath = config == null ? null : string.IsNullOrWhiteSpace(config.SpritePhysicalPath) ? null : config.SpritePhysicalPath;
            if(config != null && !string.IsNullOrEmpty(config.ContentStore))
            {
                var success = Enum.TryParse(config.ContentStore, true, out contentStore);
                if(!success)
                    throw new ConfigurationErrorsException(string.Format("{0} is not a valid Content Store.", config.ContentStore));
            }
            CreatePhysicalPath();
        }

        public IEnumerable<string> AuthorizedUserList { get; set; }

        public string SpriteVirtualPath { get; set; }

        public string SpritePhysicalPath
        {
            get { return spritePhysicalPath; }
            set 
            { 
                spritePhysicalPath = value;
                CreatePhysicalPath();
                if (PhysicalPathChange != null)
                    PhysicalPathChange();
            }
        }

        public string ContentHost
        {
            get { return config.ContentHost; }
        }

        public string ConnectionStringName
        {
            get
            {
                return config != null
                           ? string.IsNullOrEmpty(config.ConnectionStringName)
                                 ? "RRConnection"
                                 : config.ConnectionStringName
                           : "RRConnection";
            }
        }

        public Store ContentStore
        {
            get { return contentStore; }
        }

        public int SpriteSizeLimit { get; set; }

        private void CreatePhysicalPath()
        {
            if (!string.IsNullOrEmpty(spritePhysicalPath) && !Directory.Exists(spritePhysicalPath))
            {
                Directory.CreateDirectory(spritePhysicalPath);
                while (!Directory.Exists(spritePhysicalPath))
                    Thread.Sleep(0);
            }
        }
    }

    public static class ConfigExtensions
    {
        public static bool AllowsAnonymous(this IEnumerable<string> list)
        {
            if(list.Count()==1 && list.Contains(RRConfiguration.Anonymous.First(), StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}