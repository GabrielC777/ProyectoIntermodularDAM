using Microsoft.AspNetCore.Mvc;
using System.IO;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace BetaProyecto.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MusicController : ControllerBase
    {
        private readonly YoutubeClient _youtube;

        public MusicController()
        {
            _youtube = new YoutubeClient();
        }

        [HttpGet("stream")]
        public async Task<IActionResult> GetStreamUrl([FromQuery] string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return BadRequest("Falta la URL");
                //OBTENEMOS LOS DATOS DEL VIDEO
                var video = await _youtube.Videos.GetAsync(url);
                
                //OBTENEMOS EL MANIFIESTO DE STREAMS
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(url);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (streamInfo == null) return NotFound("No audio found");

                return Ok(new { 
                    url = streamInfo.Url,
                    duracion = video.Duration.HasValue ? (int)video.Duration.Value.TotalSeconds : 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}