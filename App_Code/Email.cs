using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;

namespace FLSModel
{

    public class Email
    {
        public Email()
        {
            EmailAddress = string.Empty;
            Subject = string.Empty;
            Body = string.Empty;
            smtp = null;
        }

        #region "Properties"
        public SmtpClient smtp { get; set; }
        public string EmailAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public Dictionary<int, string> Attachments{ get; set; }

        #endregion

        #region "Helper For Send Mail"
        private void SendMail(MailMessage ObjMail)
        {
            if (smtp == null)
            {
                smtp = new SmtpClient();
            }
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(ObjMail);
            ObjMail.Dispose();
            ObjMail = null;
            smtp.Dispose();
            smtp = null;
        }

        public bool SendHtmlEmail(string Bcc = "", string Cc = "", string FromAddress = "")
        {
            try
            {
                MailMessage ObjMail = new MailMessage();
                EmailAddress = EmailAddress.Replace(";", ",").TrimEnd(",".ToCharArray()).TrimEnd(";".ToCharArray());
                ObjMail.To.Add(EmailAddress);
                if ((!string.IsNullOrEmpty(Bcc)))
                {
                    ObjMail.Bcc.Add(Bcc);
                }
                if ((!string.IsNullOrEmpty(Cc)))
                {
                    ObjMail.CC.Add(Cc);
                }
                if ((!string.IsNullOrEmpty(FromAddress)))
                {
                    ObjMail.From = new MailAddress(FromAddress);
                }

                ObjMail.Subject = this.Subject;
                ObjMail.Body = this.Body;
                ObjMail.IsBodyHtml = true;
                if (Attachments != null && Attachments.Values.Count > 0)
                {
                    foreach (String attachpath in Attachments.Values)
                    {
                        try
                        {
                            Attachment mailAttachment = new Attachment(attachpath.ToString());
                            ObjMail.Attachments.Add(mailAttachment);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                SendMail(ObjMail);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        #endregion
    }

}