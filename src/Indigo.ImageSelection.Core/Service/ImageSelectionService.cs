using Indigo.ImageSelection.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Indigo.ImageSelection.Core.Service.Interface;
using Serilog;

namespace Indigo.ImageSelection.Core.Service
{
    public class ImageSelectionService : IImageSelectionService
    {
        //private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ImageSelectionService));
        private static string DataConnectionString;
        private static string BooksImagesOutputFolder;
        private static string GiftsImagesOutputFolder;
        private static string GiftsImagesDynamicURL;
        private static string BooksImagesDynamicURL;
        private static int DataCommandTimeout;
        private const string DataCommandTimeoutStringKeyName = "AppSettings:DataCommandTimeout";
        private const string TradeEnhancedContentConnectionStringKeyName = "Connectionstrings:TradeDbUs";
        private const string BooksOutputFolderStringKeyName = "AppSettings:BooksImagesOutputFolder";
        private const string GiftsOutputFolderStringKeyName = "AppSettings:GMImagesOutputFolder";
        private const string GiftsImageDynamicURLStringKeyName = "AppSettings:GiftsImageDynamicURL";
        private const string BooksImageDynamicURLStringKeyName = "AppSettings:BooksImageDynamicURL";

        public IConfiguration Config { get; set; }
        public ILogger Log { get; set; }
        public ImageSelectionService()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json");
            Config = builder.Build();
            Log = new LoggerConfiguration()
                .ReadFrom.Configuration(Config)
                .CreateLogger();
        }
        public ImageSelectionService(IConfiguration configuration)
        {
            int dataCommandTimeOut = 0;
            DataConnectionString = configuration[TradeEnhancedContentConnectionStringKeyName];
            BooksImagesOutputFolder = configuration[BooksOutputFolderStringKeyName];
            GiftsImagesOutputFolder = configuration[GiftsOutputFolderStringKeyName];
            GiftsImagesDynamicURL = configuration[GiftsImageDynamicURLStringKeyName];
            BooksImagesDynamicURL = configuration[BooksImageDynamicURLStringKeyName];
            
            if (int.TryParse(configuration[DataCommandTimeoutStringKeyName], out dataCommandTimeOut))
            {
                DataCommandTimeout = dataCommandTimeOut;
            }
            var builder = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json");
            Config = builder.Build();
            Log = new LoggerConfiguration()
                .ReadFrom.Configuration(Config)
                .CreateLogger();
        }
        public IList<ProductImage> GetProductImages(IList<ProductDetails> productDetails)
        {
            List<ProductImage> productImages = new List<ProductImage>();
            foreach (ProductDetails productDetail in productDetails)
            {
                if (productDetail.ProductType == ProductType.Book)
                {
                    DownloadBookImages(productImages, productDetail);
                }
                else if (productDetail.ProductType == ProductType.GiftMerchandise)
                {
                    DownloadGiftImages(productImages, productDetail);
                }
            }
            return productImages;
        }
        private void DownloadGiftImages(List<ProductImage> productImages, ProductDetails productDetail)
        {
            string upc = string.Empty;
            int imageIndex = 0;
            ProductImage giftImage;
            //Process the first image of the gift merchandise
            byte[] imageContent;
            upc = productDetail.UPC;
            giftImage = new ProductImage();
            giftImage.UPC = productDetail.UPC;
            giftImage.ProductType = productDetail.ProductType;
            giftImage.ImageIndex = imageIndex;
            string url = string.Empty;
            url = String.Format(GiftsImagesDynamicURL, upc);
            try
            {
                if (TryGetImage(url, out imageContent))
                {
                    //Process first image.
                    giftImage.ImageContent = imageContent;
                    giftImage.ImageFound = true;
                    giftImage.SHA256Hash = GenerateSHA256String(imageContent, url);
                    productImages.Add(giftImage);
                    //Process other images of the gift merchandise.
                    upc = upc + "_" + (++imageIndex).ToString();
                    url = String.Format(GiftsImagesDynamicURL, upc);
                    while (TryGetImage(url, out imageContent))
                    {
                        giftImage = new ProductImage();
                        giftImage.UPC = productDetail.UPC;
                        giftImage.ProductType = productDetail.ProductType;
                        giftImage.ImageIndex = imageIndex;
                        giftImage.ImageContent = imageContent;
                        giftImage.SHA256Hash = GenerateSHA256String(imageContent, url);
                        giftImage.ImageFound = true;
                        upc = giftImage.UPC + "_" + (++imageIndex).ToString();
                        productImages.Add(giftImage);
                        url = String.Format(GiftsImagesDynamicURL, upc);
                    }
                }
                else
                {
                    giftImage.ImageContent = null;
                    giftImage.ImageFound = false;
                }
                
            }
            catch (WebException wex)
            {
                if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    //Case for image not found. 404
                    if (imageIndex == 0)
                    {
                        Log.Information($"The image at {url} was not found. 404 Encountered...");
                        //Only if first image was not found, add the item to result set and mark image found = false. Ignore exceptions for upc_1, upc_2 etc.
                        giftImage.ImageFound = false;
                        giftImage.ImageContent = null;
                        productImages.Add(giftImage);
                    }
                }
            }
            catch (Exception ex)
            {
                giftImage.ImageFound = false;
                giftImage.ImageContent = null;
                productImages.Add(giftImage);
                string errorMessage = $"An exception was encountered while downloading image from url : {url}.";
                Log.Error(errorMessage, ex);
            }
            //return imageIndex;
        }
        private void DownloadBookImages(List<ProductImage> productImages, ProductDetails productDetail)
        {
            ProductImage bookImage;
            byte[] imageContent;
            //Download book images
            bookImage = new ProductImage();
            bookImage.UPC = productDetail.UPC;
            bookImage.ProductType = productDetail.ProductType;
            string url = String.Format(BooksImagesDynamicURL, bookImage.UPC);
            try
            {
                if (TryGetImage(url, out imageContent))
                {
                    bookImage.ImageFound = true;
                    bookImage.ImageContent = imageContent;
                    bookImage.SHA256Hash = GenerateSHA256String(imageContent, url);
                }
                else
                {
                    url = GetBooksUrlFromDB(bookImage.UPC);
                    if (TryGetImage(url, out imageContent))
                    {
                        bookImage.ImageFound = true;
                        bookImage.ImageContent = imageContent;
                        bookImage.SHA256Hash = GenerateSHA256String(imageContent, url);
                        if (!Directory.Exists(BooksImagesOutputFolder))
                            Directory.CreateDirectory(BooksImagesOutputFolder);
                        File.WriteAllBytes(BooksImagesOutputFolder + "\\" + bookImage.UPC + ".jpg", bookImage.ImageContent);
                    }
                    else
                    {
                        bookImage.ImageFound = false;
                        bookImage.ImageContent = null;
                    }
                }
                productImages.Add(bookImage);
            }
            catch (WebException wex)
            {
                if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Information($"The image at {url} was not found. 404 Encountered...");
                    //Case for image not found. 404
                    bookImage.ImageFound = false;
                    bookImage.ImageContent = null;
                    productImages.Add(bookImage);
                }
            }
            catch (Exception ex)
            {
                bookImage.ImageFound = false;
                bookImage.ImageContent = null;
                productImages.Add(bookImage);
                string errorMessage = $"An exception was encountered while downloading image from url : {url}.";
                Log.Error(errorMessage, ex);
            }
        }
        private bool TryGetImage(string url, out byte[] imageContent)
        {
            
            var webClient = new WebClient();
            byte[] imageBytes = null;
            try
            {
                imageBytes = webClient.DownloadData(url);
                if (imageBytes != null)
                {
                    Log.Information($"Import of image at {url} is successful....");
                    return true;
                }
                else
                {
                    Log.Information($"Import of image at {url} failed....");
                    return false;
                }
            }
            catch (WebException wex)
            {
                if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Information($"The image at {url} was not found. 404 Encountered...");
                    return false;
                }
            }
            finally
            {
                imageContent = imageBytes;
            }
            return false;
        }
        private string GenerateSHA256String(byte[] input, string url)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] hash = null;
            try
            {
                hash = sha256.ComputeHash(input);
            }
            catch (Exception ex)
            {
                string errorMessage = $"An exception was encountered while genrating hash for the image : {url}";
                Log.Error(errorMessage, ex);
            }
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }
        private string GetBooksUrlFromDB(string upc)
        {
            string url = string.Empty;
            try
            {
                using (var sqlConnection = new SqlConnection(DataConnectionString))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = new SqlCommand("spGetBooksImageUrl", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout = DataCommandTimeout
                    })
                    {
                        sqlCommand.Parameters.Add("@ISBN", SqlDbType.VarChar, 13).Value = upc;
                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    url = reader["URL"] == DBNull.Value ? string.Empty : reader["URL"].ToString().Trim();
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Cannot process the item upc: {upc}.";
                Log.Error(errorMessage, ex);
            }
            return url;
        }
    }
}


