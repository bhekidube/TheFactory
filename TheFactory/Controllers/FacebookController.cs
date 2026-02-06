using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TheFactory.Controllers
{
    [ApiController]
    [Route("api/facebook")]
    public class FacebookController : ControllerBase
    {
        // Replace with your Facebook Page Access Token
        private const string PageAccessToken = "YOUR_PAGE_ACCESS_TOKEN";
        // Replace with your Facebook Page ID
        private const string PageId = "YOUR_PAGE_ID";

        [HttpPost("post")]
        public async Task<IActionResult> PostImage([FromBody] ImageRequest request)
        {
            if (string.IsNullOrEmpty(request.Image))
                return BadRequest("Image data is required.");

            // Convert base64 to byte array
            var base64Data = request.Image.Substring(request.Image.IndexOf(",") + 1);
            byte[] imageBytes = Convert.FromBase64String(base64Data);

            // Upload image to Facebook
            using (var client = new HttpClient())
            {
                var url = $"https://graph.facebook.com/{PageId}/photos?access_token={PageAccessToken}";
                using (var content = new MultipartFormDataContent())
                {
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                    content.Add(imageContent, "source", "bus-schedule.png");
                    content.Add(new StringContent("Bus schedule for today!"), "caption");

                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                        return Ok();
                    else
                        return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
        }

        public class ImageRequest
        {
            public string Image { get; set; }
        }
    }
}
