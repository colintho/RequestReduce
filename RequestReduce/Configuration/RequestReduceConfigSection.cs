﻿using System;
using System.Configuration;

namespace RequestReduce.Configuration
{
    public class RequestReduceConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("spriteVirtualPath")]
        public string SpriteVirtualPath
        {
            get
            {
                return base["spriteVirtualPath"].ToString();
            }
        }

        [ConfigurationProperty("spritePhysicalPath")]
        public string SpritePhysicalPath
        {
            get
            {
                return base["spritePhysicalPath"].ToString();
            }
        }

        [ConfigurationProperty("contentHost")]
        public string ContentHost
        {
            get
            {
                return base["contentHost"].ToString();
            }
        }

        [ConfigurationProperty("contentStore")]
        public string ContentStore
        {
            get
            {
                return base["contentStore"].ToString();
            }
        }

        [ConfigurationProperty("connectionStringName")]
        public string ConnectionStringName
        {
            get
            {
                return base["connectionStringName"].ToString();
            }
        }

        [ConfigurationProperty("spriteSizeLimit")]
        public int SpriteSizeLimit
        {
            get
            {
                int limit;
                Int32.TryParse(base["spriteSizeLimit"].ToString(), out limit);
                return limit;
            }
        }

        [ConfigurationProperty("authorizedUserList")]
        public string AuthorizedUserList
        {
            get { return base["authorizedUserList"].ToString(); }
        }

        [ConfigurationProperty("cssProcesingDisabled")]
        public bool CssProcesingDisabled
        {
            get
            {
                bool result;
                bool.TryParse(base["cssProcesingDisabled"].ToString(), out result);
                return result;
            }
        }
    }
}
