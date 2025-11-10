using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace EmployeeMangement.Controllers.Helper
{
    public static class EmailHelper
    {
        public static string SendPassword(string email)
        {
            try
            {
                // Random mật khẩu (6 ký tự)
                string newPassword = GeneratePasswordSimple(6);

                SmtpClient smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("lethithuuyen240504@gmail.com", "ppsq msou imvk pioo"),
                    EnableSsl = true
                };

                MailMessage mail = new MailMessage
                {
                    From = new MailAddress("lethithuuyen240504@gmail.com", "Employee Management System"),
                    Subject = "Đặt lại mật khẩu tài khoản",
                    IsBodyHtml = true
                };

                mail.Body = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
<meta charset='UTF-8'>
<style>
    body {{
        font-family: Arial, sans-serif;
        background-color: #f4f6f8;
        margin: 0;
        padding: 20px;
    }}
    .container {{
        max-width: 600px;
        background: #ffffff;
        padding: 20px;
        border-radius: 8px;
        box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        margin: auto;
    }}
    h2 {{
        color: #007bff;
    }}
    .password-box {{
        margin: 15px 0;
        background: #e3f2fd;
        padding: 12px;
        border-radius: 6px;
        font-size: 18px;
        font-weight: bold;
        letter-spacing: 1px;
        width: fit-content;
    }}
    .footer {{
        margin-top: 30px;
        font-size: 12px;
        color: #777;
        text-align: center;
    }}
</style>
</head>
<body>
    <div class='container'>
        <h2>🔐 Đặt lại mật khẩu thành công</h2>
        <p>Xin chào,</p>
        <p>Bạn đã yêu cầu đặt lại mật khẩu truy cập hệ thống <strong>Employee Management</strong>.</p>
        <p>Đây là mật khẩu đăng nhập mới của bạn:</p>
        
        <div class='password-box'>
            {newPassword}
        </div>

        <p>Vui lòng đăng nhập và thay đổi mật khẩu để đảm bảo an toàn tài khoản.</p>

        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;' />

        <p>Nếu bạn không yêu cầu hành động này, vui lòng liên hệ quản trị hệ thống ngay!</p>

        <div class='footer'>
            © {DateTime.Now.Year} Employee Management System. All Rights Reserved.
        </div>
    </div>
</body>
</html>";


                mail.To.Add(email);
                smtp.Send(mail);

                Console.WriteLine("New password: " + newPassword);

                return newPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email error: " + ex.Message);
                return null;
            }
        }

        private static string GeneratePasswordSimple(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            Random rnd = new Random();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[rnd.Next(chars.Length)]);
            }

            return sb.ToString();
        }
    }
}
