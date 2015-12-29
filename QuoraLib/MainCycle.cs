using System;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using QuoraLib.DataTypes;
using QuoraLib.Utilities;

namespace QuoraLib
{
    public static class MainCycle
    {
        public static async Task<string> GetResult(PersonStruct ds, CaptchaStruct str)
        {
            try
            {
                var req = await Utils.GetRequestTaskAsync(ds.Proxy);
                var resp = req.Get("https://www.quora.com/").ToString();

                var doc = new HtmlDocument();
                doc.LoadHtml(resp);

                var formkey =
                    doc.DocumentNode.Descendants("input")
                        .First(
                            x =>
                                x.Attributes.Contains("name") &&
                                x.GetAttributeValue("name", string.Empty) == "formkey")
                        .GetAttributeValue("value", string.Empty);

                req.AddParam("formkey", formkey);
                req.AddParam("code", string.Empty);
                req.AddParam("group", string.Empty);
                req.AddParam("source_user_name", string.Empty);
                req.AddParam("source_url", string.Empty);
                req.AddParam("goog_access_token", string.Empty);
                req.AddParam("fb_access_token", string.Empty);
                req.AddParam("fb_expires", string.Empty);
                req.AddParam("twitter_oauth_key", string.Empty);
                req.AddParam("signup_form", "SignupFormBigButtonsOneClick");
                req.AddParam("source", "home");
                req.AddParam("name", $"{ds.FirstName} {ds.LastName}");
                req.AddParam("email", ds.Mail);
                req.AddParam("password", ds.Pass);
                req.AddParam("g-recaptcha-response", str.Challenge);

                resp = req.Post("https://www.quora.com/signup/signup_POST/").ToString();

                if (!resp.Contains("Continue With Email") || resp.Contains("Unconfirmed account"))
                    return "Compleate";
                if (resp.Contains("There is already an account"))
                    throw new Exception("There is already an account");                
                throw new Exception("Something going wrong");
            }
            catch (Exception ex)
            {
                //Informer.RaiseOnResultReceived(ex.Message);
                return $"Error message: {ex.Message}";
            }
        }
    }
}