using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    /// <summary>
    ///  Represents a type of text style in the form of a html link.
    /// </summary>
    public class LinkType : BaseCompassType
    {
        public const string Kind = "link";

        private string id;
        private string text;
        private string ariaLabel;
        private string classname;
        private Uri url;
        private TargetOption target;
        private IDictionary<string, object> customAttributes;
        
        /// <summary>
        /// Gets or sets the id of the hyperlink.
        /// </summary>
        public string Id
        {
            get
            {
                if (id == null)
                {
                    TryGetString("Id", out id);
                }
                return id;
            }
            set
            {
                id = value;
            }
        }

        /// <summary>
        /// Gets or sets the CSS class of the hyperlink.
        /// </summary>
        public string CssClassName
        {
            get
            {
                if (classname == null)
                {
                    TryGetString("ClassName", out classname);
                }
                return classname;
            }
            set
            {
                classname = value;
            }
        }

        /// <summary>
        /// Gets or sets the content item's display text.
        /// </summary>
        public string Text
        {
            get
            {
                if (text == null)
                {
                    text = GetString("Text");
                }
                return text;
            }
            set
            {
                text = value;
            }
        }

        /// <summary>
        /// Gets or sets the content item's url.
        /// </summary>
        public Uri Url
        {
            get
            {
                if (url == null)
                {
                    url = GetUri("Url");
                }
                return url;
            }
            set
            {
                url = value;
            }
        }

        /// <summary>
        /// Gets the content item's target value.
        /// </summary>
        public TargetOption Target
        {
            get
            {
                if (target == TargetOption.NotSpecified)
                {
                    target = GetEnum<TargetOption>("Target");
                }

                return target;
            }
            set
            {
                target = value;
            }
        }

        /// <summary>
        /// Gets the content item's target text.
        /// </summary>
        public string TargetText
        {
            get
            {
                return Target.ToString();
            }
        }

        /// <summary>
        /// Gets or sets an optional dictionary of key-value pairs to add as link attributes.
        /// </summary>
        public IDictionary<string, object> CustomAttributes
        {
            get
            {
                if (customAttributes == null)
                {
                    customAttributes = GetDictionaryOfStrings("Attributes/Strings").ToDictionary(p => p.Key, p => (object)p.Value);
                }
                return customAttributes;
            }
            set
            {
                customAttributes = value;
            }
        }

        /// <summary>
        /// Gets or sets the aria label.
        /// </summary>
        public string AriaLabel
        {
            get
            {
                if (ariaLabel == null)
                {
                    TryGetString("AriaLabel", out ariaLabel);
                }
                return ariaLabel;
            }
            set
            {
                ariaLabel = value;
            }
        }

    }
}
