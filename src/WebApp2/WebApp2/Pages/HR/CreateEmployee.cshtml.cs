using DataLibrary;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;
using System.Drawing;
using QRCoder;
using Image = iText.Layout.Element.Image;
using DataLibrary.Identity;

namespace WebApp2.Pages.HR
{
    public class CreateEmployeeModel : PageModel
    {


        private readonly UserManager<MyEmployee> _userManager;
        private readonly SignInManager<MyEmployee> _signInManager;
        private readonly RoleManager<MyDepartment> _roleManager;
        private readonly UrlEncoder _urlEncoder;

        private readonly IWebHostEnvironment _env;
        public string qrCodeImageBase64 { get; set; }
        public CreateEmployeeModel(UserManager<MyEmployee> userManager, RoleManager<MyDepartment> rolemanager,
            SignInManager<MyEmployee> signInManager, UrlEncoder urlencoder, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = rolemanager;
            _urlEncoder = urlencoder;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<SelectListItem>? DepartmentOptions { get; set; }
        public IList<SelectListItem>? WorkTypeOptions { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DepartmentID { get; set; }//user select
        [BindProperty(SupportsGet = true)]
        public string? WorkTypeID { get; set; }//user select

        public byte[]? ImgData { get; set; }
        public Image QRCodeImage { get; set; }
        string qrCodeBase64 = null;
        public string QRCodeImageBase64 { get; set; }

        public string ImageName { get; set; }


        [BindProperty]
        public bool IsMFAChecked { get; set; }

        public class InputModel
        {
            public string UserName { get; set; }

            public string Password { get; set; }

            public string ConfirmPassword { get; set; }
        }

        public void OnGet()
        {
            DepartmentOptions = _roleManager.Roles
            .Select(r => new SelectListItem
            {
                Text = r.Name,
                Value = r.Id
            }).ToList();

            WorkTypeOptions = Enum.GetValues(typeof(WorkMode))
                 .Cast<WorkMode>()
                      .Select(w => new SelectListItem
                      {
                          Text = w.ToString(),
                          Value = ((int)w).ToString()
                      })
                 .ToList();


        }

        public async Task<IActionResult> OnPostAsync()
        {

            if (ModelState.IsValid)
            {
                var user = new MyEmployee { UserName = Input.UserName };

                int workModeValue;

                if (int.TryParse(WorkTypeID, out workModeValue))
                {

                    user.EmployeeWorkMode = (WorkMode)workModeValue;
                }


                var result = await _userManager.CreateAsync(user, Input.Password);


                if (!string.IsNullOrWhiteSpace(DepartmentID))
                {
                    var role = await _roleManager.FindByIdAsync(DepartmentID);
                    string Groupname = role?.NormalizedName ?? "";


                    await _userManager.AddToRoleAsync(user, Groupname);
                }
                if (result.Succeeded)
                {

                    #region Generate QR Code for 2FA

                    if (IsMFAChecked)
                    {

                        user.TwoFactorEnabled = true;
                        await _userManager.UpdateAsync(user);



                        string AuthenticatorUri = await LoadSharedKeyAndQrCodeUriAsync(user);

                        QRCodeGenerator qrGenerator = new QRCodeGenerator();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode(AuthenticatorUri, QRCodeGenerator.ECCLevel.Q);
                        QRCode qrCode = new QRCode(qrCodeData);
                        Bitmap qrCodeImage = qrCode.GetGraphic(20);

                        ImageConverter converter = new ImageConverter();

                        byte[] qrCodeImageData = (byte[])converter.ConvertTo(qrCodeImage, typeof(byte[]));
                       
                        ImageName = "QR_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".jpg";
                        TempData["ImageName"] = ImageName;
                        string imagePath = Path.Combine(_env.WebRootPath, "Image", ImageName);



                        System.IO.File.WriteAllBytes(imagePath, qrCodeImageData);

                        TempData["QRCode"] = "true";


                    }
                    #endregion
                    TempData["Success"] = "true";
                }

                
            }


           
            return RedirectToPage("./CreateEmployee");
        }

       

        private async Task<string> LoadSharedKeyAndQrCodeUriAsync(MyEmployee user)
        {
            // Load the authenticator key & QR code URI to display on the form
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            string AuthenticatorUri = GenerateQrCodeUri(user.UserName, unformattedKey);

            return AuthenticatorUri;
        }

        private string GenerateQrCodeUri(string username, string unformattedKey)
        {
            string keyee = "";
            string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
            try
            {
                keyee = string.Format(
              AuthenticatorUriFormat,
                  _urlEncoder.Encode("My App"),
                  _urlEncoder.Encode(username),
                  unformattedKey);
            }
            catch (Exception e)
            {
                return keyee;
            }
            return keyee;
        }
       




    }
}
