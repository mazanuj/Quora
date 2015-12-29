using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akumu.Antigate;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using QuoraLib.DataTypes;
using QuoraLib.Utilities;

namespace QuoraLib.CaptchaHelper
{
    public static class GetCaptcha
    {
        static GetCaptcha()
        {
            try
            {
                Utils.WebDriver = new ChromeDriver();
                Utils.WebDriverQueue = new ChromeDriver();
            }
            catch (Exception)
            {
            }
        }

        private static Queue GetCaptchaQueue = new Queue();
        private static readonly object Locker = new object();
        private static readonly object LockerQueue = new object();
        private static IWebDriver WebDriver = Utils.WebDriver;
        private static IWebDriver WebDriverQueue = Utils.WebDriverQueue;
        
        public static async Task<CaptchaStruct> GetNoCaptcha(string agKey)
        {
            return await Task.Run(() =>
            {
                lock (Locker)
                {
                    try
                    {
                        var challenge = string.Empty;
                        while (true)
                        {
                            WebDriver = Utils.WebDriver;
                            try
                            {
                                if (!Utils.IsPermit)
                                {
                                    return new CaptchaStruct
                                    {
                                        Answer = "Error message: Stopped",
                                        Challenge = string.Empty,
                                        Date = DateTime.Now
                                    };
                                }

                                WebDriver.Navigate()
                                    .GoToUrl("https://www.quora.com/");

                                WebDriver.FindElement(By.XPath("//a[@class='signup_email_link']")).Click();

                                new WebDriverWait(WebDriver, TimeSpan.FromSeconds(60)).Until(
                                    driver =>
                                        ExpectedConditions.FrameToBeAvailableAndSwitchToIt(
                                            By.XPath("//iframe[@title='виджет reCAPTCHA']")));

                                WebDriver.SwitchTo()
                                    .Frame(WebDriver.FindElement(By.XPath("//iframe[@title='виджет reCAPTCHA']")));

                                new WebDriverWait(WebDriver, TimeSpan.FromSeconds(60)).Until(
                                    driver =>
                                        ExpectedConditions.ElementToBeClickable(
                                            By.XPath("//div[@class='recaptcha-checkbox-checkmark']")));
                                WebDriver.FindElement(By.XPath("//div[@class='recaptcha-checkbox-checkmark']")).Click();

                                WebDriver.SwitchTo().DefaultContent();

                                var cap = new AntiCaptcha(agKey)
                                {
                                    CheckDelay = 2000,
                                    ServiceProvider =
                                        Utils.AntigateService == "rucaptcha.com"
                                            ? "rucaptcha.com"
                                            : "antigate.com"
                                };
                                cap.Parameters.Set("id_constructor", "23");

                                Task.Delay(3000).Wait();

                                new WebDriverWait(WebDriver, TimeSpan.FromSeconds(60)).Until(
                                    driver =>
                                        ExpectedConditions.FrameToBeAvailableAndSwitchToIt(
                                            By.XPath("//iframe[@title='проверка recaptcha']")));
                                WebDriver.SwitchTo()
                                    .Frame(WebDriver.FindElement(By.XPath("//iframe[@title='проверка recaptcha']")));

                                var imageSelectors =
                                    new WebDriverWait(WebDriver, TimeSpan.FromSeconds(15)).Until(
                                        driver => driver.FindElements(By.XPath("//div[@class='rc-image-tile-wrapper']")));

                                if (imageSelectors.Count != 9)
                                    throw new Exception();


                                imageSelectors[2].Click();
                                imageSelectors[4].Click();
                                imageSelectors[7].Click();
                                WebDriver.FindElement(By.Id("recaptcha-verify-button")).Click();
                                Task.Delay(1000).Wait();

                                while (true)
                                {
                                    if (!Utils.IsPermit)
                                    {
                                        return new CaptchaStruct
                                        {
                                            Answer = "Error message: Stopped",
                                            Challenge = string.Empty,
                                            Date = DateTime.Now
                                        };
                                    }

                                    var img = GetCaptchaImg(WebDriver).Result;

                                    if (img.First().Value == null)
                                        break;

                                    cap.Parameters.Set("textinstructions", img.First().Key);
                                    var answ = cap.GetAnswer(img.First().Value);

                                    if (string.IsNullOrEmpty(answ))
                                    {
                                        img.First().Value.Save("filename.png", ImageFormat.Png);
                                        continue;
                                    }

                                    var picNums = Regex.Matches(answ, @"\d");
                                    imageSelectors =
                                        WebDriver.FindElements(By.XPath("//div[@class='rc-image-tile-wrapper']"));

                                    try
                                    {
                                        foreach (var z in from object x in picNums select int.Parse(x.ToString()) - 1)
                                            imageSelectors[z].Click();
                                    }
                                    catch (Exception)
                                    {
                                        challenge = WebDriverQueue.FindElement(By.Id("recaptcha-token"))
                                            .GetAttribute("value");
                                        break;
                                    }

                                    WebDriver.FindElement(By.Id("recaptcha-verify-button")).Click();
                                    Task.Delay(2000).Wait();

                                    var incorrect =
                                        WebDriver.FindElement(
                                            By.XPath("//div[@class='rc-imageselect-incorrect-response']"))
                                            .Text;
                                    var more =
                                        WebDriver.FindElement(
                                            By.XPath("//div[@class='rc-imageselect-error-select-more']"))
                                            .Text;
                                    var one =
                                        WebDriver.FindElement(By.XPath("//div[@class='rc-imageselect-error-select-one']"))
                                            .Text;

                                    if (incorrect == "" && more == "" && one == "")
                                    {
                                        challenge = WebDriver.FindElement(By.Id("recaptcha-token"))
                                            .GetAttribute("value");
                                        break;
                                    }
                                    //cap.FalseCaptcha();

                                    if (one != "" || more != "")
                                        break;
                                }

                                if (string.IsNullOrEmpty(challenge))
                                    continue;
                                break;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                        return new CaptchaStruct {Answer = string.Empty, Challenge = challenge, Date = DateTime.Now};
                    }
                    catch (Exception ex)
                    {
                        return new CaptchaStruct {Answer = $"Error message: {ex.Message}", Challenge = ""};
                    }
                }
            });
        }

        public static async Task<CaptchaStruct> GetNoCaptchaQueue(string agKey)
        {
            return await Task.Run(() =>
            {
                lock (LockerQueue)
                {
                    try
                    {
                        var challenge = string.Empty;
                        while (true)
                        {
                            WebDriverQueue = Utils.WebDriverQueue;
                            try
                            {
                                if (!Utils.IsPermit)
                                {
                                    return new CaptchaStruct
                                    {
                                        Answer = "Error message: Stopped",
                                        Challenge = string.Empty,
                                        Date = DateTime.Now
                                    };
                                }

                                WebDriverQueue.Navigate()
                                    .GoToUrl("https://www.quora.com/");

                                WebDriverQueue.FindElement(By.XPath("//a[@class='signup_email_link']")).Click();

                                new WebDriverWait(WebDriverQueue, TimeSpan.FromSeconds(60)).Until(
                                    driver =>
                                        ExpectedConditions.FrameToBeAvailableAndSwitchToIt(
                                            By.XPath("//iframe[@title='виджет reCAPTCHA']")));

                                WebDriverQueue.SwitchTo()
                                    .Frame(WebDriverQueue.FindElement(By.XPath("//iframe[@title='виджет reCAPTCHA']")));

                                new WebDriverWait(WebDriverQueue, TimeSpan.FromSeconds(60)).Until(
                                    driver =>
                                        ExpectedConditions.ElementToBeClickable(
                                            By.XPath("//div[@class='recaptcha-checkbox-checkmark']")));
                                WebDriverQueue.FindElement(By.XPath("//div[@class='recaptcha-checkbox-checkmark']"))
                                    .Click();

                                WebDriverQueue.SwitchTo().DefaultContent();

                                var cap = new AntiCaptcha(agKey)
                                {
                                    CheckDelay = 2000,
                                    ServiceProvider =
                                        Utils.AntigateService == "rucaptcha.com"
                                            ? "rucaptcha.com"
                                            : "antigate.com"
                                };
                                cap.Parameters.Set("id_constructor", "23");

                                Task.Delay(3000).Wait();

                                new WebDriverWait(WebDriverQueue, TimeSpan.FromSeconds(60)).Until(
                                    driver =>
                                        ExpectedConditions.FrameToBeAvailableAndSwitchToIt(
                                            By.XPath("//iframe[@title='проверка recaptcha']")));
                                WebDriverQueue.SwitchTo()
                                    .Frame(WebDriverQueue.FindElement(By.XPath("//iframe[@title='проверка recaptcha']")));

                                var imageSelectors =
                                    new WebDriverWait(WebDriverQueue, TimeSpan.FromSeconds(15)).Until(
                                        driver => driver.FindElements(By.XPath("//div[@class='rc-image-tile-wrapper']")));

                                if (imageSelectors.Count != 9)
                                    throw new Exception();


                                imageSelectors[2].Click();
                                imageSelectors[4].Click();
                                imageSelectors[7].Click();
                                WebDriverQueue.FindElement(By.Id("recaptcha-verify-button")).Click();
                                Task.Delay(1000).Wait();

                                while (true)
                                {
                                    if (!Utils.IsPermit)
                                    {
                                        return new CaptchaStruct
                                        {
                                            Answer = "Error message: Stopped",
                                            Challenge = string.Empty,
                                            Date = DateTime.Now
                                        };
                                    }

                                    var img = GetCaptchaImg(WebDriverQueue).Result;

                                    if (img.First().Value == null)
                                        break;

                                    cap.Parameters.Set("textinstructions", img.First().Key);
                                    var answ = cap.GetAnswer(img.First().Value);

                                    if (string.IsNullOrEmpty(answ))
                                    {
                                        img.First().Value.Save("filename.png", ImageFormat.Png);
                                        continue;
                                    }

                                    var picNums = Regex.Matches(answ, @"\d");
                                    imageSelectors =
                                        WebDriverQueue.FindElements(By.XPath("//div[@class='rc-image-tile-wrapper']"));

                                    try
                                    {
                                        foreach (var z in from object x in picNums select int.Parse(x.ToString()) - 1)
                                            imageSelectors[z].Click();
                                    }
                                    catch (Exception)
                                    {
                                        challenge = WebDriverQueue.FindElement(By.Id("recaptcha-token"))
                                            .GetAttribute("value");
                                        break;
                                    }

                                    WebDriverQueue.FindElement(By.Id("recaptcha-verify-button")).Click();
                                    Task.Delay(2000).Wait();

                                    var incorrect =
                                        WebDriverQueue.FindElement(
                                            By.XPath("//div[@class='rc-imageselect-incorrect-response']"))
                                            .Text;
                                    var more =
                                        WebDriverQueue.FindElement(
                                            By.XPath("//div[@class='rc-imageselect-error-select-more']"))
                                            .Text;
                                    var one =
                                        WebDriverQueue.FindElement(
                                            By.XPath("//div[@class='rc-imageselect-error-select-one']"))
                                            .Text;

                                    if (incorrect == "" && more == "" && one == "")
                                    {
                                        challenge = WebDriverQueue.FindElement(By.Id("recaptcha-token"))
                                            .GetAttribute("value");
                                        break;
                                    }
                                    //cap.FalseCaptcha();

                                    if (one != "" || more != "")
                                        break;
                                }

                                if (string.IsNullOrEmpty(challenge))
                                    continue;
                                break;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                        return new CaptchaStruct {Answer = string.Empty, Challenge = challenge, Date = DateTime.Now};
                    }
                    catch (Exception ex)
                    {
                        return new CaptchaStruct {Answer = $"Error message: {ex.Message}", Challenge = ""};
                    }
                }
            });
        }

        private static bool IsElementPresent(By by, ISearchContext webDriver)
        {
            try
            {
                webDriver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private static async Task<Dictionary<string, Image>> GetCaptchaImg(ISearchContext webDriver)
        {
            try
            {
                string desc;
                Bitmap candidateImg;
                Bitmap mainImg;

                var mainImgSrc = webDriver.FindElement(By.XPath("//img[@class='rc-image-tile-3']")).GetAttribute("src");
                var mainImgBytes = Utils.GetRequest.Get(mainImgSrc).ToBytes();
                //await new WebClient().DownloadDataTaskAsync(mainImgSrc);
                using (var ms = new MemoryStream(mainImgBytes))
                    mainImg = Image.FromStream(ms) as Bitmap;

                if (!IsElementPresent(By.XPath("//div[@class='rc-imageselect-desc-no-canonical']"), webDriver))
                {
                    var descPlain =
                        webDriver.FindElement(
                            By.XPath("//div[@class='rc-imageselect-desc']")).GetAttribute("innerHTML");
                    desc = Regex.Match(descPlain, @"(?<=<strong>).+(?=</strong>)").Value;
                    var candidateSrcBase64 =
                        webDriver.FindElement(By.Id("rc-imageselect-candidate"))
                            .FindElement(By.TagName("img"))
                            .GetAttribute("src");

                    var candidateBytes =
                        Convert.FromBase64String(candidateSrcBase64.Replace("data:image/jpeg;base64,", string.Empty));
                    using (var ms = new MemoryStream(candidateBytes))
                        candidateImg = Image.FromStream(ms) as Bitmap;
                }
                else
                {
                    var descPlain =
                        webDriver.FindElement(
                            By.XPath("//div[@class='rc-imageselect-desc-no-canonical']")).GetAttribute("innerHTML");
                    desc = Regex.Match(descPlain, @"(?<=<strong>).+(?=</strong>)").Value;

                    candidateImg = new Bitmap(100, 100);
                    var graph = Graphics.FromImage(candidateImg);
                    graph.Clear(Color.CornflowerBlue);
                    graph.Save();
                }

                var descImg =
                    await
                        DrawText(desc, new Font(FontFamily.GenericSansSerif, 14), Color.AliceBlue, Color.CornflowerBlue,
                            200, 100);

                var img = await CombineBitmap(new List<Image> {descImg, candidateImg, mainImg});

                //img.Save("filename.png", ImageFormat.Png);

                return new Dictionary<string, Image> {{desc, img}};
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static async Task<Image> DrawText(string text, Font font, Color textColor, Color backColor, int width,
            int heigh)
        {
            return await Task.Run(() =>
            {
                //first, create a dummy bitmap just to get a graphics object
                Image img = new Bitmap(1, 1);
                var drawing = Graphics.FromImage(img);

                //measure the string to see how big the image needs to be
                var textSize = drawing.MeasureString(text, font);

                //free up the dummy image and old graphics object
                img.Dispose();
                drawing.Dispose();

                var bitmapWidth = width == 1 ? (int) textSize.Width : width;
                var bitmapHeigh = heigh == 1 ? (int) textSize.Height : heigh;

                //create a new image of the right size
                img = new Bitmap(bitmapWidth, bitmapHeigh);

                drawing = Graphics.FromImage(img);

                //paint the background
                drawing.Clear(backColor);

                //create a brush for the text
                Brush textBrush = new SolidBrush(textColor);

                drawing.DrawString(text, font, textBrush, 0, 0);

                drawing.Save();

                textBrush.Dispose();
                drawing.Dispose();

                return img;
            });
        }

        private static async Task<Bitmap> CombineBitmap(IEnumerable<Image> files)
        {
            return await Task.Run(() =>
            {
                //read all images into memory
                var images = new List<Bitmap>();
                Bitmap finalImage = null;

                try
                {
                    images.AddRange(files.Select(image => new Bitmap(image)));

                    var width = images[2].Width;
                    var height = images[0].Height + images[2].Height;

                    //create a bitmap to hold the combined image
                    finalImage = new Bitmap(width, height);

                    //get a graphics object from the image so we can draw on it
                    using (var g = Graphics.FromImage(finalImage))
                    {
                        //set background color
                        g.Clear(Color.CornflowerBlue);

                        var offset = 0;
                        var offget = 0;

                        g.DrawImage(images[0], new Rectangle(offset, offget, images[0].Width, images[0].Height));
                        offset += images[0].Width;

                        g.DrawImage(images[1], new Rectangle(offset, offget, images[1].Width, images[1].Height));
                        offset = 0;
                        offget += images[0].Height;

                        g.DrawImage(images[2], new Rectangle(offset, offget, images[2].Width, images[2].Height));
                    }

                    return finalImage;
                }
                catch (Exception)
                {
                    finalImage?.Dispose();
                    throw;
                }
                finally
                {
                    //clean up memory
                    foreach (var image in images)
                    {
                        image.Dispose();
                    }
                }
            });
        }
    }
}