using System;
using System.Collections.Generic;
using System.Text;

namespace Indigo.ImageSelection.Core.Models
{
    public class ProductImage
    {
        public string UPC { get; set; }
        public ProductType ProductType { get; set; }
        public byte[] ImageContent { get; set; }
        public int ImageIndex { get; set; }
        public bool ImageFound { get; set; }
        public string SHA256Hash { get; set; }
    }
}
