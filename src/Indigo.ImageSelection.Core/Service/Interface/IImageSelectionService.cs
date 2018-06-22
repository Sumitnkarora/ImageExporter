using Indigo.ImageSelection.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Indigo.ImageSelection.Core.Service.Interface
{
    public interface IImageSelectionService
    {
        IList<ProductImage> GetProductImages(IList<ProductDetails> productDetails);
    }
}
