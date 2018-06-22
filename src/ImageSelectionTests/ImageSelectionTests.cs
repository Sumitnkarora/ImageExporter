using Indigo.ImageSelection.Core.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Indigo.ImageSelection.Core.Models;
using System.Xml;
using System.IO;
using System.Reflection;
using Autofac;

namespace ImageSelectionTests
{
    [TestClass]
    public class ImageSelectionTests
    {
        //private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ImageSelectionTests));

        //ImageSelectionTests()
        //{
            //XmlDocument log4netConfig = new XmlDocument();
            //log4netConfig.Load(File.OpenRead("log4net.config"));

            //var repo = log4net.LogManager.CreateRepository(
            //    Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            //log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
        //}

        [TestMethod]
        public void DownloadBooksImageFromDynamicURL()
        {
            //var imageServiceBuilder = new ContainerBuilder();
            //imageServiceBuilder.RegisterType<ConfigurationBuilder>().As<IConfigurationBuilder>();


            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            ImageSelectionService service = new ImageSelectionService(config);
            List<ProductDetails> x = new List<ProductDetails>();
            x.Add(new ProductDetails() { ProductType = ProductType.Book, UPC = "9781476751474" });
            List<ProductImage> productImages  = (List<ProductImage>)service.GetProductImages(x);
            Assert.IsTrue(productImages[0].ImageFound);
        }
        [TestMethod]
        public void DownloadGiftsImageFromDynamicURL()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            ImageSelectionService service = new ImageSelectionService(config);
            List<ProductDetails> x = new List<ProductDetails>();
            x.Add(new ProductDetails() { ProductType = ProductType.GiftMerchandise, UPC = "031009433957" });
            List<ProductImage> productImages = (List<ProductImage>)service.GetProductImages(x);
            Assert.IsTrue(productImages[0].ImageFound);
            Assert.IsTrue(productImages.Count >= 2);
        }

        [TestMethod]
        public void DownloadBooksImageFromDatabase()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            ImageSelectionService service = new ImageSelectionService(config);
            List<ProductDetails> x = new List<ProductDetails>();
            x.Add(new ProductDetails() { ProductType = ProductType.Book, UPC = "11111" });
            List<ProductImage> productImages = (List<ProductImage>)service.GetProductImages(x);
            Assert.IsTrue(productImages[0].ImageFound);
        }
    }
}
