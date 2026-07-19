using System;

namespace Blocko.Services.Helpers
{
    public static class EmailTemplates
    {
        private const string PrimaryColor = "#E8A020"; // Yellow from Blocko logo
        private const string DarkBgColor = "#111827";   // Deep dark color
        private const string WhiteColor = "#ffffff";
        private const string TextColor = "#374151";

        public static string GetOtpTemplate(string otpCode, string expirationMinutes = "15")
        {
            return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right; background-color: #f3f4f6; padding: 30px 15px;"">
    <div style=""max-width: 550px; margin: 0 auto; background-color: {WhiteColor}; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); border: 1px solid #e5e7eb;"">
        
        <!-- Header -->
        <div style=""background-color: {DarkBgColor}; padding: 25px 20px; text-align: center;"">
            <h1 style=""color: {PrimaryColor}; margin: 0; font-size: 28px; font-weight: 900; letter-spacing: 2px;"">BLOCKO</h1>
            <p style=""color: rgba(255,255,255,0.7); margin: 5px 0 0 0; font-size: 13px;"">التوريدات الإنشائية</p>
        </div>

        <!-- Body -->
        <div style=""padding: 30px 25px;"">
            <h2 style=""color: #1f2937; margin-top: 0; font-size: 20px; border-bottom: 2px solid #f3f4f6; padding-bottom: 12px;"">رمز التحقق لإعادة تعيين كلمة المرور</h2>
            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك في <strong>BLOCKO</strong>.</p>
            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">يرجى استخدام رمز التحقق (OTP) التالي لإكمال العملية:</p>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <span style=""display: inline-block; background-color: #f9fafb; border: 2px dashed {PrimaryColor}; color: {DarkBgColor}; padding: 15px 35px; font-size: 32px; font-weight: bold; letter-spacing: 6px; border-radius: 8px; font-family: monospace;"" id=""otp-code"">
                    {otpCode}
                </span>
            </div>

            <p style=""color: #ef4444; font-size: 13px; font-weight: 600; text-align: center;"">
                ⚠️ هذا الرمز صالح لمدة {expirationMinutes} دقيقة فقط. لا تشاركه مع أي شخص.
            </p>
        </div>

        <!-- Footer -->
        <div style=""background-color: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #f3f4f6;"">
            <p style=""color: #9ca3af; margin: 0; font-size: 12px;"">إذا لم تطلب هذا الرمز، يمكنك تجاهل هذا البريد الإلكتروني بأمان.</p>
            <p style=""color: #9ca3af; margin: 5px 0 0 0; font-size: 12px;"">&copy; {DateTime.UtcNow.Year} BLOCKO. جميع الحقوق محفوظة.</p>
        </div>

    </div>
</div>";
        }

        public static string GetOrderStatusTemplate(string orderNumber, string oldStatus, string newStatus, string customerName, string orderDetailsUrl)
        {
            string statusColor = newStatus switch
            {
                "Pending" or "قيد الانتظار" => "#f59e0b",
                "Processing" or "قيد المعالجة" => "#3b82f6",
                "Shipped" or "تم الشحن" => "#8b5cf6",
                "Delivered" or "تم التوصيل" => "#10b981",
                "Cancelled" or "ملغي" => "#ef4444",
                _ => PrimaryColor
            };

            return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right; background-color: #f3f4f6; padding: 30px 15px;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: {WhiteColor}; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); border: 1px solid #e5e7eb;"">
        
        <!-- Header -->
        <div style=""background-color: {DarkBgColor}; padding: 25px 20px; text-align: center;"">
            <h1 style=""color: {PrimaryColor}; margin: 0; font-size: 28px; font-weight: 900; letter-spacing: 2px;"">BLOCKO</h1>
            <p style=""color: rgba(255,255,255,0.7); margin: 5px 0 0 0; font-size: 13px;"">التوريدات الإنشائية</p>
        </div>

        <!-- Body -->
        <div style=""padding: 30px 25px;"">
            <h2 style=""color: #1f2937; margin-top: 0; font-size: 20px; border-bottom: 2px solid #f3f4f6; padding-bottom: 12px;"">تحديث حالة الطلب #{orderNumber}</h2>
            
            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">مرحباً <strong>{customerName}</strong>،</p>
            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">نود إعلامك بأنه تم تحديث حالة طلبك رقم <strong>#{orderNumber}</strong> بنجاح.</p>
            
            <!-- Status Box -->
            <div style=""background-color: #f9fafb; border-right: 4px solid {statusColor}; padding: 15px; margin: 20px 0; border-radius: 4px;"">
                <p style=""margin: 0; font-size: 14px; color: #6b7280;"">الحالة السابقة: <span style=""text-decoration: line-through;"">{oldStatus}</span></p>
                <p style=""margin: 5px 0 0 0; font-size: 16px; font-weight: bold; color: {statusColor};"">الحالة الجديدة: {newStatus}</p>
            </div>

            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">يمكنك متابعة طلبك وعرض التفاصيل الكاملة بالضغط على الزر أدناه:</p>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{orderDetailsUrl}"" style=""display: inline-block; background-color: {PrimaryColor}; color: {DarkBgColor}; padding: 14px 28px; font-size: 16px; font-weight: bold; text-decoration: none; border-radius: 6px; box-shadow: 0 4px 6px rgba(232, 160, 32, 0.2);"">
                    تفاصيل ومتابعة الطلب
                </a>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background-color: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #f3f4f6;"">
            <p style=""color: #9ca3af; margin: 0; font-size: 12px;"">نشكرك على ثقتك في BLOCKO للتوريدات الإنشائية.</p>
            <p style=""color: #9ca3af; margin: 5px 0 0 0; font-size: 12px;"">&copy; {DateTime.UtcNow.Year} BLOCKO. جميع الحقوق محفوظة.</p>
        </div>

    </div>
</div>";
        }

        public static string GetOrderConfirmationTemplate(string orderNumber, string customerName, decimal totalAmount, string paymentMethod, string orderDetailsUrl)
        {
            return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; text-align: right; background-color: #f3f4f6; padding: 30px 15px;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: {WhiteColor}; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); border: 1px solid #e5e7eb;"">
        
        <!-- Header -->
        <div style=""background-color: {DarkBgColor}; padding: 25px 20px; text-align: center;"">
            <h1 style=""color: {PrimaryColor}; margin: 0; font-size: 28px; font-weight: 900; letter-spacing: 2px;"">BLOCKO</h1>
            <p style=""color: rgba(255,255,255,0.7); margin: 5px 0 0 0; font-size: 13px;"">التوريدات الإنشائية</p>
        </div>

        <!-- Body -->
        <div style=""padding: 30px 25px;"">
            <h2 style=""color: #10b981; margin-top: 0; font-size: 20px; border-bottom: 2px solid #f3f4f6; padding-bottom: 12px; text-align: center;"">
                🎉 تم استلام طلبك بنجاح!
            </h2>
            
            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">مرحباً <strong>{customerName}</strong>،</p>
            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">شكراً لك على طلبك من <strong>BLOCKO</strong>. لقد تم تسجيل طلبك رقم <strong>#{orderNumber}</strong> بنجاح ونقوم الآن بالعمل على معالجته.</p>
            
            <!-- Summary Table -->
            <table style=""width: 100%; border-collapse: collapse; margin: 25px 0; font-size: 14px;"">
                <tr style=""background-color: #f9fafb;"">
                    <th style=""border: 1px solid #e5e7eb; padding: 12px; text-align: right;"">الرقم المرجعي</th>
                    <td style=""border: 1px solid #e5e7eb; padding: 12px;"">#{orderNumber}</td>
                </tr>
                <tr>
                    <th style=""border: 1px solid #e5e7eb; padding: 12px; text-align: right;"">المجموع الإجمالي</th>
                    <td style=""border: 1px solid #e5e7eb; padding: 12px; font-weight: bold; color: {DarkBgColor};"">{totalAmount:N2} د.أ</td>
                </tr>
                <tr style=""background-color: #f9fafb;"">
                    <th style=""border: 1px solid #e5e7eb; padding: 12px; text-align: right;"">طريقة الدفع</th>
                    <td style=""border: 1px solid #e5e7eb; padding: 12px;"">{paymentMethod}</td>
                </tr>
            </table>

            <p style=""color: {TextColor}; font-size: 15px; line-height: 1.6;"">يمكنك مراجعة تفاصيل طلبك الكاملة عبر الرابط التالي:</p>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{orderDetailsUrl}"" style=""display: inline-block; background-color: {PrimaryColor}; color: {DarkBgColor}; padding: 14px 28px; font-size: 16px; font-weight: bold; text-decoration: none; border-radius: 6px;"">
                    مشاهدة تفاصيل الطلب
                </a>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background-color: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #f3f4f6;"">
            <p style=""color: #9ca3af; margin: 0; font-size: 12px;"">إذا كان لديك أي استفسار، يرجى التواصل معنا عبر البريد الإلكتروني info@block-o.com</p>
            <p style=""color: #9ca3af; margin: 5px 0 0 0; font-size: 12px;"">&copy; {DateTime.UtcNow.Year} BLOCKO. جميع الحقوق محفوظة.</p>
        </div>

    </div>
</div>";
        }
    }
}
