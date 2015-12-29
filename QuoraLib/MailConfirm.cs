using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailKit.Net.Pop3;
using QuoraLib.Utilities;

namespace QuoraLib
{
    public static class MailConfirm
    {
        public static async Task<bool> AcceptConfirm(string mail, string pass)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using (var client = new Pop3Client())
                    {
                        await client.ConnectAsync("pop.mail.yahoo.com", 995, true);

                        // Note: since we don't have an OAuth2 token, disable
                        // the XOAUTH2 authentication mechanism.
                        client.AuthenticationMechanisms.Remove("XOAUTH2");
                        await client.AuthenticateAsync(mail, pass);

                        var msgs = await client.GetMessagesAsync(0, client.Count);
                        var confMess = msgs.First(x => x.Subject.Contains("Quora Account Confirmation"));

                        if (confMess == null)
                            return false;

                        var link = Regex.Match(confMess.HtmlBody, @"(?=https).+(?=\"")").Value;
                        Utils.GetRequest.Get(link).None();

                        await client.DeleteAllMessagesAsync();
                        client.Disconnect(true);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    //Informer.RaiseOnResultReceived(ex.Message);
                    return false;
                }
            });
        }
    }
}