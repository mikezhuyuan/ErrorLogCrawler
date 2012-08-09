using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Net.Mail;
using System.Net;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace ErrorLogCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = LoadConfig("config.txt");

            var smtp = config["smtp"];
            var from = config["from"];
            var recipients = config["recipients"];
            var errorLogUrl = config["errorLogUrl"];
            var errorDetailUrl = config["errorDetailUrl"];

            const int MAX_TRY = 5;

            for (int i = 0; i < MAX_TRY; i++)
            {
                try
                {
                    SendEmail(smtp, "LIVE Error Log", from, recipients, GrabErrorLog(errorLogUrl, errorDetailUrl));

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        static Dictionary<string, string> LoadConfig(string filename)
        {
            var result = new Dictionary<string, string>();
            foreach(var line in System.IO.File.ReadAllText(filename).Split(new[]{'\n'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(new[] { '=' }, 2);
                result.Add(parts[0].Trim(), parts[1].Trim());
            }

            return result;
        }

        static void SendEmail(string smtp, string subject, string from, string recipients, string content)
        {
            var sMail = new SmtpClient(smtp);            
            
            sMail.DeliveryMethod = SmtpDeliveryMethod.Network;
            sMail.Credentials = CredentialCache.DefaultNetworkCredentials;
            
            var msg = new MailMessage(from, recipients);
            msg.Subject = subject + " - " + DateTime.Now;
            msg.Body = content;
            msg.IsBodyHtml = true;
            
            sMail.Send(msg);
        }

        static string GrabErrorLog(string errorLogUrl, string errorDetailBase)
        {
            IWebDriver driver = null;
            try
            {
                driver = new OpenQA.Selenium.Chrome.ChromeDriver();

                driver.Navigate().GoToUrl(errorLogUrl);

                Thread.Sleep(30);

                var element = driver.FindElement(By.XPath("//option[@value='/elab']"));
                element.Click();
                element.Submit();

                Thread.Sleep(30);

                var table = driver.FindElement(By.Id("dgdResults"));
                var result = ((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].innerHTML;", table).ToString();
                result = result.Replace("ErrorLogEventDetail.aspx", errorDetailBase);
                result = System.Text.RegularExpressions.Regex.Replace(result, @"javascript:__doPostBack[^>""]*", "");
                return "<table>" + result + "</table>";
            }
            catch
            {
                throw;
            }
            finally
            {
                if (driver != null)
                    driver.Quit();
            }
        } 
    }
}